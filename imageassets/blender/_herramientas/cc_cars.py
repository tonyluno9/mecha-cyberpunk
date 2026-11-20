
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector, Matrix
import cc_tools as C

def snap_ortho(path, loc, target, scale, res=(800,500), up=(0,0,1)):
    sc=bpy.context.scene
    cam=bpy.data.objects.get('Cam_Ortho')
    if cam is None:
        cd=bpy.data.cameras.new('Cam_Ortho'); cam=bpy.data.objects.new('Cam_Ortho',cd); sc.collection.objects.link(cam)
    cam.data.type='ORTHO'; cam.data.ortho_scale=scale; cam.data.clip_end=200
    cam.location=Vector(loc)
    d=(Vector(target)-Vector(loc)).normalized()
    cam.rotation_euler=(d.to_track_quat('-Z','Y').to_euler() if abs(d.z)<0.99 else Vector((0,0,0)).to_track_quat('-Z','Y').to_euler() if False else __import__('mathutils').Euler((0,0,0))) 
    sc.camera=cam
    sc.render.resolution_x,sc.render.resolution_y=res; sc.render.resolution_percentage=100
    sc.render.image_settings.file_format='PNG'; sc.render.filepath=path
    sh=sc.display.shading; sh.light='STUDIO'; sh.color_type='TEXTURE'
    win=bpy.context.window
    area=[a for a in win.screen.areas if a.type=='VIEW_3D'][0]
    region=[r for r in area.regions if r.type=='WINDOW'][0]
    with bpy.context.temp_override(window=win,area=area,region=region):
        bpy.ops.render.opengl(write_still=True,view_context=False)
    return path

def clean_loose(o):
    bm=bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    bm.to_mesh(o.data); bm.free(); o.data.update()

def world_pts(o):
    M=o.matrix_world
    return np.array([tuple(M@v.co) for v in o.data.vertices])

def orient(o, rotz_deg, keep_xy=None):
    """rotate mesh data about Z, center in XY, ground at z=0 (object at origin, transforms applied)"""
    R=Matrix.Rotation(math.radians(rotz_deg),4,'Z')
    o.data.transform(o.matrix_world); o.matrix_world=Matrix.Identity(4)
    o.data.transform(R)
    P=np.array([tuple(v.co) for v in o.data.vertices])
    mn=P.min(0); mx=P.max(0)
    t=Vector((-(mn[0]+mx[0])/2,-(mn[1]+mx[1])/2,-mn[2]))
    o.data.transform(Matrix.Translation(t))
    o.data.update()
    return t

def set_origin(o, p):
    """move object origin to world point p without moving geometry"""
    p=Vector(p); M=o.matrix_world.copy()
    local=M.inverted()@p
    o.data.transform(Matrix.Translation(-local))
    o.matrix_world=M@Matrix.Translation(local)

def separate_faces(o, face_idx, name):
    """separate given polygon indices into new object (same transform)"""
    bm=bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table()
    sel=set(face_idx)
    me2=o.data.copy(); me2.name=name
    bm2=bmesh.new(); bm2.from_mesh(me2); bm2.faces.ensure_lookup_table()
    bmesh.ops.delete(bm2,geom=[f for f in bm2.faces if f.index not in sel],context='FACES')
    bm2.to_mesh(me2); bm2.free()
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.index in sel],context='FACES')
    bm.to_mesh(o.data); bm.free(); o.data.update()
    o2=bpy.data.objects.new(name,me2); o2.matrix_world=o.matrix_world.copy()
    for c in o.users_collection: c.objects.link(o2)
    return o2

def bisect(o, co, no):
    bm=bmesh.new(); bm.from_mesh(o.data)
    geom=bm.verts[:]+bm.edges[:]+bm.faces[:]
    bmesh.ops.bisect_plane(bm,geom=geom,plane_co=Vector(co),plane_no=Vector(no),dist=1e-6)
    bm.to_mesh(o.data); bm.free(); o.data.update()

def faces_in(o, test):
    """indices of polygons whose all vertices satisfy test(np point)"""
    V=np.array([tuple(v.co) for v in o.data.vertices])
    ok=np.array([bool(test(p)) for p in V])
    return [p.index for p in o.data.polygons if all(ok[i] for i in p.vertices)]

def export_vehicle(root, path):
    vl=bpy.context.view_layer
    for ob in vl.objects: ob.select_set(False)
    objs=[root]+[c for c in root.children_recursive]
    for ob in objs: ob.hide_set(False); ob.select_set(True)
    vl.objects.active=root
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'EMPTY','MESH'},global_scale=1.0,apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=False,add_leaf_bones=False,
        bake_anim=False,path_mode='COPY',embed_textures=False,use_mesh_modifiers=True)
    return path
