
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
