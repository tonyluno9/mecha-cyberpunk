"""T-521: quita el fondo a las piezas UI01..UI14 y recorta las hojas en PNG sueltos.
Uso (desde la raiz del repo): python -I Assets/Arte/UI/Fuentes~/procesar_ui.py
Metodo: el fondo es gris liso (UI10 y UI13 charcoal). Se ajusta un plano cuadratico al color del fondo (pixeles conectados a los bordes),
el alfa sale de la distancia al fondo y el color de borde se "des-mezcla" con el fondo. Sin rembg (no hace falta instalar nada).
"""
import os
import sys
from collections import deque

import numpy as np
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.normpath(os.path.join(AQUI, "..", "Sprites"))


def cargar(nombre):
    return np.asarray(Image.open(os.path.join(AQUI, nombre + ".png")).convert("RGB")).astype(np.float32)


def region_fondo(img, tol=14.0):
    """Pixeles conectados a los bordes cuyo color es parecido al de su vecino (fondo con vineta suave)."""
    h, w, _ = img.shape
    vis = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            vis[y, x] = True
            q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if not vis[y, x]:
                vis[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        c = img[y, x]
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not vis[ny, nx]:
                if np.abs(img[ny, nx] - c).max() <= 3.5 and np.abs(img[ny, nx] - img[0, 0]).max() <= 70:
                    vis[ny, nx] = True
                    q.append((ny, nx))
    return vis


def plano_fondo(img, mascara):
    h, w, _ = img.shape
    ys, xs = np.nonzero(mascara)
    idx = np.random.RandomState(1).choice(len(ys), min(len(ys), 40000), replace=False)
    ys, xs = ys[idx], xs[idx]
    def base(y, x):
        y = y / h
        x = x / w
        return np.stack([np.ones_like(x), x, y, x * y, x * x, y * y], axis=-1)
    A = base(ys.astype(np.float32), xs.astype(np.float32))
    gy, gx = np.mgrid[0:h, 0:w]
    B = base(gy.ravel().astype(np.float32), gx.ravel().astype(np.float32))
    fondo = np.zeros_like(img)
    for c in range(3):
        coef, *_ = np.linalg.lstsq(A, img[ys, xs, c], rcond=None)
        fondo[..., c] = (B @ coef).reshape(h, w)
    return fondo


def quitar_fondo(nombre, lo=7.0, hi=34.0):
    img = cargar(nombre)
    m = region_fondo(img)
    fondo = plano_fondo(img, m)
    dist = np.abs(img - fondo).max(axis=2)
    alfa = np.clip((dist - lo) / (hi - lo), 0, 1)
    # lo que no esta conectado al fondo (huecos cerrados) se queda opaco si es distinto; el fondo conectado usa el alfa suave
    alfa = np.where(m | (dist > lo), alfa, 1.0)
    a3 = np.maximum(alfa, 1e-3)[..., None]
    color = np.clip(fondo + (img - fondo) / a3, 0, 255)
    color = np.where(alfa[..., None] > 0.98, img, color)
    rgba = np.dstack([color, alfa * 255]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def cajas_por_proyeccion(im, eje, esperado, hueco=12, umbral=40):
    a = np.asarray(im)[..., 3] > umbral
    proy = a.sum(axis=0 if eje == "x" else 1) >= 4
    tramos, ini, vacio = [], None, 0
    for i, v in enumerate(proy):
        if v:
            if ini is None:
                ini = i
            vacio = 0
            fin = i
        elif ini is not None:
            vacio += 1
            if vacio > hueco:
                tramos.append((ini, fin))
                ini = None
    if ini is not None:
        tramos.append((ini, fin))
    tramos = [t for t in tramos if t[1] - t[0] > 20]
    if len(tramos) != esperado:
        raise SystemExit("%s: se esperaban %d piezas y salieron %d: %s" % (nombre_actual, esperado, len(tramos), tramos))
    cajas = []
    for a0, a1 in tramos:
        sub = np.asarray(im)[..., 3][:, a0:a1 + 1] if eje == "x" else np.asarray(im)[..., 3][a0:a1 + 1, :]
        m = sub > umbral
        o = m.sum(axis=1 if eje == "x" else 0) >= 4
        idx = mayor_tramo(np.nonzero(o)[0])
        if eje == "x":
            cajas.append((a0, idx[0], a1 + 1, idx[-1] + 1))
        else:
            cajas.append((idx[0], a0, idx[-1] + 1, a1 + 1))
    return cajas


def mayor_tramo(idx, hueco=14):
    """De los indices ordenados, el tramo contiguo (con huecos de hasta `hueco`) mas largo: ignora motas sueltas."""
    mejor, ini = (0, 0), 0
    for i in range(1, len(idx) + 1):
        if i == len(idx) or idx[i] - idx[i - 1] > hueco:
            if idx[i - 1] - idx[ini] >= mejor[1] - mejor[0]:
                mejor = (idx[ini], idx[i - 1])
            ini = i
    return np.array([mejor[0], mejor[1]])


def recortar(im, caja, margen=6):
    x0, y0, x1, y1 = caja
    return im.crop((max(0, x0 - margen), max(0, y0 - margen), min(im.width, x1 + margen), min(im.height, y1 + margen)))


def guardar(im, grupo, nombre):
    d = os.path.join(SALIDA, grupo)
    os.makedirs(d, exist_ok=True)
    ruta = os.path.join(d, nombre + ".png")
    im.save(ruta, optimize=True)
    print(ruta, im.size)


def hoja(codigo, eje, nombres, grupo, margen=6, hueco=12):
    global nombre_actual
    nombre_actual = codigo
    im = quitar_fondo(codigo)
    cajas = cajas_por_proyeccion(im, eje, len(nombres), hueco=hueco)
    for caja, nombre in zip(cajas, nombres):
        guardar(recortar(im, caja, margen), grupo, nombre)


def armas():
    """Cada recuadro de UI06 trae el arma naranja dentro; se extrae solo el arma por color."""
    global nombre_actual
    nombre_actual = "UI06_IconosArmas"
    img = cargar("UI06_IconosArmas")
    base = Image.open(os.path.join(AQUI, "UI06_IconosArmas.png")).convert("RGB").convert("HSV")
    hsv = np.asarray(base).astype(np.float32)
    h, s, v = hsv[..., 0] * 360 / 255, hsv[..., 1] / 255, hsv[..., 2] / 255
    naranja = ((h > 8) & (h < 40)).astype(np.float32) * np.clip((s - 0.45) / 0.3, 0, 1) * np.clip((v - 0.55) / 0.25, 0, 1)
    paneles = [(26, 296, 284, 468), (294, 296, 552, 468), (560, 296, 818, 468), (826, 296, 1084, 468), (1092, 296, 1350, 468)]
    nombres = ["arma_punos", "arma_pistola", "arma_escopeta", "arma_fusil", "arma_sniper"]
    for (x0, y0, x1, y1), nombre in zip(paneles, nombres):
        w, hh = x1 - x0, y1 - y0
        ix0, ix1 = x0 + int(w * 0.10), x0 + int(w * 0.90)
        iy0, iy1 = y0 + int(hh * 0.12), y0 + int(hh * 0.85)
        a = naranja[iy0:iy1, ix0:ix1]
        sub = img[iy0:iy1, ix0:ix1]
        ys, xs = np.nonzero(a > 0.5)
        ancho_max = max(int(xs.max() - xs.min()), 1)
        rgba = np.dstack([sub, a * 255]).astype(np.uint8)
        im = Image.fromarray(rgba, "RGBA").crop((xs.min() - 4, ys.min() - 4, xs.max() + 5, ys.max() + 5))
        guardar(im, "Armas", nombre)


def minimapa():
    global nombre_actual
    nombre_actual = "UI11_MarcoMinimapa"
    im = quitar_fondo("UI11_MarcoMinimapa", lo=9, hi=40)
    a = np.asarray(im)
    ys, xs = np.nonzero(a[..., 3] > 40)
    im = im.crop((xs.min() - 2, ys.min() - 2, xs.max() + 3, ys.max() + 3))
    # el interior del anillo se vuelve transparente: relleno desde el centro por color parecido
    arr = np.asarray(im).copy()
    h, w, _ = arr.shape
    cy, cx = h // 2, w // 2
    vis = np.zeros((h, w), bool)
    ref = arr[cy, cx, :3].astype(np.int32)
    q = deque([(cy, cx)])
    vis[cy, cx] = True
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not vis[ny, nx] and np.abs(arr[ny, nx, :3].astype(np.int32) - ref).max() < 34:
                vis[ny, nx] = True
                q.append((ny, nx))
    # el area encontrada se agranda 2 px hacia dentro del anillo para quitar el borde suave
    f = vis.copy()
    for _ in range(2):
        f = f | np.roll(f, 1, 0) | np.roll(f, -1, 0) | np.roll(f, 1, 1) | np.roll(f, -1, 1)
    arr[f, 3] = 0
    out = Image.fromarray(arr, "RGBA").resize((512, 512), Image.LANCZOS)
    guardar(out, "Marcos", "marco_minimapa")


def panel_arma():
    global nombre_actual
    nombre_actual = "UI12_PanelArma"
    im = quitar_fondo("UI12_PanelArma", lo=9, hi=40)
    a = np.asarray(im)
    ys, xs = np.nonzero(a[..., 3] > 40)
    im = im.crop((xs.min() - 2, ys.min() - 2, xs.max() + 3, ys.max() + 3))
    # la primera casilla viene resaltada en naranja: se tapa con la forma de la segunda (el resalte se dibuja en juego)
    arr = np.asarray(im).copy()
    arr[408:488, 70:210] = arr[408:488, 202:342]
    guardar(Image.fromarray(arr, "RGBA"), "Marcos", "panel_arma")


def calor():
    """UI04 trae 5 segmentos y el calor del juego tiene 3 niveles: se deja la insignia y 3 segmentos (corte entre el 3 y el 4 + la tapa derecha)
    y se dibuja el relleno de cada nivel (paralelogramo naranja) con la misma inclinacion."""
    from PIL import ImageDraw
    src = Image.open(os.path.join(SALIDA, "Barras", "barra_calor.png")).convert("RGBA")
    izq = src.crop((0, 0, 820, src.height))
    tapa = src.crop((src.width - 60, 0, src.width, src.height))
    out = Image.new("RGBA", (izq.width + tapa.width, src.height), (0, 0, 0, 0))
    out.paste(izq, (0, 0))
    out.paste(tapa, (izq.width, 0))
    guardar(out, "Barras", "barra_calor_3")
    seg = Image.new("RGBA", (190, 124), (0, 0, 0, 0))
    d = ImageDraw.Draw(seg)
    for i in range(112):
        t = i / 111
        col = (255, int(240 - 40 * t), int(225 - 60 * t), 235)
        y = 6 + i
        xi = 57 - 57 * (y / 124)
        d.line([(xi + 14, y), (xi + 133 - 6, y)], fill=col)
    guardar(seg, "Barras", "calor_relleno")


def main():
    global nombre_actual
    nombre_actual = ""
    if len(sys.argv) > 1:
        {"clinica": lambda: hoja("UI08_IconosMapa", "x", ["mapa_casa", "mapa_mercado_negro", "mapa_clinica", "mapa_taller", "mapa_bar", "mapa_casino", "mapa_mision"], "Mapa", hueco=8),
         "panel": panel_arma, "calor": calor}[sys.argv[1]]()
        return
    # 9-slice
    nombre_actual = "UI01"
    m = quitar_fondo("UI01_MarcoPanel", lo=9, hi=40)
    a = np.asarray(m)
    ys, xs = np.nonzero(a[..., 3] > 40)
    guardar(m.crop((xs.min() - 2, ys.min() - 2, xs.max() + 3, ys.max() + 3)), "Marcos", "marco_panel")
    hoja("UI02_Botones", "y", ["boton_normal", "boton_resaltado", "boton_pulsado"], "Marcos", hueco=6)
    nombre_actual = "UI03"
    m = quitar_fondo("UI03_BarraVida", lo=9, hi=40)
    a = np.asarray(m)
    ys, xs = np.nonzero(a[..., 3] > 40)
    guardar(m.crop((xs.min() - 2, ys.min() - 2, xs.max() + 3, ys.max() + 3)), "Barras", "barra_vida")
    nombre_actual = "UI04"
    m = quitar_fondo("UI04_BarraCalor", lo=9, hi=40)
    a = np.asarray(m)
    ys, xs = np.nonzero(a[..., 3] > 40)
    guardar(m.crop((xs.min() - 2, ys.min() - 2, xs.max() + 3, ys.max() + 3)), "Barras", "barra_calor")
    nombre_actual = "UI05"
    m = quitar_fondo("UI05_SimboloBiits", lo=9, hi=40)
    a = np.asarray(m)
    ys, xs = np.nonzero(a[..., 3] > 40)
    guardar(m.crop((xs.min() - 6, ys.min() - 6, xs.max() + 7, ys.max() + 7)), "Moneda", "simbolo_biits")
    armas()
    hoja("UI07_IconosEstado", "x", ["estado_respeto", "estado_calor", "estado_aliados", "estado_implantes", "estado_objetivo", "estado_telefono"], "Estado", hueco=14)
    hoja("UI08_IconosMapa", "x", ["mapa_casa", "mapa_mercado_negro", "mapa_clinica", "mapa_taller", "mapa_bar", "mapa_casino", "mapa_mision"], "Mapa", hueco=8)
    hoja("UI09_InsigniasPandillas", "x", ["insignia_skulls", "insignia_snakes", "insignia_jugador"], "Insignias", hueco=14)
    hoja("UI10_Reticulas", "x", ["reticula_mira", "reticula_sniper", "reticula_impacto"], "Reticulas", hueco=30)
    minimapa()
    panel_arma()
    calor()
    # logo y fondo del menu (para T-522)
    nombre_actual = "UI13"
    m = quitar_fondo("UI13_Logo", lo=14, hi=50)
    a = np.asarray(m)
    ys, xs = np.nonzero(a[..., 3] > 40)
    guardar(m.crop((xs.min() - 8, ys.min() - 8, xs.max() + 9, ys.max() + 9)), "Marca", "logo_cyberfix")
    guardar(Image.open(os.path.join(AQUI, "UI14_FondoMenu.png")).convert("RGB"), "Marca", "fondo_menu")


if __name__ == "__main__":
    main()
