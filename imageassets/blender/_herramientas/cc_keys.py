
"""cc_keys.py — animacion pose-a-pose por canales para PJ01 (saltos, golpes, reacciones...).
Interpolacion Catmull-Rom (velocidad continua) + retraso por grupo (superposicion / seguimiento)."""
import bpy, math
from mathutils import Vector, Matrix, Quaternion
import cc_tools as C
import cc_gait as G

UP = Vector((0, 0, 1)); FWD = Vector((0, -1, 0)); LEFT = Vector((1, 0, 0))
CH = ['hx', 'hy', 'hz', 'yaw', 'roll', 'tilt', 'lean', 'twist', 'side', 'hp', 'hyw', 'hr',
      'Lx', 'Ly', 'Lz', 'Lp', 'Lt', 'Lk', 'Lyw', 'Rx', 'Ry', 'Rz', 'Rp', 'Rt', 'Rk', 'Ryw',
      'Lsh', 'Lab', 'Ltw', 'Lel', 'Lcl', 'Lhd', 'Lfi', 'Rsh', 'Rab', 'Rtw', 'Rel', 'Rcl', 'Rhd', 'Rfi']
GROUP = {'spine': ['lean', 'twist', 'side'], 'head': ['hp', 'hyw', 'hr'],
         'arm': ['Lsh', 'Lab', 'Ltw', 'Lcl', 'Rsh', 'Rab', 'Rtw', 'Rcl'],
         'fore': ['Lel', 'Rel'], 'hand': ['Lhd', 'Rhd', 'Lfi', 'Rfi']}

def neutral(**kw):
    d = {c: 0.0 for c in CH}
    d.update(hz=0.0, Lx=0.10, Rx=-0.10, Ly=-0.02, Ry=-0.02, Lz=0.0, Rz=0.0, Lel=10, Rel=10, Lfi=0.15, Rfi=0.15)
    d.update(kw); return d

def P(base, **kw):
    d = dict(base); d.update(kw); return d

def cr(p0, p1, p2, p3, t):
    t2 = t * t; t3 = t2 * t
    return 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)

def sample(keys, f, ch):
    """keys: lista ordenada de (frame, dict). Catmull-Rom con extremos sostenidos."""
    fs = [k[0] for k in keys]
    if f <= fs[0]: return keys[0][1][ch]
    if f >= fs[-1]: return keys[-1][1][ch]
    i = max(j for j in range(len(fs)) if fs[j] <= f)
    f1, f2 = fs[i], fs[i + 1]
    t = (f - f1) / (f2 - f1)
    v1 = keys[i][1][ch]; v2 = keys[i + 1][1][ch]
    v0 = keys[i - 1][1][ch] if i > 0 else v1
    v3 = keys[i + 2][1][ch] if i + 2 < len(keys) else v2
    # tangentes escaladas por espaciado desigual
    return cr(v0, v1, v2, v3, t)

def apply(p, ref, fingers):
    T = G.T_()
    C.apply_pose_dict(ref['pose'], T)
    hip0 = ref['hip']
    G.rot_about('Hip', G.rot('Z', p['yaw'])); G.rot_about('Hip', G.rot('Y', p['roll'])); G.rot_about('Hip', G.rot('X', p['tilt']))
    C.move_hip_world(Vector((hip0.x + p['hx'], hip0.y + p['hy'], ref['hipz'] + p['hz'])), T)
    for b, k in zip(G.SPINE, (0.3, 0.35, 0.35)):
        G.rot_about(b, G.rot('Z', (p['twist'] - p['yaw']) * k))
        G.rot_about(b, G.rot('Y', (p['side'] - p['roll']) * k))
        G.rot_about(b, G.rot('X', (p['lean'] - p['tilt']) * k))
    # cabeza: relativa al pecho (sigue al cuerpo) + ajuste
    Dch = G.Rw('Spine02') @ ref['R']['Spine02'].inverted()
    Hd = G.rot('Z', p['hyw']) @ G.rot('X', p['hp']) @ G.rot('Y', p['hr']) @ Dch @ ref['R']['Head']
    Hc = G.Rw('Head')
    dlt = (Hd @ Hc.inverted()).to_quaternion()
    G.rot_about('NeckTwist01', Quaternion((1, 0, 0, 0)).slerp(dlt, 0.5).to_matrix())
    C.set_bone_world_rot('Head', Hd, T)
    Dch = G.Rw('Spine02') @ ref['R']['Spine02'].inverted()
    axX = Dch @ Vector((1, 0, 0)); axY = Dch @ Vector((0, 1, 0)); axZ = Dch @ Vector((0, 0, 1))
    for s, sg in (('L', 1), ('R', -1)):
        G.rot_about(s + '_Clavicle', Matrix.Rotation(math.radians(-sg * p[s + 'cl']), 3, axY))
        G.rot_about(s + '_Upperarm', Matrix.Rotation(math.radians(-sg * p[s + 'ab']), 3, axY))
        G.rot_about(s + '_Upperarm', Matrix.Rotation(math.radians(p[s + 'sh']), 3, axX))
        G.rot_about(s + '_Upperarm', Matrix.Rotation(math.radians(sg * p[s + 'tw']), 3, axZ))
        G.rot_about(s + '_Forearm', Matrix.Rotation(math.radians(-p[s + 'el']), 3, G.Rw(s + '_Upperarm') @ ref['R0'][s + '_Upperarm'].inverted() @ axX if False else axX))
        G.rot_about(s + '_Hand', Matrix.Rotation(math.radians(p[s + 'hd']), 3, axX))
        k = min(max(p[s + 'fi'], 0.0), 1.0)
        for n in G.FINGERS:
            if n.startswith(s + '_'):
                T.pose.bones[n].rotation_quaternion = fingers['open'][n].slerp(fingers['fist'][n], k)
    bpy.context.view_layer.update()
    err = 0
    for s, sg in (('L', 1), ('R', -1)):
        d = G.rot('Z', sg * 7 + p[s + 'yw']) @ FWD
        A = Vector((ref['hip'].x + p[s + 'x'], ref['hip'].y + p[s + 'y'], ref['ha'] + p[s + 'z']))
        hipj = G.Wp(s + '_Thigh')
        pole = (hipj + A) * 0.5 + d * 0.6 + LEFT * (sg * (0.08 + p[s + 'k']))
        err = max(err, C.two_bone_ik(s + '_Thigh', s + '_Calf', s + '_Foot', A, pole, T))
        Rf, Rt, Ry = G.foot_rot(ref, s, d, p[s + 'p'])
        C.set_bone_world_rot(s + '_Foot', Rf, T)
        ax = UP.cross(d).normalized()
        C.set_bone_world_rot(s + '_ToeBase', Matrix.Rotation(math.radians(-(p[s + 'p'] + p[s + 't'])), 3, ax) @ Ry @ ref['R'][s + '_ToeBase'], T)
    return err

def toes(p_deg):
    """(dy, dz) del tobillo al pararse en la bola del pie con pitch p (negativo)"""
    Q = {'ha': 0.122, 'heel': 0.055, 'ball': 0.155}
    g = Vector((0, 0, 0)); a = G.ankle_at(g, FWD, p_deg, Q) - (g + UP * 0.122)
    return a.y, a.z

def make(name, keys, N, lags=None, fingers=None, ref=None, hipz=0.93, cyclic=False, post=None):
    T = G.T_()
    lags = lags or {'spine': 1.0, 'head': 2.0, 'arm': 1.5, 'fore': 3.0, 'hand': 4.0}
    ref = ref or G.build_base(None, elbow=0.0, adduct=6.0)
    ref['hipz'] = hipz; ref['ha'] = 0.122
    if fingers is None:
        fingers = {'open': G.local_pose('Loco_Idle', 1, G.FINGERS), 'fist': G.local_pose('Punos_Guardia', 1, G.FINGERS)}
    lag_of = {}
    for g, chs in GROUP.items():
        for c in chs: lag_of[c] = lags.get(g, 0.0)
    C.new_action(name, T)
    errs = []
    for f in range(0, N + 1):
        p = {c: sample(keys, ((f - lag_of.get(c, 0.0)) % N) if cyclic else f - lag_of.get(c, 0.0), c) for c in keys[0][1].keys()}
        errs.append(apply(p, ref, fingers))
        if post: errs.append(post(f, p, ref) or 0.0)
        C.key_pose(f + 1, T)
    return dict(name=name, frames=(1, N + 1), ik_err=round(max(errs), 4))


def arm_ik(s, target, pole_off, aim=None, T=None):
    """lleva la muneca (cabeza de la mano) a target; codo hacia shoulder+pole_off; mano apuntando a aim"""
    T = T or G.T_()
    sh = G.Wp(s + '_Upperarm')
    e = C.two_bone_ik(s + '_Upperarm', s + '_Forearm', s + '_Hand', Vector(target), sh + Vector(pole_off), T)
    if aim is not None: C.aim_bone(s + '_Hand', Vector(aim), T)
    return e


def hand_post(f, p, ref):
    """IK de manos por canales: <s>ik (0/1), <s>Hx/Hy/Hz (destino de la muneca, mundo), <s>eu (codo arriba 0..1), <s>Ax/Ay/Az (hacia donde apunta la mano)"""
    e = 0.0
    for s, sg in (('L', 1), ('R', -1)):
        k = p.get(s + 'ik', 0.0)
        if k < 0.02: continue
        tgt = Vector((p[s + 'Hx'], p[s + 'Hy'], p[s + 'Hz']))
        if k < 0.999: tgt = G.Wp(s + '_Hand').lerp(tgt, k)
        eu = p.get(s + 'eu', 0.0)
        pole = (sg * (0.15 + 0.35 * eu), 0.05 * (1 - eu), -0.5 * (1 - eu) + 0.25 * eu)
        aim = Vector((p.get(s + 'Ax', tgt.x), p.get(s + 'Ay', tgt.y - 0.1), p.get(s + 'Az', tgt.z)))
        e = max(e, arm_ik(s, tgt, pole, aim))
    return e


def std_fingers():
    """mano abierta (Loco_Idle) y puno cerrado (dedos 80/95/65, pulgar -30/-40/-30 sobre X local)"""
    fist = {}
    for n in G.FINGERS:
        j = int(n[-1])
        ang = {1: -30, 2: -40, 3: -30}[j] if 'Thumb' in n else {1: 80, 2: 95, 3: 65}[j]
        fist[n] = Quaternion((1, 0, 0), math.radians(ang))
    return {'open': G.local_pose('Loco_Idle', 1, G.FINGERS), 'fist': fist}
