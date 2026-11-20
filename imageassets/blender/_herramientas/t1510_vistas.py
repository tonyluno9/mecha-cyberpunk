# T-1510: capturas de un carro partido (FBX de t1510_partir_carro.py): 4 vistas.
# uso: blender -b --python t1510_vistas.py -- carro.fbx salida_prefijo [piezas]
#   sin "piezas": 4 vistas con textura. Con "piezas": cada parte de un color, puertas abiertas 60 grados y ruedas giradas 30 grados (se ve que estan sueltas).
import bpy, sys, math
from mathutils import Vector, Euler

argv = sys.argv[sys.argv.index('--') + 1:]
path, out = argv[0], argv[1]
piezas = len(argv) > 2 and argv[2] == 'piezas'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)
objs = [o for o in bpy.data.objects if o.type == 'MESH']
COL = {'Carroceria': (0.5, 0.5, 0.55, 1), 'Rueda': (0.9, 0.2, 0.2, 1), 'Puerta': (0.2, 0.8, 0.2, 1), 'Cristal': (0.2, 0.5, 1.0, 1), 'Interior': (1.0, 0.9, 0.1, 1), 'Cofre': (0.8, 0.4, 0.9, 1)}
for o in objs:
    for k, c in COL.items():
        if o.name.startswith(k):
            o.color = c
    if piezas and o.name == 'Puerta_I':
        o.rotation_mode = 'XYZ'
        o.rotation_euler[2] += math.radians(-60)
    if piezas and o.name == 'Puerta_D':
        o.rotation_euler[2] += math.radians(60)
    if piezas and o.name.startswith('Rueda_'):
        o.rotation_euler[0] += math.radians(30)
bpy.context.view_layer.update()
mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
for o in objs:
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c)
        for i in range(3):
            mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
ctr = (mn + mx) / 2
size = max(mx - mn)
print('BBOX', [round(x, 3) for x in mn], [round(x, 3) for x in mx], 'largo(y)', round(mx.y - mn.y, 3), 'ancho(x)', round(mx.x - mn.x, 3), 'alto', round(mx.z, 3))
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'OBJECT' if piezas else 'TEXTURE'
sc.render.resolution_x = 800; sc.render.resolution_y = 520
sc.world = bpy.data.worlds.new('w'); sc.world.color = (0.55, 0.6, 0.65)
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = size * 1.12
views = {'frente': (0, -1, 0.0), 'lado': (1, 0, 0.0), 'arriba': (0, 0, 1), 'tres_cuartos': (-0.8, -0.7, 0.5)}
for n, d in views.items():
    d = Vector(d).normalized()
    cam.location = ctr + d * size * 4
    cam.rotation_euler = (ctr - cam.location).normalized().to_track_quat('-Z', 'Y').to_euler()
    if n == 'arriba':
        cam.location = ctr + Vector((0, 0, size * 4)); cam.rotation_euler = (0, 0, 0)
    sc.render.filepath = out + '_' + n + '.png'
    bpy.ops.render.render(write_still=True)
