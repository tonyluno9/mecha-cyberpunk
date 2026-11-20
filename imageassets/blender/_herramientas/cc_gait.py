
"""cc_gait.py — generador procedural de ciclos de locomocion para PJ01 (19-nov).
Mundo: frente del personaje = -Y, izquierda del personaje = +X, arriba = +Z. 30 fps.
"""
import bpy, math, os
from mathutils import Vector, Matrix, Quaternion
import cc_tools as C

UP = Vector((0, 0, 1)); FWD = Vector((0, -1, 0)); LEFT = Vector((1, 0, 0))
FINGERS = [s + '_' + f + str(i) for s in 'LR' for f in ('Thumb', 'Index', 'Mid', 'Ring', 'Pinky') for i in (1, 2, 3)]
SPINE = ('Waist', 'Spine01', 'Spine02')

def T_(): return bpy.data.objects['PJ01']
def Wp(b, T=None, tail=False):
    T = T or T_(); pb = T.pose.bones[b]
    return T.matrix_world @ (pb.tail if tail else pb.head)
def Rw(b, T=None): return C.bone_world_rot(b, T or T_())
def rot(axis, deg): return Matrix.Rotation(math.radians(deg), 3, axis)
def mj(s):
    s = min(max(s, 0.0), 1.0); return s * s * s * (10 - 15 * s + 6 * s * s)
def ss(s):
    s = min(max(s, 0.0), 1.0); return s * s * (3 - 2 * s)
def lerp(a, b, t): return a + (b - a) * t
def cyc(ph, peak): return math.cos(2 * math.pi * (ph - peak))

def rot_about(bn, R, pivot=None, T=None):
    T = T or T_()
    C._rot_bone_world(T, bn, R, Wp(bn, T) if pivot is None else Vector(pivot))

# ---------------------------------------------------------------- base pose
def finger_pose(src_action, frame):
    T = T_(); keep = T.animation_data.action
    C.set_action(src_action); bpy.context.scene.frame_set(frame)
    d = {n: T.pose.bones[n].rotation_quaternion.copy() for n in FINGERS}
    T.animation_data.action = keep
    return d

def rest_pose(T=None):
    T = T or T_()
    if T.animation_data: T.animation_data.action = None
    for pb in T.pose.bones:
        pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()

ARMS = [s + '_' + b for s in 'LR' for b in ('Clavicle', 'Upperarm', 'Forearm', 'Hand')] + FINGERS

def local_pose(src_action, frame, bones):
    T = T_(); keep = T.animation_data.action if T.animation_data else None
    C.set_action(src_action); bpy.context.scene.frame_set(frame)
    d = {n: T.pose.bones[n].rotation_quaternion.copy() for n in bones}
    if T.animation_data: T.animation_data.action = keep
    return d

def build_base(fingers=None, arms_from=('Loco_Idle', 1), elbow=0.0, adduct=0.0, upper_from=None, aim_fix=False):
    """rest (T-pose) + brazos/manos copiados (locales) de una pose natural"""
    T = T_()
    arms = local_pose(arms_from[0], arms_from[1], ARMS)
    if upper_from:
        arms.update(local_pose(upper_from[0], upper_from[1], ['Waist', 'Spine01', 'Spine02', 'NeckTwist01', 'NeckTwist02', 'Head'] + ARMS))
    rest_pose(T)
    for n, q in arms.items(): T.pose.bones[n].rotation_quaternion = q
    bpy.context.view_layer.update()
    if fingers:
        for n, q in fingers.items(): T.pose.bones[n].rotation_quaternion = q
        bpy.context.view_layer.update()
    if elbow:
        for s in 'LR': rot_about(s + '_Forearm', rot('X', -elbow))
    if adduct:
        for s, sg in (('L', 1), ('R', -1)): rot_about(s + '_Upperarm', rot('Y', sg * adduct))
    if aim_fix:
        # gira la cintura para que el arma (mano der.) apunte a -Y y la cabeza mire al frente
        for it in range(3):
            v = Wp('R_Hand') - Wp('R_Upperarm'); v.z = 0
            rot_about('Waist', Matrix.Rotation(yaw_between(v, FWD), 3, 'Z'))
    ref = {
        'pose': C.get_pose_dict(T),
        'hip': Wp('Hip').copy(),
        'R': {b: Rw(b) for b in ('Hip', 'Waist', 'Spine01', 'Spine02', 'NeckTwist01', 'Head', 'L_Foot', 'R_Foot', 'L_ToeBase', 'R_ToeBase', 'L_Hand', 'R_Hand')},
        'ankle': {s: Wp(s + '_Foot').copy() for s in 'LR'},
        'ball': {s: Wp(s + '_ToeBase').copy() for s in 'LR'},
        'thigh': {s: Wp(s + '_Thigh').copy() for s in 'LR'},
    }
    return ref

# ---------------------------------------------------------------- foot model
def heading_of(ref, s):
    d = ref['ball'][s] - ref['ankle'][s]; d.z = 0; return d.normalized()

def yaw_between(a, b):
    a = Vector((a.x, a.y, 0)).normalized(); b = Vector((b.x, b.y, 0)).normalized()
    return math.atan2(a.x * b.y - a.y * b.x, a.dot(b))

def foot_rot(ref, s, d, p):
    """rotacion mundial del pie: rumbo d (horizontal), pitch p grados (+ = punta arriba)"""
    yaw = yaw_between(heading_of(ref, s), d)
    Ry = Matrix.Rotation(yaw, 3, 'Z')
    ax = UP.cross(d).normalized()
    Rp = Matrix.Rotation(math.radians(-p), 3, ax)
    return Rp @ Ry @ ref['R'][s + '_Foot'], Rp @ Ry @ ref['R'][s + '_ToeBase'], Ry

def ankle_at(G, d, p, P):
    """posicion del tobillo con el pie apoyado en el ancla G (piso bajo el tobillo plano) y pitch p.
    p>0 pivotea en el talon, p<0 en la bola."""
    ha, uh, ub = P['ha'], P['heel'], P['ball']
    A0 = G + UP * ha
    if abs(p) < 1e-6: return A0
    ax = UP.cross(d).normalized()
    R = Matrix.Rotation(math.radians(-p), 3, ax)
    X = G - d * uh if p > 0 else G + d * ub
    return X + R @ (A0 - X)

def stance_pitch(s, P):
    """pitch del pie en apoyo, s=0..1 dentro del apoyo"""
    k = P['stance']   # (p_contact, s_flat, s_heeloff, p_toeoff, p_mid)
    pc, sf, sh, pt = k[0], k[1], k[2], k[3]
    pm = k[4] if len(k) > 4 else 0.0
    if s < sf: return lerp(pc, pm, ss(s / sf))
    if s < sh: return pm
    return lerp(pm, pt, ((s - sh) / (1 - sh)) ** 1.4)

# ---------------------------------------------------------------- gait
DEF = dict(stair=None, swing_vlead=1.0, v=1.65, N=31, beta=0.6, track=0.085, toe_out=7.0, land=0.25,
           ha=0.122, heel=0.055, ball=0.155,
           stance=(18, 0.18, 0.62, -38), swing_h=0.10, swing_peak=0.38, swing_lag=1.0, p_swing=6, paw=0.0,
           hip_z=0.915, hz_amp=0.02, hz_mode='walk', sway=0.022,
           p_yaw=5.0, p_roll=4.0, p_tilt=2.0, lean=3.0, t_yaw=6.0,
           arm_amp=16.0, arm_off=-2.0, arm_lag=0.04, elbow=18.0, elbow_amp=8.0, fore_lag=0.08, arm_in=2.0, clav=3.0,
           head_bob=1.5, head_lag=0.06, travel=(0, -1, 0), open_hips=0.0, feet_turn=0.5, backward=False,
           hand_lag=0.12, hand_amp=6.0)

def gait(name, P, fingers=None, ref=None, overrides=None):
    T = T_(); sc = bpy.context.scene
    Q = dict(DEF); Q.update(P)
    ref = ref or build_base(fingers, elbow=Q.get('base_elbow', 8.0), adduct=Q.get('adduct', 6.0), upper_from=Q.get('upper_from'), aim_fix=Q.get('aim_fix', False))
    N = Q['N']; Tc = N / 30.0; v = Q['v']; beta = Q['beta']
    D = Vector(Q['travel']).normalized()
    S = v * Tc
    # angulo entre frente y direccion de viaje (doblado a +-90 para atras)
    alpha = math.degrees(yaw_between(FWD, D))
    if alpha > 90: alpha -= 180
    if alpha < -90: alpha += 180
    hip_yaw0 = alpha * Q['open_hips']
    feet_head = rot('Z', alpha * Q['feet_turn']) @ FWD
    track_dir = rot('Z', alpha) @ LEFT       # perpendicular a la direccion (con signo del lado izquierdo)
    a = C.new_action(name, T)
    hip0 = ref['hip']
    steps = {'L': [], 'R': []}
    errs = []
    for f in range(N + 1):
        t = f / 30.0; ph = f / N
        C.apply_pose_dict(ref['pose'], T)
        # ---------- pelvis
        hz = Q['hip_z']
        if Q['hz_mode'] == 'walk':
            hz += Q['hz_amp'] * math.cos(4 * math.pi * (ph - (0.3 if not Q['backward'] else 0.3)))
        else:
            hz -= Q['hz_amp'] * math.cos(4 * math.pi * (ph - beta / 2))
        if Q.get('impact'):
            for c0 in (0.0, 0.5):
                x = (ph - c0) % 1.0
                w = Q.get('impact_w', 0.05)
                hz -= Q['impact'] * (x / w) * math.exp(1 - x / w) if x < 6 * w else 0.0
        sway = Q['sway'] * cyc(ph, beta / 2)
        travel = D * (v * t)
        slope = 0.0
        if Q['stair']: slope = Q['stair'][0] / Q['stair'][1] * (1 if Q['stair'][2] else -1)
        hip_t = Vector((hip0.x, hip0.y, hz + slope * v * t)) + travel + track_dir * sway
        yaw = hip_yaw0 - Q['p_yaw'] * cyc(ph, 0.0)
        roll = -Q['p_roll'] * cyc(ph, beta / 2)
        tilt = Q['p_tilt'] + 0.35 * Q['lean']
        rot_about('Hip', rot('Z', yaw)); rot_about('Hip', rot('Y', roll)); rot_about('Hip', rot('X', tilt))
        C.move_hip_world(hip_t, T)
        # ---------- torso (contrarrotacion, inclinacion)
        thor = -hip_yaw0 * 0.9 + Q['t_yaw'] * cyc(ph, 0.02)    # el pecho mira al frente
        rel = thor - (yaw - hip_yaw0) * 0.0
        spine_lean = 0.65 * Q['lean'] - Q['p_tilt']
        for b, k in zip(SPINE, (0.3, 0.35, 0.35)):
            rot_about(b, rot('Z', (thor - (yaw - hip_yaw0)) * k + (-(yaw - hip_yaw0)) * 0.0))
            rot_about(b, rot('Y', -roll * 0.8 * k))
            rot_about(b, rot('X', spine_lean * k))
        # ---------- cabeza: estable, mira al frente
        bob = Q['head_bob'] * math.cos(4 * math.pi * (ph - beta / 2 - Q['head_lag']))
        if Q.get('impact'):
            for c0 in (0.0, 0.5):
                x = (ph - c0 - 0.02) % 1.0; w = Q.get('impact_w', 0.05)
                if x < 6 * w: bob += Q.get('impact_head', 3.0) * (x / w) * math.exp(1 - x / w)
        Hd = rot('X', 0.25 * Q['lean'] + bob) @ ref['R']['Head']
        Hc = Rw('Head')
        dlt = (Hd @ Hc.inverted()).to_quaternion()
        half = Quaternion((1, 0, 0, 0)).slerp(dlt, 0.5).to_matrix()
        rot_about('NeckTwist01', half)
        C.set_bone_world_rot('Head', Hd, T)
        # ---------- brazos (en el marco del pecho)
        Dch = Rw('Spine02') @ ref['R']['Spine02'].inverted()
        axX = Dch @ Vector((1, 0, 0)); axY = Dch @ Vector((0, 1, 0)); axZ = Dch @ Vector((0, 0, 1))
        for s, sg, pk in (('L', 1, 0.5), ('R', -1, 0.0)):
            c1 = cyc(ph, pk + Q['arm_lag'])
            th = Q['arm_off'] - Q['arm_amp'] * c1              # negativo = adelante
            rot_about(s + '_Clavicle', Matrix.Rotation(math.radians(-sg * Q['clav'] * c1), 3, axZ))
            rot_about(s + '_Upperarm', Matrix.Rotation(math.radians(th), 3, axX))
            rot_about(s + '_Upperarm', Matrix.Rotation(math.radians(sg * Q['arm_in'] * max(c1, 0)), 3, axY))
            e = Q['elbow'] + Q['elbow_amp'] * cyc(ph, pk + Q['arm_lag'] + Q['fore_lag'])
            ax_e = (Rw(s + '_Upperarm') @ ref['R'][s + '_Hand'].inverted() @ ref['R'][s + '_Hand']) @ Vector((1, 0, 0))
            rot_about(s + '_Forearm', Matrix.Rotation(math.radians(-e), 3, axX))
            hpitch = Q['hand_amp'] * cyc(ph, pk + Q['arm_lag'] + Q['hand_lag'])
            rot_about(s + '_Hand', Matrix.Rotation(math.radians(hpitch), 3, axX))
        # ---------- piernas
        for s, sg, off in (('L', 1, 0.0), ('R', -1, 0.5)):
            phs = (ph - off) % 1.0
            k0 = math.floor((t / Tc) - off + 1e-9)       # indice del paso en curso
            def anchor(k):
                tk = (k + off) * Tc
                base = Vector((hip0.x, hip0.y, 0)) + D * (v * tk) + D * Q['land']
                z = 0.0
                if Q['stair']:
                    dist = D.dot(base - Vector((hip0.x, hip0.y, 0)))
                    z = (1 if Q['stair'][2] else -1) * Q['stair'][0] * round(dist / Q['stair'][1])
                return Vector((base.x, base.y, z)) + track_dir * (sg * Q['track'])
            dfoot = rot('Z', sg * Q['toe_out']) @ feet_head
            if phs < beta:
                s_ = phs / beta
                p = stance_pitch(s_, Q)
                G = anchor(k0)
                A = ankle_at(G, dfoot, p, Q)
                tp = p if p > 0 else 0.0
                if f <= N and s_ < 1e-6: steps[s].append(f)
            else:
                s_ = (phs - beta) / (1 - beta)
                G0 = anchor(k0); G1 = anchor(k0 + 1)
                pto = Q['stance'][3]; phs_ = Q['stance'][0]
                A0 = ankle_at(G0, dfoot, pto, Q); A1 = ankle_at(G1, dfoot, phs_, Q)
                hh = mj(s_ ** Q['swing_lag'])
                Hc = lambda tt: Vector((hip0.x, hip0.y, slope * v * tt)) + D * (v * tt)
                t_to = (k0 + off + beta) * Tc; t_hs = (k0 + 1 + off) * Tc
                r0 = A0 - Hc(t_to); r1 = A1 - Hc(t_hs)
                rr = r0.lerp(r1, hh)
                rr.z = lerp(r0.z, r1.z, mj(s_ ** Q['swing_vlead']))
                A = Hc(t) + rr
                pk_ = Q['swing_peak']
                bump = (math.sin(math.pi * min(s_ / (2 * pk_), 0.5)) if s_ < pk_ else math.cos(0.5 * math.pi * (s_ - pk_) / (1 - pk_)))
                A = A + UP * (Q['swing_h'] * max(bump, 0.0))
                if Q['paw']:
                    A = A + D * (Q['paw'] * math.sin(math.pi * min(max((s_ - 0.55) / 0.45, 0), 1)))
                if s_ < 0.5: p = lerp(pto, Q['p_swing'], ss(s_ / 0.5))
                else: p = lerp(Q['p_swing'], phs_, ss((s_ - 0.5) / 0.5))
                tp = p + (0 - pto) * (1 - s_) ** 3
            Rf, Rt, Ry = foot_rot(ref, s, dfoot, p)
            hipj = Wp(s + '_Thigh')
            pole = (hipj + A) * 0.5 + dfoot * 0.6 + track_dir * (sg * 0.08)
            errs.append(C.two_bone_ik(s + '_Thigh', s + '_Calf', s + '_Foot', A, pole, T))
            C.set_bone_world_rot(s + '_Foot', Rf, T)
            ax = UP.cross(dfoot).normalized()
            C.set_bone_world_rot(s + '_ToeBase', Matrix.Rotation(math.radians(-tp), 3, ax) @ Ry @ ref['R'][s + '_ToeBase'], T)
        if overrides: overrides(f, ph, Q, ref)
        C.key_pose(f + 1, T)
    a.use_cyclic = True if hasattr(a, 'use_cyclic') else None
    return dict(name=name, frames=(1, N + 1), speed=round(v, 3), stride=round(S, 3),
                pasos_L=[1], pasos_R=[round(1 + N / 2, 1)],
                ik_err_max=round(max(errs), 4), errs=[round(e,3) for e in errs])


def strip(act, frames, view='side', tag='', res=(320, 420), dist=3.2, lens=50, zc=0.9, follow=True, extra=None):
    """renderiza fotogramas para revision -> lista de rutas"""
    T = T_(); sc = bpy.context.scene
    C.set_action(act)
    sc.display.shading.show_backface_culling = False
    dirs = {'side': Vector((1, 0, 0.08)), 'front': Vector((0, -1, 0.1)), 'q': Vector((0.75, -0.65, 0.25)),
            'back': Vector((0, 1, 0.15)), 'rside': Vector((-1, 0, 0.08)), 'q2': Vector((-0.7, -0.7, 0.3))}
    dv = dirs[view].normalized()
    out = []
    for k, f in enumerate(frames):
        sc.frame_set(f)
        h = Wp('Hip') if follow else Vector((0, 0, 0))
        tgt = Vector((h.x, h.y, zc)) if follow else Vector(extra or (0, 0, zc))
        p = os.path.join(C.REV, '_tmp_g_%s_%s%s_%02d.png' % (act, view, tag, k))
        C.snap(p, tuple(tgt + dv * dist), tuple(tgt), lens, frame=f, res=res)
        out.append(p)
    return out


def sheet(paths_rows, out, scale=2):
    """compone filas de imagenes (lista de listas de rutas) en una hoja PNG; scale = submuestreo entero"""
    import numpy as np
    rows = []
    for row in paths_rows:
        ims = []
        for p in row:
            im = bpy.data.images.load(p, check_existing=False)
            w, h = im.size; a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
            bpy.data.images.remove(im)
            ims.append(a[::scale, ::scale])
        rows.append(np.concatenate(ims, axis=1))
    W = max(r.shape[1] for r in rows)
    rows = [np.pad(r, ((0, 0), (0, W - r.shape[1]), (0, 0))) for r in rows]
    A = np.concatenate(rows[::-1], axis=0)      # blender: fila 0 abajo
    img = bpy.data.images.new('_sheet', A.shape[1], A.shape[0], alpha=True)
    img.pixels[:] = A.ravel(); img.filepath_raw = out; img.file_format = 'PNG'; img.save()
    bpy.data.images.remove(img)
    for r in paths_rows:
        for p in r:
            try: os.remove(p)
            except Exception: pass
    return out

def review(act, frames, views=('side', 'q'), out=None, scale=2, **kw):
    rows = [strip(act, frames, v, **kw) for v in views]
    out = out or os.path.join(C.REV, '_rev_%s.png' % act)
    return sheet(rows, out, scale)


def stair_mesh(name, n0, n1, rise, run, origin, D, up=True, front=0.21, back=0.07, width=1.2):
    """escalera de revision: peldaño n de altura rise*n (o -rise*n) con huella [a_n-back, a_n+front]"""
    import bmesh
    me = bpy.data.meshes.get(name)
    ob = bpy.data.objects.get(name)
    if ob: bpy.data.objects.remove(ob)
    bm = bmesh.new()
    side = UP.cross(D).normalized()
    sg = 1 if up else -1
    for n in range(n0, n1 + 1):
        a = n * run; z = sg * rise * n
        p0 = origin + D * (a - back); p1 = origin + D * (a + front)
        zb = z - 3.0
        vs = [bm.verts.new(p0 + side * w + UP * zz) for w, zz in ((-width, zb), (width, zb), (width, z), (-width, z))]
        vs2 = [bm.verts.new(p1 + side * w + UP * zz) for w, zz in ((-width, zb), (width, zb), (width, z), (-width, z))]
        for i in range(4):
            j = (i + 1) % 4; bm.faces.new((vs[i], vs[j], vs2[j], vs2[i]))
        bm.faces.new(vs[::-1]); bm.faces.new(vs2)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    return ob
