"""T-1408: compara el esqueleto de E16_a y E16_b exportados (nombres, jerarquia, longitudes, escalas)."""
import bpy, sys, os
from mathutils import Vector
WT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
P = os.path.join(WT, "imageassets", "blender", "personajes")


def cargar(n):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(P, n), ignore_leaf_bones=False, automatic_bone_orientation=False)
    ar = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    me = [o for o in bpy.data.objects if o.type == 'MESH']
    d = {}
    for b in ar[0].data.bones:
        d[b.name] = dict(par=b.parent.name if b.parent else None, h=Vector(b.head_local), t=Vector(b.tail_local), L=b.length)
    info = dict(arm_scale=tuple(ar[0].scale), arm_rot=tuple(ar[0].rotation_euler), n_arm=len(ar),
                meshes=[(m.name, tuple(m.scale), tuple(m.rotation_euler), len(m.vertex_groups)) for m in me],
                parent=[(o.name, o.parent.name if o.parent else None) for o in bpy.data.objects])
    return d, info


A, ia = cargar("E16_RobotPesadoNova_a.fbx")
B, ib = cargar("E16_RobotPesadoNova_b.fbx")
print("INFO a", ia)
print("INFO b", ib)
print("solo en a", sorted(set(A) - set(B)))
print("solo en b", sorted(set(B) - set(A)))
for n in A:
    if n in B:
        a, b = A[n], B[n]
        flag = ""
        if a['par'] != b['par']:
            flag += " PADRE %s vs %s" % (a['par'], b['par'])
        print("CMP %-28s La=%.3f Lb=%.3f h_a=%s h_b=%s%s" % (n, a['L'], b['L'], tuple(round(x, 2) for x in a['h']), tuple(round(x, 2) for x in b['h']), flag))
for n in ('mixamorig:LeftArm', 'mixamorig:LeftForeArm', 'mixamorig:LeftHand', 'mixamorig:RightArm', 'mixamorig:RightForeArm', 'mixamorig:RightHand'):
    for k, D in (('a', A), ('b', B)):
        d = (D[n]['t'] - D[n]['h']).normalized()
        print("DIR", k, n, tuple(round(x, 2) for x in d))
