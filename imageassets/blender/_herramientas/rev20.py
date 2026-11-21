
"""rev20.py — revision 20-nov con Tony: medir personajes, rigs y clips. Solo diagnostica."""
import bpy, os, math, re, json
import numpy as np
from mathutils import Vector, Matrix
B = r'C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\imageassets\blender'
REV = os.path.join(B, '_REVISION_TONY'); CAP = os.path.join(REV, 'capturas')
UA = r'C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\Assets\Arte'

HUM = {  # hueso humanoide -> patrones (regex, se prueba en orden)
 'Hips': [r'(mixamorig\d*:)?Hips$', r'^Hip$', r'pelvis$', r'^Hips?$', r'CC_Base_Hip$'],
 'Spine': [r'(mixamorig\d*:)?Spine$', r'Waist$', r'spine_?0?1$'],
 'Chest': [r'(mixamorig\d*:)?Spine1$', r'Spine01$', r'spine_?0?2$'],
 'Neck': [r'(mixamorig\d*:)?Neck$', r'NeckTwist01$', r'neck_?0?1$', r'Neck$'],
 'Head': [r'(mixamorig\d*:)?Head$', r'^Head$', r'head$', r'CC_Base_Head$'],
 'LUpperArm': [r'(mixamorig\d*:)?LeftArm$', r'L_Upperarm$', r'upperarm_l$', r'L_UpperArm$'],
 'LLowerArm': [r'(mixamorig\d*:)?LeftForeArm$', r'L_Forearm$', r'lowerarm_l$'],
 'LHand': [r'(mixamorig\d*:)?LeftHand$', r'L_Hand$', r'hand_l$'],
 'RUpperArm': [r'(mixamorig\d*:)?RightArm$', r'R_Upperarm$', r'upperarm_r$', r'R_UpperArm$'],
 'RLowerArm': [r'(mixamorig\d*:)?RightForeArm$', r'R_Forearm$', r'lowerarm_r$'],
 'RHand': [r'(mixamorig\d*:)?RightHand$', r'R_Hand$', r'hand_r$'],
 'LUpperLeg': [r'(mixamorig\d*:)?LeftUpLeg$', r'L_Thigh$', r'thigh_l$'],
 'LLowerLeg': [r'(mixamorig\d*:)?LeftLeg$', r'L_Calf$', r'calf_l$'],
 'LFoot': [r'(mixamorig\d*:)?LeftFoot$', r'L_Foot$', r'foot_l$'],
 'RUpperLeg': [r'(mixamorig\d*:)?RightUpLeg$', r'R_Thigh$', r'thigh_r$'],
 'RLowerLeg': [r'(mixamorig\d*:)?RightLeg$', r'R_Calf$', r'calf_r$'],
 'RFoot': [r'(mixamorig\d*:)?RightFoot$', r'R_Foot$', r'foot_r$'],
 'LToe': [r'(mixamorig\d*:)?LeftToeBase$', r'L_ToeBase$', r'ball_l$'],
 'RToe': [r'(mixamorig\d*:)?RightToeBase$', r'R_ToeBase$', r'ball_r$'],
}
REQ = ['Hips','Spine','Head','LUpperArm','LLowerArm','LHand','RUpperArm','RLowerArm','RHand','LUpperLeg','LLowerLeg','LFoot','RUpperLeg','RLowerLeg','RFoot']

def hmap(arm):
    names = [b.name for b in arm.data.bones]; m = {}
    for k, pats in HUM.items():
        for p in pats:
            hit = [n for n in names if re.search(p, n, re.I)]
            if hit: m[k] = hit[0]; break
    return m

def coll(name, parent=None):
    c = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    par = parent or bpy.context.scene.collection
    if c.name not in par.children: par.children.link(c)
    return c

def imp(fp, cname, anim=True):
    before = set(bpy.data.objects); acts0 = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath=fp, use_anim=anim, ignore_leaf_bones=True)
    new = [o for o in bpy.data.objects if o not in before]
    c = coll(cname, coll('REVISION_PERSONAJES'))
    for o in new:
        for u in list(o.users_collection): u.objects.unlink(o)
        c.objects.link(o)
    acts = [a.name for a in bpy.data.actions if a not in acts0]
    return new, acts

def roots(objs):
    s = set(objs); return [o for o in objs if o.parent not in s]

def bbox(objs):
    dg = bpy.context.evaluated_depsgraph_get(); P = []
    for o in objs:
        if o.type != 'MESH': continue
        ev = o.evaluated_get(dg); me = ev.to_mesh()
        if len(me.vertices):
            a = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get('co', a)
            a = a.reshape(-1, 3); M = np.array(o.matrix_world)
            P.append(a @ M[:3, :3].T + M[:3, 3])
        ev.to_mesh_clear()
    if not P: return None
    P = np.concatenate(P); return P.min(0), P.max(0)

def mesh_issues(o):
    import bmesh
    bm = bmesh.new(); bm.from_mesh(o.data)
    bnd = sum(1 for e in bm.edges if e.is_boundary); nm = sum(1 for e in bm.edges if not e.is_manifold)
    # winding inconsistente entre caras vecinas
    bad = 0; tot = 0
    for e in bm.edges:
        if len(e.link_faces) == 2:
            tot += 1; f1, f2 = e.link_faces; v1, v2 = e.verts
            def d(f):
                vs = list(f.verts); i = vs.index(v1); return vs[(i + 1) % len(vs)] == v2
            if d(f1) == d(f2): bad += 1
    nw = 0
    if o.vertex_groups:
        dv = bm.verts.layers.deform.active
        if dv: nw = sum(1 for v in bm.verts if sum(v[dv].values()) < 1e-4)
    r = dict(v=len(bm.verts), f=len(bm.faces), bordes=bnd, no_manifold=nm, winding_inv=round(100.0 * bad / max(tot, 1), 2), sin_peso=nw)
    bm.free(); return r


def mat(name, col, emit=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    try:
        m.use_nodes = True
        b = [n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED']
        if b:
            b = b[0]; b.inputs['Base Color'].default_value = (*col, 1)
            if emit: b.inputs['Emission Color'].default_value = (*col, 1); b.inputs['Emission Strength'].default_value = emit
    except Exception: pass
    m.diffuse_color = (*col, 1)
    return m

def ref_figure(name='_Ref180', h=1.80):
    """figura de referencia 1.80 m: maniqui simple gris + marcas cada 10 cm"""
    if name in bpy.data.objects: return bpy.data.objects[name]
    import bmesh
    bm = bmesh.new()
    def cyl(r, z0, z1, x=0.0):
        g = bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=r, radius2=r, depth=z1 - z0)
        bmesh.ops.translate(bm, verts=g['verts'], vec=(x, 0, (z0 + z1) / 2))
    s = h / 1.80
    cyl(0.07*s, 0, 0.84*s, 0.10*s); cyl(0.07*s, 0, 0.84*s, -0.10*s)       # piernas
    cyl(0.17*s, 0.84*s, 1.48*s)                                         # torso
    cyl(0.05*s, 0.82*s, 1.44*s, 0.24*s); cyl(0.05*s, 0.82*s, 1.44*s, -0.24*s)  # brazos
    cyl(0.05*s, 1.48*s, 1.56*s)
    g = bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=0.11*s); bmesh.ops.translate(bm, verts=g['verts'], vec=(0, 0, h - 0.12*s))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); o.data.materials.append(mat('_RefGris', (0.55, 0.55, 0.6)))
    # regla
    bm = bmesh.new()
    for i in range(0, 26):
        z = i * 0.1; w = 0.18 if i % 5 == 0 else 0.08
        g = bmesh.ops.create_cube(bm, size=1); bmesh.ops.scale(bm, vec=(w, 0.01, 0.008), verts=g['verts']); bmesh.ops.translate(bm, verts=g['verts'], vec=(-0.45 + w/2, 0, z))
    g = bmesh.ops.create_cube(bm, size=1); bmesh.ops.scale(bm, vec=(0.012, 0.012, 2.5), verts=g['verts']); bmesh.ops.translate(bm, verts=g['verts'], vec=(-0.45, 0, 1.25))
    me2 = bpy.data.meshes.new(name + '_Regla'); bm.to_mesh(me2); bm.free()
    r = bpy.data.objects.new(name + '_Regla', me2); r.parent = o; r.data.materials.append(mat('_RefAmarillo', (1, 0.8, 0.1), 2))
    c = coll('_REFERENCIA'); c.objects.link(o); c.objects.link(r)
    return o

def cam(name, loc, target, lens=50, ortho=None):
    c = bpy.data.objects.get(name)
    if not c:
        c = bpy.data.objects.new(name, bpy.data.cameras.new(name)); bpy.context.scene.collection.objects.link(c)
    c.location = loc
    d = Vector(target) - Vector(loc); c.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    if ortho: c.data.type = 'ORTHO'; c.data.ortho_scale = ortho
    else: c.data.type = 'PERSP'; c.data.lens = lens
    c.data.clip_end = 500
    bpy.context.scene.camera = c; return c

def snap(path, loc, target, lens=50, ortho=None, res=(1280, 720), frame=None):
    sc = bpy.context.scene
    cam('_CamRev', loc, target, lens, ortho)
    if frame is not None: sc.frame_set(frame)
    sc.render.resolution_x, sc.render.resolution_y = res; sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = 'PNG'; sc.render.filepath = path
    sh = sc.display.shading; sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'; sh.show_backface_culling = False
    win = bpy.context.window
    area = [a for a in win.screen.areas if a.type == 'VIEW_3D'][0]
    region = [r for r in area.regions if r.type == 'WINDOW'][0]
    with bpy.context.temp_override(window=win, area=area, region=region):
        bpy.ops.render.opengl(write_still=True, view_context=False)
    return path

def sheet(rows, out, scale=1, labels=None):
    rr = []
    for row in rows:
        ims = []
        for p in row:
            im = bpy.data.images.load(p, check_existing=False); w, h = im.size
            a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4); bpy.data.images.remove(im)
            ims.append(a[::scale, ::scale])
        rr.append(np.concatenate(ims, axis=1))
    W = max(r.shape[1] for r in rr)
    rr = [np.pad(r, ((0, 0), (0, W - r.shape[1]), (0, 0))) for r in rr]
    A = np.concatenate(rr[::-1], axis=0)
    img = bpy.data.images.new('_sheet', A.shape[1], A.shape[0], alpha=True)
    img.pixels[:] = A.ravel(); img.filepath_raw = out; img.file_format = 'PNG'; img.save(); bpy.data.images.remove(img)
    for r in rows:
        for p in r:
            try: os.remove(p)
            except Exception: pass
    return out


GRUPOS = [('PRIORIDAD',['PJ01','Policia']),
('SKULLS',['SkullHombre','man','E19_PandilleroSkulls_a_2','E19_PandilleroSkulls_b','E20_MatonSkulls_a','E20_MatonSkulls_b']),
('SNAKES',['E10_PandilleroSnakes','E11_JefeSnakes']),
('NOVA',['GuardiaNova','E12_GuardiaPesadoNova','E14_JefeFinalNova','E15_CentinelaNova_a','E15_CentinelaNova_b','E16_RobotPesadoNova_a','E16_RobotPesadoNova_b','E18_EliteNova_a','E17_PerroRobotNova_a','E17_PerroRobotNova_esc']),
('ALIADOS',['Doctor','Inventor','Hacker','Mecanico','AL10','AL11','MeseraRobot','woman']),
('NPC',['NP07_Pescador_a','NP07_Pescador_b','NP08_Turista_a','NP08_Turista_b','NP09_Corredor_a','NP09_Corredor_b','NP10_Elotero_a','NP11_Estibador_a','NP11_Estibador_b','NP12_Abuela_a','NP13_Repartidor_a','NP13_Repartidor_b','NP14_Oficinista_a','NP14_Oficinista_b','RobotHombre','woman2'])]

def img_ok(i):
    if i.packed_file: return True
    try: return os.path.exists(bpy.path.abspath(i.filepath))
    except Exception: return False

def fix_missing_tex():
    """solo en el archivo de revision: texturas que no existen en disco -> se desconectan (evita magenta)"""
    fixed = set()
    def walk(nt, mn):
        for n in nt.nodes:
            if n.type == 'TEX_IMAGE' and n.image and not img_ok(n.image):
                for o in n.outputs:
                    for l in list(o.links): nt.links.remove(l); fixed.add(mn)
            if n.type == 'GROUP' and n.node_tree: walk(n.node_tree, mn)
    for m in bpy.data.materials:
        if m.node_tree: walk(m.node_tree, m.name)
    return sorted(fixed)

def build_personajes():
    L = json.load(open(os.path.join(REV, '_lista_pj.json')))
    AJ = json.load(open(os.path.join(REV, '_alturas_juego.json')))
    scg = {k: v['esc_prefab'] for k, v in AJ.items()}; scg['Inventor'] = 1.67; scg['Mecanico'] = 1.52
    fps = {os.path.basename(a)[:-4]: (a, b) for a, b in L}
    ref = ref_figure(); ref.location = (0, 30, 0)
    refc = bpy.data.collections['_REFERENCIA']; refc.instance_offset = (0, 30, 0)
    lay = {}
    for gi, (g, codes) in enumerate(GRUPOS):
        x = 0.0; y = -gi * 8.0
        gc = coll('GRUPO_' + g, coll('REVISION_PERSONAJES'))
        for code in codes:
            fp, gs = fps[code]
            new, acts = imp(fp, 'REV_' + code)
            c = bpy.data.collections['REV_' + code]
            par = coll('REVISION_PERSONAJES')
            if c.name in par.children: par.children.unlink(c)
            if c.name not in gc.children: gc.children.link(c)
            rts = roots(new); s = gs * scg.get(code, 1.0)
            for o in rts: o.scale = o.scale * s; o.location = o.location * s
            for a in new:
                if a.type == 'ARMATURE': a.data.pose_position = 'REST'
            bpy.context.view_layer.update()
            bb = bbox(new); w = float(bb[1][0] - bb[0][0]); cx = float((bb[0][0] + bb[1][0]) / 2); cy = float((bb[0][1] + bb[1][1]) / 2)
            x0 = x + 1.3 + w / 2
            for o in rts: o.location.x += x0 - cx; o.location.y += y - cy
            e = bpy.data.objects.new('ref_' + code, None); e.instance_type = 'COLLECTION'; e.instance_collection = refc; e.location = (x0 - w / 2 - 0.6, y, 0); c.objects.link(e)
            bpy.context.view_layer.update(); bb = bbox(new)
            lay[code] = dict(x=x0, y=y, w=w, d=float(bb[1][1] - bb[0][1]), h=float(bb[1][2] - bb[0][2]), grupo=g, escala_total=s, acts=acts)
            x = x0 + w / 2 + 3.4
    pjc = bpy.data.collections['REV_PJ01']; pjc.instance_offset = (lay['PJ01']['x'], lay['PJ01']['y'], 0)
    for code, v in lay.items():
        if code == 'PJ01': continue
        e2 = bpy.data.objects.new('pj01_' + code, None); e2.instance_type = 'COLLECTION'; e2.instance_collection = pjc
        e2.location = (v['x'] + v['w'] / 2 + 1.3, v['y'], 0); bpy.data.collections['REV_' + code].objects.link(e2)
    json.dump(lay, open(os.path.join(REV, '_layout.json'), 'w'), indent=1)
    return lay

def solo(codes, extra=()):
    """deja visibles solo esas colecciones REV_ (y extra)"""
    keep = set('REV_' + c for c in codes) | set(extra)
    def rec(lc):
        for ch in lc.children:
            n = ch.name
            if n.startswith('REV_'): ch.exclude = n not in keep
            elif n.startswith('GRUPO_') or n == 'REVISION_PERSONAJES': ch.exclude = False; rec(ch)
            elif n == '_REFERENCIA': ch.exclude = False
    rec(bpy.context.view_layer.layer_collection)


def _depth(b):
    d = 0
    while b.parent: d += 1; b = b.parent
    return d

def rot_world(arm, bname, axis, deg):
    """rota un hueso alrededor de su cabeza, eje del espacio de la armadura"""
    pb = arm.pose.bones[bname]
    M = pb.matrix.copy(); h = M.to_translation()
    R = Matrix.Rotation(math.radians(deg), 4, axis)
    pb.matrix = Matrix.Translation(h) @ R @ Matrix.Translation(-h) @ M
    bpy.context.view_layer.update()

POSES = {
 'A_brazos_abajo': [('LUpperArm','Y',60),('RUpperArm','Y',-60)],
 'B_brazos_arriba_codos': [('LUpperArm','Y',-75),('RUpperArm','Y',75),('LLowerArm','Z',-115),('RLowerArm','Z',115)],
 'C_sentadilla': [('LUpperLeg','X',-100),('RUpperLeg','X',-100),('LLowerLeg','X',115),('RLowerLeg','X',115),('LUpperArm','Y',55),('RUpperArm','Y',-55)],
 'D_giro_cruzar': [('Spine','Z',25),('Chest','Z',15),('LUpperArm','Z',-75),('LLowerArm','Z',-80),('RUpperArm','Y',-50),('RLowerArm','Z',100)],
}

def apply_pose(arm, pose):
    m = hmap(arm)
    arm.data.pose_position = 'POSE'
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()
    if pose is None: return []
    miss = []
    steps = sorted(POSES[pose], key=lambda s: _depth(arm.pose.bones[m[s[0]]]) if s[0] in m else 0)
    for k, ax, d in steps:
        if k not in m: miss.append(k); continue
        rot_world(arm, m[k], ax, d)
    return miss


def deform_sheet(code, lay):
    v = lay[code]; c = bpy.data.collections['REV_' + code]
    arms = [o for o in c.all_objects if o.type == 'ARMATURE']
    if not arms: return None, {}
    a = arms[0]
    if a.animation_data: a.animation_data.action = None
    solo([code])
    hid = [o for o in c.objects if o.type in ('EMPTY', 'FONT')]
    for o in hid: o.hide_viewport = True
    h = v['h']; rows = []; miss = {}
    for vw, dirv in (('34', Vector((0.75, -0.75, 0.12))), ('lado', Vector((1, 0, 0.05)))):
        row = []
        for pz in POSES:
            miss[pz] = apply_pose(a, pz)
            tgt = Vector((v['x'], v['y'], h * 0.52)); loc = tgt + dirv.normalized() * h * 1.75
            p = os.path.join(CAP, '_tmp_%s_%s_%s.png' % (code, vw, pz))
            snap(p, tuple(loc), tuple(tgt), lens=50, res=(340, 460)); row.append(p)
        rows.append(row)
    apply_pose(a, None); a.data.pose_position = 'REST'
    for o in hid: o.hide_viewport = False
    out = os.path.join(CAP, 'P01_deformacion_%s.png' % code)
    sheet(rows, out)
    return out, miss


def set_act(T, name):
    if not T.animation_data: T.animation_data_create()
    T.animation_data.action = bpy.data.actions[name]
    try:
        sl = T.animation_data.action.slots
        if len(sl): T.animation_data.action_slot = sl[0]
    except Exception: pass

def torso_faces(mesh_obj, arm, keys=('Hip','Pelvis','Waist','Spine','Chest','Neck','Head','Clavicle','Shoulder')):
    """caras del torso/cabeza (vertices con peso dominante en esos huesos)"""
    me = mesh_obj.data; vg = {g.index: g.name for g in mesh_obj.vertex_groups}
    good = np.zeros(len(me.vertices), bool)
    for v in me.vertices:
        if not v.groups: continue
        g = max(v.groups, key=lambda x: x.weight)
        n = vg.get(g.group, '')
        if any(k.lower() in n.lower() for k in keys) and 'Twist' not in n and 'arm' not in n.lower(): good[v.index] = True
    return [p.index for p in me.polygons if all(good[i] for i in p.vertices)]

def clip_metrics(T, mesh, act, bones, step=1, torso=None, hand_pts=None):
    """bones: dict hip, lf, rf (pies), lh, rh (manos). Devuelve metricas en metros/segundos (30 fps)."""
    from mathutils.bvhtree import BVHTree
    sc = bpy.context.scene; set_act(T, act)
    a = bpy.data.actions[act]; f0, f1 = int(a.frame_range[0]), int(a.frame_range[1])
    W = T.matrix_world
    def wp(b, tail=False):
        pb = T.pose.bones[b]; return W @ (pb.tail if tail else pb.head)
    hip = []; feet = {k: [] for k in ('lf', 'rf')}; toes = {k: [] for k in ('lf', 'rf')}; minz = []; inside = []
    poses = {}
    dg = None
    for f in range(f0, f1 + 1, step):
        sc.frame_set(f)
        hip.append(wp(bones['hip']))
        for k in ('lf', 'rf'):
            feet[k].append(wp(bones[k])); toes[k].append(wp(bones[k], tail=True))
        if f == f0 or f == f1:
            poses[f] = {pb.name: pb.matrix.copy() for pb in T.pose.bones}
        if mesh is not None and (f - f0) % 2 == 0:
            dg = bpy.context.evaluated_depsgraph_get(); ev = mesh.evaluated_get(dg); me = ev.to_mesh()
            n = len(me.vertices); co = np.empty(n * 3, np.float32); me.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
            M = np.array(mesh.matrix_world); P = co @ M[:3, :3].T + M[:3, 3]
            minz.append(float(P[:, 2].min()))
            if torso:
                bvh = BVHTree.FromPolygons([Vector(p) for p in P], [tuple(me.polygons[i].vertices) for i in torso])
                worst = 0.0
                for b in hand_pts or ():
                    for tail in (False, True):
                        p = wp(b, tail); hit = bvh.find_nearest(p)
                        if hit[0] is not None:
                            d = (p - hit[0]).dot(hit[1])
                            if d < worst: worst = d
                inside.append((f, round(-worst, 3)))
            ev.to_mesh_clear()
    dur = (f1 - f0) / 30.0
    dxy = (hip[-1] - hip[0]); dxy.z = 0
    speed = dxy.length / dur if dur > 0 else 0
    vz = (hip[-1].z - hip[0].z) / dur if dur > 0 else 0
    # patinaje: frames de apoyo = pie a menos de 3.5 cm de su altura minima
    skate = {}
    for k in ('lf', 'rf'):
        zs = [min(p.z, q.z) for p, q in zip(feet[k], toes[k])]; zmin = min(zs)
        best = 0.0; run = []
        for i, z in enumerate(zs):
            if z < zmin + 0.035: run.append(i)
            else:
                if len(run) >= 3:
                    pts = [toes[k][j] for j in run]
                    d = max(((p - pts[0]).to_2d()).length for p in pts); best = max(best, d)
                run = []
        if len(run) >= 3:
            pts = [toes[k][j] for j in run]; best = max(best, max(((p - pts[0]).to_2d()).length for p in pts))
        skate[k] = round(best * 100, 1)
    # loop: diferencia de pose primer/ultimo frame (grados, locales sin raiz)
    loopdeg = None
    if f0 in poses and f1 in poses:
        mx = 0.0
        for n, m0 in poses[f0].items():
            if n in (bones['hip'], 'BoneRoot'): continue
            m1 = poses[f1][n]
            d = math.degrees(m0.to_quaternion().rotation_difference(m1.to_quaternion()).angle)
            mx = max(mx, d)
        loopdeg = round(mx, 1)
    hz0, hz1 = hip[0].z, hip[-1].z
    return dict(frames='%d-%d' % (f0, f1), dur=round(dur, 2), mps=round(speed, 2), vz=round(vz, 2), dir=[round(dxy.x, 2), round(dxy.y, 2)],
                loop_deg=loopdeg, loop_hipz_cm=round((hz1 - hz0) * 100, 1), patin_cm=skate,
                piso_min_cm=round(min(minz) * 100, 1) if minz else None,
                mano_dentro_cm=round(max([x[1] for x in inside] or [0]) * 100, 1),
                mano_dentro_frames=[x[0] for x in inside if x[1] > 0.02][:12])


def cam_label(text, res, lens=50):
    cam = bpy.data.objects['_CamRev']
    t = bpy.data.objects.get('_lblCam')
    if not t:
        cu = bpy.data.curves.new('_lblCam', 'FONT'); t = bpy.data.objects.new('_lblCam', cu)
        bpy.context.scene.collection.objects.link(t); t.data.materials.append(mat('_TextoAmarillo', (1, 0.9, 0.1), 4))
    t.parent = cam; t.matrix_parent_inverse.identity()
    if cam.data.type == 'ORTHO':
        w = cam.data.ortho_scale; d = 1.0
    else:
        d = 0.5; big = 36.0 / cam.data.lens * d
        w = big if res[0] >= res[1] else big * res[0] / res[1]
    h = w * res[1] / res[0]
    if cam.data.type == 'ORTHO' and res[1] > res[0]: h = w; w = h * res[0] / res[1]
    t.data.body = text; t.data.size = w * 0.045
    t.location = (-w / 2 + w * 0.02, h / 2 - w * 0.06, -d); t.rotation_euler = (0, 0, 0)
    t.hide_viewport = not bool(text)

def snapL(path, loc, target, label='', lens=50, ortho=None, res=(1280, 720), frame=None):
    cam('_CamRev', loc, target, lens, ortho)
    if frame is not None: bpy.context.scene.frame_set(frame)
    cam_label(label, res, lens)
    return snap(path, loc, target, lens, ortho, res, None)

def clip_sheet(T, act, out, n=10, views=('lado', 'frente'), hip='Hip', res=(300, 420), dist=3.4, zc=0.95, extra_frames=None, title=''):
    set_act(T, act); a = bpy.data.actions[act]; f0, f1 = int(a.frame_range[0]), int(a.frame_range[1])
    fr = extra_frames or sorted(set(int(round(f0 + (f1 - f0) * i / (n - 1))) for i in range(n)))
    dirs = {'lado': Vector((1, 0, 0.06)), 'frente': Vector((0, -1, 0.08)), 'q': Vector((0.75, -0.65, 0.2)), 'atras': Vector((0, 1, 0.1))}
    rows = []
    for v in views:
        row = []
        for k, f in enumerate(fr):
            bpy.context.scene.frame_set(f)
            hp = T.matrix_world @ T.pose.bones[hip].head
            tgt = Vector((hp.x, hp.y, zc if zc is not None else hp.z)); loc = tgt + dirs[v].normalized() * dist
            p = os.path.join(CAP, '_tmp_c_%s_%s_%02d.png' % (act, v, k))
            lab = ('%s  %s' % (title or act, v) if k == 0 else '') + ('\n' if k == 0 else '') + 'f%d' % f
            snapL(p, tuple(loc), tuple(tgt), label=lab, lens=50, res=res, frame=f); row.append(p)
        rows.append(row)
    cam_label('', res)
    return sheet(rows, out)
