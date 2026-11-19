"""T-1408: rehace el rig de E16_RobotPesadoNova_b con el esqueleto de E16_a ajustado a la malla.
blender -b --python t1408_rig.py -- <carpeta_render> [exportar]
 1. Malla de b normalizada (2.45 m) y brazos llevados a T-pose de forma rigida (t1408_malla).
 2. Esqueleto = copia del de E16_a (52 huesos mixamorig, jerarquia y rolls identicos), movido por tramos a la malla de b
    (z por puntos de referencia: suelo, entrepierna, hombro, tope; x de piernas por el centro de las piernas;
    brazo por codo/muneca/punta de mano medidos en la malla).
 3. Pesos: los de Tripo de b (mismos nombres de hueso), limitados a 4 y normalizados.
 4. Armature y malla con escala 1, sin rotacion; exporta personajes/E16_RobotPesadoNova_b.fbx.
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1408_malla as M
import t1381_lib as L
import bpy
from mathutils import Vector

a_ = sys.argv[sys.argv.index('--') + 1:]
carpeta = a_[0]
exportar = len(a_) > 1 and a_[1] == 'exportar'
os.makedirs(carpeta, exist_ok=True)
PRE = L.PRE

# ---------------------------------------------------------------- referencia: E16_a
L.reset()
# plantilla: el E16_a YA EXPORTADO por T-1381 (el que Unity acepta como Humanoid), no el zip de Tripo
_antes = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=os.path.join(L.OUT, "E16_RobotPesadoNova_a.fbx"), ignore_leaf_bones=False, automatic_bone_orientation=False)
_nuevos = [o for o in bpy.data.objects if o not in _antes]
aa = [o for o in _nuevos if o.type == 'ARMATURE'][0]
am = max([o for o in _nuevos if o.type == 'MESH'], key=lambda o: len(o.data.vertices))
am.name = "plantilla_a"
aa.name = "plantilla_a_rig"
am.parent = None
am.matrix_world = am.matrix_world
AB = {b.name: (Vector(b.head_local), Vector(b.tail_local), b.parent.name if b.parent else None) for b in aa.data.bones}
AP = [am.matrix_world @ v.co for v in am.data.vertices]


def landmarks(P):
    top = max(p.z for p in P)
    w = 0.015 * top
    cnt = {}
    for p in P:
        if abs(p.x) < w and p.z > 0.2 * top and p.z < 0.7 * top:
            k = int(p.z / top * 200)
            cnt[k] = cnt.get(k, 0) + 1
    ent = None
    for k in sorted(cnt):
        if cnt[k] >= 6 and all(cnt.get(k + j, 0) >= 6 for j in range(1, 4)):
            ent = (k + 0.5) / 200 * top
            break
    if ent is None:
        ent = 0.47 * top
    pierna = [abs(p.x) for p in P if 0.3 * top < p.z < 0.7 * top and abs(p.x) > 0.04 * top and abs(p.x) < 0.6]
    kx = sum(pierna) / len(pierna)
    mid = [p.y for p in P if 0.3 * top < p.z < 0.7 * top and abs(p.x) < 0.5]
    return dict(top=top, ent=ent, legx=kx, ym=sum(mid) / len(mid))


LA = landmarks(AP)
a_piv = AB[PRE + 'LeftArm'][0]
a_elb = AB[PRE + 'LeftForeArm'][0].x
a_wri = AB[PRE + 'LeftHand'][0].x
a_tip = max(abs(t.x) for (_, t, _) in AB.values())
print("T1408 LM a", {k: round(v, 3) for k, v in LA.items()}, "piv", [round(x, 3) for x in a_piv], "elb", round(a_elb, 3), "wri", round(a_wri, 3), "tip", round(a_tip, 3))

# ---------------------------------------------------------------- b
me, arm0 = M.preparar(reset=False)
Aw0 = arm0.matrix_world
orig = {b.name: (Aw0 @ b.head_local, Aw0 @ b.tail_local) for b in arm0.data.bones}
info = M.brazos_a_tpose(me, arm0)
BM = {}
CF = {}   # centro (y, z) de la mano en la malla, por lado
for lado, sx in (('Left', 1), ('Right', -1)):
    h = info[lado]['pivote']
    cur = (orig[PRE + lado + 'Hand'][0] - h).normalized()
    R = cur.rotation_difference(Vector((sx, 0, 0))).to_matrix()
    elb = abs((h + R @ (orig[PRE + lado + 'ForeArm'][0] - h)).x)
    wri = abs((h + R @ (orig[PRE + lado + 'Hand'][0] - h)).x)
    ns = [PRE + lado + 'Hand'] + [PRE + lado + 'Hand' + d + str(i) for d in M.DEDOS for i in (1, 2, 3)]
    ws = M.pesos_grupo(me, ns)
    pts = [v.co for v, w in zip(me.data.vertices, ws) if w > 0.5]
    # la punta de la mano: percentil 98 en |x| (el escudo de la derecha esta pesado a la mano y la alarga)
    xs = sorted(abs(p.x) for p in pts)
    tip = xs[int(len(xs) * 0.98)]
    c = sum(pts, Vector()) / len(pts)
    BM[lado] = dict(elb=elb, wri=wri, tip=tip)
    CF[lado] = (c.y, c.z)
print("T1408 medidas brazo b", BM, CF)
# simetrico: mismo codo, muneca y punta en los dos lados
for k in ('elb', 'wri'):
    m_ = (BM['Left'][k] + BM['Right'][k]) / 2
    BM['Left'][k] = BM['Right'][k] = m_
tp = min(BM['Left']['tip'], BM['Right']['tip'])
BM['Left']['tip'] = BM['Right']['tip'] = tp
print("T1408 brazo simetrico", BM)
piv = {l: info[l]['pivote'] for l in info}
# torso sin brazos para los puntos de referencia
wc = M.pesos_grupo(me, M.cadena_brazo('Left') + M.cadena_brazo('Right'))
BP = [me.matrix_world @ v.co for v, w in zip(me.data.vertices, wc) if w < 0.3]
LB = landmarks(BP)
print("T1408 LM b", {k: round(v, 3) for k, v in LB.items()})
zsh_a = a_piv.z
zsh_b = piv['Left'].z
knots_a = [0.0, LA['ent'], zsh_a, LA['top']]
knots_b = [0.0, LB['ent'], zsh_b, LB['top']]


def mapz(z):
    for i in range(len(knots_a) - 1):
        if z <= knots_a[i + 1] or i == len(knots_a) - 2:
            t = (z - knots_a[i]) / (knots_a[i + 1] - knots_a[i])
            return knots_b[i] + t * (knots_b[i + 1] - knots_b[i])


kleg = LB['legx'] / LA['legx']
dy = LB['ym'] - LA['ym']
print("T1408 kleg", round(kleg, 3), "dy", round(dy, 3))


def lado_de(n):
    n = n.replace(PRE, '')
    return 'Left' if n.startswith('Left') else ('Right' if n.startswith('Right') else None)


def es_brazo(n):
    n = n.replace(PRE, '')
    return any(k in n for k in ('Arm', 'Hand', 'Shoulder'))


def mapx_brazo(ax, lado):
    """|x| del brazo de a -> |x| del brazo de b, por tramos (hombro, codo, muneca, punta)."""
    m = BM[lado]
    kb = [0.0, abs(piv[lado].x), m['elb'], m['wri']]
    ka = [0.0, a_piv.x, a_elb, a_wri]
    if ax <= ka[3]:
        for i in range(3):
            if ax <= ka[i + 1] or i == 2:
                t = (ax - ka[i]) / (ka[i + 1] - ka[i])
                return kb[i] + t * (kb[i + 1] - kb[i])
    s = (m['tip'] - m['wri']) / (a_tip - a_wri)
    return m['wri'] + (ax - a_wri) * s


def mapv(v, n):
    lado = lado_de(n)
    if es_brazo(n):
        sx = 1 if v.x >= 0 else -1
        ax = abs(v.x)
        s_mano = (BM[lado]['tip'] - BM[lado]['wri']) / (a_tip - a_wri)
        pv = piv[lado]
        esdedo = 'HandIndex' in n or 'HandMiddle' in n or 'HandRing' in n or 'HandPinky' in n or 'HandThumb' in n
        if esdedo:
            f = 1.0 if (ax > a_wri) else 1.0
            return Vector((sx * mapx_brazo(ax, lado), CF[lado][0] + (v.y - a_piv.y) * s_mano, CF[lado][1] + (v.z - a_piv.z) * s_mano))
        # Shoulder/Arm/ForeArm/Hand: sobre la linea del brazo (recta)
        if 'Shoulder' in n:
            return Vector((sx * mapx_brazo(ax, lado), v.y * 0.0 + (dy + v.y), mapz(v.z)))
        return Vector((sx * mapx_brazo(ax, lado), pv.y, pv.z))
    x = v.x
    if lado is not None:  # piernas
        x = v.x * kleg
    return Vector((x, v.y + dy, mapz(v.z)))


# ---------------------------------------------------------------- esqueleto nuevo
arm = aa.copy()
arm.data = aa.data.copy()
arm.name = "E16_RobotPesadoNova_b_rig"
bpy.context.scene.collection.objects.link(arm)
L._select_only([arm])
bpy.ops.object.mode_set(mode='EDIT')
eb = arm.data.edit_bones
for b in eb:
    b.use_connect = False
nuevo = {}
for b in eb:
    nuevo[b.name] = (mapv(AB[b.name][0], b.name), mapv(AB[b.name][1], b.name))
for b in eb:
    b.head, b.tail = nuevo[b.name]
# piernas rectas (vertical, sin inclinacion en x ni y): cada lado cae del hueso de la cadera
for lado in ('Left', 'Right'):
    up, lg, ft, tb = (eb[PRE + lado + n] for n in ('UpLeg', 'Leg', 'Foot', 'ToeBase'))
    xm = (up.head.x + lg.head.x + ft.head.x) / 3
    ym = (up.head.y + lg.head.y + ft.head.y) / 3
    for b in (lg, ft, tb):
        dx = xm - (ft.head.x if b in (ft, tb) else lg.head.x) if b is not tb else dx
        dyy = ym - (ft.head.y if b in (ft, tb) else lg.head.y) if b is not tb else dyy
        b.head.x += dx; b.tail.x += dx; b.head.y += dyy; b.tail.y += dyy
    up.head.x = xm; up.head.y = ym
    up.tail = Vector((xm, ym, lg.head.z)); lg.head = Vector((xm, ym, lg.head.z))
    lg.tail = Vector((xm, ym, ft.head.z)); ft.head = Vector((xm, ym, ft.head.z))
    ft.tail = tb.head.copy()
# simetria del centro: Hips..Head sobre x=0 (la plantilla ya casi lo esta)
for n in ('Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head'):
    b = eb[PRE + n]
    b.head.x = 0.0
    b.tail.x = 0.0
for n in ('LeftHand', 'LeftHandIndex1', 'LeftHandIndex3', 'RightHand', 'RightHandIndex1'):
    print("T1408 DBG", n, "a", [round(x, 3) for x in AB[PRE + n][0]], [round(x, 3) for x in AB[PRE + n][1]], "b", [round(x, 3) for x in eb[PRE + n].head], [round(x, 3) for x in eb[PRE + n].tail])
bpy.ops.object.mode_set(mode='OBJECT')
# quitar el esqueleto original de b y la referencia a
bpy.data.objects.remove(arm0, do_unlink=True)
bpy.data.objects.remove(am, do_unlink=True)
bpy.data.objects.remove(aa, do_unlink=True)

# ---------------------------------------------------------------- pesos
names = set(g.name for g in me.vertex_groups)
for b in arm.data.bones:
    if b.name not in names:
        me.vertex_groups.new(name=b.name)
for g in list(me.vertex_groups):
    if g.name not in arm.data.bones:
        me.vertex_groups.remove(g)
for m in list(me.modifiers):
    me.modifiers.remove(m)
m = me.modifiers.new("Armature", 'ARMATURE')
m.object = arm
me.parent = arm
me.matrix_parent_inverse = arm.matrix_world.inverted()
L.limitar_pesos(me, 4)
print("T1408 PESOS", L.pesos_info(me, arm))
bpy.context.view_layer.update()

pref = os.path.join(carpeta, "T1408_E16_b")
L.vistas(me, arm, pref + "_reposo", ('frente', 'lado'), esq=True, xray=True)
import t1408_validar as VAL
VAL.validar(me, arm, pref, carpeta)
if exportar:
    L.exportar(me, arm, os.path.join(L.OUT, "E16_RobotPesadoNova_b.fbx"))
    print("T1408 EXPORT")
