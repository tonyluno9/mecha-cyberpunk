
import bpy, bmesh, numpy as np
from mathutils import Vector, Matrix
import cc_veh as V
import cc_cars as K

def depth(o):
    d=0
    while o.parent: o=o.parent; d+=1
    return d

def scale_vehicle(code, S):
    """uniform rigid scale of a whole vehicle about its root origin (mesh data + child positions)"""
    root=bpy.data.objects[code]
    loc=root.location.copy(); root.location=(0,0,0)
    bpy.context.view_layer.update()
    objs=sorted(root.children_recursive,key=depth)
    mws={o.name:o.matrix_world.copy() for o in objs}
    done=set()
    for o in objs:
        if o.type=='MESH' and o.data.name not in done:
            o.data.transform(Matrix.Scale(S,4)); done.add(o.data.name)
    for dlev in sorted({depth(o) for o in objs}):
        for o in [x for x in objs if depth(x)==dlev]:
            mw=mws[o.name]
            t,r,s=mw.decompose()
            o.matrix_world=Matrix.LocRotScale(t*S,r,s)
        bpy.context.view_layer.update()
    return loc

def plate_to_engine(code, w, root):
    """replace the flat Corte_Interior plate by the shaped dark engine"""
    names=[m.name if m else '' for m in w.data.materials]
    if 'Corte_Interior' not in names: return None
    mi=names.index('Corte_Interior')
    idx=[p.index for p in w.data.polygons if p.material_index==mi]
    if not idx: return None
    P=np.array([tuple(w.data.vertices[v].co) for i in idx for v in w.data.polygons[i].vertices])
    bm=bmesh.new(); bm.from_mesh(w.data); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in idx],context='FACES'); bm.to_mesh(w.data); bm.free()
    K.clean_loose(w)
    yf,yr=float(P[:,1].min()),float(P[:,1].max()); xh=float(np.abs(P[:,0]).max())
    zt=float(P[:,2].max()); zb=max(0.2,zt-0.45)
    old=bpy.data.objects.get(code+'_Motor')
    if old: bpy.data.objects.remove(old)
    V.engine_mesh(code,yf,yr,xh,zt,zb,root)
    return dict(yf=round(yf,2),yr=round(yr,2),xh=round(xh,2),zt=round(zt,2),zb=round(zb,2),n=len(idx))

def auto_fence(w, hood_yr=None):
    P=np.array([tuple(v.co) for v in w.data.vertices]); mn,mx=P.min(0),P.max(0); L=mx[1]-mn[1]; H=mx[2]
    y0=(hood_yr-0.25) if hood_yr is not None else mn[1]+0.22*L
    return (y0, mx[1]-0.06*L, 0.5*H, H+0.02)

def clear_old_glass(code):
    root=bpy.data.objects[code]
    for o in list(root.children_recursive):
        if ('Cristal' in o.name or 'Interior' in o.name) and o.name.startswith(code): bpy.data.objects.remove(o)


def unglass(code):
    """put glass faces back into the body (original material), delete Cristal objects (not door children)"""
    w=bpy.data.objects[code+'_Carroceria']; root=bpy.data.objects[code]
    bm=bmesh.new(); bm.from_mesh(w.data); n0=len(bm.verts)
    k=0
    for g in [o for o in root.children if 'Cristal' in o.name]:
        me=g.data.copy(); me.transform(w.matrix_world.inverted()@g.matrix_world)
        for p in me.polygons: p.material_index=0
        me.materials.clear()
        bm.from_mesh(me); bpy.data.meshes.remove(me); bpy.data.objects.remove(g); k+=1
    bm.verts.ensure_lookup_table()
    bmesh.ops.remove_doubles(bm,verts=bm.verts[:],dist=1e-5)
    bm.to_mesh(w.data); bm.free()
    return k


def ray_panes(code, w, view, zmin, zmax, umin, umax, step=0.025, thr=0.12, zbot=0.85, name='Cristal_X', root=None, inset=0.02):
    """glass panes for open window holes. view: '+y' (rays travel +y, from front), '-y' (from rear), '-x' (from +x side), '+x'.
    A cell is window if its hit is deeper than the frame around it by > thr (or no hit), and it is enclosed:
    shallow cells left/right in its row and a shallow cell below (z>=zbot) and above in its column."""
    from mathutils.bvhtree import BVHTree
    me=w.data
    T=BVHTree.FromPolygons([v.co for v in me.vertices],[list(p.vertices) for p in me.polygons])
    axis={'+y':(1,1),'-y':(1,-1),'-x':(0,-1),'+x':(0,1)}[view]
    ai,sg=axis; ui=1-ai if ai==1 else 1   # u axis: x for y-views, y for x-views
    ui=0 if ai==1 else 1
    us=np.arange(umin,umax+1e-6,step); zs=np.arange(zmin,zmax+1e-6,step)
    D=np.full((len(zs),len(us)),np.inf)
    start=-sg*10.0
    for i,z in enumerate(zs):
        for j,u in enumerate(us):
            o=[0,0,z]; o[ui]=u; o[ai]=start
            d=[0,0,0]; d[ai]=sg
            h=T.ray_cast(Vector(o),Vector(d),20)
            if h[0] is not None: D[i,j]=(h[0][ai]-start)*sg
    shallow=np.zeros_like(D,bool); win=np.zeros_like(D,bool); depth=np.full_like(D,np.nan)
    # per row: runs of deep cells bounded by shallow cells
    for i in range(len(zs)):
        row=D[i]; fin=np.isfinite(row)
        if fin.sum()<3: continue
        j=0
        while j<len(us):
            if not fin[j]: j+=1; continue
            # find next cell deeper than row[j]+thr
            k=j+1
            while k<len(us) and fin[k] and abs(row[k]-row[j])<thr: k+=1
            if k>=len(us): break
            if row[k]<row[j]-thr: j=k; continue
            # run of deep cells from k
            m=k
            while m<len(us) and (not np.isfinite(row[m]) or row[m]>row[k-1]+thr): m+=1
            if m<len(us) and abs(row[m]-row[k-1])<thr*1.5:
                a,b=row[k-1],row[m]
                for q in range(k,m):
                    t=(q-(k-1))/(m-(k-1)); win[i,q]=True; depth[i,q]=a+(b-a)*t
            j=m
    # column check: enclosed below (shallow at z>=zbot) and above
    for j in range(len(us)):
        col=win[:,j]; i=0
        while i<len(zs):
            if not col[i]: i+=1; continue
            k=i
            while k<len(zs) and col[k]: k+=1
            below_ok = i>0 and np.isfinite(D[i-1,j]) and zs[i-1]>=zbot and D[i-1,j]<np.nanmin(depth[i:k,j])+thr
            above_ok = k<len(zs) and np.isfinite(D[k,j])
            if not (below_ok and above_ok): win[i:k,j]=False
            i=k
    # keep components with >= 12 cells
    lab=np.zeros_like(D,int); n=0
    for i in range(len(zs)):
        for j in range(len(us)):
            if win[i,j] and lab[i,j]==0:
                n+=1; st=[(i,j)]; lab[i,j]=n
                while st:
                    a,b=st.pop()
                    for da,db in ((1,0),(-1,0),(0,1),(0,-1)):
                        x,y=a+da,b+db
                        if 0<=x<len(zs) and 0<=y<len(us) and win[x,y] and lab[x,y]==0: lab[x,y]=n; st.append((x,y))
    for k in range(1,n+1):
        if (lab==k).sum()<12: win[lab==k]=False
    if not win.any(): return None
    bm=bmesh.new(); nodes={}
    def node(i,j):
        if (i,j) in nodes: return nodes[(i,j)]
        ds=[depth[a,b] for a in (i-1,i) for b in (j-1,j) if 0<=a<len(zs) and 0<=b<len(us) and win[a,b]]
        dd=float(np.mean(ds))+inset
        p=[0,0,0]; p[ui]=umin+(j-0.5)*step; p[2]=zmin+(i-0.5)*step; p[ai]=start+sg*dd
        v=bm.verts.new(p); nodes[(i,j)]=v; return v
    for i in range(len(zs)):
        for j in range(len(us)):
            if win[i,j]:
                vs=[node(i,j),node(i,j+1),node(i+1,j+1),node(i+1,j)]
                f=bm.faces.new(vs)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces[:])
    me2=bpy.data.meshes.new(code+'_'+name); bm.to_mesh(me2); bm.free()
    ob=bpy.data.objects.new(code+'_'+name,me2)
    for cl in root.users_collection: cl.objects.link(ob)
    ob.parent=root
    me2.materials.append(V.cristal_mat())
    return ob
