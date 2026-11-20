
import bpy, math
from mathutils import Vector, Matrix, Quaternion
import cc_tools as C

def Wp(T,b): return T.matrix_world @ T.pose.bones[b].head
def Rw(T,b): return (T.matrix_world @ T.pose.bones[b].matrix).to_3x3().normalized()

def heel_strike(src, side='L', T=None):
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    a = C.set_action(src); f0, f1 = [int(x) for x in a.frame_range]
    best = None
    for f in range(f0, f1):
        sc.frame_set(f); d = (Wp(T, side+'_Foot') - C.hip_world(T)).y
        if best is None or d < best[1]: best = (f, d)
    return best[0]

def pin_legs(T, orig):
    for s in ('L','R'):
        ank, knee, rf, rt = orig[s]
        hip = Wp(T, s+'_Thigh')
        pole = knee + (knee - (hip + ank) * 0.5).normalized() * 0.3
        C.two_bone_ik(s+'_Thigh', s+'_Calf', s+'_Foot', ank, pole, T)
        C.set_bone_world_rot(s+'_Foot', rf, T)
        C.set_bone_world_rot(s+'_ToeBase', rt, T)

def stylize(src, out, P, T=None):
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    a = C.set_action(src); f0, f1 = [int(x) for x in a.frame_range]
    N = f1 - f0           # cycle length (last frame == first)
    fL = P.get('fL') or heel_strike(src, 'L', T)
    C.set_action(src)
    # first pass: arm pitch mean
    pitch = {'L': [], 'R': []}
    for f in range(f0, f1):
        sc.frame_set(f)
        for s in ('L','R'):
            v = Wp(T, s+'_Forearm') - Wp(T, s+'_Upperarm')
            pitch[s].append(math.degrees(math.atan2(-v.y, -v.z)))
    mean = {s: sum(pitch[s]) / len(pitch[s]) for s in pitch}
    poses = []
    for f in range(f0, f1 + 1):
        C.set_action(src); sc.frame_set(f)
        T.animation_data.action = None
        ph = 2 * math.pi * ((f - fL) % N) / N
        orig = {}
        for s in ('L','R'):
            orig[s] = (Wp(T, s+'_Foot').copy(), Wp(T, s+'_Calf').copy(), Rw(T, s+'_Foot'), Rw(T, s+'_ToeBase'))
        head0 = Rw(T, 'Head')
        hip = C.hip_world(T)
        # hip sway (toward stance leg), roll and yaw
        C.move_hip_world(hip + Vector((P['sway'] * math.sin(ph), 0, P.get('hip_up', 0.0))), T)
        roll = -P['roll'] * math.sin(ph); yaw = -P['yaw'] * math.cos(ph)
        C.rot_world_about('Hip', yaw, 'Z', None, T)
        C.rot_world_about('Hip', roll, 'Y', None, T)
        # torso: counter roll + shoulders counter yaw + chest up
        C.rot_world_about('Waist', -roll * P['croll'], 'Y', None, T)
        tot = -yaw + P['syaw'] * math.cos(ph)
        C.rot_world_about('Spine01', tot * 0.5, 'Z', None, T)
        C.rot_world_about('Spine02', tot * 0.5, 'Z', None, T)
        if P.get('chest'): C.rot_world_about('Spine02', P['chest'], 'X', None, T)
        # arms: reduce swing, relaxed elbows, closer to body
        k = (f - f0) % N
        for s, sg in (('L', -1), ('R', 1)):
            d = pitch[s][k] - mean[s]
            C.rot_world_about(s+'_Upperarm', d * P['swing_cut'], 'X', None, T)
            if P.get('add'): C.rot_world_about(s+'_Upperarm', -sg * P['add'], 'Y', None, T)
            if P.get('fwd_abd'): C.rot_world_about(s+'_Upperarm', sg * P['fwd_abd'] * max(0.0, d), 'Y', None, T)
            C.rot_world_about(s+'_Forearm', -(P['elbow'] + max(0.0, d) * P['elbow_fwd']), 'X', None, T)
        # head stable
        Rn = Rw(T, 'Head')
        q = Rn.to_quaternion().slerp(head0.to_quaternion(), P['head_keep'])
        C.set_bone_world_rot('Head', q.to_matrix(), T)
        pin_legs(T, orig)
        for pb in T.pose.bones: pb.scale = (1, 1, 1)
        poses.append(C.get_pose_dict(T))
    C.new_action(out, T)
    for i, Pd in enumerate(poses):
        C.apply_pose_dict(Pd, T); C.key_pose(i + 1, T)
    return bpy.data.actions[out], fL
