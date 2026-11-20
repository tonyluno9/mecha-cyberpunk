
import bpy, math
from mathutils import Vector, Quaternion, Matrix
from mathutils.bvhtree import BVHTree
import cc_tools as C, cc_hands as HN
FING=('Index','Mid','Ring','Pinky','Thumb')
CURL={'Index':(1,1.2,0.8),'Mid':(1,1.2,0.8),'Ring':(1,1.2,0.8),'Pinky':(1,1.2,0.8),'Thumb':(0.6,1,1)}
def local_bvh(root_name='REVW03_Root'):
    root=bpy.data.objects[root_name]; Ri=root.matrix_world.inverted(); V=[];P=[]
    dg=bpy.context.evaluated_depsgraph_get()
    for o in root.children:
        if o.type!='MESH': continue
        m=o.evaluated_get(dg).to_mesh(); n=len(V)
        V+=[Ri@(o.matrix_world@v.co) for v in m.vertices]; P+=[[n+i for i in p.vertices] for p in m.polygons]
        o.evaluated_get(dg).to_mesh_clear()
    return BVHTree.FromPolygons(V,P)
def _inside(bvh,q):
    hits=0
    for dvec in (Vector((0,0,1)),Vector((1,0,0)),Vector((0,1,0))):
        o=q.copy(); c=0
        for _ in range(40):
            loc,nor,idx,d=bvh.ray_cast(o,dvec,5.0)
            if loc is None: break
            c+=1; o=loc+dvec*1e-4
        hits+=c%2
    return hits>=2
def sdist(bvh,root,p):
    q=root.matrix_world.inverted()@p; loc,nor,idx,d=bvh.find_nearest(q,1.0)
    if loc is None: return 1.0
    d=d*root.matrix_world.to_scale()[0]
    return -d if _inside(bvh,q) else d
def nearest_w(bvh,root,p):
    q=root.matrix_world.inverted()@p; loc,nor,idx,d=bvh.find_nearest(q,1.0)
    return root.matrix_world@loc
def bend_axes(T):
    B=T.data.bones; ax={}
    for s in 'LR':
        n=HN._palm(B,s)
        for f in FING:
            for i in (1,2,3):
                b=B['%s_%s%d'%(s,f,i)]; Ri=b.matrix_local.to_3x3().inverted()
                ax[b.name]=(Ri@HN._d(b).cross(n)).normalized()
    return ax
def palm_point(T,s):
    W=T.matrix_world
    return W@T.pose.bones[s+'_Hand'].head*0.35+W@T.pose.bones[s+'_Mid1'].head*0.65
def solve_curl(T,bvh,root,ax,sides='LR',gap=0.004,ks=None):
    """current pose: per finger find extra curl k (deg scale) so the tip is ~gap from the weapon, nothing inside."""
    W=T.matrix_world; ks=ks or [x*5 for x in range(-6,13)]
    out={}
    for s in sides:
        for f in FING:
            names=['%s_%s%d'%(s,f,i) for i in (1,2,3)]
            base=[T.pose.bones[n].rotation_quaternion.copy() for n in names]
            best=None
            for k in ks:
                for n,q,w in zip(names,base,CURL[f]):
                    T.pose.bones[n].rotation_quaternion=q@Quaternion(ax[n],math.radians(k*w))
                bpy.context.view_layer.update()
                pts=[W@T.pose.bones[n].tail for n in names]+[W@T.pose.bones[names[1]].head,W@T.pose.bones[names[2]].head]
                ds=[sdist(bvh,root,p) for p in pts]
                tip=ds[2]
                pen=sum(max(0,-d+0.0) for d in ds)
                e=abs(tip-gap)+pen*10
                if best is None or e<best[0]: best=(e,k,round(tip*100,2))
            for n,q,w in zip(names,base,CURL[f]):
                T.pose.bones[n].rotation_quaternion=q@Quaternion(ax[n],math.radians(best[1]*w))
            bpy.context.view_layer.update()
            out[s+f]=(best[1],best[2])
    return out


def box_d(q,bmin,bmax):
    dx=max(bmin.x-q.x,0,q.x-bmax.x); dy=max(bmin.y-q.y,0,q.y-bmax.y); dz=max(bmin.z-q.z,0,q.z-bmax.z)
    inside=bmin.x<q.x<bmax.x and bmin.y<q.y<bmax.y and bmin.z<q.z<bmax.z
    if inside: return -min(q.x-bmin.x,bmax.x-q.x,q.y-bmin.y,bmax.y-q.y,q.z-bmin.z,bmax.z-q.z)
    return (dx*dx+dy*dy+dz*dz)**0.5
def curl_to_box(T,root,bmin,bmax,ax,side='L',gap=0.006,fingers=('Index','Mid','Ring','Pinky'),ks=None):
    W=T.matrix_world; Ri=root.matrix_world.inverted(); ks=ks or [x*5 for x in range(-6,19)]
    out={}
    for f in fingers:
        names=['%s_%s%d'%(side,f,i) for i in (1,2,3)]
        base=[T.pose.bones[n].rotation_quaternion.copy() for n in names]
        best=None
        for k in ks:
            for n,q,w in zip(names,base,CURL[f]): T.pose.bones[n].rotation_quaternion=q@Quaternion(ax[n],math.radians(k*w))
            bpy.context.view_layer.update()
            pts=[Ri@(W@T.pose.bones[n].head) for n in names[1:]]+[Ri@(W@T.pose.bones[names[2]].tail)]
            # sample along segments too
            segs=[]
            for n in names:
                a=Ri@(W@T.pose.bones[n].head); b=Ri@(W@T.pose.bones[n].tail)
                segs+= [a.lerp(b,t) for t in (0.33,0.66,1.0)]
            ds=[box_d(q,bmin,bmax) for q in segs]
            tip=box_d(pts[-1],bmin,bmax)
            pen=sum(max(0,0.004-d) for d in ds)   # keep finger centerline >=4mm off the surface (skin)
            e=abs(tip-gap)+pen*20
            if best is None or e<best[0]: best=(e,k,round(tip*100,2),round(min(ds)*100,2))
        for n,q,w in zip(names,base,CURL[f]): T.pose.bones[n].rotation_quaternion=q@Quaternion(ax[n],math.radians(best[1]*w))
        bpy.context.view_layer.update()
        out[side+f]=best[1:]
    return out
