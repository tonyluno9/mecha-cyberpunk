
import bpy, math
from mathutils import Vector
import cc_tools as C
import cc_climb as CL
from cc_walk import Wp, Rw

GUARD = Vector((-0.041, -0.268, 0.005)); GUARD_AIM = Vector((0.045, -0.365, -0.01))

def pose(T, base, hip, feet, lean=0.0, head=0.0, extra=None, knee_poles=None):
    """base: pose dict; feet: {'L':(ankle,toe), 'R':(ankle,toe)}; extra: fn(T) for recoil etc."""
    C.apply_pose_dict(base, T)
    C.move_hip_world(hip, T)
    if lean: C.rot_world_about('Waist', lean * 0.5, 'X', None, T); C.rot_world_about('Spine01', lean * 0.5, 'X', None, T)
    if head: C.rot_world_about('Head', head, 'X', None, T)
    if extra: extra(T)
    for s, sx in (('L', 1), ('R', -1)):
        ank, toe = feet[s]
        th = Wp(T, s + '_Thigh')
        pole = knee_poles[s] if knee_poles and s in knee_poles else (th + ank) * 0.5 + Vector((sx * 0.1, -0.6, 0.2))
        C.two_bone_ik(s + '_Thigh', s + '_Calf', s + '_Foot', ank, pole, T)
        C.aim_bone(s + '_Foot', toe, T)
    bpy.context.view_layer.update()
    w = bpy.data.objects['REVW03_Root']
    tgt = w.matrix_world @ GUARD; aim = w.matrix_world @ GUARD_AIM
    sh = Wp(T, 'L_Upperarm')
    C.two_bone_ik('L_Upperarm', 'L_Forearm', 'L_Hand', tgt, sh + Vector((0.35, 0.1, -0.5)), T)
    C.aim_bone('L_Hand', aim, T)
    for pb in T.pose.bones: pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    return C.get_pose_dict(T)
