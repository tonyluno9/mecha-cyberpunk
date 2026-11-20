
import bpy, os, math
from mathutils import Matrix, Vector, Quaternion

ROOT = r"C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\blender"
CLIPS = os.path.join(ROOT, "pj01_clips")
REV = os.path.join(ROOT, "_PENDIENTE_BLENDER_2", "_REVISION")
PEND = os.path.join(ROOT, "_PENDIENTE_BLENDER_2")

MAP = {
 'Hips':'Hip','Spine':'Waist','Spine1':'Spine01','Spine2':'Spine02','Neck':'NeckTwist01','Head':'Head',
 'LeftShoulder':'L_Clavicle','LeftArm':'L_Upperarm','LeftForeArm':'L_Forearm','LeftHand':'L_Hand',
 'RightShoulder':'R_Clavicle','RightArm':'R_Upperarm','RightForeArm':'R_Forearm','RightHand':'R_Hand',
 'LeftUpLeg':'L_Thigh','LeftLeg':'L_Calf','LeftFoot':'L_Foot','LeftToeBase':'L_ToeBase',
 'RightUpLeg':'R_Thigh','RightLeg':'R_Calf','RightFoot':'R_Foot','RightToeBase':'R_ToeBase',
}
for side,S in (('Left','L'),('Right','R')):
    for f,F in (('Thumb','Thumb'),('Index','Index'),('Middle','Mid'),('Ring','Ring'),('Pinky','Pinky')):
        for i in (1,2,3):
            MAP[side+'Hand'+f+str(i)] = S+'_'+F+str(i)

def strip_prefix(n):
    return n.split(':')[-1] if ':' in n else n

def import_mixamo(fp, keep_name=None):
    """Importa un FBX de Mixamo; deja solo el armature. Devuelve (arm, action)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fp, use_anim=True, ignore_leaf_bones=True, automatic_bone_orientation=False)
    new = [o for o in bpy.data.objects if o not in before]
    arm = [o for o in new if o.type == 'ARMATURE'][0]
    for o in new:
        if o is not arm:
            bpy.data.objects.remove(o, do_unlink=True)
    act = arm.animation_data.action
    nm = keep_name or os.path.splitext(os.path.basename(fp))[0]
    act.name = 'SRC_' + nm
    arm.name = 'SRC_' + nm
    return arm, act

def _order(arm):
    out = []
    def rec(b):
        out.append(b)
        for c in b.children: rec(c)
    for b in arm.data.bones:
        if b.parent is None: rec(b)
    return out

def _q(m3):
    return m3.normalized().to_quaternion()

def retarget(src, name, T=None, f0=None, f1=None, in_place=False, hip_k=None):
    """Pasa la accion activa de src (mixamorig) a PJ01 en una accion nueva 'name'. Devuelve la accion."""
    T = T or bpy.data.objects['PJ01']
    sc = bpy.context.scene
    bpy.context.view_layer.update()
    sc.frame_set(sc.frame_current)
    sact = src.animation_data.action
    a0, a1 = [int(round(x)) for x in sact.frame_range]
    f0 = a0 if f0 is None else f0
    f1 = a1 if f1 is None else f1
    sbones = {strip_prefix(b.name): b.name for b in src.data.bones}
    Sw = src.matrix_world.copy(); Tw = T.matrix_world.copy()
    Tw3 = Tw.to_3x3().normalized(); Tw3i = Tw3.inverted()
    # rest world rotations
    srest = {k: (Sw.to_3x3().normalized() @ src.data.bones[v].matrix_local.to_3x3()).normalized() for k, v in sbones.items()}
    trest = {b.name: (Tw3 @ b.matrix_local.to_3x3()).normalized() for b in T.data.bones}
    tmap = {}
    for k, v in MAP.items():
        if k in sbones and v in T.data.bones: tmap[v] = k
    order = _order(T)
    # hip scale factor
    sh0 = Sw @ src.data.bones[sbones['Hips']].head_local
    th0 = Tw @ T.data.bones['Hip'].head_local
    if hip_k is None:
        hip_k = th0.z / max(sh0.z, 1e-6)
    # prepare action
    old = T.animation_data.action if T.animation_data else None
    if T.animation_data is None: T.animation_data_create()
    act = bpy.data.actions.get(name)
    if act: bpy.data.actions.remove(act)
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    slot = act.slots.new(id_type='OBJECT', name='PJ01')
    layer = act.layers.new('Layer'); strip = layer.strips.new(type='KEYFRAME')
    cb = strip.channelbag(slot, ensure=True)
    frames = list(range(f0, f1 + 1))
    data = {b.name: [] for b in order}
    hiploc = []
    rel = {}
    for b in order:
        if b.parent:
            rel[b.name] = (b.parent.matrix_local.inverted() @ b.matrix_local).to_3x3().normalized()
        else:
            rel[b.name] = b.matrix_local.to_3x3().normalized()
    for f in frames:
        sc.frame_set(f)
        poseArm = {}
        for b in order:
            n = b.name
            par = b.parent.name if b.parent else None
            parR = poseArm[par] if par else Matrix.Identity(3)
            if n in tmap:
                k = tmap[n]; sp = src.pose.bones[sbones[k]]
                Pw = (Sw.to_3x3().normalized() @ sp.matrix.to_3x3()).normalized()
                D = Pw @ srest[k].inverted()
                Rw = D @ trest[n]
                Ra = (Tw3i @ Rw)
                basis = rel[n].inverted() @ parR.inverted() @ Ra
                q = _q(basis)
                poseArm[n] = Ra
            else:
                q = Quaternion((1, 0, 0, 0))
                poseArm[n] = parR @ rel[n]
            prev = data[n][-1] if data[n] else None
            if prev is not None and q.dot(prev) < 0: q.negate()
            data[n].append(q)
        # hip location
        sp = src.pose.bones[sbones['Hips']]
        sh = Sw @ sp.head
        d = (sh - sh0) * hip_k
        if in_place: d.x = 0; d.y = 0
        twh = th0 + d
        harm = Tw.inverted() @ twh
        hb = T.data.bones['Hip']
        Mpose = Matrix.Translation(harm) @ poseArm['Hip'].to_4x4()
        # pose_arm(Hip) = rest_arm(BoneRoot)@rel... BoneRoot basis identity -> rest_arm(Hip) @ basis
        B = hb.matrix_local.inverted() @ Mpose
        hiploc.append(B.to_translation())
    # write fcurves
    for n, qs in data.items():
        dp = 'pose.bones["%s"].rotation_quaternion' % n
        for i in range(4):
            fc = cb.fcurves.new(dp, index=i, group_name=n)
            fc.keyframe_points.add(len(frames))
            co = []
            for f, q in zip(frames, qs): co += [f - f0 + 1, q[i]]
            fc.keyframe_points.foreach_set('co', co)
            fc.update()
    for n in [b.name for b in order]:
        dp = 'pose.bones["%s"].location' % n
        for i in range(3):
            fc = cb.fcurves.new(dp, index=i, group_name=n)
            fc.keyframe_points.add(len(frames))
            co = []
            for j, f in enumerate(frames):
                co += [f - f0 + 1, hiploc[j][i] if n == 'Hip' else 0.0]
            fc.keyframe_points.foreach_set('co', co)
            fc.update()
    for pb in T.pose.bones: pb.rotation_mode = 'QUATERNION'; pb.scale = (1, 1, 1)
    T.animation_data.action = act; T.animation_data.action_slot = act.slots[0]
    return act

def set_action(name, T=None):
    T = T or bpy.data.objects['PJ01']
    a = bpy.data.actions[name]
    T.animation_data.action = a; T.animation_data.action_slot = a.slots[0]
    f0, f1 = [int(round(x)) for x in a.frame_range]
    sc = bpy.context.scene; sc.frame_start = f0; sc.frame_end = f1
    return a

def foot_min_z(T=None):
    T = T or bpy.data.objects['PJ01']
    mz = 1e9
    for b in ('L_Foot','R_Foot','L_ToeBase','R_ToeBase'):
        pb = T.pose.bones[b]
        for p in (pb.head, pb.tail):
            mz = min(mz, (T.matrix_world @ p).z)
    return mz

def mesh_min_z(obj_name='PJ01_Malla', step=7):
    o = bpy.data.objects[obj_name]
    dg = bpy.context.evaluated_depsgraph_get(); ev = o.evaluated_get(dg); me = ev.to_mesh()
    W = o.matrix_world
    z = min((W @ me.vertices[i].co).z for i in range(0, len(me.vertices), step))
    ev.to_mesh_clear()
    return z

def shift_hip_z(act_name, dz_world, T=None):
    """Sube/baja toda la accion dz (m en mundo) moviendo la location del Hip en espacio de armature."""
    T = T or bpy.data.objects['PJ01']
    a = bpy.data.actions[act_name]
    hb = T.data.bones['Hip']
    v = (T.matrix_world.to_3x3().inverted() @ Vector((0, 0, dz_world)))
    vb = hb.matrix_local.to_3x3().inverted() @ v
    cb = a.layers[0].strips[0].channelbags[0]
    for i in range(3):
        fc = cb.fcurves.find('pose.bones["Hip"].location', index=i)
        for k in fc.keyframe_points:
            k.co.y += vb[i]; k.handle_left.y += vb[i]; k.handle_right.y += vb[i]
        fc.update()

def export_clip(act_name, out_name=None, outdir=CLIPS):
    T = bpy.data.objects['PJ01']; sc = bpy.context.scene; vl = bpy.context.view_layer
    out_name = out_name or act_name
    keep = (T.scale.copy(), sc.render.fps, sc.frame_start, sc.frame_end, sc.name, T.animation_data.action, [o for o in vl.objects if o.select_get()], vl.objects.active, T.hide_get())
    sc.render.fps = 30; sc.render.fps_base = 1.0
    T.scale = (1, 1, 1)
    for pb in T.pose.bones: pb.scale = (1, 1, 1)
    set_action(act_name)
    sc.name = out_name
    for o in vl.objects: o.select_set(False)
    T.hide_set(False); T.select_set(True); vl.objects.active = T
    os.makedirs(outdir, exist_ok=True)
    fp = os.path.join(outdir, 'PJ01_' + out_name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=fp, use_selection=True, object_types={'ARMATURE'}, global_scale=1.0, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=False, add_leaf_bones=False,
        use_armature_deform_only=False, primary_bone_axis='Y', secondary_bone_axis='X',
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='AUTO')
    T.scale = keep[0]; sc.render.fps = keep[1]; sc.name = keep[4]
    if keep[5]:
        T.animation_data.action = keep[5]; T.animation_data.action_slot = keep[5].slots[0]
    sc.frame_start, sc.frame_end = keep[2], keep[3]
    for o in vl.objects: o.select_set(o in keep[6])
    vl.objects.active = keep[7]; T.hide_set(keep[8])
    return fp

def camera(name='Cam_Revision', loc=(2.5, -2.5, 1.4), target=(0, 0, 0.9), lens=40):
    sc = bpy.context.scene
    cam = bpy.data.objects.get(name)
    if cam is None:
        cd = bpy.data.cameras.new(name); cam = bpy.data.objects.new(name, cd); sc.collection.objects.link(cam)
    cam.data.lens = lens
    cam.location = Vector(loc)
    d = Vector(target) - Vector(loc)
    cam.rotation_mode = 'QUATERNION'
    cam.rotation_quaternion = d.to_track_quat('-Z', 'Y')
    sc.camera = cam
    return cam

def snap(path, loc, target, lens=40, frame=None, res=(1280, 720)):
    sc = bpy.context.scene
    camera('Cam_Snap', loc, target, lens)
    if frame is not None: sc.frame_set(frame)
    sc.render.resolution_x, sc.render.resolution_y = res; sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = 'PNG'
    sc.render.filepath = path
    sh = sc.display.shading
    sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'
    win = bpy.context.window
    area = [a for a in win.screen.areas if a.type == 'VIEW_3D'][0]
    region = [r for r in area.regions if r.type == 'WINDOW'][0]
    with bpy.context.temp_override(window=win, area=area, region=region):
        bpy.ops.render.opengl(write_still=True, view_context=False)
    return path

def view_camera_play(cam_name='Cam_Revision', play=True):
    sc = bpy.context.scene
    cam = bpy.data.objects.get(cam_name)
    if cam: sc.camera = cam
    win = bpy.context.window
    for area in win.screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces[0].region_3d.view_perspective = 'CAMERA'
            region = [r for r in area.regions if r.type == 'WINDOW'][0]
            if play and not bpy.context.screen.is_animation_playing:
                with bpy.context.temp_override(window=win, area=area, region=region):
                    bpy.ops.screen.animation_play()
            break

def bitacora(texto):
    p = os.path.join(REV, 'BITACORA.md')
    new = not os.path.exists(p)
    with open(p, 'a', encoding='utf-8') as f:
        if new: f.write('# Bitácora de tareas Blender — CyberCity ronda 2 (16-nov)\n\n')
        f.write(texto.rstrip() + '\n\n')
    return p

def body_bvh(obj_name='PJ01_Malla'):
    from mathutils.bvhtree import BVHTree
    o = bpy.data.objects[obj_name]
    dg = bpy.context.evaluated_depsgraph_get(); ev = o.evaluated_get(dg); me = ev.to_mesh(); W = o.matrix_world
    pts = [W @ v.co for v in me.vertices]
    bvh = BVHTree.FromPolygons(pts, [p.vertices[:] for p in me.polygons]); ev.to_mesh_clear()
    return bvh, pts


def transform_root(act_name, theta_deg=0.0, pivot=(0,0,0), dxy=(0,0), dz_fn=None, T=None):
    """Rota (Z mundo) la raiz de toda la accion alrededor de pivot, la traslada dxy y suma dz_fn(frame) en altura."""
    T = T or bpy.data.objects['PJ01']
    a = bpy.data.actions[act_name]
    set_action(act_name)
    sc = bpy.context.scene
    cb = a.layers[0].strips[0].channelbags[0]
    floc = [cb.fcurves.find('pose.bones["Hip"].location', index=i) for i in range(3)]
    frot = [cb.fcurves.find('pose.bones["Hip"].rotation_quaternion', index=i) for i in range(4)]
    frames = [int(k.co.x) for k in floc[0].keyframe_points]
    Tw = T.matrix_world; Twi = Tw.inverted()
    hb = T.data.bones['Hip']; rest = hb.matrix_local
    piv = Vector(pivot)
    R = Matrix.Translation(piv) @ Matrix.Rotation(math.radians(theta_deg), 4, 'Z') @ Matrix.Translation(-piv)
    new = []
    for f in frames:
        sc.frame_set(f)
        Mw = Tw @ T.pose.bones['Hip'].matrix
        dz = dz_fn(f) if dz_fn else 0.0
        Mw2 = Matrix.Translation((dxy[0], dxy[1], dz)) @ R @ Mw
        Marm = Twi @ Mw2
        # remove scale from Marm (Tw has uniform scale, Twi@Tw cancels)
        B = rest.inverted() @ Marm
        loc, rot, _ = B.decompose()
        new.append((f, loc, rot))
    prev = None
    for f, loc, rot in new:
        if prev is not None and rot.dot(prev) < 0: rot.negate()
        prev = rot
    def setk(fc, f, v):
        for k in fc.keyframe_points:
            if int(k.co.x) == f:
                d = v - k.co.y; k.co.y = v; k.handle_left.y += d; k.handle_right.y += d; return
    for f, loc, rot in new:
        for i in range(3): setk(floc[i], f, loc[i])
        for i in range(4): setk(frot[i], f, rot[i])
    for fc in floc + frot: fc.update()

def smooth(f, f0, f1, v0, v1):
    if f <= f0: return v0
    if f >= f1: return v1
    t = (f - f0) / (f1 - f0); t = t * t * (3 - 2 * t)
    return v0 + (v1 - v0) * t

def hip_world(T=None):
    T = T or bpy.data.objects['PJ01']
    return T.matrix_world @ T.pose.bones['Hip'].head


def bake_visual(name, f0, f1, T=None, step=1):
    """Hornea la pose visual (con constraints) de PJ01 a una accion nueva con rot quaternion de todos los huesos y loc del Hip."""
    T = T or bpy.data.objects['PJ01']
    sc = bpy.context.scene
    order = _order(T)
    frames = list(range(f0, f1 + 1, step))
    data = {b.name: [] for b in order}; hl = []
    for f in frames:
        sc.frame_set(f)
        for b in order:
            pb = T.pose.bones[b.name]
            if b.parent:
                pm = T.pose.bones[b.parent.name].matrix
                basis = (b.parent.matrix_local.inverted() @ b.matrix_local).inverted() @ pm.inverted() @ pb.matrix
            else:
                basis = b.matrix_local.inverted() @ pb.matrix
            loc, q, _ = basis.decompose()
            prev = data[b.name][-1] if data[b.name] else None
            if prev is not None and q.dot(prev) < 0: q.negate()
            data[b.name].append(q)
            if b.name == 'Hip': hl.append(loc)
    act = bpy.data.actions.get(name)
    if act: bpy.data.actions.remove(act)
    act = bpy.data.actions.new(name); act.use_fake_user = True
    slot = act.slots.new(id_type='OBJECT', name='PJ01')
    cb = act.layers.new('Layer').strips.new(type='KEYFRAME').channelbag(slot, ensure=True)
    for n, qs in data.items():
        for i in range(4):
            fc = cb.fcurves.new('pose.bones["%s"].rotation_quaternion' % n, index=i, group_name=n)
            fc.keyframe_points.add(len(frames))
            co = []
            for j, f in enumerate(frames): co += [f - f0 + 1, qs[j][i]]
            fc.keyframe_points.foreach_set('co', co); fc.update()
        for i in range(3):
            fc = cb.fcurves.new('pose.bones["%s"].location' % n, index=i, group_name=n)
            fc.keyframe_points.add(len(frames))
            co = []
            for j, f in enumerate(frames): co += [f - f0 + 1, hl[j][i] if n == 'Hip' else 0.0]
            fc.keyframe_points.foreach_set('co', co); fc.update()
    return act

def empty(name, loc=(0,0,0), size=0.05, coll=None):
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, None); o.empty_display_type = 'SPHERE'; o.empty_display_size = size
        (coll or bpy.context.scene.collection).objects.link(o)
    o.location = Vector(loc)
    return o

def clear_constraints(T=None):
    T = T or bpy.data.objects['PJ01']
    for pb in T.pose.bones:
        for c in list(pb.constraints): pb.constraints.remove(c)


def _rot_bone_world(T, bn, Rw, pivot):
    pb = T.pose.bones[bn]; W = T.matrix_world
    Mw = W @ pb.matrix
    M = Matrix.Translation(pivot) @ Rw.to_4x4() @ Matrix.Translation(-pivot) @ Mw
    pb.matrix = W.inverted() @ M
    pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()

def two_bone_ik(A, B, C, target, pole, T=None):
    """A,B,C nombres (muslo, pantorrilla, pie) o (brazo, antebrazo, mano). Lleva la cabeza de C a target; rodilla/codo hacia pole."""
    T = T or bpy.data.objects['PJ01']; W = T.matrix_world
    pa = W @ T.pose.bones[A].head; pb_ = W @ T.pose.bones[B].head; pc = W @ T.pose.bones[C].head
    a = (pb_ - pa).length; b = (pc - pb_).length
    t = Vector(target); d = t - pa; L = d.length
    L = max(min(L, (a + b) * 0.999), abs(a - b) + 1e-4)
    dn = d.normalized()
    # knee position
    x = (a * a - b * b + L * L) / (2 * L)
    h = math.sqrt(max(a * a - x * x, 0.0))
    pv = Vector(pole) - pa
    pv = (pv - dn * pv.dot(dn))
    if pv.length < 1e-6: pv = dn.orthogonal()
    pv.normalize()
    knee = pa + dn * x + pv * h
    end = pa + dn * L
    # rotate A so that A->B points to knee (minimal rotation)
    q1 = (pb_ - pa).rotation_difference(knee - pa)
    _rot_bone_world(T, A, q1.to_matrix(), pa)
    pb_ = W @ T.pose.bones[B].head; pc = W @ T.pose.bones[C].head
    q2 = (pc - pb_).rotation_difference(end - pb_)
    _rot_bone_world(T, B, q2.to_matrix(), pb_)
    return (W @ T.pose.bones[C].head - Vector(target)).length

def set_bone_world_rot(bn, Rw3, T=None):
    """Pone la rotacion mundial del hueso (mantiene su cabeza)."""
    T = T or bpy.data.objects['PJ01']; W = T.matrix_world
    pb = T.pose.bones[bn]
    Mw = W @ pb.matrix; h = Mw.to_translation(); k = W.to_scale()[0]
    M = Matrix.Translation(h) @ Rw3.to_4x4() @ Matrix.Scale(k, 4)
    pb.matrix = W.inverted() @ M
    pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()

def bone_world_rot(bn, T=None):
    T = T or bpy.data.objects['PJ01']
    return (T.matrix_world @ T.pose.bones[bn].matrix).to_3x3().normalized()

def aim_bone(bn, target, T=None):
    """Gira el hueso (minimo) para que su eje Y apunte a target."""
    T = T or bpy.data.objects['PJ01']; W = T.matrix_world
    pb = T.pose.bones[bn]; h = W @ pb.head; tl = W @ pb.tail
    q = (tl - h).rotation_difference(Vector(target) - h)
    _rot_bone_world(T, bn, q.to_matrix(), h)

def key_pose(f, T=None, bones=None):
    T = T or bpy.data.objects['PJ01']
    for pb in (T.pose.bones if bones is None else [T.pose.bones[b] for b in bones]):
        pb.keyframe_insert('rotation_quaternion', frame=f, group=pb.name)
        pb.keyframe_insert('location', frame=f, group=pb.name)

def new_action(name, T=None):
    T = T or bpy.data.objects['PJ01']
    a = bpy.data.actions.get(name)
    if a: bpy.data.actions.remove(a)
    a = bpy.data.actions.new(name); a.use_fake_user = True
    s = a.slots.new(id_type='OBJECT', name='PJ01')
    if T.animation_data is None: T.animation_data_create()
    T.animation_data.action = a; T.animation_data.action_slot = s
    return a

def apply_pose_dict(d, T=None):
    T = T or bpy.data.objects['PJ01']
    for n, (q, l) in d.items():
        pb = T.pose.bones[n]; pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = q; pb.location = l; pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()

def get_pose_dict(T=None):
    T = T or bpy.data.objects['PJ01']
    return {pb.name: (pb.rotation_quaternion.copy(), pb.location.copy()) for pb in T.pose.bones}

def move_hip_world(target, T=None):
    T = T or bpy.data.objects['PJ01']; W = T.matrix_world
    cur = hip_world(T); hb = T.data.bones['Hip']
    T.pose.bones['Hip'].location += hb.matrix_local.to_3x3().inverted() @ (W.inverted().to_3x3() @ (Vector(target) - cur))
    bpy.context.view_layer.update()

def rot_world_about(bn, ang, axis='X', pivot=None, T=None):
    T = T or bpy.data.objects['PJ01']; W = T.matrix_world
    pb = T.pose.bones[bn]; h = (W @ pb.head) if pivot is None else Vector(pivot)
    _rot_bone_world(T, bn, Matrix.Rotation(math.radians(ang), 3, axis), h)


def nla_sequence(obj, names, start=1, gap=0):
    ad = obj.animation_data or obj.animation_data_create()
    for t in list(ad.nla_tracks): ad.nla_tracks.remove(t)
    ad.action = None
    tr = ad.nla_tracks.new(); tr.name = 'Revision'
    f = start
    for n in names:
        a = bpy.data.actions[n]
        st = tr.strips.new(n, int(f), a)
        try: st.action_slot = a.slots[0]
        except Exception: pass
        st.extrapolation = 'NOTHING' if n != names[-1] else 'HOLD'
        f = st.frame_end + gap
    sc = bpy.context.scene; sc.frame_start = start; sc.frame_end = int(f)
    return int(f)

def dup_rig(suffix, offset=(0,0,0), mesh_names=('PJ01_Malla',)):
    T = bpy.data.objects['PJ01']
    A = T.copy(); A.data = T.data; A.name = 'PJ01_' + suffix
    A.animation_data_clear()
    for c in T.users_collection: c.objects.link(A)
    A.location = T.location + Vector(offset)
    out = [A]
    for mn in mesh_names:
        m = bpy.data.objects[mn]; mc = m.copy(); mc.name = mn + '_' + suffix
        for c in m.users_collection: c.objects.link(mc)
        mc.parent = A
        for md in mc.modifiers:
            if md.type == 'ARMATURE': md.object = A
        out.append(mc)
    return out

def save_revision(fname):
    fp = os.path.join(REV, fname)
    bpy.ops.wm.save_as_mainfile(filepath=fp, copy=True)
    return fp

def cleanup_master(keep_prefix=()):
    """Quita objetos temporales del maestro (SRC_*, referencias, empties IK, duplicados de revision) y NLA."""
    rm = []
    for o in list(bpy.data.objects):
        n = o.name
        if n.startswith(('SRC_', 'IK_', 'AIM_', 'POLE_', 'Cam_Snap', 'Cam_Revision', 'PJ01_REV', 'PJ01_Malla_REV')) or (o.users_collection and any(c.name.startswith('Ref_') for c in o.users_collection)):
            if not n.startswith(keep_prefix):
                rm.append(n); bpy.data.objects.remove(o, do_unlink=True)
    for c in list(bpy.data.collections):
        if c.name.startswith('Ref_') and not c.objects: bpy.data.collections.remove(c)
    for a in list(bpy.data.actions):
        if a.name.startswith('SRC_'): bpy.data.actions.remove(a)
    T = bpy.data.objects['PJ01']
    if T.animation_data:
        for t in list(T.animation_data.nla_tracks): T.animation_data.nla_tracks.remove(t)
    for pb in T.pose.bones:
        pb.scale = (1, 1, 1)
        for c in list(pb.constraints): pb.constraints.remove(c)
    for b in bpy.data.meshes:
        if b.users == 0: bpy.data.meshes.remove(b)
    return rm


def resample(raw, out, times, T=None):
    """Nueva accion 'out' con la pose de 'raw' muestreada en tiempos (cuadros fraccionarios)."""
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    poses = []
    for t in times:
        set_action(raw); sc.frame_set(int(t), subframe=t - int(t)); poses.append(get_pose_dict(T))
    new_action(out, T)
    for i, P in enumerate(poses):
        apply_pose_dict(P, T); key_pose(i + 1, T)
    return bpy.data.actions[out]

def ground_fix(act_name, lie_hip=0.55, smooth_n=2, allow_lower=True, T=None, step=5):
    """Ajusta la altura para que el cuerpo no atraviese el piso; si esta tirado (cadera baja) lo apoya en el piso."""
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    a = set_action(act_name); f0, f1 = [int(x) for x in a.frame_range]
    offs = []
    for f in range(f0, f1 + 1):
        sc.frame_set(f); mz = mesh_min_z(step=step); hz = hip_world(T).z
        if mz < 0: o = -mz
        elif allow_lower and hz < lie_hip: o = -mz
        else: o = 0.0
        offs.append(o)
    sm = []
    for i in range(len(offs)):
        w = offs[max(0, i - smooth_n): i + smooth_n + 1]
        sm.append(max(offs[i], sum(w) / len(w)) if offs[i] > 0 else sum(w) / len(w))
    hb = T.data.bones['Hip']; Wi = T.matrix_world.inverted().to_3x3()
    cb = a.layers[0].strips[0].channelbags[0]
    fl = [cb.fcurves.find('pose.bones["Hip"].location', index=i) for i in range(3)]
    for j, f in enumerate(range(f0, f1 + 1)):
        vb = hb.matrix_local.to_3x3().inverted() @ (Wi @ Vector((0, 0, sm[j])))
        for i in range(3):
            for k in fl[i].keyframe_points:
                if int(round(k.co.x)) == f:
                    k.co.y += vb[i]; k.handle_left.y += vb[i]; k.handle_right.y += vb[i]
    for c in fl: c.update()
    return max(sm), min(sm)

def clip_stats(act_name, T=None):
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    a = set_action(act_name); f0, f1 = [int(x) for x in a.frame_range]
    mz = []
    for f in range(f0, f1 + 1, 2):
        sc.frame_set(f); mz.append(mesh_min_z(step=5))
    return {'frames': f1 - f0 + 1, 'min_z': round(min(mz), 3), 'max_floor_gap_when_low': None}


ARM_KEYS = ('Upperarm','Forearm','Hand','Thumb','Index','Mid','Ring','Pinky')
def implant_hits(act_name, step=3, T=None):
    """Cuenta choques brazo<->implantes: IMP01 (piernas) contra brazos y IMP02 (brazos) contra piernas/torso."""
    from mathutils.bvhtree import BVHTree
    from mathutils.kdtree import KDTree
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene; Mo = bpy.data.objects['PJ01_Malla']
    grp = {g.index: g.name for g in Mo.vertex_groups}
    owner = []
    for v in Mo.data.vertices:
        b = max(v.groups, key=lambda g: g.weight, default=None)
        owner.append(grp[b.group] if b else '')
    isarm = [any(k in n for k in ARM_KEYS) for n in owner]
    imps = [bpy.data.objects[n] for n in ('IMP01_LVL1','IMP01_LVL2','IMP02_LVL1','IMP02_LVL2') if n in bpy.data.objects]
    hid = {o.name: o.hide_get() for o in imps}
    for o in imps: o.hide_set(False)
    a = set_action(act_name); f0, f1 = [int(x) for x in a.frame_range]
    res = {o.name: 0 for o in imps}; worst = {o.name: (0, 0) for o in imps}
    for f in range(f0, f1 + 1, step):
        sc.frame_set(f); dg = bpy.context.evaluated_depsgraph_get()
        eb = Mo.evaluated_get(dg); mb = eb.to_mesh(); W = Mo.matrix_world
        pts = [W @ v.co for v in mb.vertices]
        bvh = BVHTree.FromPolygons(pts, [p.vertices[:] for p in mb.polygons])
        kd = KDTree(len(pts))
        for i, q in enumerate(pts): kd.insert(q, i)
        kd.balance(); eb.to_mesh_clear()
        for o in imps:
            ei = o.evaluated_get(dg); mi = ei.to_mesh(); WI = o.matrix_world
            n = 0
            arm_imp = o.name.startswith('IMP02')
            for k in range(0, len(mi.vertices), 3):
                q = WI @ mi.vertices[k].co; loc, nor, _, dd = bvh.find_nearest(q)
                if loc is not None and (q - loc).dot(nor) < -0.004:
                    _, j, _ = kd.find(loc)
                    if (not arm_imp and isarm[j]) or (arm_imp and not isarm[j] and ('Thigh' in owner[j] or 'Pelvis' in owner[j] or 'Calf' in owner[j])):
                        n += 1
            ei.to_mesh_clear()
            res[o.name] += n
            if n > worst[o.name][1]: worst[o.name] = (f, n)
    for o in imps: o.hide_set(hid[o.name])
    return res, worst

def posture_fix(src, out, sh=10.0, ab=12.0, chest=0.0, T=None):
    """Copia 'src' a 'out' subiendo hombros (clavicula +-sh) y separando brazos (upperarm +-ab) en el mundo, cuadro a cuadro."""
    T = T or bpy.data.objects['PJ01']; sc = bpy.context.scene
    a = set_action(src); f0, f1 = [int(x) for x in a.frame_range]
    poses = []
    for f in range(f0, f1 + 1):
        sc.frame_set(f); T.animation_data.action = None
        if chest:
            rot_world_about('Spine01', -chest * 0.4, 'X'); rot_world_about('Spine02', -chest * 0.6, 'X')
            rot_world_about('NeckTwist01', chest, 'X')
        rot_world_about('L_Clavicle', -sh, 'Y'); rot_world_about('R_Clavicle', sh, 'Y')
        rot_world_about('L_Upperarm', -ab, 'Y'); rot_world_about('R_Upperarm', ab, 'Y')
        poses.append(get_pose_dict(T))
        set_action(src)
    new_action(out, T)
    for i, P in enumerate(poses): apply_pose_dict(P, T); key_pose(i + 1, T)
    return bpy.data.actions[out]
