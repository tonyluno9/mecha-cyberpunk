
import bpy, math
from mathutils import Vector, Matrix
import cc_tools as C
from cc_walk import Wp, Rw
UP=Vector((0,0,1))
def _search(fn,lo=-120,hi=120):
    best=None
    for i in range(lo,hi+1):
        e=fn(math.radians(i))+abs(i)*1e-5
        if best is None or e<best[0]: best=(e,math.radians(i))
    return best[1]
def kneel_foot(T,side='R',ankle_z=0.19,ball_z=0.035,w=1.0):
    if w<=1e-4: return None
    W=T.matrix_world; pb=T.pose.bones
    K=W@pb[side+'_Calf'].head; A=W@pb[side+'_Foot'].head
    FR=Rw(T,side+'_Foot').copy(); TR=Rw(T,side+'_ToeBase').copy()
    B_rel=(W@pb[side+'_ToeBase'].head)-A
    tip_rel=(W@pb[side+'_ToeBase'].tail)-(W@pb[side+'_ToeBase'].head)
    tz=A.z+(ankle_z-A.z)*w
    ax1=(A-K).normalized().cross(UP).normalized()
    a=_search(lambda r:abs((K+Matrix.Rotation(r,3,ax1)@(A-K)).z-tz))
    C._rot_bone_world(T,side+'_Calf',Matrix.Rotation(a,3,ax1),K)
    A2=W@pb[side+'_Foot'].head
    C.set_bone_world_rot(side+'_Foot',FR,T)
    bz=(A+B_rel).z+(ball_z-(A+B_rel).z)*w
    ax2=B_rel.normalized().cross(UP).normalized()
    b=_search(lambda r:abs((A2+Matrix.Rotation(r,3,ax2)@B_rel).z-bz))
    C.set_bone_world_rot(side+'_Foot',Matrix.Rotation(b,3,ax2)@FR,T)
    B2=W@pb[side+'_ToeBase'].head
    # toes: lay them flat (tip at ball height), keep heading
    C.set_bone_world_rot(side+'_ToeBase',TR,T)
    t_rel=(W@pb[side+'_ToeBase'].tail)-B2
    ax3=t_rel.normalized().cross(UP).normalized() if abs(t_rel.normalized().dot(UP))<0.99 else Vector((1,0,0))
    tgt_tz=B2.z+(t_rel.z)*(1-w)
    g=_search(lambda r:abs((B2+Matrix.Rotation(r,3,ax3)@t_rel).z-tgt_tz))
    C.set_bone_world_rot(side+'_ToeBase',Matrix.Rotation(g,3,ax3)@TR,T)
    bpy.context.view_layer.update()
    return [round(math.degrees(x),1) for x in (a,b,g)]
