"""T-1381: rig generico de 4 patas para E17_PerroRobotNova_a + clips Perro_Idle y Perro_Caminar.
blender -b --python t1381_perro.py -- <carpeta_render> [exportar]

Huesos (sin prefijo; izquierda = +X de Blender, frente = -Y):
  Raiz > Cadera > Columna > Cuello > Cabeza ; Cadera > Cola > Cola2
  Columna > Pata_DI_1/2/3 (delantera izquierda), Pata_DD_* (delantera derecha)
  Cadera  > Pata_TI_1/2/3 (trasera izquierda),  Pata_TD_* (trasera derecha)
"""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1381_lib as L
import bpy
from mathutils import Vector, Matrix

a = sys.argv[sys.argv.index('--') + 1:]
carpeta = a[0]
exportar = len(a) > 1 and a[1] == 'exportar'
os.makedirs(carpeta, exist_ok=True)
NOMBRE = "E17_PerroRobotNova_a"

L.reset()
me, _ = L.importar(NOMBRE)
me.rotation_euler = (0, 0, math.radians(90))
L._select_only([me]); bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
s = L.normalizar(me, None, L.ALTURA['E17'])
mn, mx = L.bbox(me)
print("T1381 PERRO bbox", [round(x, 3) for x in mn], [round(x, 3) for x in mx], "escala", round(s, 3))
k = s  # el esqueleto se dio en unidades de la malla original (sin escalar de Tripo)


def P(xo, yo, zo):
    """coordenadas del modelo original (cabeza hacia -x) a final (cabeza hacia -y, metros)."""
    return Vector((-yo * k, xo * k, zo * k))


# (nombre, padre, cabeza, cola)  en coordenadas originales (x largo con cabeza en -x, y ancho, z alto)
FY, HY = 0.20, 0.21
H = [
    ('Raiz', None, (0.15, 0, 0.00), (0.15, 0, 0.12)),
    ('Cadera', 'Raiz', (0.33, 0, 0.50), (0.17, 0, 0.52)),
    ('Columna', 'Cadera', (0.17, 0, 0.52), (-0.10, 0, 0.55)),
    ('Cuello', 'Columna', (-0.10, 0, 0.55), (-0.28, 0, 0.55)),
    ('Cabeza', 'Cuello', (-0.28, 0, 0.55), (-0.48, 0, 0.50)),
    ('Cola', 'Cadera', (0.38, 0, 0.52), (0.46, 0, 0.44)),
    ('Cola2', 'Cola', (0.46, 0, 0.44), (0.50, 0, 0.34)),
]
for sufijo, sy in (('I', 1), ('D', -1)):
    H += [
        ('Pata_D%s_1' % sufijo, 'Columna', (-0.14, sy * FY, 0.50), (-0.12, sy * FY, 0.27)),
        ('Pata_D%s_2' % sufijo, 'Pata_D%s_1' % sufijo, (-0.12, sy * FY, 0.27), (-0.15, sy * FY, 0.08)),
        ('Pata_D%s_3' % sufijo, 'Pata_D%s_2' % sufijo, (-0.15, sy * FY, 0.08), (-0.26, sy * FY, 0.015)),
        ('Pata_T%s_1' % sufijo, 'Cadera', (0.32, sy * HY, 0.50), (0.40, sy * HY, 0.30)),
        ('Pata_T%s_2' % sufijo, 'Pata_T%s_1' % sufijo, (0.40, sy * HY, 0.30), (0.34, sy * HY, 0.11)),
        ('Pata_T%s_3' % sufijo, 'Pata_T%s_2' % sufijo, (0.34, sy * HY, 0.11), (0.25, sy * HY, 0.015)),
    ]

ad = bpy.data.armatures.new("Perro_rig")
arm = bpy.data.objects.new("E17_PerroRobotNova_a_rig", ad)
bpy.context.scene.collection.objects.link(arm)
L._select_only([arm])
bpy.ops.object.mode_set(mode='EDIT')
for n, pa, h, t in H:
    b = ad.edit_bones.new(n)
    b.head = P(*h); b.tail = P(*t)
    b.roll = 0
for n, pa, h, t in H:
    if pa:
        ad.edit_bones[n].parent = ad.edit_bones[pa]
bpy.ops.object.mode_set(mode='OBJECT')
ad.display_type = 'STICK'

# ------------------------------------------------------------- pesos por cercania al segmento del hueso
for g in list(me.vertex_groups):
    me.vertex_groups.remove(g)
for n, *_ in H:
    me.vertex_groups.new(name=n)
segs = [(n, Vector(ad.bones[n].head_local), Vector(ad.bones[n].tail_local)) for n, *_ in H if n != 'Raiz']
POT = 5.0


def dseg(p, h, t):
    d = t - h
    u = max(0.0, min(1.0, (p - h).dot(d) / d.length_squared))
    return (p - (h + d * u)).length


for v in me.data.vertices:
    p = v.co
    ds = sorted(((dseg(p, h, t) + 0.004, n) for n, h, t in segs))[:3]
    w = [(n, 1.0 / (d ** POT)) for d, n in ds]
    tot = sum(x for _, x in w)
    for n, x in w:
        if x / tot > 0.03:
            me.vertex_groups[n].add([v.index], x / tot, 'REPLACE')
me.parent = arm
me.matrix_parent_inverse = arm.matrix_world.inverted()
mod = me.modifiers.new("Armature", 'ARMATURE'); mod.object = arm
L.limitar_pesos(me, 4)
print("T1381 PERRO pesos", L.pesos_info(me, arm))

# ------------------------------------------------------------- clips
sc = bpy.context.scene
sc.render.fps = 30
RX = lambda g: L.rot_mundo('X', g)
PATAS = {'DI': 0.0, 'TD': 0.0, 'DD': math.pi, 'TI': math.pi}   # trote: diagonales juntas


def pose_caminar(t, raiz_extra=0.0):
    w = 2 * math.pi * t
    d = {}
    for p, ph in PATAS.items():
        fr = p[0] == 'D'
        A = 26 if fr else 24
        th = -A * math.sin(w + ph) * -1       # angulo del muslo (+ = hacia atras)
        flex = (55 if fr else 50) * max(0.0, -math.cos(w + ph))   # doblar solo al balancear
        d['Pata_%s_1' % p] = RX(th)
        d['Pata_%s_2' % p] = RX(th + flex)
        d['Pata_%s_3' % p] = RX(th + flex * 0.6 - (10 if fr else -10) * math.sin(w + ph))
    d['Columna'] = RX(2.0 * math.sin(w * 2 + 0.5))
    d['Cabeza'] = RX(3.0 * math.sin(w * 2 + 1.5))
    d['Cola'] = L.rot_mundo('Z', 12 * math.sin(w))
    d['Cola2'] = L.rot_mundo('Z', 20 * math.sin(w - 0.8))
    bob = 0.012 * k / 1.0 * abs(math.sin(w))
    return d, Vector((0, 0, bob))


def pose_idle(t):
    w = 2 * math.pi * t
    d = {
        'Columna': RX(-1.8 * math.sin(w)),
        'Cuello': RX(1.5 * math.sin(w - 0.4)),
        'Cabeza': RX(-2.5 * math.sin(w - 0.9)) @ L.rot_mundo('Z', 3 * math.sin(w * 0.5)),
        'Cola': L.rot_mundo('Z', 10 * math.sin(w * 2)),
        'Cola2': L.rot_mundo('Z', 14 * math.sin(w * 2 - 0.7)),
    }
    return d, Vector((0, 0, 0.006 * math.sin(w)))


def hacer_clip(nombre, nframes, fnpose):
    act = bpy.data.actions.new(nombre)
    act.use_fake_user = True
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = act
    for f in range(0, nframes + 1):
        t = (f % nframes) / nframes
        d, mov = fnpose(t)
        L.poner_pose(arm, d, mov, raiz='Raiz')
        for pb in arm.pose.bones:
            pb.keyframe_insert('rotation_quaternion', frame=f + 1)
            pb.keyframe_insert('location', frame=f + 1)
    return act


acts = {}
acts['idle'] = hacer_clip("Perro_Idle", 60, pose_idle)
acts['walk'] = hacer_clip("Perro_Caminar", 18, pose_caminar)
arm.animation_data.action = None

# ------------------------------------------------------------- renders
pref = os.path.join(carpeta, "E17_PerroRobotNova_a")
for pb in arm.pose.bones:
    pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
bpy.context.view_layer.update()
L.vistas(me, arm, pref + "_reposo", ('lado', 'frente', '34'), esq=True, xray=True)
mnx, mxx = L.bbox(me)


def foto(nombre, vistas_=('lado',)):
    L.vistas(me, arm, pref + "_" + nombre, vistas_)


for f in (0, 4, 9, 13):
    d, mov = pose_caminar(f / 18.0)
    L.poner_pose(arm, d, mov, raiz='Raiz')
    foto("caminar%02d" % f, ('lado', 'frente'))
for f, nm in ((15, 'idle_a'), (45, 'idle_b')):
    d, mov = pose_idle(f / 60.0)
    L.poner_pose(arm, d, mov, raiz='Raiz')
    foto(nm, ('lado',))

if exportar:
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    L._select_only([me, arm])
    ruta = os.path.join(L.OUT, NOMBRE + ".fbx")
    os.makedirs(L.OUT, exist_ok=True)
    arm.animation_data.action = acts['idle']
    sc.frame_start = 1; sc.frame_end = 61
    bpy.ops.export_scene.fbx(
        filepath=ruta, use_selection=True, object_types={'ARMATURE', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', global_scale=1.0,
        axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_bones=True, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        path_mode='COPY', embed_textures=True, mesh_smooth_type='FACE',
        primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL')
    print("T1381 EXPORT", NOMBRE)
