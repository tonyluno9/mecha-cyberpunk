"""T-546: texturas generadas para los interiores (paso 4 del plan de la noche 3-nov).
Salida en Assets/Arte/Interiores/Texturas/:
- T546_Poster_1..3.png: pósters de cuarto de joven (formas de colores, estilo cartel de concierto y de luchador).
- T546_AlfombraCasino.png: alfombra estampada roja con grecas doradas (tileable).
- T546_PapelPicado.png: tira de papel picado de colores (alfa recortado).
- T546_Pizarron.png: pizarrón verde con dibujos de dron y engranes en gis.
Uso: python "Assets/Arte/Exteriores/Fuentes~/generar_interiores.py"
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter

random.seed(546)
SALIDA = os.path.join(os.path.dirname(__file__), "..", "..", "Interiores", "Texturas")
os.makedirs(SALIDA, exist_ok=True)


def poster(n, fondo, colores):
    w, h = 512, 720
    im = Image.new("RGB", (w, h), fondo)
    d = ImageDraw.Draw(im)
    for _ in range(9):
        c = random.choice(colores)
        x, y = random.randint(-60, w), random.randint(-60, h)
        r = random.randint(60, 220)
        if random.random() < 0.5:
            d.ellipse([x, y, x + r, y + r], fill=c)
        else:
            d.polygon([(x, y), (x + r, y + random.randint(-80, 80)), (x + random.randint(-80, 80), y + r)], fill=c)
    # Franja de título y letras de bloque (sin texto legible: solo formas).
    d.rectangle([30, h - 170, w - 30, h - 60], fill=(15, 12, 20))
    for i in range(7):
        x = 50 + i * 60
        d.rectangle([x, h - 150, x + 40, h - 80], fill=random.choice(colores))
    im = im.filter(ImageFilter.GaussianBlur(0.6))
    # Desgaste: esquinas más claras y manchas.
    capa = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    dc = ImageDraw.Draw(capa)
    for _ in range(40):
        x, y = random.randint(0, w), random.randint(0, h)
        dc.ellipse([x, y, x + 12, y + 12], fill=(240, 230, 210, 40))
    im = Image.alpha_composite(im.convert("RGBA"), capa).convert("RGB")
    im.save(os.path.join(SALIDA, f"T546_Poster_{n}.png"))


poster(1, (40, 20, 60), [(255, 60, 170), (40, 230, 220), (255, 200, 60)])
poster(2, (230, 120, 40), [(30, 30, 40), (250, 240, 220), (200, 30, 60)])
poster(3, (20, 70, 90), [(255, 90, 60), (250, 220, 90), (120, 255, 200)])

# Alfombra: rojo vino con grecas doradas y rombos (tileable 512).
N = 512
alf = Image.new("RGB", (N, N), (175, 34, 44))  # claro: el posproceso de noche tiñe de verde los rojos oscuros
d = ImageDraw.Draw(alf)
oro = (240, 185, 80)
oscuro = (105, 20, 28)
for y0 in range(0, N, 128):
    d.rectangle([0, y0 + 56, N, y0 + 72], fill=oscuro)
    for x0 in range(0, N, 32):
        # Greca escalonada.
        d.rectangle([x0, y0 + 58, x0 + 16, y0 + 62], fill=oro)
        d.rectangle([x0 + 12, y0 + 58, x0 + 16, y0 + 70], fill=oro)
        d.rectangle([x0 + 12, y0 + 66, x0 + 28, y0 + 70], fill=oro)
    for x0 in range(0, N, 128):
        cx, cy = x0 + 64, y0 + 22
        d.polygon([(cx, cy - 18), (cx + 30, cy), (cx, cy + 18), (cx - 30, cy)], outline=oro, fill=(210, 52, 60))
        d.polygon([(cx, cy - 8), (cx + 14, cy), (cx, cy + 8), (cx - 14, cy)], fill=oro)
        cy2 = y0 + 100
        d.polygon([(cx - 64, cy2 - 14), (cx - 40, cy2), (cx - 64, cy2 + 14), (cx - 88, cy2)], outline=(230, 90, 160))
alf = alf.filter(ImageFilter.GaussianBlur(0.7))
alf.save(os.path.join(SALIDA, "T546_AlfombraCasino.png"))

# Papel picado: 6 banderitas por tira, con hoyitos (transparentes) y cordón arriba.
W, H = 1024, 256
pp = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(pp)
colores = [(255, 60, 170), (40, 220, 230), (255, 170, 40), (140, 255, 90), (180, 90, 255), (255, 80, 60)]
d.line([(0, 8), (W, 8)], fill=(40, 40, 40, 255), width=4)
ancho = W // 6
for i in range(6):
    x0 = i * ancho + 8
    c = colores[i % len(colores)] + (235,)
    d.rectangle([x0, 10, x0 + ancho - 16, H - 40], fill=c)
    # Picos abajo.
    for k in range(6):
        px = x0 + k * (ancho - 16) / 6
        d.polygon([(px, H - 40), (px + (ancho - 16) / 12, H - 12), (px + (ancho - 16) / 6, H - 40)], fill=c)
    # Calado: flores y rombos.
    cx, cy = x0 + (ancho - 16) / 2, 110
    for a in range(8):
        ang = a * math.pi / 4
        d.ellipse([cx + math.cos(ang) * 38 - 9, cy + math.sin(ang) * 38 - 9, cx + math.cos(ang) * 38 + 9, cy + math.sin(ang) * 38 + 9], fill=(0, 0, 0, 0))
    d.ellipse([cx - 16, cy - 16, cx + 16, cy + 16], fill=(0, 0, 0, 0))
    for k in range(4):
        rx = x0 + 18 + k * (ancho - 50) / 3
        d.polygon([(rx, 185), (rx + 8, 177), (rx + 16, 185), (rx + 8, 193)], fill=(0, 0, 0, 0))
pp.save(os.path.join(SALIDA, "T546_PapelPicado.png"))

# Pizarrón: verde oscuro con dibujos de gis (dron, engranes, flechas, renglones de fórmulas).
W, H = 1024, 640
pz = Image.new("RGB", (W, H), (28, 52, 40))
d = ImageDraw.Draw(pz)
gis = (225, 230, 220)
d.rectangle([0, 0, W - 1, H - 1], outline=(110, 80, 50), width=18)
# Dron.
cx, cy = 300, 260
d.rectangle([cx - 70, cy - 25, cx + 70, cy + 25], outline=gis, width=4)
for sx in (-1, 1):
    d.line([(cx + sx * 70, cy), (cx + sx * 150, cy - 60)], fill=gis, width=4)
    d.ellipse([cx + sx * 150 - 55, cy - 80, cx + sx * 150 + 55, cy - 40], outline=gis, width=3)
d.line([(cx - 120, cy + 90), (cx + 120, cy + 90)], fill=gis, width=2)
# Engranes.
for (gx, gy, r) in [(720, 200, 70), (830, 300, 45)]:
    d.ellipse([gx - r, gy - r, gx + r, gy + r], outline=gis, width=4)
    d.ellipse([gx - r / 3, gy - r / 3, gx + r / 3, gy + r / 3], outline=gis, width=3)
    for a in range(10):
        ang = a * math.pi / 5
        d.line([(gx + math.cos(ang) * r, gy + math.sin(ang) * r), (gx + math.cos(ang) * (r + 16), gy + math.sin(ang) * (r + 16))], fill=gis, width=6)
# Renglones de "fórmulas" (rayas cortas, sin letras).
for row in range(5):
    y = 430 + row * 36
    x = 80
    while x < W - 120:
        largo = random.randint(18, 70)
        d.line([(x, y), (x + largo, y + random.randint(-3, 3))], fill=gis, width=3)
        x += largo + random.randint(12, 30)
d.line([(470, 260), (620, 210)], fill=(255, 210, 90), width=4)
d.polygon([(620, 210), (600, 205), (606, 224)], fill=(255, 210, 90))
pz = pz.filter(ImageFilter.GaussianBlur(0.8))
pz.save(os.path.join(SALIDA, "T546_Pizarron.png"))
print("listo", os.path.abspath(SALIDA))
