"""T-1014: genera H05-H09 (subfusil, revólver, lanzagranadas, granada, rifle de plasma) con el estilo de H01-H04: líneas turquesa con brillo sobre negro, 1376x768.
Origen: el arte conceptual imageassets/armas/W05-W08 y, para la granada, el render del modelo W09 (imageassets/ui/hologramas/_origen_W09_Granada.png, ver imageassets/blender/granada).
Uso: python generar_t1014.py   (desde la raíz del repo)"""
import numpy as np
from PIL import Image
from scipy import ndimage

ANCHO, ALTO = 1376, 768
MARGEN = 0.06  # fracción libre alrededor del arma
ORIGENES = {
    "H05_Subfusil": "imageassets/armas/W05_Subfusil.png",
    "H06_Revolver": "imageassets/armas/W06_Revolver.png",
    "H07_Lanzagranadas": "imageassets/armas/W08_Lanzagranadas.png",
    "H08_Granada": "imageassets/ui/hologramas/_origen_W09_Granada.png",
    "H09_RiflePlasma": "imageassets/armas/W07_RiflePlasma.png",
}
TURQUESA = np.array([0.10, 0.93, 0.90])
BLANCO = np.array([0.72, 1.0, 1.0])
BLANCO_ROTO = 238


def cargar(ruta):
    im = Image.open(ruta)
    if im.mode == "RGBA":
        fondo = Image.new("RGBA", im.size, (255, 255, 255, 255))
        fondo.alpha_composite(im)
        im = fondo
    return np.asarray(im.convert("RGB")).astype(float)


def lineas_de(ruta):
    """La intensidad (0 a 1) de las líneas del dibujo, recortada al arma."""
    rgb = cargar(ruta)
    h, w, _ = rgb.shape
    mascara = rgb.min(axis=2) < BLANCO_ROTO
    mascara = ndimage.binary_opening(mascara, iterations=2)
    mascara = ndimage.binary_closing(mascara, iterations=6)
    ys, xs = np.where(mascara)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    rgb = rgb[y0:y1, x0:x1]
    mascara = mascara[y0:y1, x0:x1]
    gris = rgb @ np.array([0.299, 0.587, 0.114]) / 255.0
    gris = ndimage.gaussian_filter(gris, 0.8)
    gx = ndimage.sobel(gris, axis=1)
    gy = ndimage.sobel(gris, axis=0)
    borde = np.hypot(gx, gy)
    borde = np.clip(borde / np.percentile(borde[mascara], 99), 0, 1) ** 1.6
    contorno = mascara ^ ndimage.binary_erosion(mascara, iterations=2)
    relleno = (1.0 - gris) * mascara * 0.10  # lo oscuro del dibujo brilla un poco (cuerpo translúcido)
    return np.clip(borde * 1.2 + relleno + contorno * 0.75, 0, 1)


def generar(ruta, salida):
    intensidad = lineas_de(ruta)
    # Encaja en el lienzo, centrado, conservando la proporción.
    escala = min(ANCHO * (1 - 2 * MARGEN) / intensidad.shape[1], ALTO * (1 - 2 * MARGEN) / intensidad.shape[0])
    nuevo = (max(1, int(intensidad.shape[1] * escala)), max(1, int(intensidad.shape[0] * escala)))
    imagen = Image.fromarray((intensidad * 255).astype(np.uint8)).resize(nuevo, Image.LANCZOS)
    lienzo = np.zeros((ALTO, ANCHO))
    ox, oy = (ANCHO - nuevo[0]) // 2, (ALTO - nuevo[1]) // 2
    lienzo[oy:oy + nuevo[1], ox:ox + nuevo[0]] = np.asarray(imagen) / 255.0
    brillo = ndimage.gaussian_filter(lienzo, 6) * 0.9 + ndimage.gaussian_filter(lienzo, 2.0) * 0.35
    total = np.clip((lienzo * 0.9 + brillo) ** 0.7 * 1.25, 0, 1)
    lineas = 0.86 + 0.14 * np.sin(np.arange(ALTO) * np.pi / 1.4)[:, None]  # líneas de barrido
    total = total * lineas
    color = TURQUESA[None, None, :] * total[..., None] + BLANCO[None, None, :] * (np.clip(total - 0.72, 0, 1) * 1.8)[..., None]
    Image.fromarray((np.clip(color, 0, 1) * 255).astype(np.uint8)).save(salida)


CARPETA_ICONOS = "Assets/_Proyecto/UI/Resources/Menus/"
ICONOS = {"Cat_Subfusil": "H05_Subfusil", "Cat_Revolver": "H06_Revolver", "Cat_Lanzagranadas": "H07_Lanzagranadas", "Cat_Granada": "H08_Granada", "Cat_RiflePlasma": "H09_RiflePlasma"}


def icono(ruta, salida, lado=256, margen=0.08):
    """Icono de categoría del inventario (como Cat_Fusil): líneas blancas sobre transparente, 256x256."""
    intensidad = lineas_de(ruta) ** 0.6
    escala = lado * (1 - 2 * margen) / max(intensidad.shape)
    nuevo = (max(1, int(intensidad.shape[1] * escala)), max(1, int(intensidad.shape[0] * escala)))
    imagen = np.asarray(Image.fromarray((intensidad * 255).astype(np.uint8)).resize(nuevo, Image.LANCZOS)) / 255.0
    alfa = np.zeros((lado, lado))
    ox, oy = (lado - nuevo[0]) // 2, (lado - nuevo[1]) // 2
    alfa[oy:oy + nuevo[1], ox:ox + nuevo[0]] = imagen
    rgba = np.zeros((lado, lado, 4), dtype=np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = (np.clip(alfa * 1.4, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(salida)


if __name__ == "__main__":
    for nombre, holo in ICONOS.items():
        icono(ORIGENES[holo], CARPETA_ICONOS + nombre + ".png")
        print("icono", nombre)
    for nombre, origen in ORIGENES.items():
        generar(origen, "imageassets/ui/hologramas/" + nombre + ".png")
        print("listo", nombre)
