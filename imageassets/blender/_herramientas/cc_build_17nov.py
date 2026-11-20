
import bpy, bmesh, os, math, zipfile
import numpy as np
from mathutils import Vector, Matrix
import cc_cars as K, cc_tools as C
CAR=os.path.join(C.ROOT,'carros'); F=os.path.join(CAR,'_fuentes')
M3=os.path.join(os.path.dirname(C.ROOT),'modelos3d')
SRCS={'V01A':'piezas/V01CARROZA.zip','V01B':'piezas/V01BCARROZA.zip','V05':'piezas/V05CARROZA.zip','V07':'V07_MotoReparto.zip','V08':'V08.zip','V11':'V11_CamionetaPolicia.zip'}
POS={'V07':(0,8),'V08':(1.2,8),'V11':(2.4,8),'V01A':(3.6,8),'V01B':(4.8,8),'V05':(6.0,8)}

def import_src(code):
    if 'SRC_'+code in bpy.data.objects: return bpy.data.objects['SRC_'+code]
    d=os.path.join(F,code+('P' if code=='V05' else ''))
    if not os.path.exists(d):
        os.makedirs(d,exist_ok=True)
        with zipfile.ZipFile(os.path.join(M3,SRCS[code])) as f: f.extractall(d)
    fbx=[os.path.join(r,x) for r,ds,fs in os.walk(d) for x in fs if x.lower().endswith('.fbx')][0]
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fbx)
    new=[o for o in bpy.data.objects if o not in before]
    ms=[o for o in new if o.type=='MESH']
    for o in new:
        if o.type!='MESH': bpy.data.objects.remove(o)
    o=ms[0]; o.name='SRC_'+code
    o.data.transform(o.matrix_world); o.matrix_world=Matrix.Identity(4)
    o.location=(0,-4,0); o.hide_set(True)
    return o

def work_copy(code,suffix):
    src=import_src(code)
    col=bpy.data.collections.get(code) or bpy.data.collections.new(code)
    if col.name not in bpy.context.scene.collection.children: bpy.context.scene.collection.children.link(col)
    w=bpy.data.objects.new(code+'_'+suffix,src.data.copy()); w.data.name=code+'_'+suffix
    col.objects.link(w)
    root=bpy.data.objects.get(code) or bpy.data.objects.new(code,None)
    if root.name not in col.objects: col.objects.link(root)
    root.location=(POS[code][0],POS[code][1],0)
    return w,col,root

def parent_all(code,root):
    for o in bpy.data.objects:
        if o.name.startswith(code+'_') and o.type=='MESH':
            o.parent=root; o.matrix_parent_inverse.identity()

def ru01(code,col,nm,loc,r,W):
    src=bpy.data.objects['SRC_RU01COCHE']
    me=src.data.copy(); me.name=code+'_'+nm
    me.transform(Matrix.Diagonal((W/0.392,r/0.49,r/0.49,1)))
    ob=bpy.data.objects.new(code+'_'+nm,me); col.objects.link(ob); ob.location=loc
    return ob

def build_moto(code,wheels):
    w,col,root=work_copy(code,'Cuerpo')
    K.orient(w,90)
    for nm,cy,cz,r,hw in wheels:
        idx=K.faces_in(w,lambda p,cy=cy,cz=cz,r=r,hw=hw:(p[1]-cy)**2+(p[2]-cz)**2<=r*r and abs(p[0])<=hw)
        ob=K.separate_faces(w,idx,code+'_'+nm); K.clean_loose(ob)
        P=np.array([tuple(v.co) for v in ob.data.vertices]); c=(P.min(0)+P.max(0))/2
        K.set_origin(ob,(0,c[1],c[2]))
    K.clean_loose(w); parent_all(code,root)

def build_v11():
    code='V11'; w,col,root=work_copy(code,'Carroceria')
    K.orient(w,0)
    CZ=0.13
    idx=[]
    for sx in (1,-1):
        for cy in (-0.2765,0.2195):
            idx+=K.faces_in(w,lambda p,sx=sx,cy=cy: sx*p[0]>=0.115 and ((p[1]-cy)**2+(p[2]-CZ)**2)**0.5<=0.127)
    bm=bmesh.new(); bm.from_mesh(w.data); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in set(idx)],context='FACES'); bm.to_mesh(w.data); bm.free(); K.clean_loose(w)
    for nm,sx,cy in (('Rueda_DI',1,-0.2765),('Rueda_DD',-1,-0.2765),('Rueda_TI',1,0.2195),('Rueda_TD',-1,0.2195)):
        ru01(code,col,nm,(sx*0.17,cy,CZ),0.13,0.11)
    parent_all(code,root)

def build_carroza(code,r,cz,yf,yr,W):
    w,col,root=work_copy(code,'Carroceria')
    K.orient(w,90)
    w.data.transform(Matrix.Translation((0,0,r-cz))); w.data.update()
    P=np.array([tuple(v.co) for v in w.data.vertices]); xm=P[:,0].max(); xc=xm-0.008-W/2
    for nm,sx,cy in (('Rueda_DI',1,yf),('Rueda_DD',-1,yf),('Rueda_TI',1,yr),('Rueda_TD',-1,yr)):
        ru01(code,col,nm,(sx*xc,cy,r),r,W)
    parent_all(code,root)

def build_all():
    build_moto('V07',[('Rueda_D',-0.341,0.147,0.150,0.045),('Rueda_T',0.288,0.147,0.150,0.05)])
    build_moto('V08',[('Rueda_D',-0.347,0.159,0.158,0.07),('Rueda_T',0.338,0.142,0.145,0.07)])
    build_v11()
    build_carroza('V01A',0.088,0.026,-0.266,0.296,0.075)
    build_carroza('V01B',0.092,0.028,-0.276,0.294,0.08)
    build_carroza('V05',0.086,0.020,-0.274,0.264,0.072)

def cut_hood(code, yf, yr, xh, zmin, tol=0.015):
    import bpy, bmesh
    import numpy as np
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    import cc_cars as K
    w=bpy.data.objects[code+'_Carroceria']
    for co,no in (((0,yf,0),(0,1,0)),((0,yr,0),(0,1,0)),((xh,0,0),(1,0,0)),((-xh,0,0),(1,0,0))):
        K.bisect(w,co,no)
    me=w.data
    V=[v.co.copy() for v in me.vertices]; Fc=[list(p.vertices) for p in me.polygons]
    T=BVHTree.FromPolygons(V,Fc)
    sel=[]
    for p in me.polygons:
        c=p.center
        if not (yf<c.y<yr and abs(c.x)<xh and c.z>zmin): continue
        h=T.ray_cast(Vector((c.x,c.y,2.0)),Vector((0,0,-1)),3.0)
        if h[0] is not None and c.z>h[0].z-tol and p.normal.z>-0.2: sel.append(p.index)
    hood=K.separate_faces(w,sel,code+'_Cofre'); K.clean_loose(hood); K.clean_loose(w)
    # underside (so it is visible when opened)
    bm=bmesh.new(); bm.from_mesh(hood.data)
    d=bmesh.ops.duplicate(bm,geom=bm.faces[:])
    nf=[g for g in d['geom'] if isinstance(g,bmesh.types.BMFace)]
    bmesh.ops.reverse_faces(bm,faces=nf)
    bmesh.ops.translate(bm,verts=[g for g in d['geom'] if isinstance(g,bmesh.types.BMVert)],vec=(0,0,-0.003))
    bm.to_mesh(hood.data); bm.free()
    P=np.array([tuple(v.co) for v in hood.data.vertices])
    # hinge at rear edge
    rear=P[P[:,1]>yr-0.01]
    hz=float(rear[:,2].max()) if len(rear) else float(P[:,2].max())
    K.set_origin(hood,(0,yr,hz))
    # dark engine bay plate under the opening
    import cc_cut
    zb=float(P[:,2].min())-0.025
    bm=bmesh.new()
    vs=[bm.verts.new(v) for v in ((-xh,yf,zb),(xh,yf,zb),(xh,yr,zb),(-xh,yr,zb))]
    bm.faces.new(vs)
    mb=bpy.data.meshes.new(code+'_Motor_tmp'); bm.to_mesh(mb); bm.free()
    mat=cc_cut.corte_mat()
    if mat.name not in [m.name for m in w.data.materials]: w.data.materials.append(mat)
    mi=[m.name for m in w.data.materials].index(mat.name)
    for p in mb.polygons: p.material_index=mi
    for m in w.data.materials: mb.materials.append(m)
    plate=bpy.data.objects.new(code+'_Motor_tmp',mb); bpy.context.scene.collection.objects.link(plate)
    plate.matrix_world=w.matrix_world.copy()
    with bpy.context.temp_override(active_object=w,selected_editable_objects=[w,plate],object=w):
        bpy.ops.object.join()
    return dict(n=len(sel),hinge=(0,yr,round(hz,4)),bbox=[P.min(0).round(3).tolist(),P.max(0).round(3).tolist()])

def cut_door(code, nm, sx, yf, yr, zb, zt, tol=0.03):
    import bpy, bmesh
    import numpy as np
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    import cc_cars as K, cc_cut
    w=bpy.data.objects[code+'_Carroceria']; root=bpy.data.objects[code]
    for co,no in (((0,yf,0),(0,1,0)),((0,yr,0),(0,1,0)),((0,0,zb),(0,0,1)),((0,0,zt),(0,0,1))):
        K.bisect(w,co,no)
    me=w.data
    V=[v.co.copy() for v in me.vertices]; Fc=[list(p.vertices) for p in me.polygons]
    T=BVHTree.FromPolygons(V,Fc)
    sel=[]
    for p in me.polygons:
        c=p.center
        if not (yf<c.y<yr and zb<c.z<zt and sx*c.x>0.12): continue
        h=T.ray_cast(Vector((sx*2.0,c.y,c.z)),Vector((-sx,0,0)),3.0)
        if h[0] is not None and sx*c.x>sx*h[0].x-tol: sel.append(p.index)
    door=K.separate_faces(w,sel,code+'_'+nm); K.clean_loose(door); K.clean_loose(w)
    bm=bmesh.new(); bm.from_mesh(door.data)
    d=bmesh.ops.duplicate(bm,geom=bm.faces[:])
    bmesh.ops.reverse_faces(bm,faces=[g for g in d['geom'] if isinstance(g,bmesh.types.BMFace)])
    bmesh.ops.translate(bm,verts=[g for g in d['geom'] if isinstance(g,bmesh.types.BMVert)],vec=(-sx*0.003,0,0))
    bm.to_mesh(door.data); bm.free()
    P=np.array([tuple(v.co) for v in door.data.vertices])
    xo=float(P[:,0].max()) if sx>0 else float(P[:,0].min())
    xin=float(P[:,0].min()) if sx>0 else float(P[:,0].max())
    hinge=Vector((xo-sx*0.01,yf,(zb+zt)/2))
    K.set_origin(door,root.matrix_world@hinge)
    mw=door.matrix_world.copy(); door.parent=root; door.matrix_world=mw
    # dark inner panel behind the opening
    mat=cc_cut.corte_mat(); names=[m.name for m in me.materials]
    if mat.name not in names: me.materials.append(mat); names.append(mat.name)
    mi=names.index(mat.name)
    bm=bmesh.new(); bm.from_mesh(me)
    xp=xin-sx*0.03
    q=[(xp,yf,zb),(xp,yr,zb),(xp,yr,zt),(xp,yf,zt)]
    if sx<0: q=q[::-1]
    vs=[bm.verts.new(v) for v in q]
    f=bm.faces.new(vs); f.material_index=mi
    bm.to_mesh(me); bm.free(); me.update()
    return dict(n=len(sel),hinge=[round(x,4) for x in hinge],xo=round(xo,3),xin=round(xin,3))
