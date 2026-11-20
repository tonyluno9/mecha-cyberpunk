
import bpy, math
from mathutils import Vector, Matrix
import cc_tools as C
from cc_walk import Wp, Rw

def ss(x): x = max(0.0, min(1.0, x)); return x * x * (3 - 2 * x)
def bl(P, Q, w): return {n: (P[n][0].slerp(Q[n][0], w), P[n][1].lerp(Q[n][1], w)) for n in P}

def keys_interp(keys, t):
    """keys: [(t, v)] con t en [0,1], periódico; interpolación suave."""
    for i in range(len(keys) - 1):
        t0, v0 = keys[i]; t1, v1 = keys[i + 1]
        if t0 <= t <= t1:
            return v0 + (v1 - v0) * ss((t - t0) / (t1 - t0) if t1 > t0 else 0)
    return keys[-1][1]

def make_idle(src, out, P, T=None):
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    L = P['len']; X = P['xfade']; s0 = X + 1
    C.set_action(src)
    cache = {}
    def pose_at(s):
        if s not in cache:
            C.set_action(src); sc.frame_set(s); cache[s] = C.get_pose_dict(T)
        return cache[s]
    poses = []
    for k in range(1, L + 1):
        s = k + X
        base = pose_at(s)
        if s > L:
            base = bl(base, pose_at(s - L), ss((s - L) / X))
        T.animation_data.action = None
        C.apply_pose_dict(base, T)
        t = (k - 1) / L
        # feet targets (narrower stance, toes out)
        orig = {}
        for sd in ('L', 'R'):
            a = Wp(T, sd + '_Foot').copy(); kn = Wp(T, sd + '_Calf').copy()
            a.x = P['footx'][sd]
            rz = Matrix.Rotation(math.radians(P['toe_out'] * (1 if sd == 'L' else -1)), 3, 'Z')
            orig[sd] = (a, kn, rz @ Rw(T, sd + '_Foot'), rz @ Rw(T, sd + '_ToeBase'))
        head0 = Rw(T, 'Head')
        br = math.sin(2 * math.pi * P['breaths'] * t)
        sway = P['shift'] + P['shift_var'] * math.sin(2 * math.pi * t + 0.6)
        h = C.hip_world(T)
        C.move_hip_world(h + Vector((sway, 0, -P['drop'] + 0.004 * br)), T)
        roll = P['roll'] + P['roll_var'] * math.sin(2 * math.pi * t + 0.6)
        C.rot_world_about('Hip', roll, 'Y', None, T)
        C.rot_world_about('Waist', -roll * 0.6, 'Y', None, T)
        C.rot_world_about('Spine01', -roll * 0.5, 'Y', None, T)
        if P.get('chest'): C.rot_world_about('Spine02', P['chest'], 'X', None, T)
        # breathing
        C.rot_world_about('Spine01', -P['breath'] * 0.4 * br, 'X', None, T)
        C.rot_world_about('Spine02', -P['breath'] * 0.6 * br, 'X', None, T)
        C.rot_world_about('L_Clavicle', -P['clav'] * br, 'Y', None, T)
        C.rot_world_about('R_Clavicle', P['clav'] * br, 'Y', None, T)
        # head: keep level then gaze
        Rn = Rw(T, 'Head'); q = Rn.to_quaternion().slerp(head0.to_quaternion(), 0.6)
        C.set_bone_world_rot('Head', q.to_matrix(), T)
        yaw = keys_interp(P['gaze_yaw'], t); pit = keys_interp(P['gaze_pitch'], t)
        C.rot_world_about('NeckTwist01', yaw * 0.4, 'Z', None, T)
        C.rot_world_about('Head', yaw * 0.6, 'Z', None, T)
        C.rot_world_about('Head', pit, 'X', None, T)
        # arms relaxed a bit closer + tiny swing with breathing
        for sd, sg in (('L', -1), ('R', 1)):
            C.rot_world_about(sd + '_Upperarm', -sg * P['arm_in'], 'Y', None, T)
            C.rot_world_about(sd + '_Upperarm', P.get('arm_back', 0.0), 'X', None, T)
            C.rot_world_about(sd + '_Forearm', -P['elbow'], 'X', None, T)
            if P.get('wrist'): C.rot_world_about(sd + '_Hand', P['wrist'], 'X', None, T)
        # legs to targets
        for sd in ('L', 'R'):
            a, kn, rf, rt = orig[sd]
            hp = Wp(T, sd + '_Thigh')
            pole = kn + Vector((0, -0.4, 0.0))
            C.two_bone_ik(sd + '_Thigh', sd + '_Calf', sd + '_Foot', a, pole, T)
            C.set_bone_world_rot(sd + '_Foot', rf, T); C.set_bone_world_rot(sd + '_ToeBase', rt, T)
        for pb in T.pose.bones: pb.scale = (1, 1, 1)
        poses.append(C.get_pose_dict(T))
    poses.append(poses[0])
    C.new_action(out, T)
    for i, Pd in enumerate(poses):
        C.apply_pose_dict(Pd, T); C.key_pose(i + 1, T)
    return bpy.data.actions[out]


def relax_idle(src, out, k=0.5, stretch=2, head_still=True, T=None,
               keep=('Thigh','Calf','Foot','ToeBase','Thumb','Index','Mid','Ring','Pinky')):
    """Slow an idle loop by `stretch`, scale motion around its mean by k, keep head still, re-pin feet."""
    from mathutils import Quaternion
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    A = bpy.data.actions[src]
    f0, f1 = [int(round(x)) for x in A.frame_range]
    n = (f1 - f0) * stretch + 1
    D = []; feet = []; headR = []
    for g in range(n):
        s = f0 + g / stretch
        C.set_action(src); sc.frame_set(int(s) + 1); sc.frame_set(int(s), subframe=s - int(s))
        D.append(C.get_pose_dict(T))
        feet.append({sd: (Wp(T, sd + '_Foot').copy(), Wp(T, sd + '_Calf').copy(), Rw(T, sd + '_Foot').copy(), Rw(T, sd + '_ToeBase').copy()) for sd in 'LR'})
        headR.append(Rw(T, 'Head').to_quaternion())
    names = list(D[0].keys())
    rel = [nm for nm in names if not any(kk in nm for kk in keep)]
    for nm in rel:
        qs = [d[nm][0].copy() for d in D]
        for i in range(1, len(qs)):
            if qs[i].dot(qs[i - 1]) < 0: qs[i] = -qs[i]
        m = Quaternion((sum(q.w for q in qs), sum(q.x for q in qs), sum(q.y for q in qs), sum(q.z for q in qs))).normalized()
        ls = [d[nm][1].copy() for d in D]
        lm = sum(ls, Vector()) / len(ls)
        for i, d in enumerate(D):
            q = m.slerp(qs[i], k) if nm not in ('Head', 'Neck', 'NeckTwist01', 'NeckTwist02') or not head_still else m.slerp(qs[i], k)
            d[nm] = (q, lm.lerp(ls[i], k))
    # head world target: mean of original head world rotation
    if head_still:
        hq = [q.copy() for q in headR]
        for i in range(1, len(hq)):
            if hq[i].dot(hq[i - 1]) < 0: hq[i] = -hq[i]
        HQ = Quaternion((sum(q.w for q in hq), sum(q.x for q in hq), sum(q.y for q in hq), sum(q.z for q in hq))).normalized()
    poses = []
    T.animation_data.action = None
    for i, d in enumerate(D):
        C.apply_pose_dict(d, T); bpy.context.view_layer.update()
        if head_still:
            C.set_bone_world_rot('Head', HQ.to_matrix(), T)
        for sd in ('L', 'R'):
            a, kn, rf, rt = feet[i][sd]
            pole = kn + Vector((0, -0.4, 0.0))
            C.two_bone_ik(sd + '_Thigh', sd + '_Calf', sd + '_Foot', a, pole, T)
            C.set_bone_world_rot(sd + '_Foot', rf, T); C.set_bone_world_rot(sd + '_ToeBase', rt, T)
        for pb in T.pose.bones: pb.scale = (1, 1, 1)
        poses.append(C.get_pose_dict(T))
    poses[-1] = poses[0]
    C.new_action(out, T)
    for i, Pd in enumerate(poses):
        C.apply_pose_dict(Pd, T); C.key_pose(i + 1, T)
    return bpy.data.actions[out]
