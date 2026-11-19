"""T-1381: libreria comun para rigear los personajes nuevos de Tripo (esqueleto mixamorig, como E10/E12).

Se usa desde los scripts t1381_*.py (Blender 5.2 en modo batch).
Convenciones: Z arriba, frente hacia -Y de Blender (= +Z de Unity), pies en z = 0, metros.
"""
import bpy, os, sys, math, glob, zipfile, tempfile, shutil
from mathutils import Vector, Matrix, Quaternion

MAIN = r"C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk"
NUEVOS = os.path.join(MAIN, "imageassets", "modelos-nuevos", "nuevos-18nov")
WALK_FBX = os.path.join(MAIN, "Assets", "Arte", "Personajes", "Animaciones", "Locomocion", "Loco_Caminar.fbx")
IDLE_FBX = os.path.join(MAIN, "Assets", "Arte", "Personajes", "Animaciones", "Locomocion", "Loco_Idle.fbx")

HERE = os.path.dirname(os.path.abspath(__file__))
WT = os.path.normpath(os.path.join(HERE, "..", "..", ".."))          # raiz del worktree
OUT = os.path.join(WT, "imageassets", "blender", "personajes")
REV = os.path.join(WT, "imageassets", "blender", "_PENDIENTE_BLENDER_2", "_REVISION")
TMP = os.path.join(tempfile.gettempdir(), "t1381_src")

# altura real (m) por codigo
ALTURA = {
    'E15': 1.85, 'E16': 2.45, 'E17': 0.90, 'E18': 1.90, 'E19': 1.78, 'E20': 1.88,
    'NP07': 1.72, 'NP08': 1.75, 'NP09': 1.78, 'NP10': 1.65, 'NP11': 1.82, 'NP12': 1.55,
    'NP13': 1.72, 'NP14': 1.75,
}

PRE = 'mixamorig:'
CUERPO = ['Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head']


def codigo(nombre):
    return nombre.split('_')[0]


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def extraer(nombre):
    """Extrae <nombre>.zip y devuelve la ruta del fbx."""
    zp = os.path.join(NUEVOS, nombre + ".zip")
    d = os.path.join(TMP, nombre)
    if not os.path.isdir(d):
        os.makedirs(d, exist_ok=True)
        with zipfile.ZipFile(zp) as z:
            z.extractall(d)
    fb = glob.glob(os.path.join(d, "*.fbx"))
    return fb[0]


def importar(nombre):
    """Importa el FBX de Tripo. Devuelve (malla, armature|None)."""
    fb = extraer(nombre)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fb, ignore_leaf_bones=True, automatic_bone_orientation=False)
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == 'MESH']
    arms = [o for o in new if o.type == 'ARMATURE']
    for o in new:
        if o.type not in ('MESH', 'ARMATURE'):
            bpy.data.objects.remove(o, do_unlink=True)
    me = max(meshes, key=lambda o: len(o.data.vertices))
    for m in meshes:
        if m is not me:
            bpy.data.objects.remove(m, do_unlink=True)
    arm = arms[0] if arms else None
    me.name = nombre
    if arm:
        arm.name = nombre + "_rig"
    return me, arm


def bbox(me):
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for c in me.bound_box:
        w = me.matrix_world @ Vector(c)
        for i in range(3):
            mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
    return mn, mx


def _select_only(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


def normalizar(me, arm, altura):
    """Escala a 'altura' m (uniforme, aplicada), pies en z=0, centrado en x. Aplica transformaciones."""
    bpy.context.view_layer.update()
    # soltar parentesco conservando transformacion
    if me.parent is not None:
        mw = me.matrix_world.copy()
        me.parent = None
        me.matrix_world = mw
    objs = [me] + ([arm] if arm else [])
    mn, mx = bbox(me)
    s = altura / (mx.z - mn.z)
    cx = (mn.x + mx.x) / 2
    for o in objs:
        o.location = (0, 0, 0); o.rotation_euler = (0, 0, 0); o.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    # aplicar: primero escalar y mover
    for o in objs:
        o.scale = (s, s, s)
        o.location = (-cx * s, 0, -mn.z * s)
    bpy.context.view_layer.update()
    _select_only(objs)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return s


def pesos_info(me, arm):
    """Estadisticas de pesos: grupos, vertices sin peso, vertices con >4 huesos, grupos usados."""
    names = [g.name for g in me.vertex_groups]
    bones = set(b.name for b in arm.data.bones) if arm else set()
    sin = 0; mas4 = 0; usados = set(); fuera = 0
    for v in me.data.vertices:
        gs = [g for g in v.groups if g.weight > 1e-4]
        if not gs:
            sin += 1
        if len(gs) > 4:
            mas4 += 1
        for g in gs:
            usados.add(names[g.group])
    return dict(grupos=len(names), sin_peso=sin, mas4=mas4, verts=len(me.data.vertices),
                usados=len(usados), grupos_no_hueso=len([n for n in names if n not in bones]))


def limitar_pesos(me, n=4):
    _select_only([me])
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL', limit=n)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)


# ---------------------------------------------------------------- pose
def _orden(arm):
    out = []
    def rec(b):
        out.append(b)
        for c in b.children:
            rec(c)
    for b in arm.data.bones:
        if b.parent is None:
            rec(b)
    return out


def poner_pose(arm, deltas, hips_mov=None, raiz=None):
    """deltas: {hueso: Matrix3 de rotacion en MUNDO respecto a su reposo}. Resto = reposo (hijos siguen al padre).
    hips_mov: Vector desplazamiento de Hips en mundo."""
    Aw3 = arm.matrix_world.to_3x3().normalized()
    Aw3i = Aw3.inverted()
    poseR = {}
    for b in _orden(arm):
        pb = arm.pose.bones[b.name]
        pb.rotation_mode = 'QUATERNION'
        rest = b.matrix_local.to_3x3().normalized()
        if b.parent:
            rel = (b.parent.matrix_local.inverted() @ b.matrix_local).to_3x3().normalized()
            parR = poseR[b.parent.name]
        else:
            rel = rest
            parR = Matrix.Identity(3)
        if b.name in deltas:
            Ra = Aw3i @ deltas[b.name] @ Aw3 @ rest      # rotacion objetivo en espacio armature
        else:
            Ra = parR @ rel
        basis = rel.inverted() @ parR.inverted() @ Ra
        q = basis.to_quaternion()
        pb.rotation_quaternion = q
        pb.location = (0, 0, 0)
        poseR[b.name] = Ra
    if hips_mov is not None:
        raiz = raiz or (PRE + 'Hips')
        hb = arm.data.bones[raiz]
        dl = (hb.matrix_local.to_3x3().normalized().inverted() @ (Aw3i @ hips_mov))
        arm.pose.bones[raiz].location = dl
    bpy.context.view_layer.update()


def rot_mundo(eje, grados):
    return Matrix.Rotation(math.radians(grados), 3, eje)


def pose_prueba(arm, tipo):
    """Poses de prueba (rotaciones en mundo). tipo: reposo | brazos_abajo | paso | sentadilla"""
    d = {}
    L = lambda n: PRE + n
    hips = None
    if tipo == 'brazos_abajo':
        # T-pose: brazos hacia +-X. Bajar: rotar sobre Y (L: +X baja al girar con +angulo sobre Y? probar)
        d[L('LeftArm')] = rot_mundo('Y', 70)
        d[L('RightArm')] = rot_mundo('Y', -70)
        d[L('LeftForeArm')] = rot_mundo('Y', 70) @ rot_mundo('Z', -35)
        d[L('RightForeArm')] = rot_mundo('Y', -70) @ rot_mundo('Z', 35)
    elif tipo == 'paso':
        d[L('LeftUpLeg')] = rot_mundo('X', -40)       # pierna adelante
        d[L('LeftLeg')] = rot_mundo('X', -5)
        d[L('RightUpLeg')] = rot_mundo('X', 25)       # pierna atras
        d[L('RightLeg')] = rot_mundo('X', 55)         # rodilla doblada
        d[L('LeftArm')] = rot_mundo('Y', 70) @ rot_mundo('Z', 20)
        d[L('RightArm')] = rot_mundo('Y', -70) @ rot_mundo('Z', -20)
        d[L('LeftForeArm')] = rot_mundo('Y', 70) @ rot_mundo('X', -40)
        d[L('RightForeArm')] = rot_mundo('Y', -70)
    elif tipo == 'sentadilla':
        d[L('Hips')] = rot_mundo('X', -10)
        d[L('LeftUpLeg')] = rot_mundo('X', -85)
        d[L('RightUpLeg')] = rot_mundo('X', -85)
        d[L('LeftLeg')] = rot_mundo('X', 100)
        d[L('RightLeg')] = rot_mundo('X', 100)
        d[L('LeftFoot')] = rot_mundo('X', -15)
        d[L('RightFoot')] = rot_mundo('X', -15)
        d[L('LeftArm')] = rot_mundo('Y', 30) @ rot_mundo('Z', 20)
        d[L('RightArm')] = rot_mundo('Y', -30) @ rot_mundo('Z', -20)
        hips = Vector((0, 0.12, -0.42 * arm.dimensions.z / 1.8))
    poner_pose(arm, d, hips)


# ---------------------------------------------------------------- caminar (clip existente)
def cargar_fuente(fp):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fp, use_anim=True, ignore_leaf_bones=True, automatic_bone_orientation=False)
    new = [o for o in bpy.data.objects if o not in before]
    arm = [o for o in new if o.type == 'ARMATURE'][0]
    for o in new:
        if o is not arm:
            bpy.data.objects.remove(o, do_unlink=True)
    arm.hide_viewport = True
    return arm


def pose_de_clip(src, tgt, frame, in_place=True):
    """Copia la pose de un clip Mixamo (src, rotado/escalado) al esqueleto tgt (mismos nombres) en 'frame'."""
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    Sw3 = src.matrix_world.to_3x3().normalized()
    deltas = {}
    for b in src.data.bones:
        if b.name not in tgt.data.bones:
            continue
        srest = (Sw3 @ b.matrix_local.to_3x3()).normalized()
        pw = (Sw3 @ src.pose.bones[b.name].matrix.to_3x3()).normalized()
        deltas[b.name] = pw @ srest.inverted()
    # hips: desplazamiento vertical escalado
    hb = src.data.bones[PRE + 'Hips']
    sh0 = src.matrix_world @ hb.head_local
    sh = src.matrix_world @ src.pose.bones[PRE + 'Hips'].head
    k = tgt.data.bones[PRE + 'Hips'].head_local.z / max(sh0.z, 1e-6)
    d = (sh - sh0) * k
    if in_place:
        d.x = 0; d.y = 0
    poner_pose(tgt, deltas, d)


# ---------------------------------------------------------------- render
def escena_render(res=720):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.render.resolution_x = res; sc.render.resolution_y = res
    sc.render.film_transparent = False
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'; sh.show_cavity = False
    sh.background_type = 'WORLD'
    sc.world = bpy.data.worlds.new("w") if not sc.world else sc.world
    sc.world.color = (0.55, 0.58, 0.62)
    sc.render.image_settings.file_format = 'PNG'
    return sc


def camara(sc, centro, ancho, direccion):
    """direccion: vector desde el objetivo hacia la camara (p. ej. (0,-1,0) = frente)."""
    cam = bpy.data.objects.get("camT") or bpy.data.objects.new("camT", bpy.data.cameras.new("camT"))
    if cam.name not in sc.collection.objects:
        sc.collection.objects.link(cam)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ancho
    cam.data.clip_start = 0.01; cam.data.clip_end = 100
    dirn = Vector(direccion).normalized()
    cam.location = Vector(centro) + dirn * 10
    cam.rotation_euler = (-dirn).to_track_quat('-Z', 'Y').to_euler()
    sc.camera = cam
    return cam


def esqueleto_visible(arm, nombre="skelviz", radio=0.006):
    """Crea una curva con una linea por hueso (en la pose actual) para verla en el render."""
    old = bpy.data.objects.get(nombre)
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    cu = bpy.data.curves.new(nombre, 'CURVE')
    cu.dimensions = '3D'; cu.bevel_depth = radio; cu.bevel_resolution = 1
    for b in arm.pose.bones:
        sp = cu.splines.new('POLY'); sp.points.add(1)
        h = arm.matrix_world @ b.head; t = arm.matrix_world @ b.tail
        sp.points[0].co = (h.x, h.y, h.z, 1); sp.points[1].co = (t.x, t.y, t.z, 1)
    ob = bpy.data.objects.new(nombre, cu)
    bpy.context.scene.collection.objects.link(ob)
    mat = bpy.data.materials.new(nombre + "m"); mat.diffuse_color = (1, 0.1, 0.1, 1)
    cu.materials.append(mat)
    ob.show_in_front = True
    return ob


def renderizar(sc, ruta):
    sc.render.filepath = ruta
    bpy.ops.render.render(write_still=True)


def vistas(me, arm, prefijo, vistas_=('frente', 'lado'), esq=False, xray=False):
    """Renderiza vistas ortograficas del personaje en su pose actual."""
    sc = escena_render()
    sh = sc.display.shading
    sh.show_xray = xray
    if xray:
        sh.xray_alpha = 0.45
    mn, mx = bbox(me)
    alt = max(mx.z - mn.z, 0.5)
    c = ((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, (mn.z + mx.z) / 2)
    sk = esqueleto_visible(arm, radio=0.004 * alt / 1.8) if (esq and arm) else None
    for v in vistas_:
        d = {'frente': (0, -1, 0), 'lado': (1, 0, 0), 'atras': (0, 1, 0), '34': (-0.7, -0.7, 0.25)}[v]
        camara(sc, c, max(alt * 1.15, (mx.x - mn.x) * 1.1, (mx.y - mn.y) * 1.1), d)
        renderizar(sc, prefijo + "_" + v + ".png")
    if sk:
        bpy.data.objects.remove(sk, do_unlink=True)


def exportar(me, arm, ruta, con_anim=False):
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    _select_only([me, arm])
    bpy.ops.object.mode_set(mode='OBJECT') if bpy.context.object and bpy.context.object.mode != 'OBJECT' else None
    # reposo antes de exportar
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(
        filepath=ruta, use_selection=True, object_types={'ARMATURE', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', global_scale=1.0,
        axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True, add_leaf_bones=False,
        bake_anim=con_anim, path_mode='COPY', embed_textures=True, mesh_smooth_type='FACE',
        primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL',
        use_armature_deform_only=False)


# ---------------------------------------------------------------- llevar a T-pose
def angulo_brazo(arm, lado='Left'):
    """Angulo (grados) del brazo respecto a la horizontal (0 = T-pose, 90 = colgando)."""
    b = arm.data.bones[PRE + lado + 'Arm']
    d = (b.tail_local - b.head_local).normalized()
    return math.degrees(math.asin(max(-1, min(1, -d.z))))


def a_tpose(me, arm):
    """Si los brazos no estan horizontales, los lleva a T-pose: posa los huesos, aplica el modificador y la pose como reposo."""
    _select_only([arm])
    for lado, sx in (('Left', 1), ('Right', -1)):
        pass
    deltas = {}
    Aw3 = arm.matrix_world.to_3x3().normalized()
    for lado, sx in (('Left', 1), ('Right', -1)):
        for n in ('Arm', 'ForeArm', 'Hand'):
            b = arm.data.bones[PRE + lado + n]
            cur = (Aw3 @ (b.tail_local - b.head_local)).normalized()
            obj = Vector((sx, 0, 0))
            deltas[b.name] = cur.rotation_difference(obj).to_matrix()
    poner_pose(arm, deltas)
    # aplicar el modificador (malla queda en T-pose) y la pose como reposo
    _select_only([me])
    for m in list(me.modifiers):
        if m.type == 'ARMATURE':
            bpy.ops.object.modifier_apply(modifier=m.name)
    me.parent = None
    _select_only([arm])
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.select_all(action='SELECT')
    bpy.ops.pose.armature_apply()
    bpy.ops.object.mode_set(mode='OBJECT')
    m = me.modifiers.new("Armature", 'ARMATURE'); m.object = arm
    me.parent = arm
    me.matrix_parent_inverse = arm.matrix_world.inverted()
