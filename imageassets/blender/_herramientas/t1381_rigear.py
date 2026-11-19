"""T-1381: pone rig mixamorig a un personaje de Tripo que no lo trae, usando como plantilla el esqueleto de un hermano ya rigeado.
blender -b --python t1381_rigear.py -- <Variante> <Plantilla> <carpeta_render> [auto|transfer] [exportar]
Ej.: -- E15_CentinelaNova_b E15_CentinelaNova_a C:/tmp/r transfer exportar
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1381_lib as L
import bpy
from mathutils import Vector, Matrix

a = sys.argv[sys.argv.index('--') + 1:]
nombre, plantilla, carpeta = a[0], a[1], a[2]
metodo = a[3] if len(a) > 3 else 'transfer'
exportar = len(a) > 4 and a[4] == 'exportar'
os.makedirs(carpeta, exist_ok=True)
cod = L.codigo(nombre)
alt = L.ALTURA[cod]

L.reset()
me, _ = L.importar(nombre)
L.normalizar(me, None, alt)
tm, ta = L.importar(plantilla)
L.normalizar(tm, ta, L.ALTURA[L.codigo(plantilla)] if L.codigo(plantilla) == cod else alt)


def puntos(o):
    return [o.matrix_world @ v.co for v in o.data.vertices]


def landmarks(o):
    P = puntos(o)
    zs = [p.z for p in P]
    top = max(zs)
    hw = max(abs(p.x) for p in P)
    # altura de los brazos: z media de vertices del 15% mas externo en x
    ext = [p for p in P if abs(p.x) > hw * 0.80]
    zarm = sum(p.z for p in ext) / len(ext)
    # entrepierna: la z mas baja (sobre 0.25 top) donde hay vertices cerca de x=0 (|x|<0.012*top) de forma continua
    w = 0.015 * top
    cnt = {}
    for p in P:
        if abs(p.x) < w and p.z > 0.2 * top and abs(p.z) < 0.7 * top:
            k = int(p.z / top * 200)
            cnt[k] = cnt.get(k, 0) + 1
    ent = None
    for k in sorted(cnt):
        if cnt[k] >= 6 and all(cnt.get(k + j, 0) >= 6 for j in range(1, 4)):
            ent = (k + 0.5) / 200 * top
            break
    if ent is None:
        ent = 0.47 * top
    # ancho de pies: x maxima a z < 0.06 top
    feet = [abs(p.x) for p in P if p.z < 0.06 * top]
    fx = max(feet) if feet else 0.12 * top
    ym = sum(p.y for p in P if 0.3 * top < p.z < 0.7 * top) / max(1, len([1 for p in P if 0.3 * top < p.z < 0.7 * top]))
    return dict(top=top, hw=hw, zarm=zarm, ent=ent, fx=fx, ym=ym)


lt, ls = landmarks(tm), landmarks(me)
print("T1381 LM plantilla", {k: round(v, 3) for k, v in lt.items()})
print("T1381 LM objetivo ", {k: round(v, 3) for k, v in ls.items()})


def mapz(z):
    # lineal por tramos: 0->0, ent_t->ent_s, top_t->top_s
    if z <= lt['ent']:
        return z / lt['ent'] * ls['ent']
    return ls['ent'] + (z - lt['ent']) / (lt['top'] - lt['ent']) * (ls['top'] - ls['ent'])


# duplicar el esqueleto de la plantilla y adaptarlo
bpy.context.view_layer.update()
arm = ta.copy(); arm.data = ta.data.copy(); arm.name = nombre + "_rig"
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb = arm.data.edit_bones
kx_arm = ls['hw'] / lt['hw']
kx_leg = ls['fx'] / lt['fx']
dz_arm = ls['zarm'] - mapz(lt['zarm'])
dy = ls['ym'] - lt['ym']


def mapv(v, es_brazo):
    z = mapz(v.z)
    kx = kx_arm if es_brazo else kx_leg
    if not es_brazo and abs(v.x) < 0.3 * lt['hw'] and v.z > lt['ent']:
        kx = 1.0 + (kx_arm - 1.0) * 0.5
    z2 = z + (dz_arm if es_brazo else 0)
    return Vector((v.x * kx, v.y + dy, z2))


def brazo(n):
    n = n.replace(L.PRE, '')
    return any(k in n for k in ('Shoulder', 'Arm', 'Hand'))


for b in eb:
    ba = brazo(b.name)
    b.head = mapv(b.head.copy(), ba)
    b.tail = mapv(b.tail.copy(), ba)
bpy.ops.object.mode_set(mode='OBJECT')
# conservar uniones padre-hijo
bpy.context.view_layer.update()

# ---------------------------------------------------------------- pesos
me.parent = None
for g in list(me.vertex_groups):
    me.vertex_groups.remove(g)
for b in arm.data.bones:
    me.vertex_groups.new(name=b.name)
if metodo == 'transfer':
    dt = me.modifiers.new("dt", 'DATA_TRANSFER')
    dt.object = tm
    dt.use_vert_data = True
    dt.data_types_verts = {'VGROUP_WEIGHTS'}
    dt.vert_mapping = 'POLYINTERP_NEAREST'
    dt.layers_vgroup_select_src = 'ALL'
    dt.layers_vgroup_select_dst = 'NAME'
    L._select_only([me])
    bpy.ops.object.modifier_apply(modifier="dt")
    # los grupos de la plantilla se llaman igual (mixamorig:*)
else:
    L._select_only([me, arm])
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
# modificador armature y parent
if not any(m.type == 'ARMATURE' for m in me.modifiers):
    m = me.modifiers.new("Armature", 'ARMATURE'); m.object = arm
me.parent = arm
me.matrix_parent_inverse = arm.matrix_world.inverted()
L.limitar_pesos(me, 4)
print("T1381 PESOS", L.pesos_info(me, arm))
bpy.data.objects.remove(tm, do_unlink=True)
bpy.data.objects.remove(ta, do_unlink=True)

pref = os.path.join(carpeta, nombre)
L.vistas(me, arm, pref + "_reposo", ('frente', 'lado'), esq=True, xray=True)
for tipo in ('brazos_abajo', 'paso', 'sentadilla'):
    L.pose_prueba(arm, tipo)
    L.vistas(me, arm, pref + "_" + tipo, ('frente', 'lado'))
src = L.cargar_fuente(L.WALK_FBX)
for fr in (6, 14, 24):
    L.pose_de_clip(src, arm, fr)
    L.vistas(me, arm, pref + "_caminar%02d" % fr, ('frente', 'lado'))
bpy.data.objects.remove(src, do_unlink=True)
if exportar:
    L.exportar(me, arm, os.path.join(L.OUT, nombre + ".fbx"))
    print("T1381 EXPORT", nombre)
