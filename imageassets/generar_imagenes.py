#!/usr/bin/env python3
"""Genera imagenes del lote con WaveSpeed (nano-banana-2.1). Requiere WAVESPEED_API_KEY."""
import argparse
import csv
import json
import mimetypes
import os
import re
import sys
import time
from datetime import datetime
from pathlib import Path

import requests

API = "https://api.wavespeed.ai/api/v3"
BASE = Path(__file__).resolve().parent
REPO = BASE.parent
LOTE = BASE / "lote_prompts.json"
SALIDA = BASE / "generadas"
REGISTRO = SALIDA / "registro.csv"
POLL_S = 3
TIMEOUT_S = 600

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def headers(key, json_body=True):
    h = {"Authorization": f"Bearer {key}"}
    if json_body:
        h["Content-Type"] = "application/json"
    return h


def subir(key, ruta, cache, raiz=BASE):
    """Sube una referencia local y devuelve su URL (cacheada por ruta)."""
    ruta = Path(ruta)
    if not ruta.is_absolute():
        ruta = raiz / ruta
    if ruta in cache:
        return cache[ruta]
    if not ruta.exists():
        raise FileNotFoundError(f"Referencia no encontrada: {ruta}")
    mime = mimetypes.guess_type(ruta.name)[0] or "application/octet-stream"
    with open(ruta, "rb") as f:
        r = requests.post(f"{API}/media/upload/binary", headers=headers(key, False),
                          files={"file": (ruta.name, f, mime)}, timeout=120)
    r.raise_for_status()
    data = r.json().get("data") or {}
    url = data.get("download_url") or data.get("url")
    if not url:
        raise RuntimeError(f"Respuesta de subida sin URL: {r.text[:300]}")
    cache[ruta] = url
    return url


def generar(key, trabajo, ref_urls):
    """Lanza la prediccion, espera y devuelve (url_imagen, data_final)."""
    body = {
        "prompt": trabajo["prompt"],
        "resolution": trabajo.get("resolution", "1k"),
        "output_format": "png",
    }
    if trabajo.get("aspect_ratio"):
        body["aspect_ratio"] = trabajo["aspect_ratio"]
    if trabajo["endpoint"].endswith("/edit"):
        body["images"] = ref_urls
    r = requests.post(f"{API}/{trabajo['endpoint']}", headers=headers(key),
                      json=body, timeout=60)
    r.raise_for_status()
    data = r.json()["data"]
    get_url = (data.get("urls") or {}).get("get") or f"{API}/predictions/{data['id']}/result"
    t0 = time.time()
    while True:
        status = data.get("status")
        if status == "completed":
            outs = data.get("outputs") or []
            if not outs:
                raise RuntimeError("Completado sin outputs")
            return outs[0], data
        if status in ("failed", "cancelled", "timeout", "deleted"):
            raise RuntimeError(f"Estado {status}: {data.get('error')}")
        if time.time() - t0 > TIMEOUT_S:
            raise TimeoutError(f"Sin resultado tras {TIMEOUT_S}s (id {data.get('id')})")
        time.sleep(POLL_S)
        rr = requests.get(get_url, headers=headers(key, False), timeout=60)
        rr.raise_for_status()
        data = rr.json()["data"]


def costo(data):
    for k in ("cost", "price", "total_cost", "credits"):
        if data.get(k) not in (None, ""):
            return data[k]
    return ""


def registrar(fila):
    SALIDA.mkdir(exist_ok=True)
    nuevo = not REGISTRO.exists()
    with open(REGISTRO, "a", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        if nuevo:
            w.writerow(["fecha", "id", "archivo", "estado", "costo"])
        w.writerow(fila)


def ids_hechos():
    """IDs con al menos una fila ok en registro.csv cuyo archivo existe."""
    if not REGISTRO.exists():
        return set()
    with open(REGISTRO, newline="", encoding="utf-8-sig") as f:
        return {r["id"] for r in csv.DictReader(f)
                if r["estado"] == "ok" and (SALIDA / r["archivo"]).exists()}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--lote", help="JSON de trabajos (por defecto lote_prompts.json, relativo a imageassets)")
    ap.add_argument("--ids", nargs="+", help="IDs a generar (ej. CY01 CY02)")
    ap.add_argument("--fase", type=int, help="solo trabajos con este valor en el campo fase (ej. 1)")
    ap.add_argument("--parte", help="Numero o texto de la parte (ej. 1)")
    ap.add_argument("--variantes", type=int, help="fuerza este numero de variantes para todos")
    ap.add_argument("--rehacer", action="store_true",
                    help="genera variantes nuevas en el siguiente numero libre (_v2, _v3...)")
    ap.add_argument("--saltar-hechos", action="store_true",
                    help="omite IDs con imagen ya registrada como ok en registro.csv")
    a = ap.parse_args()

    key = os.environ.get("WAVESPEED_API_KEY")
    if not key:
        sys.exit("Falta la variable de entorno WAVESPEED_API_KEY")

    lote = Path(a.lote) if a.lote else LOTE
    if not lote.is_absolute() and not lote.exists():
        lote = BASE / lote
    lote_data = json.load(open(lote, encoding="utf-8"))
    trabajos = lote_data["trabajos"]
    # Lotes con rutas relativas a la raiz del repo (p. ej. lote_escenas.json)
    raiz_refs = REPO if "repo" in str(lote_data.get("rutas_relativas_a", "")).lower() else BASE
    if a.ids:
        want = {i.upper() for i in a.ids}
        trabajos = [t for t in trabajos if t["id"].upper() in want]
        faltan = want - {t["id"].upper() for t in trabajos}
        if faltan:
            sys.exit(f"IDs no encontrados: {', '.join(sorted(faltan))}")
    if a.fase is not None:
        trabajos = [t for t in trabajos if str(t.get("fase", "")) == str(a.fase)]
    if a.parte:
        pat = re.compile(rf"\bparte\s*{re.escape(a.parte)}\b" if a.parte.isdigit()
                         else re.escape(a.parte), re.I)
        trabajos = [t for t in trabajos if pat.search(t.get("parte", ""))]
    if not trabajos:
        sys.exit("Ningun trabajo coincide")

    if a.saltar_hechos:
        hechos = ids_hechos()
        for t in trabajos:
            if t["id"] in hechos:
                print(f"[{t['id']}] ya hecho, se omite")
        trabajos = [t for t in trabajos if t["id"] not in hechos]
        if not trabajos:
            sys.exit("Nada pendiente")

    SALIDA.mkdir(exist_ok=True)
    cache = {}
    total, n_ok, n_err = 0.0, 0, 0
    for t in trabajos:
        refs = None
        raiz = REPO if t.get("salida") else raiz_refs
        if t.get("salida"):
            # Salida fija: una sola imagen en esa ruta exacta, sin sufijo ni sobreescritura
            destino = REPO / t["salida"]
            if destino.exists():
                print(f"[{t['id']}] {t['salida']} ya existe, se omite")
                continue
            destino.parent.mkdir(parents=True, exist_ok=True)
            ahora = datetime.now().isoformat(timespec="seconds")
            try:
                refs = [subir(key, p, cache, raiz) for p in t.get("referencias") or []]
                print(f"[{t['id']}] generando ({len(refs)} referencias)...")
                url, data = generar(key, t, refs)
                img = requests.get(url, timeout=120)
                img.raise_for_status()
                with open(destino, "xb") as f:
                    f.write(img.content)
                c = costo(data)
                registrar([ahora, t["id"], t["salida"], "ok", c])
                n_ok += 1
                try:
                    total += float(c)
                except (TypeError, ValueError):
                    pass
                print(f"[{t['id']}] guardado {t['salida']} (costo: {c or 'no viene'})")
            except Exception as e:
                registrar([ahora, t["id"], t["salida"], f"error: {e}", ""])
                n_err += 1
                print(f"[{t['id']}] ERROR: {e}")
            continue
        total_v = a.variantes or t.get("variantes", 1)
        if a.rehacer:
            libre = 1
            while (SALIDA / f"{t['archivo']}_v{libre}.png").exists():
                libre += 1
            numeros = range(libre, libre + total_v)
        else:
            numeros = range(1, total_v + 1)
        for n in numeros:
            destino = SALIDA / f"{t['archivo']}_v{n}.png"
            if destino.exists():
                print(f"[{t['id']}] v{n} ya existe, se omite")
                continue
            ahora = datetime.now().isoformat(timespec="seconds")
            try:
                if refs is None:
                    refs = [subir(key, p, cache, raiz) for p in t.get("referencias") or []]
                print(f"[{t['id']}] generando v{n}...")
                url, data = generar(key, t, refs)
                img = requests.get(url, timeout=120)
                img.raise_for_status()
                with open(destino, "xb") as f:  # "x": nunca sobreescribe
                    f.write(img.content)
                c = costo(data)
                registrar([ahora, t["id"], destino.name, "ok", c])
                n_ok += 1
                try:
                    total += float(c)
                except (TypeError, ValueError):
                    pass
                print(f"[{t['id']}] guardado {destino.name}")
            except Exception as e:
                registrar([ahora, t["id"], destino.name, f"error: {e}", ""])
                n_err += 1
                print(f"[{t['id']}] ERROR v{n}: {e}")
    print(f"Resumen: {n_ok} ok, {n_err} con error, costo en respuesta: {total if total else 'no viene'}")


if __name__ == "__main__":
    main()
