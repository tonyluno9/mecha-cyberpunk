"""T-535: textura de asfalto mojado para la calle (tileable, 1024 px = 8 m).

Genera en Assets/Arte/Exteriores/Texturas/:
- T535_Asfalto_Color.png: color con grano, piedritas, grietas, parches de bacheo y manchas de aceite; alfa = qué tan mojado/liso (smoothness).
- T535_Asfalto_Normal.png: mapa de normales a partir de la altura (grietas hundidas, parches un poco arriba).
Uso: python "Assets/Arte/Exteriores/Fuentes~/generar_asfalto.py" (carpeta con ~: Unity no la importa)
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

N = 1024
rng = np.random.default_rng(535)
SALIDA = os.path.join(os.path.dirname(__file__), "..", "Texturas")


def ruido(escala, octavas=4):
    """Ruido de valor tileable (suma de octavas)."""
    total = np.zeros((N, N))
    amp = 1.0
    norma = 0.0
    for o in range(octavas):
        celdas = max(2, int(N / escala * (2 ** o)))
        base = rng.random((celdas, celdas))
        rep = np.kron(base, np.ones((N // celdas + 1, N // celdas + 1)))[:N, :N]
        rep = ndimage.gaussian_filter(rep, sigma=N / celdas / 2.0, mode="wrap")
        total += rep * amp
        norma += amp
        amp *= 0.5
    total /= norma
    return (total - total.min()) / (total.max() - total.min() + 1e-9)


def grietas(cantidad):
    """Grietas: caminatas aleatorias que se ramifican (tileables con módulo)."""
    m = np.zeros((N, N))
    for _ in range(cantidad):
        x, y = rng.integers(0, N, 2).astype(float)
        ang = rng.random() * np.pi * 2
        largo = rng.integers(120, 420)
        ancho = rng.choice([1, 1, 2])
        for _ in range(largo):
            ang += rng.normal(0, 0.13)
            x = (x + np.cos(ang) * 1.6) % N
            y = (y + np.sin(ang) * 1.6) % N
            xi, yi = int(x), int(y)
            m[yi:yi + ancho, xi:xi + ancho] = 1.0
            if rng.random() < 0.01:
                ang += rng.choice([-1, 1]) * np.pi / 3
    return ndimage.gaussian_filter(m, 0.6, mode="wrap")


def parches(cantidad):
    """Parches de bacheo: rectángulos de asfalto más nuevo (más oscuro) con borde."""
    m = np.zeros((N, N))
    for _ in range(cantidad):
        w, h = rng.integers(60, 220, 2)
        x, y = rng.integers(0, N, 2)
        ys = (np.arange(y, y + h) % N)[:, None]
        xs = (np.arange(x, x + w) % N)[None, :]
        m[ys, xs] = 1.0
    return ndimage.gaussian_filter(m, 1.2, mode="wrap")


def manchas(cantidad, radio):
    m = np.zeros((N, N))
    for _ in range(cantidad):
        x, y = rng.integers(0, N, 2)
        m[y, x] = 1.0
    m = ndimage.gaussian_filter(m, radio, mode="wrap")
    return m / (m.max() + 1e-9)


grano = ruido(8, 3)
mancha_grande = ruido(300, 4)
piedras = (rng.random((N, N)) > 0.985).astype(float)
piedras = ndimage.gaussian_filter(piedras, 0.7, mode="wrap") * 3
g = grietas(14)
p = parches(5)
aceite = np.clip(manchas(10, 26) * 1.8, 0, 1)
mojado = np.clip((ruido(220, 3) - 0.45) * 2.4, 0, 1)

# Color: asfalto gris azulado oscuro; los parches más oscuros; grietas casi negras; piedritas claras.
base = 0.17 + 0.07 * grano + 0.05 * (mancha_grande - 0.5)
base += 0.08 * np.clip(piedras, 0, 1)
base -= 0.06 * p
base -= 0.10 * np.clip(g * 3, 0, 1)
base -= 0.06 * aceite
base -= 0.03 * mojado  # lo mojado se ve más oscuro
base = np.clip(base, 0.02, 1)
r = base * 0.95
gch = base * 0.98
b = base * 1.06 + 0.01 * aceite  # el aceite deja un tinte frío
liso = np.clip(0.22 + 0.55 * mojado + 0.25 * aceite - 0.15 * g, 0, 1)
color = np.dstack([r, gch, b, liso])
Image.fromarray((np.clip(color, 0, 1) * 255).astype(np.uint8), "RGBA").save(os.path.join(SALIDA, "T535_Asfalto_Color.png"))

# Normal: altura = grano fino + parches arriba - grietas abajo.
altura = 0.35 * grano + 0.25 * p - 0.9 * np.clip(g * 2, 0, 1) + 0.2 * piedras
dx = (np.roll(altura, -1, axis=1) - np.roll(altura, 1, axis=1)) * 4.0
dy = (np.roll(altura, -1, axis=0) - np.roll(altura, 1, axis=0)) * 4.0
nz = np.ones_like(dx)
largo = np.sqrt(dx * dx + dy * dy + nz * nz)
normal = np.dstack([-dx / largo, dy / largo, nz / largo]) * 0.5 + 0.5
Image.fromarray((normal * 255).astype(np.uint8), "RGB").save(os.path.join(SALIDA, "T535_Asfalto_Normal.png"))
print("listo", SALIDA)
