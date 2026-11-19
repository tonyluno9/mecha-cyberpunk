# T-1380: vistas de lado/arriba/frente con rejilla rotulada para medir (metros de Blender)
# uso: blender -b --python t1380_medir.py -- <carpeta_fbx> <salida> <vista:lado|arriba|frente> <escala_m> COD ...
import bpy, sys, os, math
import numpy as np
from mathutils import Vector

argv = sys.argv[sys.argv.index('--') + 1:]
src, outdir, view, scale = argv[0], argv[1], argv[2], float(argv[3])
codes = argv[4:]
os.makedirs(outdir, exist_ok=True)
RES = (1500, 750)


def bar(name, a, b, col):
    me = bpy.data.meshes.new(name)
    me.from_pydata([a, (a[0], a[1], a[2]), b, b], [], [])
    return None


def line_obj(p0, p1, w, col):
    p0 = Vector(p0); p1 = Vector(p1)
    d = p1 - p0
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.location = (p0 + p1) / 2
    o.scale = (max(abs(d.x), w), max(abs(d.y), w), max(abs(d.z), w))
    o.color = col
    return o


def label(txt, loc, size, rot, col):
    cu = bpy.data.curves.new('t', 'FONT'); cu.body = txt; cu.size = size
    cu.align_x = 'CENTER'; cu.align_y = 'CENTER'
    o = bpy.data.objects.new('t', cu); bpy.context.scene.collection.objects.link(o)
    o.location = loc; o.rotation_euler = rot; o.color = col
    return o


for code in codes:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.render.resolution_x, sc.render.resolution_y = RES
    w = bpy.data.worlds.new('W'); w.color = (0.9, 0.9, 0.9); sc.world = w
    sh = sc.display.shading; sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'
    bpy.ops.import_scene.fbx(filepath=os.path.join(src, code + '.fbx'))
    ms = [o for o in sc.objects if o.type == 'MESH']
    P = np.array([tuple(o.matrix_world @ Vector(c)) for o in ms for c in o.bound_box])
    mn, mx = P.min(0), P.max(0)
    ctr = (mn + mx) / 2
    cd = bpy.data.cameras.new('C'); cam = bpy.data.objects.new('C', cd); sc.collection.objects.link(cam)
    cd.type = 'ORTHO'; cd.ortho_scale = scale; cd.clip_end = 1000; sc.camera = cam
    sc.render.film_transparent = False
    step = 0.05 if scale < 3 else (0.25 if scale < 8 else 1.0)
    if view == 'lado':
        cam.location = (ctr[0] + 20, ctr[1], ctr[2]); cam.rotation_euler = (math.radians(90), 0, math.radians(90))
        # reja en plano x = mx+0.2 : y horizontal, z vertical
        xp = mx[0] + 0.3
        ys = np.arange(math.floor(mn[1] / step) * step, mx[1] + step, step)
        zs = np.arange(0, mx[2] + step, step)
        for y in ys:
            k = abs(round(y / (step * 2)) - y / (step * 2)) < 1e-6
            line_obj((xp, y, 0), (xp, y, mx[2] + 0.05), 0.002 if not k else 0.004, (1, 0, 0, 1) if k else (0.2, 0.2, 1, 1))
            if k: label('%.2f' % y, (xp, y, mx[2] + 0.12 * scale / 1.3 * 0.5), 0.035 * scale / 1.3, (math.radians(90), 0, math.radians(90)), (1, 0, 0, 1))
        for z in zs:
            k = abs(round(z / (step * 2)) - z / (step * 2)) < 1e-6
            line_obj((xp, mn[1] - 0.05, z), (xp, mx[1] + 0.05, z), 0.002 if not k else 0.004, (1, 0, 0, 1) if k else (0.2, 0.2, 1, 1))
            if k: label('%.2f' % z, (xp, mn[1] - 0.1 * scale / 1.3, z), 0.03 * scale / 1.3, (math.radians(90), 0, math.radians(90)), (0, 0.4, 0, 1))
        # los objetos de reja estan delante: se dibujan detras de la malla en la cam? usar x un poco por detras del auto
        for o in list(sc.objects):
            if o.type != 'MESH' or o.name in [m.name for m in ms]: continue
            o.location.x = mn[0] - 0.3 if False else o.location.x
    elif view == 'arriba':
        cam.location = (ctr[0], ctr[1], mx[2] + 20); cam.rotation_euler = (0, 0, 0)
        zp = mx[2] + 0.3
        for y in np.arange(math.floor(mn[1] / step) * step, mx[1] + step, step):
            k = abs(round(y / (step * 2)) - y / (step * 2)) < 1e-6
            line_obj((mn[0] - 0.05, y, zp), (mx[0] + 0.05, y, zp), 0.002 if not k else 0.004, (1, 0, 0, 1) if k else (0.2, 0.2, 1, 1))
            if k: label('%.2f' % y, (mn[0] - 0.1 * scale / 1.3, y, zp), 0.03 * scale / 1.3, (0, 0, 0), (1, 0, 0, 1))
        for x in np.arange(math.floor(mn[0] / step) * step, mx[0] + step, step):
            k = abs(round(x / (step * 2)) - x / (step * 2)) < 1e-6
            line_obj((x, mn[1] - 0.05, zp), (x, mx[1] + 0.05, zp), 0.002 if not k else 0.004, (1, 0, 0, 1) if k else (0.2, 0.2, 1, 1))
            if k: label('%.2f' % x, (x, mx[1] + 0.1 * scale / 1.3, zp), 0.03 * scale / 1.3, (0, 0, 0), (0, 0.4, 0, 1))
    # los cubos de reja usan color de objeto
    for o in sc.objects:
        if o.type in ('MESH', 'FONT') and o.name not in [m.name for m in ms]:
            pass
    sh.color_type = 'TEXTURE'
    # color de objeto solo para reja: usar material simple con color de viewport
    for o in sc.objects:
        if o.type in ('MESH', 'FONT') and o.name not in [m.name for m in ms]:
            m = bpy.data.materials.new('g'); m.diffuse_color = tuple(o.color)
            o.data.materials.append(m)
    sc.render.filepath = os.path.join(outdir, 'M_%s_%s.png' % (view, code))
    bpy.ops.render.render(write_still=True)
    print('OK', code)
