"""Letreros de neon en espanol para el barrio (T-082). Misma estetica que kit_urbano.py (marco metalico, tubos de neon,
texto en relieve, soportes). Usa sus funciones de apoyo (se leen de kit_urbano.py sin correr su main).
Uso: blender -b -P letreros_neon.py -- [engine EEVEE|WORKBENCH] [solo1,solo2]
Metros, origen en la base, frente hacia -Y (Blender). Salida: .glb + .png en la carpeta del script.
Planos: la pieza se cuelga de una fachada; sus soportes llegan a 0.415 m detras del origen.
Banderas (verticales, dos caras): el origen esta en la placa de pared, el cuerpo sale hacia +X."""
import os, sys, math

AQUI = os.path.dirname(os.path.abspath(__file__))
KIT = os.path.join(AQUI, "..", "kit-urbano", "kit_urbano.py")
src = open(KIT, encoding="utf-8").read()
cola = src.rindex("\nmain()")
ns = {"__file__": os.path.join(AQUI, "letreros_neon.py"), "__name__": "kit_urbano"}
exec(compile(src[:cola], KIT, "exec"), ns)
globals().update({k: v for k, v in ns.items() if not k.startswith("__")})

import bpy
from mathutils import Matrix

BRAZO = 0.4  # fondo de los soportes de un letrero plano (plano de apoyo en y = 0.415)


def mats_extra(M):
    M["roj"] = mk_mat("Neon_Rojo", (0.06, 0.0, 0.0), 0.3, 0, (1.0, 0.06, 0.04), 6)
    M["ama"] = mk_mat("Neon_Amarillo", (0.06, 0.05, 0.0), 0.3, 0, (1.0, 0.8, 0.08), 6)
    return M


def ajustar(o, ancho, alto):
    """Escala el texto (en su plano local XY) para que quepa en ancho x alto y lo centra."""
    xs = [v.co.x for v in o.data.vertices]
    ys = [v.co.y for v in o.data.vertices]
    wx, wy = max(xs) - min(xs), max(ys) - min(ys)
    f = min(ancho / wx, alto / wy)
    cx, cy = (max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2
    o.data.transform(Matrix.Translation((-cx, -cy, 0)))
    o.data.transform(Matrix.Diagonal((f, f, f, 1.0)))


def texto_fit(P, s, ancho, alto, loc, mat, lado=1, ext=0.035, ls=1.0):
    rot = (math.radians(90), 0, 0 if lado == 1 else math.pi)
    o = text(P, s, 1.0, loc, mat, rot=rot, ext=ext, ls=ls)
    ajustar(o, ancho, alto)
    return o


def marco(P, M, w, h, frame, y, z0=0.0, m=0.07, r=0.014):
    tube(P, [(-w / 2 + m, y, z0 + m), (w / 2 - m, y, z0 + m), (w / 2 - m, y, z0 + h - m), (-w / 2 + m, y, z0 + h - m)], r, frame, 1, True)


def cuerpo(P, M, w, h, z0=0.0, lado=1):
    box(P, (w, 0.14, h), (0, 0, z0 + h / 2), M["met"], 0.025)
    box(P, (w - 0.1, 0.02, h - 0.1), (0, -0.075 * lado, z0 + h / 2), M["conD"], 0.01)


def soportes_planos(P, M, w, h, z0=0.0):
    """Dos brazos con placa que se atornillan a la pared (atras, +Y)."""
    for sx in (-1, 1):
        x = sx * max(w / 2 - 0.45, w * 0.28)
        box(P, (0.1, BRAZO - 0.05, 0.08), (x, 0.07 + (BRAZO - 0.05) / 2, z0 + h / 2), M["met"], 0.01)
        box(P, (0.2, 0.03, 0.3), (x, BRAZO, z0 + h / 2), M["met"], 0.01)
        rivets(P, [(x + dx, BRAZO - 0.02, z0 + h / 2 + dz) for dx in (-0.07, 0.07) for dz in (-0.1, 0.1)], M["met"], 0.012, (math.radians(90), 0, 0))
    cable(P, (w * 0.3, 0.2, z0 + 0.1), (w * 0.35, 0.3, z0 - 0.0), 0.12, M["gro"], 0.012, 5)


def estrellas(P, M, pts, mat, lado=1):
    for x, z in pts:
        box(P, (0.06, 0.02, 0.06), (x, -0.115 * lado, z), mat, rot=(0, math.radians(45), 0))


# ================================================================ planos
def plano(M, nombre, w, h, texto, marco_m, texto_m, adorno=None, ls=1.0, z0=0.0, patas=False):
    P = Prop(nombre)
    cuerpo(P, M, w, h, z0)
    marco(P, M, w, h, marco_m, -0.1, z0)
    texto_fit(P, texto, w - 1.15, h - 0.4, (0, -0.12, z0 + h / 2), texto_m, ls=ls)
    if adorno:
        adorno(P, M, w, h, z0)
    if patas:
        # azotea: dos patas con cruceta que se apoyan en el techo
        for sx in (-1, 1):
            x = sx * (w / 2 - 0.4)
            box(P, (0.1, 0.12, z0 + 0.05), (x, 0.0, (z0 + 0.05) / 2), M["met"], 0.01)
            box(P, (0.34, 0.34, 0.05), (x, 0.0, 0.025), M["met"], 0.01)
        tube(P, [(-(w / 2 - 0.4), 0.09, 0.05), (w / 2 - 0.4, 0.09, z0)], 0.025, M["met"], 0)
        tube(P, [(w / 2 - 0.4, 0.09, 0.05), (-(w / 2 - 0.4), 0.09, z0)], 0.025, M["met"], 0)
    else:
        soportes_planos(P, M, w, h, z0)
    return [finish(P)], None


def ad_birria(P, M, w, h, z0):
    for sx in (-1, 1):
        tube(P, [(sx * (w / 2 - 0.3), -0.11, z0 + h * 0.3), (sx * (w / 2 - 0.3), -0.11, z0 + h * 0.7)], 0.02, M["tur"], 0)
        sph(P, 0.05, (sx * (w / 2 - 0.3), -0.115, z0 + h * 0.78), M["mag"], 5, 3)
    estrellas(P, M, [(-w / 2 + 0.3, z0 + 0.25), (w / 2 - 0.3, z0 + 0.25)], M["cian"])


def ad_refacciones(P, M, w, h, z0):
    for sx in (-1, 1):
        a = [(sx * (w / 2 - 0.3) + math.cos(i * math.pi / 3) * 0.14, -0.11, z0 + h / 2 + math.sin(i * math.pi / 3) * 0.14) for i in range(6)]
        tube(P, a, 0.014, M["nar"], 0, True)
        sph(P, 0.035, (sx * (w / 2 - 0.3), -0.115, z0 + h / 2), M["cian"], 5, 3)


def ad_lavanderia(P, M, w, h, z0):
    for x, z, r in ((-w / 2 + 0.3, 0.3, 0.07), (-w / 2 + 0.45, 0.55, 0.045), (w / 2 - 0.35, 0.3, 0.06), (w / 2 - 0.2, 0.62, 0.04)):
        a = [(x + math.cos(t * math.pi / 3) * r, -0.11, z0 + z + math.sin(t * math.pi / 3) * r) for t in range(6)]
        tube(P, a, 0.01, M["tur"], 0, True)


def ad_peleas(P, M, w, h, z0):
    for sx in (-1, 1):
        x = sx * (w / 2 - 0.35)
        tube(P, [(x - 0.1, -0.11, z0 + h - 0.35), (x + 0.05, -0.11, z0 + h / 2 + 0.05), (x - 0.08, -0.11, z0 + h / 2 - 0.02), (x + 0.1, -0.11, z0 + 0.35)], 0.024, M["mag"], 0)
    estrellas(P, M, [(-w / 2 + 0.7, z0 + 0.22), (w / 2 - 0.7, z0 + 0.22), (0, z0 + h - 0.2)], M["cian"])


def ad_casino(P, M, w, h, z0):
    for sx in (-1, 1):
        x = sx * (w / 2 - 0.35)
        a = [(x + math.cos(t * math.pi / 4) * 0.17, -0.11, z0 + h / 2 + math.sin(t * math.pi / 4) * 0.17) for t in range(8)]
        tube(P, a, 0.016, M["mag"], 0, True)
        box(P, (0.1, 0.02, 0.1), (x, -0.115, z0 + h / 2), M["ama"], rot=(0, math.radians(45), 0))
    estrellas(P, M, [(-0.7, z0 + 0.2), (0.7, z0 + 0.2), (0, z0 + h - 0.17)], M["tur"])


def ad_hotel(P, M, w, h, z0):
    a = [(-w / 2 + 0.35 + math.cos(t) * 0.16, -0.11, z0 + h / 2 + math.sin(t) * 0.16) for t in [math.pi * 0.35 + i * math.pi * 1.3 / 8 for i in range(9)]]
    tube(P, a, 0.016, M["tur"], 0)
    estrellas(P, M, [(w / 2 - 0.35, z0 + h / 2 + 0.1), (w / 2 - 0.5, z0 + h / 2 - 0.18)], M["ama"])


# ================================================================ banderas (dos caras)
BR = 0.15  # largo del brazo de una bandera


def bandera(M, nombre, w, h, texto, marco_m, texto_m, adorno_arriba):
    P = Prop(nombre)
    cx = BR + w / 2  # centro del cuerpo en X
    box(P, (w, 0.14, h), (cx, 0, h / 2), M["met"], 0.025)
    for lado in (1, -1):
        y = -0.1 * lado
        box(P, (w - 0.1, 0.02, h - 0.1), (cx, -0.075 * lado, h / 2), M["conD"], 0.01)
        tube(P, [(cx - w / 2 + 0.07, y, 0.07), (cx + w / 2 - 0.07, y, 0.07), (cx + w / 2 - 0.07, y, h - 0.07), (cx - w / 2 + 0.07, y, h - 0.07)], 0.014, marco_m, 1, True)
        texto_fit(P, texto, w - 0.18, h - 1.0, (cx, -0.12 * lado, h * 0.45), texto_m, lado=lado, ext=0.03, ls=0.82)
        adorno_arriba(P, M, cx, h, lado)
        for sz in (0.05, h - 0.05):
            rivets(P, [(cx + sx * (w / 2 - 0.04), -0.075 * lado, sz) for sx in (-1, 1)], M["met"], 0.013, (math.radians(90 * lado), 0, 0))
    # brazos y placa de pared (en x = 0)
    for z in (0.3 * h, 0.82 * h):
        box(P, (BR + 0.05, 0.1, 0.08), (BR / 2 + 0.025, 0, z), M["met"], 0.01)
    box(P, (0.04, 0.2, 0.9), (0.02, 0, 0.3 * h + 0.2), M["met"], 0.01)
    box(P, (0.04, 0.2, 0.5), (0.02, 0, 0.82 * h), M["met"], 0.01)
    cable(P, (BR + 0.1, 0.1, 0.2), (BR + 0.2, 0.2, 0.0), 0.1, M["gro"], 0.012, 5)
    return [finish(P)], None


def adorno_hotel(P, M, cx, h, lado):
    y = -0.115 * lado
    pts = []
    for i in range(8):
        r = 0.2 if i % 2 == 0 else 0.07
        a = i * math.pi / 4
        pts.append((cx + math.cos(a) * r, y, h - 0.4 + math.sin(a) * r))
    tube(P, pts, 0.015, M["ama"], 0, True)
    estrellas(P, M, [(cx - 0.15, 0.22), (cx + 0.15, 0.22)], M["cian"], lado)


def adorno_ciber(P, M, cx, h, lado):
    y = -0.115 * lado
    tube(P, [(cx - 0.2, y, h - 0.55), (cx + 0.2, y, h - 0.55), (cx + 0.2, y, h - 0.2), (cx - 0.2, y, h - 0.2)], 0.014, M["tur"], 0, True)
    tube(P, [(cx - 0.1, y, h - 0.38), (cx - 0.03, y, h - 0.38)], 0.014, M["cian"], 0)
    box(P, (0.06, 0.02, 0.012), (cx + 0.06, y, h - 0.45), M["mag"])


# ================================================================ flecha
def flecha(M):
    P = Prop("Letrero_FLECHA")
    w, h = 1.9, 1.0
    cuerpo(P, M, w, h)
    tube(P, [(-w / 2 + 0.3, -0.1, h / 2), (w / 2 - 0.55, -0.1, h / 2)], 0.03, M["nar"], 1)
    tube(P, [(w / 2 - 0.65, -0.1, h / 2 + 0.28), (w / 2 - 0.2, -0.1, h / 2), (w / 2 - 0.65, -0.1, h / 2 - 0.28)], 0.03, M["nar"], 1)
    marco(P, M, w, h, M["cian"], -0.1)
    estrellas(P, M, [(-w / 2 + 0.2, h - 0.2), (-w / 2 + 0.2, 0.2)], M["mag"])
    soportes_planos(P, M, w, h)
    return [finish(P)], None


def construir(M):
    return {
        "letrero_hotel": bandera(M, "Letrero_HOTEL", 0.56, 3.0, "H\nO\nT\nE\nL", M["mag"], M["cian"], adorno_hotel),
        "letrero_ciber": bandera(M, "Letrero_CIBER", 0.56, 3.0, "C\nI\nB\nE\nR", M["tur"], M["mag"], adorno_ciber),
        "letrero_birria": plano(M, "Letrero_BIRRIA", 3.4, 1.0, "BIRRIA 24H", M["nar"], M["ama"], ad_birria),
        "letrero_refacciones": plano(M, "Letrero_REFACCIONES", 4.4, 1.0, "REFACCIONES", M["cian"], M["nar"], ad_refacciones),
        "letrero_lavanderia": plano(M, "Letrero_LAVANDERIA", 3.9, 1.0, "LAVANDERÍA", M["tur"], M["cian"], ad_lavanderia),
        "letrero_peleas": plano(M, "Letrero_PELEAS", 5.0, 1.6, "PELEAS", M["roj"], M["ama"], ad_peleas),
        "letrero_casino": plano(M, "Letrero_CASINO", 4.6, 1.5, "CASINO", M["ama"], M["mag"], ad_casino),
        "letrero_flecha": flecha(M),
        "letrero_hotel_azotea": plano(M, "Letrero_HOTEL_AZOTEA", 3.4, 1.2, "HOTEL", M["mag"], M["cian"], ad_hotel, z0=1.3, patas=True),
    }


def main2():
    clear_scene()
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)
    M = mats_extra(make_mats())
    piezas = construir(M)
    objs = []
    for nombre, (lista, _) in piezas.items():
        if ONLY and nombre not in ONLY:
            continue
        o = lista[0]
        export(o, nombre)
        print("TRIS", nombre, tris(o))
        objs.append(o)
    # render de referencia: todas en una fila
    x = 0.0
    for o in objs:
        xs = [v.co.x for v in o.data.vertices]
        o.location.x = x - min(xs)
        x += max(xs) - min(xs) + 0.8
    bpy.context.view_layer.update()
    render("letreros_neon", objs, 1600, 520, (0.0, -1, 0.12))
    print("DONE")


main2()
