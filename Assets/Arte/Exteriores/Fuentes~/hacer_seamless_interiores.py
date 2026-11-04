"""T-550: vuelve sin costura las texturas de pared X01-X05 (imageassets/interiores) y las deja en Assets/Arte/Interiores/Texturas/.
Mezcla la imagen con ella misma desplazada media imagen, con una ventana que vale 0 en los bordes (ahí manda la copia desplazada, que es continua)."""
import numpy as np
from PIL import Image

for i in range(1, 6):
    a = np.asarray(Image.open("imageassets/interiores/X0%d.png" % i).convert("RGB")).astype(np.float32)
    h, w, _ = a.shape
    wx = np.sin(np.linspace(0, np.pi, w)) ** 2
    wy = np.sin(np.linspace(0, np.pi, h)) ** 2
    m = (wy[:, None] * wx[None, :])[:, :, None]
    r = np.roll(np.roll(a, h // 2, 0), w // 2, 1)
    o = a * m + r * (1 - m)
    Image.fromarray(np.clip(o, 0, 255).astype(np.uint8)).save("Assets/Arte/Interiores/Texturas/X0%d.png" % i)
