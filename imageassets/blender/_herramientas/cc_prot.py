
import bpy, math
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
import cc_hands as H

ARMB=('Upperarm','Forearm','Hand','Thumb','Index','Mid','Ring','Pinky','Elbow')

def _ra(h,ax,ang): return Matrix.Translation(h)@Matrix.Rotation(math.radians(ang),4,ax)@Matrix.Translation(-h)

def body_polys(body):
    ex=set(g.index for g in body.vertex_groups if g.name.startswith('R_') and any(k in g.name for k in ARMB))
    excl=set(v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in ex)>0.3)
    return [list(p.vertices) for p in body.data.polygons if not any(i in excl for i in p.vertices)]

def gauntlet_local(arm,o,step=12):
    Bi=(arm.matrix_world@arm.data.bones['R_Forearm'].matrix_local).inverted()
    return [Bi@(o.matrix_world@o.data.vertices[i].co) for i in range(0,len(o.data.vertices),step)]

def body_tree(body,polys):
    dg=bpy.context.evaluated_depsgraph_get()
    ev=body.evaluated_get(dg); bm=ev.to_mesh(); V=[body.matrix_world@v.co for v in bm.vertices]; ev.to_mesh_clear()
    return BVHTree.FromPolygons(V,polys)

def depth(T,AW,M,GL,k=3):
    """k-th largest inside depth (robust to single-vertex false positives)"""
    W=AW@M; top=[0.0]*k
    for q in GL:
        p=W@q; loc,n,i,d=T.find_nearest(p,0.1)
        if loc is not None and (p-loc).dot(n)<0 and d>top[-1]:
            top.append(d); top.sort(reverse=True); top=top[:k]
    return top[-1]

def frame_geom(arm):
    pu=arm.pose.bones['R_Upperarm']; pf=arm.pose.bones['R_Forearm']; ps=arm.pose.bones['Spine02']
    S=ps.matrix.to_3x3()
    hinge=(pu.matrix.to_3x3().col[1]).cross(pf.matrix.to_3x3().col[1])
    if hinge.length<1e-4: hinge=pu.matrix.to_3x3().col[0]
    return pu.head.copy(), pf.head.copy(), pf.matrix.copy(), S.col[0].normalized(), S.col[2].normalized(), hinge.normalized()

def delta_M(g,a,b,c):
    h,e,Mf,ax0,ax2,hin=g
    Rs=_ra(h,ax0,a)@_ra(h,ax2,b); return _ra(Rs@e,Rs.to_3x3()@hin,c)@Rs@Mf, Rs

def search(arm,body,o,frames,thr=0.003,arange=20,astep=2.5,crange=20,cstep=5,step=12):
    sc=bpy.context.scene; AW=arm.matrix_world
    polys=body_polys(body); GL=gauntlet_local(arm,o,step)
    na=int(arange/astep); nc=int(crange/cstep)
    G=[(i*astep,j*astep,k*cstep) for i in range(-na,na+1) for j in range(-na,na+1) for k in range(-nc,nc+1)]
    G.sort(key=lambda t:t[0]**2+t[1]**2+0.6*t[2]**2)
    res={}
    for f in frames:
        sc.frame_set(f); T=body_tree(body,polys); g=frame_geom(arm)
        d0=depth(T,AW,g[2],GL)
        if d0<thr: res[f]=(0,0,0); continue
        best=None
        for a,b,c in G:
            if depth(T,AW,delta_M(g,a,b,c)[0],GL)<thr: best=(a,b,c); break
        res[f]=best if best else (0,0,0)
    return res

def smooth_profile(res,f0,f1,dil=3,sig=2.0):
    fr=list(range(f0,f1+1)); keys=sorted(res)
    def interp(f,k):
        if f<=keys[0]: return res[keys[0]][k]
        if f>=keys[-1]: return res[keys[-1]][k]
        for i in range(len(keys)-1):
            if keys[i]<=f<=keys[i+1]:
                t=(f-keys[i])/(keys[i+1]-keys[i]); return res[keys[i]][k]*(1-t)+res[keys[i+1]][k]*t
    raw={f:[interp(f,k) for k in range(3)] for f in fr}
    dl={}
    for f in fr:
        v=[]
        for k in range(3):
            win=[raw[g][k] for g in fr if abs(g-f)<=dil]
            v.append(max(win,key=abs))
        dl[f]=v
    out={}
    for f in fr:
        ws=0; acc=[0,0,0]
        for g in fr:
            if abs(g-f)>3*sig: continue
            w=math.exp(-((g-f)/sig)**2/2); ws+=w
            for k in range(3): acc[k]+=w*dl[g][k]
        out[f]=[x/ws for x in acc]
    return out

def apply_profile(arm,act,prof,off):
    sc=bpy.context.scene
    pu=arm.pose.bones['R_Upperarm']; pf=arm.pose.bones['R_Forearm']
    QU={}; QF={}
    for f,(a,b,c) in prof.items():
        sc.frame_set(f); g=frame_geom(arm)
        Mu=pu.matrix.copy()
        Mfn,Rs=delta_M(g,a,b,c)
        pu.matrix=Rs@Mu; bpy.context.view_layer.update()
        pf.matrix=Mfn; bpy.context.view_layer.update()
        QU[f-off]=pu.rotation_quaternion.copy(); QF[f-off]=pf.rotation_quaternion.copy()
    for bn,Qd in (('R_Upperarm',QU),('R_Forearm',QF)):
        f0,Q=H.sample_rot(act,bn); Q=list(Q)
        for af,q in Qd.items():
            i=int(round(af-f0))
            if 0<=i<len(Q):
                if q.dot(Q[i])<0: q=-q
                Q[i]=q
        H.write_rot(act,bn,f0,Q)
    sc.frame_set(sc.frame_current)

def check(arm,body,o,frames,thr=0.003,step=4):
    sc=bpy.context.scene; AW=arm.matrix_world
    polys=body_polys(body); GL=gauntlet_local(arm,o,step); bad=[]
    for f in frames:
        sc.frame_set(f); T=body_tree(body,polys)
        d=depth(T,AW,arm.pose.bones['R_Forearm'].matrix,GL)
        if d>thr: bad.append((f,round(d,3)))
    return bad
