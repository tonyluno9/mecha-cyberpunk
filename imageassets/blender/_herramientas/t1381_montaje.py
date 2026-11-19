"""T-1381: junta los renders de prueba de un personaje en 1 hoja (Pillow).
python t1381_montaje.py <carpeta_render> <Nombre> <salida.png>
"""
import sys, os
from PIL import Image, ImageDraw

carpeta, nombre, salida = sys.argv[1:4]
celdas = [
    ("reposo_frente", "T-pose + esqueleto"), ("brazos_abajo_frente", "brazos abajo"),
    ("paso_lado", "paso largo (lado)"), ("sentadilla_lado", "sentadilla (lado)"),
    ("caminar06_lado", "caminar f6 (lado)"), ("caminar14_frente", "caminar f14 (frente)"),
    ("caminar24_lado", "caminar f24 (lado)"), ("paso_frente", "paso (frente)"),
]
W = 400
cols = 4
rows = (len(celdas) + cols - 1) // cols
hoja = Image.new("RGB", (W * cols, W * rows), (140, 148, 158))
for i, (k, t) in enumerate(celdas):
    p = os.path.join(carpeta, "%s_%s.png" % (nombre, k))
    if not os.path.exists(p):
        continue
    im = Image.open(p).convert("RGB").resize((W, W), Image.LANCZOS)
    d = ImageDraw.Draw(im)
    d.text((6, 4), t, fill=(255, 255, 0))
    hoja.paste(im, ((i % cols) * W, (i // cols) * W))
os.makedirs(os.path.dirname(salida), exist_ok=True)
hoja.save(salida)
print("hoja", salida)
