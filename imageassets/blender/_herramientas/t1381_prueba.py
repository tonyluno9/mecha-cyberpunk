"""T-1381: analiza y prueba un personaje con rig.
blender -b --python t1381_prueba.py -- <Codigo_Nombre_a> <carpeta_render> [modo]
modo: info (solo pesos) | todo (renders de poses + caminar)
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib, t1381_lib as L
import bpy
from mathutils import Vector

a = sys.argv[sys.argv.index('--') + 1:]
nombre, carpeta = a[0], a[1]
modo = a[2] if len(a) > 2 else 'todo'
os.makedirs(carpeta, exist_ok=True)

L.reset()
me, arm = L.importar(nombre)
cod = L.codigo(nombre)
print("T1381 IMPORT", nombre, "arm" if arm else "SIN-ARM", "mods", [m.type for m in me.modifiers], "parent", me.parent.name if me.parent else None)
s = L.normalizar(me, arm, L.ALTURA[cod])
mn, mx = L.bbox(me)
print("T1381 BBOX", nombre, [round(x, 3) for x in mn], [round(x, 3) for x in mx], "escala", round(s, 3))
if arm:
    print("T1381 HUESOS", len(arm.data.bones), "Hips z", round(arm.data.bones[L.PRE + 'Hips'].head_local.z, 3))
    print("T1381 PESOS", L.pesos_info(me, arm))
    # frente: dedo del pie vs pie
    f = arm.data.bones[L.PRE + 'LeftFoot'].head_local; t = arm.data.bones[L.PRE + 'LeftToeBase'].tail_local
    print("T1381 FRENTE", "-Y" if t.y < f.y else "+Y")
if arm:
    ang = L.angulo_brazo(arm)
    print("T1381 ANGBRAZO", round(ang, 1))
    if ang > 25:
        L.a_tpose(me, arm)
        print("T1381 A_TPOSE hecho, angulo ahora", round(L.angulo_brazo(arm), 1))
if modo == 'exp' and arm:
    L.exportar(me, arm, os.path.join(L.OUT, nombre + ".fbx"))
    print("T1381 EXPORT", nombre)
    sys.exit(0)
if modo == 'info' or not arm:
    sys.exit(0)

pref = os.path.join(carpeta, nombre)
L.vistas(me, arm, pref + "_reposo", ('frente', 'lado'), esq=True, xray=True)
for tipo in ('brazos_abajo', 'paso', 'sentadilla'):
    L.pose_prueba(arm, tipo)
    L.vistas(me, arm, pref + "_" + tipo, ('frente', 'lado'))
src = L.cargar_fuente(L.WALK_FBX)
for fr in (6, 14, 24):
    L.pose_de_clip(src, arm, fr)
    L.vistas(me, arm, pref + "_caminar%02d" % fr, ('frente', 'lado'))
if len(a) > 3 and a[3] == 'exportar':
    bpy.data.objects.remove(src, do_unlink=True)
    L.exportar(me, arm, os.path.join(L.OUT, nombre + ".fbx"))
    print("T1381 EXPORT", nombre)
