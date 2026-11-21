#!/usr/bin/env python3
"""T-1530: importa los sonidos nuevos del 20-nov ("3d sonidos") a Assets/Arte/Audio.

Uso:
  python importar_sonidos_20nov.py "<3d sonidos>" "<Assets/Arte/Audio>" [--referencia "<Assets/Arte/Audio real>"]
         [--log importar-20nov.log] [--csv-comparacion comparacion-20nov.csv] [--csv-ids ids-20nov.csv]
         [--sin-regenerar-19nov] [--solo-comparar]

Es idempotente: se puede correr cuantas veces haga falta; reescribe lo suyo con el mismo resultado.
--referencia es la carpeta Assets/Arte/Audio con los WAV REALES del 19-nov (en un worktree sin LFS los .wav son
punteros y no se pueden leer); por omision es la misma carpeta de destino. Con --solo-comparar no escribe audio.

Que hace (ver docs/reporte-sonidos-20nov.md y LEEME_SONIDOS.md):
 1. Compara cada archivo de la fuente (menos 98_Descartados y 99_No_es_sonido) con lo que ya hay en Assets/Arte/Audio
    por CONTENIDO (frecuencia, largo y diferencia de muestras, no solo el nombre): NUEVO, IGUAL, RENOMBRADO o de
    otra carpeta (se mueve con git mv conservando el .meta y el GUID), REEMPLAZA o QUITADO.
 2. Para los 01..10 del 19-nov regenera con importar_sonidos_19nov.py (motores, pasos, loops) y comprueba que sigue igual.
 3. Importa lo nuevo: PCM 16 bits, mono en los sonidos del mundo, estereo en UI/HUD/radio/rueda, 44.1 o 48 kHz
    (96 y 192 kHz se bajan a 48), MP3 -> WAV, sin clic al inicio ni al final.
 4. Recortes: rueda de camara lenta en dos, ROB_MUERTE 1 s, FUE_LOOP_GRANDE y NOV_JETPACK_LOOP a un loop corto.
 5. Loops: corta los bordes casi en silencio y, si el empalme tiene salto de muestras o de nivel, lo cierra con
    crossfade (120 ms, busca el punto de mayor parecido; lineal si hay coherencia, de potencia constante si no).
 6. Escribe comparacion-20nov.csv, ids-20nov.csv, los .meta de lo nuevo (plantilla del importador; Unity los
    reescribe al importar) y un log con todas las medidas.
Requiere numpy y scipy; ffmpeg (PATH o variable FFMPEG) para los MP3 y los WAV que scipy no lee.
"""
import argparse
import csv
import hashlib
import os
import shutil
import subprocess
import sys
import tempfile
import uuid
import warnings
from math import gcd

import numpy as np
from scipy.io import wavfile
from scipy.signal import fftconvolve, resample_poly

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importar_sonidos_19nov as V19  # noqa: E402

CARPETAS_19 = V19.CARPETAS
CARPETAS_NUEVAS = ["11_Nova", "12_NPCs", "13_Robots", "14_Ambiente", "15_Exo_Implantes", "16_Casino",
                   "17_Misiones_HUD", "18_Radio_Musica"]
CARPETAS_2D = {"01_UI", "17_Misiones_HUD", "18_Radio_Musica"}
IGNORAR_FUENTE = {"98_Descartados", "99_No_es_sonido"}

# archivo fuente (carpeta/nombre) -> (carpeta destino, id). Solo lo que no conserva su nombre y carpeta.
SIN_NOMBRE = {
    "00_Sin_nombre_ponselo_tu/percusion_cumbia.wav": ("18_Radio_Musica", "RAD_EST1_CUMBIA_PERCUSION"),
    "00_Sin_nombre_ponselo_tu/zumbido_refrigerador.wav": ("14_Ambiente", "OBJ_REFRIGERADOR"),
    "00_Sin_nombre_ponselo_tu/tele_reiniciando.mp3": ("14_Ambiente", "OBJ_TELE_REINICIO"),
}

# Renombrados por el usuario (mismo audio, otro nombre): nuevo -> viejo (ruta relativa a Audio/). Se mueven con
# git mv conservando el .meta. El contenido se valida contra la referencia cuando esta disponible.
RENOMBRES = {
    "02_Armas/ARM_W07_DISPARO.wav": "02_Armas/ARM_W07_DISPARO_01.wav",
    "02_Armas/ARM_W07_RAYO_LOOP.wav": "02_Armas/ARM_W07_RAYO.wav",
    "02_Armas/ARM_W07_GATILLO.wav": "02_Armas/ARM_NOVA_DISPARO_01.wav",
    "02_Armas/ARM_W07_SIN_ENERGIA.wav": "02_Armas/ARM_NOVA_CARGA_DISPARO.wav",
    "11_Nova/NOV_CAIDA_Y_ENCENDIDO.wav": "10_Policia/POL_NOVA_AVISO.wav",
}

# Quitados de la fuente (ahora en 98_Descartados): se anotan, NO se borran de Assets (el codigo los usa).
QUITADOS = {"10_Policia/POL_RADIO_DESPACHO_01.wav": "LEEME: cambiarlos por POL_RADIO_AVISO_*",
            "10_Policia/POL_RADIO_DESPACHO_02.wav": "LEEME: cambiarlos por POL_RADIO_AVISO_*"}

LOOPS = {
    "NOV_JETPACK_LOOP", "FUE_LOOP_GRANDE", "ARM_W07_RAYO_LOOP", "ARM_W07_RAYO_CAPA_EXTRA", "NAV_ROTORES_LOOP",
    "AMB_SIRENA_LOOP", "POL_SIRENA_MURO", "RAD_EST1_CUMBIA_PERCUSION", "OBJ_REFRIGERADOR",
    "AMB_AZOTEA_CIUDAD", "AMB_LOMAS", "AMB_NOVA", "AMB_SUBURBIOS_NOCHE", "CLI_LLUVIA_DENTRO_CARRO", "CLI_LLUVIA_FUERTE",
    "CLI_LLUVIA_SOBRE_LAMINA", "CLI_TORMENTA_TRUENOS", "CLI_VIENTO", "INT_ARMERIA_VENTILADOR", "INT_BODEGA_GOTERAS",
    "INT_TALLER_CANEPA", "INT_TIENDA_TINO_RADIO", "INT_TIENDA_TINO_VENTILADOR", "OBJ_AIRE_ACONDICIONADO",
    "OBJ_CAMARA_SEGURIDAD", "OBJ_TRANSFORMADOR",
}
# El nombre dice LOOP pero el audio decae a silencio: se deja como evento (loop = n) con fades.
LOOP_QUE_NO_ES = {"EXO_PUESTO_LOOP_01", "EXO_PUESTO_LOOP_02", "CAS_RULETA_GIRA", "OBJ_MAQUINA_EXPENDEDORA"}
# Loops rítmicos: no se hace crossfade (rompe el compas); solo se mide y se cuidan los bordes.
LOOPS_RITMICOS = {"RAD_EST1_CUMBIA_PERCUSION"}
# Grabaciones largas que se recortan a un loop de N segundos (ventana mas estable).
VENTANA_LOOP = {"FUE_LOOP_GRANDE": 28.0, "NOV_JETPACK_LOOP": 16.0}
USO_2D_EXTRA = ("UI_", "HUD_", "TIE_", "RAD_", "PAN_", "MIS_", "EXO_RUEDA_")
LOG = []


def log(msg=""):
    print(msg)
    LOG.append(msg)


# ------------------------------------------------------------------------------------------ lectura
def es_puntero_lfs(ruta):
    try:
        with open(ruta, "rb") as f:
            return f.read(24).startswith(b"version https://git-lfs")
    except OSError:
        return False


def buscar_ffmpeg():
    p = os.environ.get("FFMPEG") or shutil.which("ffmpeg")
    if p:
        return p
    for c in ("/c/ffmpeg/ffmpeg-8.1.2-essentials_build/bin/ffmpeg", "C:/ffmpeg/ffmpeg-8.1.2-essentials_build/bin/ffmpeg.exe"):
        if os.path.exists(c) or os.path.exists(c + ".exe"):
            return c
    return "ffmpeg"


def leer(ruta):
    """(sr, x float64 (n, canales), via). No escribe junto a la fuente: los temporales van al directorio temporal."""
    if ruta.lower().endswith(".wav"):
        try:
            with warnings.catch_warnings():
                warnings.simplefilter("ignore")
                sr, d = wavfile.read(ruta)
            return sr, V19.a_float(d), "scipy"
        except Exception:
            pass
    tmpd = tempfile.mkdtemp(prefix="snd20_")
    try:
        tmp = os.path.join(tmpd, "x.wav")
        subprocess.run([buscar_ffmpeg(), "-y", "-loglevel", "error", "-i", ruta, "-c:a", "pcm_s16le", tmp], check=True)
        with warnings.catch_warnings():
            warnings.simplefilter("ignore")
            sr, d = wavfile.read(tmp)
        return sr, V19.a_float(d), "ffmpeg"
    finally:
        shutil.rmtree(tmpd, ignore_errors=True)


def descripcion_formato(ruta):
    if not ruta.lower().endswith(".wav"):
        return "mp3"
    f = V19.formato_wav(ruta)
    if not f:
        return "wav ilegible"
    tag, bits, ch, sr, decl, real = f
    return "%s %dbit" % ("PCM" if tag == 1 else ("extensible" if tag == 65534 else "fmt%d" % tag), bits)


# ------------------------------------------------------------------------------------------ utilidades
def a_mono(x, nombre=""):
    if x.shape[1] == 1:
        return x
    m = x.mean(axis=1, keepdims=True)
    rm = float(np.sqrt((m ** 2).mean()))
    rc = float(np.mean([np.sqrt((x[:, c] ** 2).mean()) for c in range(x.shape[1])]))
    if rc > 1e-9 and rm / rc < 0.5:
        log("    AVISO %s: al pasar a mono se pierde %.1f dB (canales muy fuera de fase)" % (nombre, 20 * np.log10(rm / rc)))
    return m


def bajar_a_48k(sr, x):
    if sr <= 48000:
        return sr, x
    g = gcd(48000, sr)
    y = resample_poly(x, 48000 // g, sr // g, axis=0)
    return 48000, y


def metricas_costura(m, sr):
    """Salto de muestras (en desviaciones de la diferencia tipica) y diferencia de nivel (dB) entre fin y principio."""
    n = len(m)
    w = max(int(sr * 0.02), min(int(sr * 0.1), n // 8))
    sig = float(np.std(np.diff(m))) + 1e-9
    salto = abs(float(m[0]) - float(m[-1])) / sig
    pred = abs(float(m[0]) - (2 * float(m[-1]) - float(m[-2]))) / sig
    a = float(np.sqrt((m[:w] ** 2).mean())) + 1e-9
    b = float(np.sqrt((m[-w:] ** 2).mean())) + 1e-9
    return salto, pred, 20 * np.log10(b / a)


def nivel_ventanas(m, sr, v=0.05):
    h = max(1, int(sr * v))
    n = len(m) // h
    r = np.sqrt((m[: n * h].reshape(n, h) ** 2).mean(axis=1))
    return 20 * np.log10(r + 1e-9), h


def quitar_bordes_flojos(x, sr, margen_db=12.0, max_s=1.5):
    """Recorta del principio y del final lo que esta mas de margen_db por debajo de la mediana (colas y silencios)."""
    m = x.mean(axis=1)
    db, h = nivel_ventanas(m, sr)
    if len(db) < 8:
        return x, 0.0, 0.0
    med = float(np.median(db))
    ok = np.where(db > med - margen_db)[0]
    if len(ok) == 0:
        return x, 0.0, 0.0
    i0, i1 = int(ok[0]) * h, (int(ok[-1]) + 1) * h
    i0 = min(i0, int(max_s * sr))
    i1 = max(i1, len(m) - int(max_s * sr))
    i0 = V19.cruce_cero(m, i0, sr, 0.005) if i0 > 0 else 0
    i1 = V19.cruce_cero(m, i1, sr, 0.005) if i1 < len(m) else len(m)
    return x[i0:i1], i0 / sr, (len(m) - i1) / sr


def cerrar_loop(x, sr, xf_s=0.12, busqueda_s=6.0, forzar=False, nombre=""):
    """Loop sin clic. Busca el largo L (en los ultimos busqueda_s) donde x[L:L+nx] mas se parece a x[:nx] y mezcla la cola
    con la cabeza (lineal si la correlacion es alta, de potencia constante si no). Devuelve (loop, texto)."""
    n = len(x)
    m = x.mean(axis=1)
    nx = max(int(sr * 0.01), min(int(sr * xf_s), n // 8))
    salto0, pred0, niv0 = metricas_costura(m, sr)
    limpio = salto0 <= 4.0 and pred0 <= 6.0 and abs(niv0) <= 3.0
    if limpio and not forzar:
        return x, "empalme ya limpio (salto %.1f sigma, nivel %+.1f dB): sin cambios" % (salto0, niv0)
    S = min(int(busqueda_s * sr), n // 4, n - 3 * nx)  # nunca acorta el loop a menos de 3/4 de lo que habia
    S = max(S, 0)
    cola = m[n - nx - S: n]
    cab = m[:nx]
    dots = fftconvolve(cola, cab[::-1], mode="valid")  # S+1 valores, posicion i -> L = n - nx - S + i
    en = np.concatenate([[0.0], np.cumsum(cola ** 2)])
    loc = en[nx:nx + len(dots)] - en[: len(dots)]
    ncc = dots / (np.sqrt(loc * float((cab ** 2).sum())) + 1e-12)
    i = int(np.argmax(ncc))
    L = n - nx - S + i
    corr = float(ncc[i])
    t = np.linspace(0.0, 1.0, nx)[:, None]
    if corr >= 0.6:
        w_in, w_out = t, 1.0 - t
        modo = "lineal"
    else:
        w_in, w_out = np.sin(t * np.pi / 2), np.cos(t * np.pi / 2)
        modo = "potencia constante"
    out = x[:L].copy()
    out[:nx] = x[:nx] * w_in + x[L:L + nx] * w_out
    salto1, pred1, niv1 = metricas_costura(out.mean(axis=1), sr)
    txt = ("crossfade %d ms (%s, parecido %.2f), largo %.2f s -> %.2f s | empalme antes: salto %.1f sigma, nivel %+.1f dB; "
           "despues: salto %.1f sigma, nivel %+.1f dB" % (nx * 1000 // sr, modo, corr, n / sr, L / sr, salto0, niv0, salto1, niv1))
    return out, txt


def ventana_estable_loop(x, sr, largo_s):
    m = x.mean(axis=1)
    dur = len(m) / sr
    largo_s = min(largo_s, dur)
    a_s = V19.ventana_estable(m, sr, 0.0, dur, largo_s + 0.2)
    a = max(0, int(a_s * sr))
    return x[a: a + int((largo_s + 0.2) * sr)], a / sr


def sin_clic(x, sr, nombre):
    """Fade de 1 ms al inicio y 8 ms al final solo si el primer o el ultimo sample no estan casi en cero."""
    pk = float(np.abs(x).max()) + 1e-12
    y = x.copy()
    nota = []
    if np.abs(y[0]).max() > 0.01 * pk:
        k = max(2, int(sr * 0.001))
        y[:k] *= np.linspace(0, 1, k)[:, None]
        nota.append("fade-in 1 ms")
    if np.abs(y[-1]).max() > 0.01 * pk:
        k = min(len(y) // 2, max(2, int(sr * 0.008)))
        y[-k:] *= np.linspace(1, 0, k)[:, None]
        nota.append("fade-out 8 ms")
    return y, nota


def pico_rms_db(x):
    m = x.mean(axis=1)
    return 20 * np.log10(float(np.abs(x).max()) + 1e-9), 20 * np.log10(float(np.sqrt((m ** 2).mean())) + 1e-9)


def ventana_bordes(m, sr, t, radio=0.02):
    return V19.cruce_cero(m, int(t * sr), sr, radio)


# ------------------------------------------------------------------------------------------ analisis del plasma
def analizar_plasma(nombre, x, sr):
    m = x.mean(axis=1)
    dur = len(m) / sr
    db, h = nivel_ventanas(m, sr, 0.05)
    dt = h / sr
    pk = float(db.max())
    t_pico = float(db.argmax()) * dt
    act = db > pk - 20
    t0 = float(np.argmax(act)) * dt
    t1 = (len(act) - float(np.argmax(act[::-1]))) * dt
    q = max(1, len(db) // 4)
    cuartos = [float(db[i * q:(i + 1) * q].mean()) for i in range(4)]
    sp = np.abs(np.fft.rfft(m))
    fr = np.fft.rfftfreq(len(m), 1 / sr)
    cen = float((sp * fr).sum() / (sp.sum() + 1e-12))
    estable = max(cuartos) - min(cuartos) < 5.0
    sube = cuartos[3] - cuartos[0] > 6.0
    baja = cuartos[0] - cuartos[3] > 6.0
    salto, pred, niv = metricas_costura(m, sr)
    veredicto = {
        "ARM_W07_GATILLO": ("encaja" if dur < 1.5 and t_pico < 0.3 and baja else "revisar",
                            "golpe corto que decae (%.2f s, pico a %.2f s); algo largo para un clic de gatillo" % (dur, t_pico)),
        "ARM_W07_CARGA": ("encaja" if sube and t_pico > dur * 0.6 else "revisar",
                          "sube de %.0f a %.0f dB y el pico esta al final (%.2f s): sirve para cargar" % (cuartos[0], cuartos[3], t_pico)),
        "ARM_W07_DISPARO": ("encaja" if dur < 2 and t_pico < 0.4 else "revisar",
                            "ataque rapido (%.2f s) y cuerpo corto: sirve para el disparo al terminar la carga" % t_pico),
        "ARM_W07_RAYO_LOOP": ("encaja" if estable else "revisar",
                              "nivel estable (%s dB por cuartos): cuerpo de rayo para loop" % ", ".join("%.0f" % c for c in cuartos)),
        "ARM_W07_RAYO_CAPA_EXTRA": ("encaja" if estable and dur > 5 else "revisar",
                                    "capa estable y larga (%.1f s, %.0f Hz de centroide, muy brillante): capa extra del rayo" % (dur, cen)),
        "ARM_W07_ENFRIAR": ("encaja" if baja and cen > 4000 else "revisar",
                            "siseo de vapor que decae (centroide %.0f Hz, %.0f a %.0f dB): sirve para enfriar" % (cen, cuartos[0], cuartos[3])),
        "ARM_W07_SIN_ENERGIA": ("encaja con reservas" if dur < 1.3 and baja else "revisar",
                                "golpe grave corto que decae (%.2f s): suena mas a 'boom' que a clic seco" % dur),
    }.get(nombre)
    if veredicto:
        log("    plasma %s: %s -> %s [dur %.2f s, pico %.1f dB a %.2f s, activo(-20 dB) %.2f-%.2f s, cuartos %s dB, centroide %.0f Hz]" % (
            nombre, veredicto[0], veredicto[1], dur, pk, t_pico, t0, t1, ", ".join("%.0f" % c for c in cuartos), cen))
    return veredicto[0] + ": " + veredicto[1] if veredicto else ""


# ------------------------------------------------------------------------------------------ escritura
def plantilla_meta(guid, carpeta, nombre, dur, mono, loop):
    largo = dur > 8.0
    corto = dur < 2.0 or nombre.startswith(("UI_", "HUD_", "TIE_", "IMP_", "MEL_", "PAS_", "NOV_PASOS"))
    if largo:
        carga, comp, pre, bg = 2, 1, 0, 1
    elif corto:
        carga, comp, pre, bg = 0, 2, 1, 0
    else:
        carga, comp, pre, bg = 1, 1, (1 if loop else 0), 0
    return (
        "fileFormatVersion: 2\nguid: %s\nAudioImporter:\n  externalObjects: {}\n  serializedVersion: 8\n  defaultSettings:\n"
        "    serializedVersion: 2\n    loadType: %d\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n"
        "    compressionFormat: %d\n    quality: 0.7\n    conversionMode: 0\n    preloadAudioData: %d\n"
        "  platformSettingOverrides: {}\n  forceToMono: %d\n  normalize: 1\n  loadInBackground: %d\n  ambisonic: 0\n"
        "  3D: 1\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % (guid, carga, comp, pre, 1 if mono else 0, bg))


META_CARPETA = ("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n"
                "  assetBundleName: \n  assetBundleVariant: \n")


def guid_estable(clave):
    return hashlib.md5(("mecha-cyberpunk-sonidos-20nov:" + clave).encode("utf-8")).hexdigest()


def asegurar_carpeta(destino, carpeta):
    d = os.path.join(destino, carpeta)
    os.makedirs(d, exist_ok=True)
    meta = d + ".meta"
    if not os.path.exists(meta):
        with open(meta, "w", newline="\n") as f:
            f.write(META_CARPETA % guid_estable("carpeta/" + carpeta))
    return d


def proteger_pico(x, tope=0.98):
    """Al bajar de 96/192 kHz o sumar canales el pico puede pasar de 1.0 (se recortaria al pasar a 16 bits)."""
    pk = float(np.abs(x).max())
    return x * (tope / pk) if pk > tope else x


def escribir_wav(destino, carpeta, nombre, sr, x, loop):
    x = proteger_pico(x)
    d = asegurar_carpeta(destino, carpeta)
    ruta = os.path.join(d, nombre + ".wav")
    V19.escribir16(ruta, sr, x)
    meta = ruta + ".meta"
    if not os.path.exists(meta):
        mono = x.shape[1] == 1
        with open(meta, "w", newline="\n") as f:
            f.write(plantilla_meta(guid_estable(carpeta + "/" + nombre), carpeta, nombre, len(x) / sr, mono, loop))
    return ruta


def git_mv(destino, viejo, nuevo):
    """Mueve el .wav y su .meta con git mv (conserva el GUID). Si git falla, os.rename."""
    for suf in ("", ".meta"):
        a, b = viejo + suf, nuevo + suf
        if not os.path.exists(a):
            continue
        os.makedirs(os.path.dirname(b), exist_ok=True)
        r = subprocess.run(["git", "-C", destino, "mv", a, b], capture_output=True, text=True)
        if r.returncode != 0:
            os.rename(a, b)


# ------------------------------------------------------------------------------------------ comparacion
def huella(ruta):
    sr, x, _ = leer(ruta)
    m = x.mean(axis=1)
    return {"sr": sr, "n": len(m), "m": m}


def mismo_audio(a, b, tol=2e-3):
    if a["sr"] != b["sr"] or abs(a["n"] - b["n"]) > 1:
        return False
    n = min(a["n"], b["n"])
    return float(np.abs(a["m"][:n] - b["m"][:n]).max()) < tol


def listar_fuente(fuente):
    res = []
    for c in sorted(os.listdir(fuente)):
        p = os.path.join(fuente, c)
        if not os.path.isdir(p):
            continue
        for f in sorted(os.listdir(p)):
            if f.lower().endswith((".wav", ".mp3")):
                res.append((c, f, os.path.join(p, f)))
    return res


def destino_de(c, f):
    clave = c + "/" + f
    if clave in SIN_NOMBRE:
        return SIN_NOMBRE[clave]
    return c, os.path.splitext(f)[0]


# ------------------------------------------------------------------------------------------ principal
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("fuente")
    ap.add_argument("destino")
    ap.add_argument("--referencia", default=None)
    base = os.path.dirname(os.path.abspath(__file__))
    ap.add_argument("--log", default=os.path.join(base, "importar-20nov.log"))
    ap.add_argument("--csv-comparacion", default=os.path.join(base, "comparacion-20nov.csv"))
    ap.add_argument("--csv-ids", default=os.path.join(base, "ids-20nov.csv"))
    ap.add_argument("--sin-regenerar-19nov", action="store_true")
    ap.add_argument("--solo-comparar", action="store_true")
    a = ap.parse_args()
    ref = a.referencia or a.destino

    # ---- 1. inventario de lo que ya hay (referencia real)
    existentes = {}  # ruta relativa -> ruta fisica
    for c in sorted(os.listdir(ref)):
        p = os.path.join(ref, c)
        if os.path.isdir(p) and c[:2].isdigit():
            for f in os.listdir(p):
                if f.lower().endswith((".wav", ".mp3")):
                    existentes[c + "/" + f] = os.path.join(p, f)
    punteros = [k for k, v in existentes.items() if es_puntero_lfs(v)]
    if punteros:
        log("AVISO: %d archivos de la referencia son punteros LFS (p. ej. %s): use --referencia con los WAV reales." % (len(punteros), punteros[0]))
        if not a.referencia:
            sys.exit(2)
    log("== Referencia: %s (%d archivos)" % (ref, len(existentes)))

    fuente = listar_fuente(a.fuente)
    fuente_util = [(c, f, p) for c, f, p in fuente if c not in IGNORAR_FUENTE]

    # ---- 2. regeneracion de los recortes del 19-nov para comprobar que siguen igual
    derivados_19 = set()  # archivos del 19-nov que salen de grabaciones largas (motores, pasos, loops)
    regen_ok = {}
    scratch = tempfile.mkdtemp(prefix="snd20_regen_")
    try:
        for c in CARPETAS_19:
            sd = os.path.join(a.fuente, c)
            if not os.path.isdir(sd):
                continue
            for f in sorted(os.listdir(sd)):
                if not f.lower().endswith(".wav"):
                    continue
                b = f[:-4]
                if b in V19.MOTORES or b in V19.PASOS or b in V19.LOOPS_LIMPIOS:
                    if a.sin_regenerar_19nov:
                        continue
                    dd = os.path.join(scratch, c)
                    os.makedirs(dd, exist_ok=True)
                    sr, x, _ = V19.leer(os.path.join(sd, f))
                    pico = float(np.abs(x).max())
                    if b in V19.MOTORES:
                        V19.procesar_motor(b, x, sr, pico, dd, V19.MOTORES[b])
                    elif b in V19.PASOS:
                        V19.procesar_pasos(b, x, sr, pico, dd, V19.PASOS[b])
                    else:
                        V19.procesar_loop_limpio(b, x, sr, pico, dd)
        del V19.LOG[:]
        for c in os.listdir(scratch):
            for f in os.listdir(os.path.join(scratch, c)):
                k = c + "/" + f
                derivados_19.add(k)
                if k in existentes and not es_puntero_lfs(existentes[k]):
                    with open(os.path.join(scratch, c, f), "rb") as g1, open(existentes[k], "rb") as g2:
                        regen_ok[k] = g1.read() == g2.read()
                else:
                    regen_ok[k] = None
        if regen_ok:
            log("== Recortes del 19-nov regenerados desde la fuente: %d archivos, iguales byte a byte a Assets: %d, distintos: %d, sin comparar: %d" % (
                len(regen_ok), sum(1 for v in regen_ok.values() if v is True), sum(1 for v in regen_ok.values() if v is False),
                sum(1 for v in regen_ok.values() if v is None)))
            for k, v in sorted(regen_ok.items()):
                if v is False:
                    log("   DISTINTO: %s" % k)
    finally:
        shutil.rmtree(scratch, ignore_errors=True)
    nombres_fuente_19 = {c + "/" + f for c, f, _ in fuente_util if c in CARPETAS_19}
    LARGAS_19 = set(V19.MOTORES) | set(V19.PASOS) | set(V19.LOOPS_LIMPIOS)

    # ---- 3. clasificacion
    filas = []   # comparacion
    plan = []    # (c, f, ruta, carpeta_dest, id, estado, existente, nota)
    huellas_exist = {}

    def hx(k):
        if k not in huellas_exist:
            huellas_exist[k] = huella(existentes[k])
        return huellas_exist[k]

    for c, f, p in fuente_util:
        cd, ident = destino_de(c, f)
        rel = cd + "/" + ident + ".wav"
        clave = c + "/" + f
        estado, existente, nota = "NUEVO", "", ""
        sr, x, _ = leer(p)
        t_src = {"sr": sr, "n": len(x), "m": x.mean(axis=1)}
        if rel in RENOMBRES:
            viejo = RENOMBRES[rel]
            estado, existente = "RENOMBRADO", viejo
            if viejo in existentes and not es_puntero_lfs(existentes[viejo]):
                ok = mismo_audio(t_src, hx(viejo))
                nota = "mismo audio que %s (%s)" % (viejo, "verificado" if ok else "NO COINCIDE: revisar")
                if not ok:
                    estado = "REEMPLAZA"
            else:
                nota = "mismo audio que %s" % viejo
        elif clave in nombres_fuente_19 and ident in LARGAS_19:
            estado, existente = "IGUAL", ""
            nota = "grabacion larga: el 19-nov la recorto en varios archivos (%s)" % (
                "regenerados desde la fuente: identicos a Assets" if regen_ok and all(v is True for v in regen_ok.values()) else "ver log")
        elif rel in existentes:
            existente = rel
            igual = mismo_audio(t_src, hx(rel))
            estado = "IGUAL" if igual else "REEMPLAZA"
            nota = "mismo contenido (frecuencia, largo y muestras)" if igual else "mismo ID con audio distinto: se usa el nuevo"
        else:
            otras = [k for k in existentes if k.split("/", 1)[1] == ident + ".wav" and k.split("/", 1)[0] != cd]
            if otras:
                estado, existente, nota = "RENOMBRADO", otras[0], "cambio de carpeta"
            else:
                igual_a = [k for k in existentes if not es_puntero_lfs(existentes[k]) and k not in derivados_19
                           and mismo_audio(t_src, hx(k))]
                if igual_a:
                    nota = "audio identico a %s (es otro ID: se importa igual)" % igual_a[0]
        plan.append((c, f, p, cd, ident, estado, existente, nota, sr, x))
        if estado in ("NUEVO", "REEMPLAZA") and p.lower().endswith(".mp3"):
            nota = (nota + "; " if nota else "") + "MP3 -> WAV PCM16"
        filas.append({"carpeta_fuente": c, "archivo_fuente": f, "id": ident, "carpeta_destino": cd, "estado": estado,
                      "existente": existente, "frecuencia": sr, "canales": x.shape[1], "formato": descripcion_formato(p),
                      "duracion_s": "%.2f" % (len(x) / sr), "nota": nota})

    # quitados: en Assets pero ya no en la fuente (y que no son un derivado ni un renombrado)
    renombrados_viejos = {v for v in RENOMBRES.values()}
    claves_fuente_dest = {cd + "/" + i + ".wav" for _, _, _, cd, i, _, _, _, _, _ in plan}
    for k in sorted(existentes):
        c0 = k.split("/", 1)[0]
        if c0 not in CARPETAS_19 or k in claves_fuente_dest or k in derivados_19 or k in renombrados_viejos:
            continue
        motivo = QUITADOS.get(k, "ya no esta en la fuente")
        filas.append({"carpeta_fuente": "(Assets)", "archivo_fuente": k, "id": os.path.splitext(k.split("/")[1])[0],
                      "carpeta_destino": c0, "estado": "SE QUITO", "existente": k, "frecuencia": "", "canales": "",
                      "formato": "", "duracion_s": "", "nota": motivo + " (no se borra de Assets: el codigo puede usarlo)"})
    for c, f, p in fuente:
        if c in IGNORAR_FUENTE:
            filas.append({"carpeta_fuente": c, "archivo_fuente": f, "id": os.path.splitext(f)[0], "carpeta_destino": "",
                          "estado": "IGNORADO", "existente": "", "frecuencia": "", "canales": "", "formato": "",
                          "duracion_s": "", "nota": "98_Descartados/99_No_es_sonido: no se importa"})

    cuenta = {}
    for fi in filas:
        cuenta[fi["estado"]] = cuenta.get(fi["estado"], 0) + 1
    log("== Comparacion: %s" % ", ".join("%s %d" % kv for kv in sorted(cuenta.items())))
    with open(a.csv_comparacion, "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=["carpeta_fuente", "archivo_fuente", "id", "carpeta_destino", "estado", "existente",
                                           "frecuencia", "canales", "formato", "duracion_s", "nota"])
        w.writeheader()
        w.writerows(filas)
    if a.solo_comparar:
        _escribir_log(a.log)
        return

    # ---- 4. renombrados con git mv (conserva .meta y GUID)
    for rel, viejo in RENOMBRES.items():
        pv, pn = os.path.join(a.destino, viejo), os.path.join(a.destino, rel)
        if os.path.exists(pv) and not os.path.exists(pn):
            asegurar_carpeta(a.destino, rel.split("/")[0])
            git_mv(a.destino, pv, pn)
            log("== git mv %s -> %s (con su .meta)" % (viejo, rel))
        elif os.path.exists(pn):
            log("== ya movido: %s" % rel)

    # ---- 5. importar lo nuevo
    resultados = {}  # id -> dict(archivo, duracion, ...)
    log("== Importacion")
    for c, f, p, cd, ident, estado, existente, nota, sr, x in plan:
        if estado == "IGUAL":
            continue
        loop = ident in LOOPS
        es2d = cd in CARPETAS_2D or ident.startswith("EXO_RUEDA_")
        mono_out = not es2d
        formato = descripcion_formato(p)
        log("  %s/%s -> %s/%s [%s] %.2f s %d Hz %d canales %s" % (c, f, cd, ident, estado, len(x) / sr, sr, x.shape[1], formato))
        notas = []
        if p.lower().endswith(".mp3"):
            notas.append("MP3 -> WAV PCM16")
        elif formato.startswith("extensible") or formato.startswith("fmt"):
            notas.append("WAV extensible -> PCM16")
        elif "24bit" in formato:
            notas.append("24 -> 16 bits")
        salidas = []  # (id, x, sr, loop, nota)
        if estado == "RENOMBRADO":
            # solo se mueve; el audio no cambia (identico al existente)
            resultados[ident] = {"cd": cd, "ruta": os.path.join(a.destino, cd, ident + ".wav"), "loop": loop,
                                 "nota": "; ".join(["movido con git mv (mismo GUID)"] + ([nota] if nota else []))}
            if ident.startswith("ARM_W07_") and ident != "ARM_W07_RAYO_LOOP":
                analizar_plasma(ident, x, sr)
            continue
        if mono_out:
            if x.shape[1] > 1:
                notas.append("estereo -> mono")
            x = a_mono(x, ident)
        sr0 = sr
        sr, x = bajar_a_48k(sr, x)
        if sr != sr0:
            notas.append("%d -> %d Hz" % (sr0, sr))
        veredicto = ""
        if ident.startswith("ARM_W07_"):
            veredicto = analizar_plasma(ident, x, sr)

        if ident == "EXO_RUEDA_CAMARA_LENTA":
            m = x.mean(axis=1)
            corte = V19.cruce_cero(m, int(2.0 * sr), sr, 0.01)
            ent = x[:corte]
            ent, n1 = sin_clic(ent, sr, "ENTRAR")
            sal = x[corte:]
            db, h = nivel_ventanas(sal.mean(axis=1), sr, 0.05)
            util = np.where(db > -60.0)[0]
            fin = min(len(sal), (int(util.max()) + 3) * h) if len(util) else len(sal)
            sal = sal[:fin]
            sal, n2 = sin_clic(sal, sr, "SALIR")
            salidas.append(("EXO_RUEDA_CAMARA_LENTA_ENTRAR", ent, loop, "primeros %.2f s (entrada a camara lenta)" % (len(ent) / sr)))
            salidas.append(("EXO_RUEDA_CAMARA_LENTA_SALIR", sal, loop,
                            "de %.2f s hasta que baja de -60 dB (%.2f s). El ultimo segundo literal (%.2f-%.2f s) ya esta casi en silencio "
                            "(-41 a -57 dB) y perderia la caida, por eso se parte en 2.0 s" % (corte / sr, len(sal) / sr, (len(m) - sr) / sr, len(m) / sr)))
        elif ident == "ROB_MUERTE":
            m = x.mean(axis=1)
            fin = V19.cruce_cero(m, int(1.0 * sr), sr, 0.01)
            seg = V19.fade(x[:fin], sr, 0.002, 0.06)
            salidas.append((ident, seg, False, "solo el primer segundo (%.2f s), fade-out 60 ms (LEEME)" % (fin / sr)))
        elif ident in VENTANA_LOOP:
            seg, ini = ventana_estable_loop(x, sr, VENTANA_LOOP[ident])
            lp, txt = cerrar_loop(seg, sr, forzar=True, nombre=ident)
            salidas.append((ident, lp, True, "ventana mas estable de %.1f s tomada de %.1f s; %s" % (VENTANA_LOOP[ident], ini, txt)))
        elif ident in LOOPS:
            y = x
            if ident not in LOOPS_RITMICOS:
                y, i0, i1 = quitar_bordes_flojos(x, sr)
                bn = ""
                if i0 > 0 or i1 > 0:
                    bn = "bordes flojos recortados (inicio %.2f s, final %.2f s); " % (i0, i1)
            else:
                bn = "loop ritmico: sin crossfade; "
            if ident in LOOPS_RITMICOS:
                lp, txt = y, "empalme medido (salto %.1f sigma, nivel %+.1f dB)" % (metricas_costura(y.mean(axis=1), sr)[0], metricas_costura(y.mean(axis=1), sr)[2])
                lp, nn = sin_clic(lp, sr, ident)
                txt += ("; " + ", ".join(nn)) if nn else ""
            else:
                xf = 0.12 if len(y) / sr > 3.0 else 0.04
                lp, txt = cerrar_loop(y, sr, xf_s=xf, nombre=ident)
            salidas.append((ident, lp, True, bn + txt))
        else:
            y, nn = sin_clic(x, sr, ident)
            if ident in LOOP_QUE_NO_ES:
                nn.append("el audio decae a silencio (no tiene cuerpo estable): se deja como evento, loop=n")
            salidas.append((ident, y, False, "; ".join(nn)))

        for sid, y, lp, nt in salidas:
            ruta = escribir_wav(a.destino, cd, sid, sr, y, lp)
            pk, rms = pico_rms_db(proteger_pico(y))
            total_notas = "; ".join([t for t in notas + ([nt] if nt else []) + ([veredicto] if veredicto else []) if t])
            log("    -> %s/%s: %.2f s, %d KB, pico %.1f dB, RMS %.1f dB | %s" % (cd, sid, len(y) / sr, os.path.getsize(ruta) // 1024, pk, rms, nt))
            resultados[sid] = {"cd": cd, "ruta": ruta, "loop": lp, "nota": total_notas, "sr": sr}
            if ident in LOOPS:
                salto, pred, niv = metricas_costura(y.mean(axis=1), sr)
                log("       empalme final: salto %.1f sigma (prediccion %.1f), nivel %+.1f dB" % (salto, pred, niv))

    # ---- 5b. RAYO_LOOP renombrado: se reescribe en su sitio como loop limpio (conserva el GUID)
    for c, f, p, cd, ident, estado, existente, nota, sr, x in plan:
        if ident != "ARM_W07_RAYO_LOOP":
            continue
        analizar_plasma(ident, x, sr)
        sr2, y = bajar_a_48k(sr, a_mono(x, ident))
        y, i0, i1 = quitar_bordes_flojos(y, sr2, 12.0, 0.3)
        lp, txt = cerrar_loop(y, sr2, 0.12, nombre=ident)
        ruta = os.path.join(a.destino, cd, ident + ".wav")
        V19.escribir16(ruta, sr2, proteger_pico(lp))
        meta = ruta + ".meta"
        log("    -> %s/%s: reescrito en su sitio como loop limpio (mono, %d Hz, %.2f s): %s" % (cd, ident, sr2, len(lp) / sr2, txt))
        resultados[ident] = {"cd": cd, "ruta": ruta, "loop": True, "sr": sr2,
                             "nota": "renombrado de ARM_W07_RAYO (git mv, mismo GUID) y reescrito como loop limpio: " + txt}

    # ---- 6. ids-20nov.csv
    escribir_ids(a, plan, resultados, existentes)
    _escribir_log(a.log)


def _escribir_log(ruta):
    os.makedirs(os.path.dirname(os.path.abspath(ruta)), exist_ok=True)
    with open(ruta, "w", encoding="utf-8") as fh:
        fh.write("\n".join(LOG) + "\n")


# ------------------------------------------------------------------------------------------ ids-20nov.csv
USO = {
    "UI_SELECCIONAR": "Aceptar; rueda: elegir habilidad; checkpoint y marcador (LEEME)",
    "UI_MOVER": "Mover el cursor; rueda de armas/habilidades: mover",
    "UI_ERROR": "Error / sin dinero; mision fallida; exo sin energia (alternativa)",
    "UI_MENU_ABRIR": "Abrir pausa, telefono y mapa",
    "UI_MENU_CERRAR": "Cerrar pausa, telefono y mapa",
    "HUD_NOTIFICACION": "Mision nueva, objetivo actualizado y guardado de partida (UI_GUARDADO)",
    "HUD_LOGRO": "Mision completada",
    "TIE_EQUIPAR": "Equipar; clics al instalar un implante con el Doc",
    "IMP_BALA_ZUMBIDO_01": "Bala que pasa cerca; indicador de dano",
    "ARM_W06_SACAR": "Sacar revolver (sacar/guardar demas armas: se usa la recarga de cada arma)",
    "MEL_GOLPE_01": "Golpe cuerpo a cuerpo; arrestado (MEL_GOLPE + sirena alejandose + esposas)",
    "JUG_ATERRIZAJE": "Caida del jugador; aterrizaje del super salto (+ EXP_ONDA bajito)",
    "POL_RADIO_AVISO_01": "Voz Mexican Police (aviso); el altavoz tambien sale de estas voces",
    "POL_RADIO_AVISO_02": "Voz Mexican Police (aviso)",
    "POL_RADIO_AVISO_03": "Voz Mexican Police (aviso)",
    "POL_RADIO_AVISO_04": "Voz Mexican Police (aviso)",
    "PAS_METAL_01": "Pasos en metal; pasos del centinela robot",
    "VEH_CHOQUE_01": "Choque; tambien contacto con el carro (charco, rin, cajuela, llaves, direccionales, tijera, raspon, atropello)",
    "VEH_CHOQUE_02": "Choque; tambien contacto con el carro",
    "VEH_CHOQUE_FUERTE_01": "Choque fuerte; tambien contacto con el carro",
    "VEH_CHOQUE_FUERTE_02": "Choque fuerte; tambien contacto con el carro",
    # --- 11_Nova
    "NOV_CAIDA_Y_ENCENDIDO": "El agente de Nova cae del cielo y se enciende (antes POL_NOVA_AVISO: aviso de llegada de Nova)",
    "NOV_ARMADURA_MOVER": "Roce de armadura de Nova al moverse",
    "NOV_ESCUDO_ACTIVAR": "Enemigo Nova se cubre con el escudo; el jugador reutiliza activar/recargar escudo (pitch)",
    "NOV_ESCUDO_IMPACTO": "Balas contra el escudo de Nova (el escudo no se rompe)",
    "NOV_JETPACK_LOOP": "Jetpack de Nova en vuelo; encendido y apagado con fade de volumen",
    # --- 12_NPCs
    "NPC_H_DOLOR": "Atropello y dano a NPC hombre",
    "NPC_M_DOLOR": "Atropello y dano a NPC mujer",
    # --- 13_Robots
    "ROB_VOZ_ALERTA": "Voz de robot al detectar al jugador (centinela, pesado, perro)",
    "ROB_ENCENDER": "Robot se enciende / despierta",
    "ROB_SERVOS": "Servos al moverse; tambien brazos/fuerza y activar exo (pitch); apagado y caida con pitch bajo",
    "ROB_CHISPAS_DANIO": "Chispas al dano de robot",
    "ROB_MUERTE": "Muerte de robot (solo el primer segundo); caida/apagado reciclan este con pitch bajo",
    "ROB_PES_LLEGADA_01": "Aparecen los robots gigantes; tambien pasos del pesado con pitch bajo",
    "ROB_PES_LLEGADA_02": "Aparecen los robots gigantes (variante)",
    "ROB_PES_HIDRAULICA": "Hidraulica del robot pesado",
    "ROB_PES_PLACA_CAE": "Placa del robot pesado cae; caida con pitch bajo",
    "ROB_PERRO_MOVER": "Pasos/movimiento del perro robot",
    "ROB_PERRO_GRUNIDO": "Gruñido del perro robot",
    "DRN_PES_AMETRALLADORA": "Un tiro de la ametralladora del dron pesado; se repite para la rafaga",
    # --- 14_Ambiente
    "AMB_NOVA": "Fondo cerca de las calles de Nova (loop)",
    "AMB_LOMAS": "Ambiente de Las Lomas",
    "AMB_SUBURBIOS_NOCHE": "Ambiente de los suburbios de noche",
    "AMB_AZOTEA_CIUDAD": "Ambiente en azoteas de la ciudad",
    "CLI_LLUVIA_FUERTE": "Lluvia fuerte (clima)",
    "CLI_TORMENTA_TRUENOS": "Tormenta con truenos incluidos",
    "CLI_VIENTO": "Viento (clima)",
    "CLI_LLUVIA_SOBRE_LAMINA": "Lluvia sobre lamina (techos, bodegas)",
    "CLI_LLUVIA_DENTRO_CARRO": "Lluvia oida desde dentro del carro",
    "INT_TIENDA_TINO_RADIO": "Interior tienda de Tino: radio (suena junto al ventilador)",
    "INT_TIENDA_TINO_VENTILADOR": "Interior tienda de Tino: ventilador (suena junto a la radio)",
    "INT_TALLER_CANEPA": "Interior del taller de Canepa",
    "INT_ARMERIA_VENTILADOR": "Interior de la armeria: ventilador",
    "INT_BODEGA_GOTERAS": "Interior de bodega: goteras",
    "OBJ_TRANSFORMADOR": "Zumbido de transformador de poste (a volumen bajo, como pidio el usuario)",
    "OBJ_AIRE_ACONDICIONADO": "Aire acondicionado en la pared",
    "OBJ_CAMARA_SEGURIDAD": "Camara de seguridad (Lomas, Nova)",
    "OBJ_MAQUINA_EXPENDEDORA": "Maquina expendedora (zumbido y golpe de la lata/moneda; evento de 1.2 s que decae, no loop)",
    "OBJ_REFRIGERADOR": "Zumbido de refrigerador para la tiendita y la clinica (idea de Tony)",
    "OBJ_TELE_REINICIO": "Tele de casa o del hacker que se reinicia (idea de Tony)",
    # --- 15_Exo_Implantes
    "EXO_PUESTO_LOOP_01": "Llevar puesta la protesis o el exo (reemplaza el golpe del brazo robotico); evento que decae a silencio",
    "EXO_PUESTO_LOOP_02": "Llevar puesta la protesis o el exo (variante); evento que decae a silencio",
    "EXO_SERVOS_CAMINAR": "Servos del exo al caminar; tambien brazos/fuerza y activar exo",
    "EXO_RUEDA_CAMARA_LENTA_ENTRAR": "Abrir la rueda de armas/habilidades: entrada a camara lenta (2D)",
    "EXO_RUEDA_CAMARA_LENTA_SALIR": "Cerrar la rueda: salida de camara lenta (2D)",
    "EXO_PIERNAS_SALTO_SUPER": "Super salto del exo (aterrizaje = JUG_ATERRIZAJE + EXP_ONDA bajito)",
    "EXO_ESCUDO_GOLPE": "Golpe al escudo del jugador",
    "EXO_VISOR_ACTIVAR": "Activar el visor (loop y apagado: el mismo bajito o en reversa)",
    "IMP_INSTALAR_TALADRO": "Instalar implante con el Doc: taladro",
    "IMP_SINAPSIS_01": "Instalar implante: pitido de sinapsis",
    "IMP_SINAPSIS_02": "Instalar implante: pitido de sinapsis (variante)",
    # --- 16_Casino
    "CAS_TRAGAMONEDAS": "Tragamonedas: palanca + giro + paro (5 s)",
    "CAS_PREMIO_CHICO": "Tragamonedas: premio chico",
    "CAS_PREMIO_GRANDE": "Tragamonedas: premio grande",
    "CAS_FICHAS": "Fichas del casino",
    "CAS_RULETA_GIRA": "Ruleta girando con la bolita (28 s; se completa sola, no es loop)",
    "CAS_CARTAS_BARAJAR_01": "Barajar cartas",
    "CAS_CARTAS_BARAJAR_02": "Barajar cartas (variante)",
    "CAS_CARTAS_REPARTIR": "Repartir cartas",
    # --- 17_Misiones_HUD
    "MIS_TEMPORIZADOR": "Temporizador de mision",
    "HUD_MENSAJE_CELULAR": "Mensaje de celular; sirve tambien para UI_TELEFONO",
    "PAN_ARRESTADO_ESPOSAS": "Arrestado: esposas (junto a MEL_GOLPE + POL_SIRENA_LOOP alejandose)",
    "PAN_HAS_MUERTO": "Pantalla de muerte: ultimos latidos",
    # --- 18_Radio_Musica
    "RAD_EST1_CUMBIA_PERCUSION": "Estacion 1 (cumbia) de la radio y bocina de casa en fiesta (OBJ_BOCINA_FIESTA); percusion en loop (idea de Tony)",
    # --- armas
    "ARM_W07_GATILLO": "Plasma W07: al apretar el gatillo (antes ARM_NOVA_DISPARO_01)",
    "ARM_W07_CARGA": "Plasma W07: mientras carga",
    "ARM_W07_DISPARO": "Plasma W07: justo al terminar la carga (antes ARM_W07_DISPARO_01)",
    "ARM_W07_RAYO_LOOP": "Plasma W07: mientras dura el rayo (antes ARM_W07_RAYO)",
    "ARM_W07_RAYO_CAPA_EXTRA": "Plasma W07: capa extra sobre el rayo",
    "ARM_W07_ENFRIAR": "Plasma W07: al soltar (enfriar)",
    "ARM_W07_SIN_ENERGIA": "Plasma W07: sin energia/balas (antes ARM_NOVA_CARGA_DISPARO); el arma de Nova recicla el plasma con otro pitch; tambien exo sin energia",
    "ARM_SIN_BALAS": "Clic vacio al disparar sin balas",
    "ARM_W06_MECANICA_01": "Revolver: mecanica (tambor/martillo); audio igual a ARM_W06_SACAR",
    "ARM_W06_MECANICA_02": "Revolver: mecanica (tambor/martillo)",
    "ARM_W06_MECANICA_03": "Revolver: mecanica (tambor/martillo)",
    "ARM_W06_MECANICA_04": "Revolver: mecanica (tambor/martillo)",
    "ARM_W06_MECANICA_05": "Revolver: mecanica (tambor/martillo)",
    "ARM_W08_DISPARO": "Lanzagranadas: disparo (bloop); audio igual a ARM_GRANADA_REBOTE, a revisar",
    "CAS_CHICO_01": "Casquillo chico cayendo",
    "CAS_CHICO_02": "Casquillo chico cayendo",
    "CAS_GRANDE_01": "Casquillo grande cayendo",
    "IMP_MISILES_LANZAR": "Lanzamiento de misiles",
    # --- explosiones
    "EXP_CARRO": "Explosion de carro (reemplaza VEH_EXPLOSION / EXP_ONDA en el carro)",
    "EXP_DRON_ROBOT": "Explosion de dron o robot",
    "EXP_NAVE": "Explosion de la patrulla voladora que cae",
    "EXP_BARRIL": "Explosion de barril",
    "EXP_ESCOMBROS": "Escombros justo despues de una explosion grande",
    "EXP_CRISTALES": "Cristales justo despues de una explosion grande",
    "FUE_ENCENDER": "Fwoosh al prenderse algo; sigue FUE_LOOP_GRANDE",
    "FUE_LOOP_GRANDE": "Fuego grande en loop (recortado de 145 s)",
    "DES_CRISTAL_VENTANA": "Ventana que se rompe",
    # --- jugador, interaccion, consumibles
    "JUG_ROPA_ROCE": "Roce de ropa del jugador",
    "JUG_LATIDO_VIDA_BAJA": "Latido con vida baja",
    "JUG_ZUMBIDO_EXPLOSION": "Pitido de oido tras explosion cercana",
    "MEL_BLOQUEO": "Bloqueo de golpe",
    "MEL_GOLPE_METAL": "Golpe a metal/armadura/robot",
    "INT_RECOGER_DINERO": "Recoger dinero",
    "CON_PAPITAS_ABRIR": "Abrir bolsa de papitas",
    "RAD_CAMBIAR_ESTACION": "Estatica al cambiar de estacion de radio",
    # --- vehiculos y policia
    "MOTO_ARRANQUE_RALENTI_APAGAR": "(grabacion larga; solo se usan sus recortes MOTO_*_03)",
    "AMB_SIRENA_LOOP": "Sirena de ambulancia en loop",
    "POL_ESTRELLA_SUBE": "Sube una estrella de busqueda",
    "NAV_ROTORES_LOOP": "Rotores de la nave policial en loop",
    "NAV_DANIADA": "Nave policial danada",
    "NAV_REFLECTOR": "Reflector de la nave",
    "POL_RADIO_AVISO_05": "Voz Mexican Police (aviso); el altavoz tambien sale de estas voces",
    "POL_RADIO_ESTATICA": "Estatica sola para la radio de la patrulla",
    "POL_SIRENA_MURO": "Sirena de la torre del muro cuando sube mucho la busqueda (opcional)",
}


def uso_de(ident, estado):
    if ident in USO:
        return USO[ident]
    for pref, txt in (("VEH_NORMAL_", "Carro normal: arranque/ralenti (loop)/apagar"), ("MOTO_", "Moto: arranque/ralenti (loop)/apagar; la moto grande con otro pitch"),
                      ("VEH_PESADO_", "Camion/pesado"), ("PAS_TIERRA_", "Pisadas sueltas en tierra"), ("PAS_GRAVA_", "Pisadas sueltas en grava"),
                      ("NOV_PASOS_CAMINAR_", "Nova camina con armadura (12 pasos; el pesado los reutiliza con pitch bajo)"),
                      ("NOV_PASOS_CORRER_", "Nova corre con armadura (10 pasos)")):
        if ident.startswith(pref):
            return txt
    return "Ya conectado el 19-nov (ver docs/reporte-sonidos-19nov.md)"


def duracion_wav(ruta):
    try:
        sr, x, _ = leer(ruta)
        return len(x) / sr, x.shape[1], sr
    except Exception:
        return 0.0, 0, 0


def escribir_ids(a, plan, resultados, existentes):
    filas = []
    estado_de = {t[4]: (t[5], t[6]) for t in plan}
    vistos = set()
    # lo que sale de la fuente + derivados + todo lo que hay en 01..18
    carpetas = CARPETAS_19 + CARPETAS_NUEVAS
    for c in carpetas:
        d = os.path.join(a.destino, c)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.lower().endswith(".wav"):
                continue
            ident = os.path.splitext(f)[0]
            ruta = os.path.join(d, f)
            if c + "/" + f in QUITADOS:
                continue
            if ident in vistos:
                continue
            vistos.add(ident)
            r = resultados.get(ident)
            if es_puntero_lfs(ruta):
                # worktree sin LFS: leer del referencia
                rel_ant = RENOMBRES.get(c + "/" + f, c + "/" + f)
                alt = os.path.join(a.referencia or a.destino, rel_ant)
                ruta_lect = alt if os.path.exists(alt) and not es_puntero_lfs(alt) else None
            else:
                ruta_lect = ruta
            dur = ""
            if ruta_lect:
                dur = "%.2f" % duracion_wav(ruta_lect)[0]
            elif r and "dur" in r:
                dur = "%.2f" % r["dur"]
            est, reemp = "igual", ""
            if ident in estado_de:
                e, ex = estado_de[ident]
                est = {"NUEVO": "nuevo", "IGUAL": "igual", "RENOMBRADO": "renombrado", "REEMPLAZA": "reemplaza"}.get(e, e.lower())
                reemp = ex if est in ("renombrado", "reemplaza") else ""
            elif ident.startswith("EXO_RUEDA_CAMARA_LENTA_"):
                est, reemp = "nuevo", "EXO_RUEDA_CAMARA_LENTA (recorte de la grabacion fuente)"
            es_loop = "s" if (ident in LOOPS or "_RALENTI" in ident or ident in ("VEH_ALARMA", "VEH_LLANTAS_GRAVA", "FUE_LOOP_CHICO", "POL_SIRENA_LOOP")) else "n"
            if r:
                es_loop = "s" if r.get("loop") else "n"
            es2d = c in CARPETAS_2D or ident.startswith(USO_2D_EXTRA) or ident in ("JUG_LATIDO_VIDA_BAJA", "JUG_ZUMBIDO_EXPLOSION")
            filas.append([ident, "Assets/Arte/Audio/%s/%s" % (c, f), c, dur, es_loop, "2D" if es2d else "3D", est, reemp, uso_de(ident, est),
                          (r or {}).get("nota", "")])
    for k, motivo in QUITADOS.items():
        ident = os.path.splitext(k.split("/")[1])[0]
        pr = os.path.join(a.referencia or a.destino, k)
        dq = "%.2f" % duracion_wav(pr)[0] if os.path.exists(pr) and not es_puntero_lfs(pr) else ""
        filas.append([ident, "Assets/Arte/Audio/" + k, k.split("/")[0], dq, "n", "3D", "quitado", "", motivo + " (sigue en Assets; el codigo la usa)", ""])
    with open(a.csv_ids, "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["id", "archivo", "carpeta", "duracion_s", "loop(s/n)", "2D/3D", "estado", "reemplaza_a", "uso_previsto", "notas"])
        w.writerows(filas)
    log("== ids-20nov.csv: %d filas" % len(filas))


if __name__ == "__main__":
    main()
