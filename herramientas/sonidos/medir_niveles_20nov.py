"""Mide el nivel (RMS, pico, LUFS aproximado) de cada WAV de las carpetas 01_..18_ de Assets/Arte/Audio.
Uso: python medir_niveles_20nov.py <carpeta Assets/Arte/Audio con los WAV reales> <salida.csv>
Los WAV del repo son punteros LFS: apuntar a una carpeta con los reales. Un ID con varias variantes (_01, _02...) se promedia en energia.
LUFS: filtro K de ITU-R BS.1770 (biquads calculados para la frecuencia del archivo), canales sumados por igual, compuerta -70 LUFS absoluta
y -10 LU relativa; si el archivo dura menos de 0.4 s (sin bloques) se usa la energia total sin compuerta. Es una medida aproximada para igualar categorias."""
import csv, glob, math, os, re, sys
import numpy as np
from scipy.io import wavfile
from scipy.signal import lfilter


def k_weight(x, fs):
    # etapa 1: shelving alto; etapa 2: pasa altos (coeficientes de BS.1770 generalizados a cualquier fs)
    f0, G, Q = 1681.974450955533, 3.999843853973347, 0.7071752369554196
    K = math.tan(math.pi * f0 / fs)
    Vh = 10 ** (G / 20)
    Vb = Vh ** 0.4996667741545416
    a0 = 1 + K / Q + K * K
    b = [(Vh + Vb * K / Q + K * K) / a0, 2 * (K * K - Vh) / a0, (Vh - Vb * K / Q + K * K) / a0]
    a = [1, 2 * (K * K - 1) / a0, (1 - K / Q + K * K) / a0]
    y = lfilter(b, a, x, axis=0)
    f0, Q = 38.13547087602444, 0.5003270373238773
    K = math.tan(math.pi * f0 / fs)
    a0 = 1 + K / Q + K * K
    b = [1, -2, 1]
    a = [1, 2 * (K * K - 1) / a0, (1 - K / Q + K * K) / a0]
    return lfilter(b, a, y, axis=0)


def a_float(d):
    if d.dtype == np.int16: return d / 32768.0
    if d.dtype == np.int32: return d / 2147483648.0
    if d.dtype == np.uint8: return (d - 128) / 128.0
    return d.astype(np.float64)


def medir(ruta):
    fs, d = wavfile.read(ruta)
    x = a_float(d)
    if x.ndim == 1: x = x[:, None]
    n = x.shape[0]
    if n == 0: return None
    ms = float(np.mean(x ** 2))
    rms = 10 * math.log10(ms + 1e-12)
    pico = 20 * math.log10(float(np.max(np.abs(x))) + 1e-9)
    z = k_weight(x, fs)
    blq = int(0.4 * fs)
    if n < blq:
        lufs = -0.691 + 10 * math.log10(float(np.sum(np.mean(z ** 2, axis=0))) + 1e-12)
    else:
        paso = int(0.1 * fs)
        e = []
        for i in range(0, n - blq + 1, paso):
            e.append(float(np.sum(np.mean(z[i:i + blq] ** 2, axis=0))))
        e = np.array(e)
        l = -0.691 + 10 * np.log10(e + 1e-12)
        sel = e[l > -70]
        if len(sel) == 0: lufs = -70.0
        else:
            rel = -0.691 + 10 * math.log10(float(np.mean(sel))) - 10
            sel = e[l > max(-70, rel)]
            lufs = -0.691 + 10 * math.log10(float(np.mean(sel)))
    return fs, n / fs, rms, pico, lufs


def main():
    raiz, salida = sys.argv[1], sys.argv[2]
    ids = {}
    for ruta in sorted(glob.glob(os.path.join(raiz, '[0-9][0-9]_*', '*.wav'))):
        carpeta = os.path.basename(os.path.dirname(ruta))
        nombre = os.path.splitext(os.path.basename(ruta))[0]
        id_ = re.match(r'^(.+?)(_\d\d)?$', nombre).group(1)
        m = medir(ruta)
        if m is None: continue
        ids.setdefault((carpeta, id_), []).append(m)
    with open(salida, 'w', newline='', encoding='utf-8') as f:
        w = csv.writer(f)
        w.writerow(['carpeta', 'id', 'archivos', 'duracion_s', 'rms_db', 'pico_db', 'lufs'])
        for (c, i), ms in sorted(ids.items()):
            def prom(k): return 10 * math.log10(np.mean([10 ** (m[k] / 10) for m in ms]))
            w.writerow([c, i, len(ms), round(max(m[1] for m in ms), 2), round(prom(2), 1), round(max(m[3] for m in ms), 1), round(prom(4), 1)])


main()
