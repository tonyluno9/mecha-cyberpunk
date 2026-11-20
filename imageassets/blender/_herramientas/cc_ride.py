
import bpy, math
from mathutils import Vector, Matrix, Quaternion
import cc_tools as C
from cc_walk import Wp, Rw
GRIP={'L':Vector((0.285,-0.44,1.005)),'R':Vector((-0.285,-0.44,1.005))}
ARML=0.479
def wrap_center(T,s):
    W=T.matrix_world
    pts=[W@T.pose.bones[s+'_'+b].head for b in ('Index1','Index2','Index3','Mid1','Mid2','Mid3')]+[W@T.pose.bones[s+'_Mid3'].tail,W@T.pose.bones[s+'_Index3'].tail]
    return sum(pts,Vector())/len(pts)
def knuckle_ang(T,s):
    W=T.matrix_world
    d=W@T.pose.bones[s+'_Pinky1'].head-W@T.pose.bones[s+'_Index1'].head
    return math.atan2(d.y,d.x if s=='L' else -d.x)
def hand_targets(T,grip=GRIP,rot=True):
    W=T.matrix_world; MT={}
    for s in 'LR':
        Mh=(W@T.pose.bones[s+'_Hand'].matrix).copy(); Wc=wrap_center(T,s)
        th=(-knuckle_ang(T,s)*(1 if s=='L' else -1)) if rot else 0.0
        MT[s]=Matrix.Translation(grip[s])@Matrix.Rotation(th,4,'Z')@Matrix.Translation(-Wc)@Mh
    return MT
def lean(T):
    W=T.matrix_world; h=W@T.pose.bones['Hip'].head; n=W@T.pose.bones['Head'].head; d=n-h
    return math.degrees(math.atan2(-d.y,d.z))
def apply_ride(T,a,d,MT,w=1.0,pin_hands=True,dz=0.0,head_up=0.0):
    """current pose in T -> lean back by a (deg, split over waist/spine), hip forward d, head keeps world rot,
    hands to MT (blend w), feet stay."""
    W=T.matrix_world
    feet={s:(Wp(T,s+'_Foot').copy(),Wp(T,s+'_Calf').copy(),Rw(T,s+'_Foot').copy(),Rw(T,s+'_ToeBase').copy()) for s in 'LR'}
    head0=Rw(T,'Head').copy()
    hands0={s:(W@T.pose.bones[s+'_Hand'].matrix).copy() for s in 'LR'}
    elb0={s:(W@T.pose.bones[s+'_Forearm'].head).copy() for s in 'LR'}
    C.move_hip_world(C.hip_world(T)+Vector((0,-d*w,dz*w)),T)
    for b in ('Waist','Spine01','Spine02'): C.rot_world_about(b,a*w/3.0,'X',None,T)
    C.set_bone_world_rot('Head',head0,T)
    if head_up: C.rot_world_about('Head',-head_up*w,'X',None,T)
    if pin_hands:
        for s in 'LR':
            M0=hands0[s]; M1=MT[s]
            p=M0.translation.lerp(M1.translation,w)
            q=M0.to_quaternion().slerp(M1.to_quaternion(),w)
            sg=1 if s=='L' else -1
            sh=W@T.pose.bones[s+'_Upperarm'].head
            pole=elb0[s]+Vector((sg*0.15,0.05,-0.1))
            C.two_bone_ik(s+'_Upperarm',s+'_Forearm',s+'_Hand',p,pole,T)
            C.set_bone_world_rot(s+'_Hand',q.to_matrix(),T)
    for s in 'LR':
        a_,kn,rf,rt=feet[s]
        C.two_bone_ik(s+'_Thigh',s+'_Calf',s+'_Foot',a_,kn+Vector((0,-0.3,0.1)),T)
        C.set_bone_world_rot(s+'_Foot',rf,T); C.set_bone_world_rot(s+'_ToeBase',rt,T)
    for pb in T.pose.bones: pb.scale=(1,1,1)
    bpy.context.view_layer.update()


SEG=[('Hip','Waist'),('Waist','Spine01'),('Spine01','Spine02'),('Spine02','Head')]
def seg_angles(T):
    W=T.matrix_world; out=[]
    for a,b in SEG:
        d=W@T.pose.bones[b].head-W@T.pose.bones[a].head
        out.append(math.degrees(math.atan2(-d.y,d.z)))
    return out
def straighten(T,targets,MT,w=1.0,d=0.0,head_up=0.0,pin_hands=True):
    W=T.matrix_world
    feet={s:(Wp(T,s+'_Foot').copy(),Wp(T,s+'_Calf').copy(),Rw(T,s+'_Foot').copy(),Rw(T,s+'_ToeBase').copy()) for s in 'LR'}
    head0=Rw(T,'Head').copy()
    hands0={s:(W@T.pose.bones[s+'_Hand'].matrix).copy() for s in 'LR'}
    elb0={s:(W@T.pose.bones[s+'_Forearm'].head).copy() for s in 'LR'}
    if d: C.move_hip_world(C.hip_world(T)+Vector((0,-d*w,0)),T)
    for (a,b),tg in zip(SEG,targets):
        dv=W@T.pose.bones[b].head-W@T.pose.bones[a].head
        cur=math.degrees(math.atan2(-dv.y,dv.z))
        C.rot_world_about(a,(tg-cur)*w,'X',None,T)
    C.set_bone_world_rot('Head',head0,T)
    if head_up: C.rot_world_about('Head',-head_up*w,'X',None,T)
    if pin_hands:
        for s in 'LR':
            M0=hands0[s]; M1=MT[s]
            p=M0.translation.lerp(M1.translation,w); q=M0.to_quaternion().slerp(M1.to_quaternion(),w)
            sg=1 if s=='L' else -1
            pole=elb0[s]+Vector((sg*0.15,0.05,-0.1))
            C.two_bone_ik(s+'_Upperarm',s+'_Forearm',s+'_Hand',p,pole,T)
            C.set_bone_world_rot(s+'_Hand',q.to_matrix(),T)
    for s in 'LR':
        a_,kn,rf,rt=feet[s]
        C.two_bone_ik(s+'_Thigh',s+'_Calf',s+'_Foot',a_,kn+Vector((0,-0.3,0.1)),T)
        C.set_bone_world_rot(s+'_Foot',rf,T); C.set_bone_world_rot(s+'_ToeBase',rt,T)
    for pb in T.pose.bones: pb.scale=(1,1,1)
    bpy.context.view_layer.update()


def bake_ride(act, MT, delta, wfun, T=None, head_up=6.0):
    """Apply straighten (per-frame: current seg angles + delta*w) and hands->MT with weight w(f)."""
    import cc_hands as HN
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    a=bpy.data.actions[act]; ad.action=a; ad.action_slot=a.slots[0]
    f0,f1=[int(round(x)) for x in a.frame_range]
    D=[]
    for f in range(f0,f1+1):
        sc.frame_set(f+1); sc.frame_set(f)
        D.append({pb.name:(pb.rotation_quaternion.copy(),pb.location.copy()) for pb in T.pose.bones})
    ad.action=None
    out=[]
    for i,f in enumerate(range(f0,f1+1)):
        for nm,(q,l) in D[i].items():
            pb=T.pose.bones[nm]; pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=q; pb.location=l
        bpy.context.view_layer.update()
        w=wfun(f)
        if w>1e-4:
            cur=seg_angles(T); tg=[c+dd for c,dd in zip(cur,delta)]
            straighten(T,tg,MT,w,0.0,head_up=head_up,pin_hands=True)
        out.append({pb.name:(pb.rotation_quaternion.copy(),pb.location.copy()) for pb in T.pose.bones})
    names=list(out[0].keys())
    for nm in names:
        Q=[o[nm][0] for o in out]
        for j in range(1,len(Q)):
            if Q[j].dot(Q[j-1])<0: Q[j]=-Q[j]
        HN.write_rot(a,nm,f0,Q)
    # hip location
    cb=a.layers[0].strips[0].channelbag(a.slots[0])
    for nm in ('Hip',):
        for idx in range(3):
            fc=cb.fcurves.find('pose.bones["%s"].location'%nm,index=idx)
            if fc is None: fc=cb.fcurves.new('pose.bones["%s"].location'%nm,index=idx,group_name=nm)
            fc.keyframe_points.clear(); fc.keyframe_points.add(len(out))
            for j,o in enumerate(out): fc.keyframe_points[j].co=(f0+j,o[nm][1][idx]); fc.keyframe_points[j].interpolation='LINEAR'
            fc.update()
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
