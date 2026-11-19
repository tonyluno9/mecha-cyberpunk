"""T-1408: validacion de requisitos de Humanoid de Unity + prueba de pose/caminar. Se usa desde t1408_rig.py
y como script suelto sobre un FBX: blender -b --python t1408_validar.py -- <fbx> <carpeta>"""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1381_lib as L
import bpy
from mathutils import Vector

PRE = L.PRE
OBLIG = ['Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head',
         'LeftShoulder', 'LeftArm', 'LeftForeArm', 'LeftHand', 'RightShoulder', 'RightArm', 'RightForeArm', 'RightHand',
         'LeftUpLeg', 'LeftLeg', 'LeftFoot', 'LeftToeBase', 'RightUpLeg', 'RightLeg', 'RightFoot', 'RightToeBase']
# padre esperado (jerarquia Humanoid)
PADRE = {'Spine': 'Hips', 'Spine1': 'Spine', 'Spine2': 'Spine1', 'Neck': 'Spine2', 'Head': 'Neck',
         'LeftShoulder': 'Spine2', 'LeftArm': 'LeftShoulder', 'LeftForeArm': 'LeftArm', 'LeftHand': 'LeftForeArm',
         'RightShoulder': 'Spine2', 'RightArm': 'RightShoulder', 'RightForeArm': 'RightArm', 'RightHand': 'RightForeArm',
         'LeftUpLeg': 'Hips', 'LeftLeg': 'LeftUpLeg', 'LeftFoot': 'LeftLeg', 'LeftToeBase': 'LeftFoot',
         'RightUpLeg': 'Hips', 'RightLeg': 'RightUpLeg', 'RightFoot': 'RightLeg', 'RightToeBase': 'RightFoot'}


def comprobar(me, arm):
    """Devuelve (lista_de_fallos, lista_de_avisos). Imprime cada comprobacion."""
    fallos, avisos = [], []
    B = arm.data.bones

    def chk(ok, msg, fatal=True):
        print("VAL", "OK  " if ok else ("FALLA" if fatal else "AVISO"), msg)
        if not ok:
            (fallos if fatal else avisos).append(msg)
    # 1 jerarquia
    for n in OBLIG:
        chk((PRE + n) in B, "hueso obligatorio " + n)
    for h, p in PADRE.items():
        if (PRE + h) in B:
            chk(B[PRE + h].parent is not None and B[PRE + h].parent.name == PRE + p, "padre de %s = %s" % (h, p))
    raices = [b.name for b in B if b.parent is None]
    chk(raices == [PRE + 'Hips'], "una sola raiz (Hips): %s" % raices)
    chk(len(B) == 52, "52 huesos (hay %d)" % len(B))
    # 2 escala/rotacion de objetos
    for o in (arm, me):
        chk(all(abs(s - 1) < 1e-5 for s in o.scale), "%s escala 1 %s" % (o.name, tuple(round(s, 4) for s in o.scale)))
        chk(all(abs(r) < 1e-4 for r in o.rotation_euler), "%s sin rotacion %s" % (o.name, tuple(round(r, 4) for r in o.rotation_euler)))
    # 3 longitud cero / duplicados
    cortos = [b.name for b in B if b.length < 1e-3]
    chk(not cortos, "ningun hueso de longitud cero %s" % cortos)
    pos = {}
    dup = []
    for b in B:
        k = tuple(round(x, 4) for x in b.head_local)
        if k in pos:
            dup.append((b.name, pos[k]))
        pos[k] = b.name
    chk(not dup, "ninguna cabeza duplicada %s" % dup)
    # 4 cada hijo en el extremo/dentro de su cadena: la cabeza del hijo no esta a mas de 0.35 m de la cola del padre
    lejos = []
    for b in B:
        if b.parent and b.name.replace(PRE, '') not in ('LeftUpLeg', 'RightUpLeg', 'LeftShoulder', 'RightShoulder', 'Spine'):
            d = (b.head_local - b.parent.tail_local).length
            if d > 0.35 * max(b.parent.length, 0.05) + 0.03:
                lejos.append((b.name, round(d, 3)))
    chk(not lejos, "hijo pegado a la cola del padre %s" % lejos, fatal=False)
    # 5 T-pose: brazos horizontales, piernas rectas
    for lado in ('Left', 'Right'):
        for n in ('Arm', 'ForeArm', 'Hand'):
            b = B[PRE + lado + n]
            d = (b.tail_local - b.head_local).normalized()
            ang = math.degrees(math.asin(max(-1, min(1, d.z))))
            chk(abs(ang) < 8 and abs(d.x) > 0.95, "%s%s horizontal (%.1f grados)" % (lado, n, ang))
        up, lg, ft = B[PRE + lado + 'UpLeg'], B[PRE + lado + 'Leg'], B[PRE + lado + 'Foot']
        for nm, b in (('UpLeg', up), ('Leg', lg)):
            d = (b.tail_local - b.head_local).normalized()
            ang = math.degrees(math.acos(max(-1, min(1, -d.z))))
            chk(ang < 8, "%s%s vertical (%.1f grados)" % (lado, nm, ang))
        chk(ft.head_local.z > 0.05 and ft.head_local.z < 0.4, "%sFoot a altura razonable %.2f" % (lado, ft.head_local.z))
        tb = B[PRE + lado + 'ToeBase']
        chk(tb.head_local.y < ft.head_local.y, "%s dedo hacia -Y (frente)" % lado)
    # 6 simetria
    for n in ('Arm', 'ForeArm', 'Hand', 'UpLeg', 'Leg', 'Foot'):
        l, r = B[PRE + 'Left' + n], B[PRE + 'Right' + n]
        chk(abs(l.head_local.x + r.head_local.x) < 0.05 and abs(l.head_local.z - r.head_local.z) < 0.05, "simetria %s (L %.2f R %.2f)" % (n, l.head_local.x, r.head_local.x), fatal=False)
    # 7 orden vertical de la columna
    z = [B[PRE + n].head_local.z for n in ('Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head')]
    chk(all(z[i] < z[i + 1] for i in range(5)), "columna ascendente %s" % [round(v, 2) for v in z])
    ent = B[PRE + 'LeftUpLeg'].head_local.z
    chk(B[PRE + 'LeftLeg'].head_local.z < ent < B[PRE + 'Hips'].head_local.z + 0.05, "piernas bajo la cadera")
    # 8 huesos dentro de la malla (bbox)
    mn, mx = L.bbox(me)
    fuera = [b.name for b in B if not (mn.z - 0.05 <= b.head_local.z <= mx.z + 0.05 and mn.x - 0.05 <= b.head_local.x <= mx.x + 0.05)]
    chk(not fuera, "huesos dentro de la malla %s" % fuera)
    # 9 pesos
    pi = L.pesos_info(me, arm)
    chk(pi['sin_peso'] == 0, "vertices sin peso %d de %d" % (pi['sin_peso'], pi['verts']))
    chk(pi['mas4'] == 0, "vertices con mas de 4 huesos %d" % pi['mas4'])
    chk(pi['grupos_no_hueso'] == 0, "grupos que no son hueso %d" % pi['grupos_no_hueso'])
    return fallos, avisos


def validar(me, arm, pref, carpeta):
    fallos, avisos = comprobar(me, arm)
    print("T1408 VALIDACION estructura: %d fallos, %d avisos" % (len(fallos), len(avisos)), fallos)
    # poses de prueba y caminar
    for tipo in ('brazos_abajo', 'paso', 'sentadilla'):
        L.pose_prueba(arm, tipo)
        L.vistas(me, arm, pref + "_" + tipo, ('frente', 'lado'))
    src = L.cargar_fuente(L.WALK_FBX)
    for fr in (6, 14, 24):
        L.pose_de_clip(src, arm, fr)
        L.vistas(me, arm, pref + "_caminar%02d" % fr, ('frente', 'lado'))
    bpy.data.objects.remove(src, do_unlink=True)
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    bpy.context.view_layer.update()
    return fallos


if __name__ == "__main__":
    a = sys.argv[sys.argv.index('--') + 1:]
    L.reset()
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=a[0], ignore_leaf_bones=False, automatic_bone_orientation=False)
    new = [o for o in bpy.data.objects if o not in before]
    me = [o for o in new if o.type == 'MESH'][0]
    arm = [o for o in new if o.type == 'ARMATURE'][0]
    f, av = comprobar(me, arm)
    print("T1408 REIMPORT: %d fallos, %d avisos" % (len(f), len(av)), f)
