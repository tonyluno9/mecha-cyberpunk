
import bpy, math
from mathutils import Vector, Matrix, Quaternion
import cc_tools as C
from cc_walk import Wp, Rw

FINGERS = ('Thumb', 'Index', 'Mid', 'Ring', 'Pinky')
def ss(x): x = max(0.0, min(1.0, x)); return x * x * (3 - 2 * x)
def bl(P, Q, w, only=None):
    out = {}
    for n in P:
        if only is None or only(n):
            out[n] = (P[n][0].slerp(Q[n][0], w), P[n][1].lerp(Q[n][1], w))
        else:
            out[n] = P[n]
    return out

def pose_of(act, f, T=None):
    T = T or bpy.data.objects['PJ01']; C.set_action(act); bpy.context.scene.frame_set(f)
    d = C.get_pose_dict(T); T.animation_data.action = None; return d

def track(keys, f):
    """keys: [(frame, Vector)], interpolación suave por tramos; arco opcional (frame, Vector, arc_vec)."""
    if f <= keys[0][0]: return Vector(keys[0][1])
    for i in range(len(keys) - 1):
        f0, p0 = keys[i][0], Vector(keys[i][1]); f1, p1 = keys[i + 1][0], Vector(keys[i + 1][1])
        if f0 <= f <= f1:
            s = ss((f - f0) / (f1 - f0)) if f1 > f0 else 1.0
            p = p0.lerp(p1, s)
            if len(keys[i + 1]) > 2 and keys[i + 1][2] is not None:
                p = p + Vector(keys[i + 1][2]) * math.sin(math.pi * s)
            return p
    return Vector(keys[-1][1])

def scalar(keys, f):
    if f <= keys[0][0]: return keys[0][1]
    for i in range(len(keys) - 1):
        f0, v0 = keys[i]; f1, v1 = keys[i + 1]
        if f0 <= f <= f1:
            return v0 + (v1 - v0) * (ss((f - f0) / (f1 - f0)) if f1 > f0 else 1)
    return keys[-1][1]

def solve(T, base, hip, lean, head_pitch, limbs, grip, gripdict, sway_roll=0.0):
    """limbs: {'LH':(wrist, aimpt), 'RH':..., 'LF':(ankle, toept), 'RF':...}; grip: {'L':w,'R':w}"""
    C.apply_pose_dict(base, T)
    C.move_hip_world(hip, T)
    if sway_roll: C.rot_world_about('Hip', sway_roll, 'Y', None, T)
    if lean: C.rot_world_about('Waist', lean * 0.5, 'X', None, T); C.rot_world_about('Spine01', lean * 0.5, 'X', None, T)
    if head_pitch: C.rot_world_about('Head', head_pitch, 'X', None, T)
    for s, sx in (('L', 1), ('R', -1)):
        if (s + 'F') in limbs and limbs[s + 'F'] is not None:
            ank, toe = limbs[s + 'F']
            th = Wp(T, s + '_Thigh')
            pole = (th + ank) * 0.5 + Vector((sx * 0.12, -0.6, 0.25))
            C.two_bone_ik(s + '_Thigh', s + '_Calf', s + '_Foot', ank, pole, T)
            C.aim_bone(s + '_Foot', toe, T)
        if (s + 'H') in limbs and limbs[s + 'H'] is not None:
            wr, aim = limbs[s + 'H']
            sh = Wp(T, s + '_Upperarm')
            pole = sh + Vector((sx * 0.4, 0.25, -0.55))
            C.two_bone_ik(s + '_Upperarm', s + '_Forearm', s + '_Hand', wr, pole, T)
            if aim is not None: C.aim_bone(s + '_Hand', aim, T)
    # fingers
    for s in ('L', 'R'):
        w = grip.get(s, 0.0)
        if w > 0:
            for pb in T.pose.bones:
                if pb.name.startswith(s + '_') and any(k in pb.name for k in FINGERS):
                    pb.rotation_quaternion = pb.rotation_quaternion.slerp(gripdict[pb.name][0], w)
    for pb in T.pose.bones: pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    return C.get_pose_dict(T)

def bake(name, poses, T=None):
    T = T or bpy.data.objects['PJ01']
    C.new_action(name, T)
    for i, Pd in enumerate(poses):
        C.apply_pose_dict(Pd, T); C.key_pose(i + 1, T)
    T.animation_data.action = None
    return bpy.data.actions[name]

def reverse(src, out, T=None):
    T = T or bpy.data.objects['PJ01']
    a = bpy.data.actions[src]; f0, f1 = [int(x) for x in a.frame_range]
    poses = [pose_of(src, f, T) for f in range(f1, f0 - 1, -1)]
    return bake(out, poses, T)
