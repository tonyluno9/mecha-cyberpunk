# T-1380: renders de revision de carros y drones (Workbench, en batch)
# uso: blender -b --python t1380_render.py -- <modo> <carpeta_fbx> <carpeta_salida> COD1 COD2 ...
#   modo: carro | dron
# Carro: fila 1 con textura (3/4, lado, frente, 3/4 atras); fila 2 por colores de pieza con puertas y cofre abiertos
#   rojo=ruedas, verde=puertas, azul=cofre, cian=cristal, gris=carroceria, naranja=otras
# Dron: fila 1 con textura (3/4, lado, frente, arriba); fila 2 con rotores resaltados en rojo (cuerpo gris)
import bpy, sys, os, math
import numpy as np
from mathutils import Vector, Euler

argv = sys.argv[sys.argv.index('--') + 1:]
mode, src, outdir = argv[0], argv[1], argv[2]
codes = argv[3:]
W, H = 480, 320


def setup_scene():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.render.resolution_x, sc.render.resolution_y = W, H
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = 'PNG'
    w = bpy.data.worlds.new('W'); w.color = (0.55, 0.58, 0.62); sc.world = w
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.show_cavity = False; sh.show_object_outline = True
    return sc


def world_bbox(objs):
    pts = []
    for o in objs:
        if o.type == 'MESH':
            M = o.matrix_world
            pts += [M @ Vector(c) for c in o.bound_box]
    P = np.array([tuple(p) for p in pts])
    return P.min(0), P.max(0)


def make_cam(sc, loc, target, ortho=None, lens=40):
    cd = bpy.data.cameras.new('C'); cam = bpy.data.objects.new('C', cd)
    sc.collection.objects.link(cam)
    if ortho:
        cd.type = 'ORTHO'; cd.ortho_scale = ortho
    else:
        cd.lens = lens
    cd.clip_end = 1000; cd.clip_start = 0.01
    cam.location = Vector(loc)
    d = Vector(target) - Vector(loc)
    up = 'Y'
    cam.rotation_euler = d.to_track_quat('-Z', up).to_euler()
    sc.camera = cam
    return cam


def shot(sc, path, loc, target, ortho=None, lens=40):
    cam = make_cam(sc, loc, target, ortho, lens)
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)


def classify_car(n):
    if n.startswith('Rueda'): return (0.9, 0.1, 0.1, 1)
    if n.startswith('Puerta'): return (0.1, 0.8, 0.2, 1)
    if n.startswith('Cofre'): return (0.15, 0.3, 1.0, 1)
    if n.startswith('Cristal'): return (0.1, 0.9, 0.9, 1)
    if n.startswith('Carroceria') or n.startswith('Cuerpo'): return (0.6, 0.6, 0.6, 1)
    return (1.0, 0.6, 0.1, 1)


def classify_dron(n):
    if n.startswith('Rotor'): return (0.95, 0.1, 0.1, 1)
    if n.startswith('Cuerpo'): return (0.6, 0.6, 0.6, 1)
    return (1.0, 0.6, 0.1, 1)


def tile(paths, ncols, out):
    imgs = [bpy.data.images.load(p) for p in paths]
    nrows = (len(paths) + ncols - 1) // ncols
    canvas = np.zeros((nrows * H, ncols * W, 4), dtype=np.float32)
    for i, im in enumerate(imgs):
        a = np.array(im.pixels[:], dtype=np.float32).reshape(H, W, 4)
        r = nrows - 1 - i // ncols; c = i % ncols
        canvas[r * H:(r + 1) * H, c * W:(c + 1) * W] = a
    res = bpy.data.images.new('tile', ncols * W, nrows * H, alpha=False)
    res.pixels = canvas.ravel()
    res.filepath_raw = out; res.file_format = 'PNG'; res.save()
    for im in imgs: bpy.data.images.remove(im)
    bpy.data.images.remove(res)


def open_car(objs):
    """abre puertas y cofre como dice la bitacora: cofre -X, Puerta_I -Z, Puerta_D +Z"""
    for o in objs:
        if o.name.startswith('Cofre'):
            o.rotation_mode = 'XYZ'; o.rotation_euler.x = math.radians(-50)
        elif o.name.startswith('Puerta_I'):
            o.rotation_mode = 'XYZ'; o.rotation_euler.z = math.radians(-60)
        elif o.name.startswith('Puerta_D'):
            o.rotation_mode = 'XYZ'; o.rotation_euler.z = math.radians(60)


def close_car(objs):
    for o in objs:
        if o.name.startswith(('Cofre', 'Puerta')):
            o.rotation_euler = (0, 0, 0)


os.makedirs(outdir, exist_ok=True)
for code in codes:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = setup_scene()
    fp = os.path.join(src, code + '.fbx')
    bpy.ops.import_scene.fbx(filepath=fp)
    objs = [o for o in sc.objects if o.type == 'MESH']
    mn, mx = world_bbox(objs)
    ctr = (mn + mx) / 2
    size = float(np.linalg.norm(mx - mn))
    tmp = os.path.join(outdir, '_t_%s_%%d.png' % code)
    paths = []
    ctrv = Vector(tuple(ctr))
    d = size * 1.25
    if mode == 'carro':
        views = [
            ('34', (ctr[0] + d * .75, ctr[1] - d * .75, ctr[2] + d * .35), None),
            ('lado', (ctr[0] + d * 2, ctr[1], ctr[2]), size * 0.95),
            ('frente', (ctr[0], ctr[1] - d * 2, ctr[2]), size * 0.7),
            ('34t', (ctr[0] - d * .75, ctr[1] + d * .75, ctr[2] + d * .35), None),
        ]
    else:
        views = [
            ('34', (ctr[0] + d * .8, ctr[1] - d * .8, ctr[2] + d * .6), None),
            ('lado', (ctr[0] + d * 2, ctr[1], ctr[2]), size * 1.1),
            ('frente', (ctr[0], ctr[1] - d * 2, ctr[2]), size * 1.1),
            ('arriba', (ctr[0], ctr[1] - 0.001, ctr[2] + d * 2), size * 1.1),
        ]
    k = 0
    sh = sc.display.shading
    sh.color_type = 'TEXTURE'
    for nm, loc, orth in views:
        p = tmp % k; k += 1
        shot(sc, p, loc, ctrv, orth); paths.append(p)
    sh.color_type = 'OBJECT'
    cls = classify_car if mode == 'carro' else classify_dron
    for o in objs:
        o.color = cls(o.name)
    if mode == 'carro':
        open_car(objs)
    else:
        for o in objs:
            if o.name.startswith('Rotor'): o.rotation_euler.z = math.radians(30)
    for nm, loc, orth in views:
        p = tmp % k; k += 1
        shot(sc, p, loc, ctrv, orth); paths.append(p)
    out = os.path.join(outdir, 'T1380_%s_%s.png' % (mode, code))
    tile(paths, 4, out)
    for p in paths: os.remove(p)
    print('OK', code, out)
