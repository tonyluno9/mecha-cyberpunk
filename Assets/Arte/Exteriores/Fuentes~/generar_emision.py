"""T-541/T-542: mapa de emisión a partir del color de un modelo de Tripo: solo lo saturado y claro (neón, letras, ventanas encendidas,
foquitos) brilla; lo demás queda negro. Uso: python generar_emision.py <ruta_basecolor.jpg> <salida.png> [umbral_saturacion] [umbral_valor]"""
import sys
import numpy as np
from PIL import Image

entrada, salida = sys.argv[1], sys.argv[2]
umbral_s = float(sys.argv[3]) if len(sys.argv) > 3 else 0.45
umbral_v = float(sys.argv[4]) if len(sys.argv) > 4 else 0.5
img = np.asarray(Image.open(entrada).convert("RGB")).astype(np.float32) / 255.0
v = img.max(axis=2)
mn = img.min(axis=2)
s = np.where(v > 1e-5, (v - mn) / np.maximum(v, 1e-5), 0)
mascara = ((s > umbral_s) & (v > umbral_v)) | (v > 0.9)
suave = np.clip((s - umbral_s) * 4 + (v - umbral_v) * 3, 0, 1)
fuerza = np.where(mascara, np.maximum(suave, 0.6), 0.0)[..., None]
Image.fromarray((img * fuerza * 255).astype(np.uint8), "RGB").save(salida)
print("listo", salida, "brilla", round(float(mascara.mean()) * 100, 1), "%")
