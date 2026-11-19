"""T-1408: prepara la malla de E16_b: normaliza y lleva los brazos a T-pose girando la malla de forma RIGIDA
(todo el brazo con una sola rotacion sobre el hombro, mezclada por pesos de la cadena del brazo), para no estirar nada.
Funciones usadas por t1408_rig.py."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1381_lib as L
import bpy
from mathutils import Vector, Matrix

PRE = L.PRE
DEDOS = ['Index', 'Middle', 'Ring', 'Pinky', 'Thumb']


def cadena_brazo(lado):
    n = [lado + 'Arm', lado + 'ForeArm', lado + 'Hand']
    for d in DEDOS:
        for i in (1, 2, 3):
            n.append(lado + 'Hand' + d + str(i))
    return [PRE + x for x in n]


def pesos_grupo(me, nombres):
    """peso total por vertice en los grupos indicados."""
    idx = {g.index: g.name for g in me.vertex_groups}
    ns = set(nombres)
    out = []
    for v in me.data.vertices:
        out.append(sum(g.weight for g in v.groups if idx[g.group] in ns))
    return out


def brazos_a_tpose(me, arm):
    """Gira los brazos de la malla a horizontal. Devuelve info de pivotes (hombro) por lado."""
    info = {}
    Aw = arm.matrix_world
    for lado, sx in (('Left', 1), ('Right', -1)):
        b = arm.data.bones[PRE + lado + 'Arm']
        h = Aw @ b.head_local
        # direccion actual del brazo: de la cabeza del brazo al centro de masa de los dedos/mano
        mano = arm.data.bones[PRE + lado + 'Hand']
        t = Aw @ mano.head_local
        cur = (t - h).normalized()
        obj = Vector((sx, 0, 0))
        R = cur.rotation_difference(obj).to_matrix()
        w = pesos_grupo(me, cadena_brazo(lado))
        for v, wi in zip(me.data.vertices, w):
            if wi < 1e-4:
                continue
            wi = min(wi, 1.0)
            p = Vector(v.co)
            q = h + R @ (p - h)
            v.co = p.lerp(q, wi)
        info[lado] = dict(pivote=h.copy(), ang=cur.angle(obj) * 180 / 3.14159265)
    me.data.update()
    return info


def _bbox_v(me):
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for v in me.data.vertices:
        w = me.matrix_world @ v.co
        for i in range(3):
            mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
    return mn, mx


L.bbox = _bbox_v   # la bound_box de Blender queda vieja tras editar vertices a mano


def preparar(nombre="E16_RobotPesadoNova_b", reset=True):
    if reset:
        L.reset()
    me, arm0 = L.importar(nombre)
    L.normalizar(me, arm0, L.ALTURA['E16'])
    me.parent = None
    return me, arm0
