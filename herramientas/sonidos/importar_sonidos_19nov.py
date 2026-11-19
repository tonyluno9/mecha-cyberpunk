#!/usr/bin/env python3
"""T-1470: importa los sonidos nuevos del 19-nov a Assets/Arte/Audio y recorta las grabaciones largas.

Uso: python importar_sonidos_19nov.py "<carpeta fuente (3d sonidos)>" "<Assets/Arte/Audio>" [--log ruta.log]

- Copia cada WAV de 01_UI..10_Policia con el mismo nombre (ignora 99_No_es_sonido).
- Convierte a PCM 16 bits (misma frecuencia) los WAV "extensible", con cabecera truncada o que scipy no lee.
- Recorta: arranque / ralenti (loop) / apagado de motores, pasos sueltos de las secuencias, y loops
  limpios de VEH_ALARMA y VEH_LLANTAS_GRAVA. Las grabaciones largas originales no se copian.
Requiere numpy y scipy; ffmpeg (en el PATH) solo como respaldo de lectura.
"""
import argparse
import os
import shutil
import struct
import subprocess
import sys
import warnings

import numpy as np
from scipy.io import wavfile
from scipy.signal import find_peaks

CARPETAS = ["01_UI", "02_Armas", "03_Impactos", "04_Explosiones", "05_Pasos", "06_Jugador",
            "07_Interaccion", "08_Consumibles", "09_Vehiculos", "10_Policia"]

# Grabaciones largas: nombre origen -> (prefijo, sufijo arranque, ralenti, apagar, numero de variante).
MOTORES = {
    "VEH_NORMAL_ARRANQUE_RALENTI_APAGAR_01": ("VEH_NORMAL", "ARRANQUE", "RALENTI", "APAGAR", "01"),
    "VEH_NORMAL_ARRANQUE_RALENTI_APAGAR_02": ("VEH_NORMAL", "ARRANQUE", "RALENTI", "APAGAR", "02"),
    "VEH_NORMAL_ARRANQUE_RALENTI_APAGAR_03": ("VEH_NORMAL", "ARRANQUE", "RALENTI", "APAGAR", "03"),
    "MOTO_ARRANQUE_01": ("MOTO", "ARRANQUE", "RALENTI", "APAGAR", "01"),
    "MOTO_ARRANQUE_02": ("MOTO", "ARRANQUE", "RALENTI", "APAGAR", "02"),
    "MOTO_ARRANQUE_RALENTI_APAGAR": ("MOTO", "ARRANQUE", "RALENTI", "APAGAR", "03"),
    "VEH_PESADO_RALENTI": ("VEH_PESADO", None, "RALENTI", "APAGAR", "01"),
}
PASOS = {"PAS_TIERRA_SECUENCIA": "PAS_TIERRA", "PAS_GRAVA_SECUENCIA": "PAS_GRAVA"}
LOOPS_LIMPIOS = ["VEH_ALARMA", "VEH_LLANTAS_GRAVA"]

LOG = []


def log(msg):
    print(msg)
    LOG.append(msg)


# ---------------------------------------------------------------- lectura y escritura
def formato_wav(ruta):
    """Devuelve (etiqueta de formato, bits, canales, frecuencia, bytes declarados del data, bytes reales)."""
    tam = os.path.getsize(ruta)
    with open(ruta, "rb") as f:
        cab = f.read(12)
        if cab[:4] != b"RIFF" or cab[8:12] != b"WAVE":
            return None
        fmt = None
        while True:
            h = f.read(8)
            if len(h) < 8:
                break
            idc, n = h[:4], struct.unpack("<I", h[4:])[0]
            if idc == b"fmt ":
                b = f.read(n + (n & 1))
                tag, ch, sr, _, _, bits = struct.unpack("<HHIIHH", b[:16])
                fmt = (tag, bits, ch, sr)
            elif idc == b"data":
                pos = f.tell()
                return fmt + (n, tam - pos) if fmt else None
            else:
                f.seek(n + (n & 1), 1)
    return None


def a_float(d):
    if d.dtype == np.int32:
        x = d.astype(np.float64) / 2147483648.0
    elif d.dtype == np.int16:
        x = d.astype(np.float64) / 32768.0
    elif d.dtype == np.uint8:
        x = (d.astype(np.float64) - 128.0) / 128.0
    else:
        x = d.astype(np.float64)
    return x if x.ndim == 2 else x[:, None]


def leer(ruta):
    """Lee a float64 (n, canales). Usa scipy y, si falla, ffmpeg. Devuelve (sr, x, via)."""
    try:
        with warnings.catch_warnings():
            warnings.simplefilter("ignore")
            sr, d = wavfile.read(ruta)
        return sr, a_float(d), "scipy"
    except Exception:
        tmp = ruta + ".tmp_ffmpeg.wav"
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", ruta, "-c:a", "pcm_s16le", tmp], check=True)
        with warnings.catch_warnings():
            warnings.simplefilter("ignore")
            sr, d = wavfile.read(tmp)
        os.remove(tmp)
        return sr, a_float(d), "ffmpeg"


def escribir16(ruta, sr, x):
    x = np.clip(x, -1.0, 1.0)
    d = np.round(x * 32767.0).astype(np.int16)
    if d.shape[1] == 1:
        d = d[:, 0]
    wavfile.write(ruta, sr, d)


def necesita_conversion(ruta):
    """Extensible, cabecera con data truncada, formato no PCM entero o que scipy no lee."""
    f = formato_wav(ruta)
    if f is None:
        return "cabecera ilegible"
    tag, bits, ch, sr, decl, real = f
    if tag != 1:
        return "formato %s (extensible/otro)" % (hex(tag))
    if bits not in (16, 24, 32):
        return "%d bits" % bits
    if decl > real:
        return "data truncada (%d bytes menos)" % (decl - real)
    try:
        with warnings.catch_warnings(record=True) as ws:
            warnings.simplefilter("always")
            wavfile.read(ruta)
        # un chunk LIST/otro que scipy se salta no es problema; cualquier otro aviso si
        otros = [str(w.message) for w in ws if "non-data" not in str(w.message)]
        if otros:
            return "scipy avisa: %s" % otros[0][:40]
    except Exception as e:
        return "scipy: %s" % str(e)[:40]
    return None


# ---------------------------------------------------------------- utilidades de audio
def mono(x):
    return x.mean(axis=1)


def rms_db(m, sr, ventana=0.05):
    h = max(1, int(sr * ventana))
    n = len(m) // h
    r = np.sqrt((m[: n * h].reshape(n, h) ** 2).mean(axis=1))
    return 20 * np.log10(r + 1e-9), h


def cruce_cero(m, i, sr, radio=0.02):
    """Indice de cruce por cero (de - a +) mas cercano a i."""
    r = int(sr * radio)
    a, b = max(1, i - r), min(len(m) - 1, i + r)
    seg = m[a - 1: b]
    c = np.where((seg[:-1] <= 0) & (seg[1:] > 0))[0] + a
    if len(c) == 0:
        return i
    return int(c[np.argmin(np.abs(c - i))])


def fade(x, sr, entrada=0.0, salida=0.0):
    x = x.copy()
    ne, ns = int(sr * entrada), int(sr * salida)
    if ne > 0:
        x[:ne] *= np.linspace(0, 1, ne)[:, None]
    if ns > 0:
        x[-ns:] *= np.linspace(1, 0, ns)[:, None]
    return x


def hacer_loop(x, sr, ini, largo, xf=0.1):
    """Loop sin clic: toma x[ini : ini+largo+xf] y mezcla (potencia constante) la cola con la cabeza."""
    nx = int(sr * xf)
    seg = x[ini: ini + largo + nx].copy()
    out = seg[:largo].copy()
    t = np.linspace(0, np.pi / 2, nx)
    w_in, w_out = np.sin(t)[:, None], np.cos(t)[:, None]
    out[:nx] = seg[:nx] * w_in + seg[largo: largo + nx] * w_out
    return out


def salto_loop(x, sr):
    """Nivel del ultimo vs primer bloque de 10 ms y salto entre el ultimo y el primer sample."""
    b = int(sr * 0.01)
    m = mono(x)
    ult, pri = np.sqrt((m[-b:] ** 2).mean()), np.sqrt((m[:b] ** 2).mean())
    db = 20 * np.log10((ult + 1e-9) / (pri + 1e-9))
    salto = float(np.abs(m[0] - m[-1]))
    pasos = float(np.abs(np.diff(m)).mean())
    return db, salto, pasos


def ganancia(recorte_pico, pico_orig, tope_db=6.0):
    """Factor para que el pico del recorte iguale el del original, sin subir mas de tope_db ni pasar de 0.98."""
    if recorte_pico <= 1e-6:
        return 1.0
    g = pico_orig / recorte_pico
    return min(g, 10 ** (tope_db / 20), 0.98 / recorte_pico)


def periodo_envolvente(m, sr, minimo=0.2, maximo=4.0):
    h = int(sr * 0.01)
    n = len(m) // h
    e = np.sqrt((m[: n * h].reshape(n, h) ** 2).mean(axis=1))
    e = e - e.mean()
    k = min(n, 3000)
    ac = np.correlate(e[:k], e[:k], mode="full")[k - 1:]
    a, b = int(minimo / 0.01), int(maximo / 0.01)
    lag = a + int(np.argmax(ac[a:b]))
    return lag * 0.01


# ---------------------------------------------------------------- motores
def analizar_motor(m, sr):
    """Devuelve (inicio_ralenti, inicio_caida, nivel mediano dB) en segundos. Caida = None si no hay apagado."""
    db, h = rms_db(m, sr, 0.05)
    dt = h / sr
    n = len(db)
    med = float(np.median(db[n // 4: 3 * n // 4]))
    k = 5
    sm = np.convolve(np.pad(db, (k // 2, k // 2), mode="edge"), np.ones(k) / k, mode="valid")
    # entra al ralenti: primer punto desde el que 3 s seguidos varian menos de 5 dB
    w = int(3.0 / dt)
    ini_idx = 0
    for i in range(0, max(1, n - w)):
        v = sm[i: i + w]
        if v.max() - v.min() < 5.0:
            ini_idx = i
            break
    # caida: despues del ultimo punto que aun esta a menos de 1.5 dB bajo la mediana (si sobran >= 0.3 s)
    ok = np.where(sm >= med - 1.5)[0]
    caida = None
    if len(ok):
        j = int(ok.max())
        if (n - 1 - j) * dt >= 0.3:
            caida = j * dt
    return ini_idx * dt, caida, med


def ventana_estable(m, sr, desde, hasta, largo):
    """Inicio (s) de la ventana de 'largo' s con menos variacion de nivel (std de RMS de 100 ms)."""
    db, h = rms_db(m, sr, 0.1)
    dt = h / sr
    w = int(largo / dt)
    a, b = int(desde / dt), int(hasta / dt) - w
    if b <= a:
        return desde
    mejor, mi = 1e9, a
    for i in range(a, b, 2):
        s = db[i: i + w].std()
        if s < mejor:
            mejor, mi = s, i
    return mi * dt


def procesar_motor(nombre, x, sr, pico, destino, info):
    pref, suf_arr, suf_ral, suf_apa, nn = info
    m = mono(x)
    dur = len(x) / sr
    ini, caida, med = analizar_motor(m, sr)
    log("  %s: %.1f s, ralenti desde %.2f s, caida %s, nivel %.1f dB" % (
        nombre, dur, ini, ("%.2f s" % caida) if caida else "no hay", med))
    salidas = []
    largo_s = 5.0
    lim = (caida if caida else dur) - 0.5
    desde = ini + 0.5
    if lim - desde < largo_s + 0.5:
        largo_s = max(3.0, min(5.0, lim - desde - 0.3))
    a = ventana_estable(m, sr, desde, lim, largo_s + 0.15)
    a = cruce_cero(m, int(a * sr), sr)
    L = int(largo_s * sr)
    L = cruce_cero(m, a + L, sr) - a
    ral = hacer_loop(x, sr, a, L, 0.12)
    salidas.append(("%s_RALENTI_%s" % (pref, nn), ral,
                    "ralenti: %.2f s a %.2f s (loop %.2f s, crossfade 120 ms)" % (a / sr, (a + L) / sr, L / sr)))
    if suf_arr:
        fin = max(ini, 1.0)  # si el ralenti entra casi de inmediato se deja al menos 1 s de arranque (a revisar a oido)
        i1 = cruce_cero(m, int(fin * sr), sr)
        seg = fade(x[:i1], sr, 0.002, 0.1)
        salidas.append(("%s_ARRANQUE_%s" % (pref, nn), seg, "arranque: 0.00 s a %.2f s" % (i1 / sr)))
    if caida is not None:
        i0 = cruce_cero(m, max(0, int((caida - 0.15) * sr)), sr)
        # recorta la cola que ya es casi silencio (< -66 dBFS)
        dbf, hf = rms_db(m[i0:], sr, 0.05)
        audibles = np.where(dbf > -66.0)[0]
        i1 = len(x)
        if len(audibles):
            i1 = min(len(x), i0 + (int(audibles.max()) + 2) * hf)
        seg = fade(x[i0:i1], sr, 0.05, 0.1 if i1 < len(x) else 0.02)
        salidas.append(("%s_APAGAR_%s" % (pref, nn), seg, "apagado: %.2f s a %.2f s (cola de silencio recortada)" % (i0 / sr, i1 / sr)))
    else:
        log("    sin apagado detectable: no se crea %s_APAGAR_%s" % (pref, nn))
    escribir_grupo(destino, sr, salidas, pico)


def escribir_grupo(destino, sr, salidas, pico):
    pk = max(float(np.abs(s[1]).max()) for s in salidas)
    g = ganancia(pk, pico)
    for nombre, seg, desc in salidas:
        ruta = os.path.join(destino, nombre + ".wav")
        escribir16(ruta, sr, seg * g)
        sz = os.path.getsize(ruta)
        extra = ""
        if "RALENTI" in nombre or nombre in LOOPS_LIMPIOS:
            db, salto, paso = salto_loop(seg * g, sr)
            extra = " | loop: bloque final/inicial %.1f dB, salto %.4f (diff media %.4f)" % (db, salto, paso)
        log("    -> %s: %.2f s, %d KB, ganancia x%.2f | %s%s" % (nombre, len(seg) / sr, sz // 1024, g, desc, extra))


# ---------------------------------------------------------------- pasos
def procesar_pasos(nombre, x, sr, pico, destino, pref):
    m = mono(x)
    h = int(sr * 0.005)
    n = len(m) // h
    env = np.sqrt((m[: n * h].reshape(n, h) ** 2).mean(axis=1))
    env = np.convolve(env, np.ones(3) / 3, mode="same")
    pk = env.max()
    picos, props = find_peaks(env, height=pk * 0.15, distance=int(0.30 / 0.005), prominence=pk * 0.08)
    log("  %s: %.2f s, %d picos candidatos en s: %s" % (
        nombre, len(m) / sr, len(picos), ", ".join("%.2f" % (p * 0.005) for p in picos)))
    cand = []
    for p, hgt in zip(picos, props["peak_heights"]):
        ruido = np.concatenate([env[max(0, p - 30): max(0, p - 10)], env[min(n, p + 70): min(n, p + 80)]])
        r = float(np.median(ruido)) if len(ruido) else 0.0
        cand.append((hgt / (r + 1e-9), p * 0.005, hgt))
    elegidos = sorted(sorted(cand, reverse=True)[:8], key=lambda c: c[1])
    salidas = []
    for i, (calidad, t, hgt) in enumerate(elegidos, 1):
        i0 = cruce_cero(m, max(0, int((t - 0.05) * sr)), sr, 0.005)
        i1 = min(len(x), int((t + 0.35) * sr))
        seg = fade(x[i0:i1], sr, 0.005, 0.02)
        salidas.append(("%s_%02d" % (pref, i), seg,
                        "pisada en %.2f s (corte %.2f a %.2f s, relacion pico/ruido %.1f)" % (t, i0 / sr, i1 / sr, calidad)))
    if salidas:
        escribir_grupo(destino, sr, salidas, pico)


# ---------------------------------------------------------------- loops limpios
def procesar_loop_limpio(nombre, x, sr, pico, destino):
    m = mono(x)
    dur = len(m) / sr
    P = periodo_envolvente(m, sr)
    ini, caida, med = analizar_motor(m, sr)
    lim = (caida if caida else dur) - 0.5
    desde = max(ini, 1.0) + 0.5
    L_s = 9.0
    if nombre == "VEH_ALARMA":
        L_s = max(1, int(9.0 / P)) * P  # multiplo del periodo del patron de la alarma
    a = cruce_cero(m, int(ventana_estable(m, sr, desde, lim, L_s + 0.15) * sr), sr)
    L = cruce_cero(m, a + int(L_s * sr), sr) - a
    seg = hacer_loop(x, sr, a, L, 0.12)
    desc = "loop de %.2f s tomado de %.2f s (periodo detectado %.2f s, crossfade 120 ms)" % (L / sr, a / sr, P)
    log("  %s: %.1f s original, nivel %.1f dB" % (nombre, dur, med))
    escribir_grupo(destino, sr, [(nombre, seg, desc)], pico)


# ---------------------------------------------------------------- principal
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("fuente")
    ap.add_argument("destino")
    ap.add_argument("--log", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "importar-19nov.log"))
    a = ap.parse_args()
    for carpeta in CARPETAS:
        src = os.path.join(a.fuente, carpeta)
        dst = os.path.join(a.destino, carpeta)
        if not os.path.isdir(src):
            log("FALTA carpeta " + src)
            continue
        os.makedirs(dst, exist_ok=True)
        log("== %s" % carpeta)
        for f in sorted(os.listdir(src)):
            if not f.lower().endswith(".wav"):
                continue
            base = f[:-4]
            ruta = os.path.join(src, f)
            if base in MOTORES or base in PASOS or base in LOOPS_LIMPIOS:
                sr, x, via = leer(ruta)
                pico = float(np.abs(x).max())
                if base in MOTORES:
                    procesar_motor(base, x, sr, pico, dst, MOTORES[base])
                elif base in PASOS:
                    procesar_pasos(base, x, sr, pico, dst, PASOS[base])
                else:
                    procesar_loop_limpio(base, x, sr, pico, dst)
                continue
            motivo = necesita_conversion(ruta)
            if motivo:
                sr, x, via = leer(ruta)
                escribir16(os.path.join(dst, f), sr, x)
                log("  %s: CONVERTIDO a PCM16 %d Hz (%s) | %.2f s, %d KB" % (
                    f, sr, motivo, len(x) / sr, os.path.getsize(os.path.join(dst, f)) // 1024))
            else:
                shutil.copyfile(ruta, os.path.join(dst, f))
                sr, x, via = leer(ruta)
                log("  %s: copiado | %.2f s, %d Hz, %d KB" % (f, len(x) / sr, sr, os.path.getsize(ruta) // 1024))
    os.makedirs(os.path.dirname(os.path.abspath(a.log)), exist_ok=True)
    with open(a.log, "w", encoding="utf-8") as fh:
        fh.write("\n".join(LOG) + "\n")


if __name__ == "__main__":
    sys.exit(main())
