import bpy, os, json, math
from mathutils import Vector, Quaternion, Matrix
HERE=os.path.dirname(__file__)
FING=('Index','Mid','Ring','Pinky')
CURL={'Index':(16,20,10),'Mid':(20,22,12),'Ring':(24,24,12),'Pinky':(28,26,12)}
def _palm(B,s):
    h=B[s+'_Hand'].head_local
    n=(B[s+'_Index1'].head_local-h).cross(B[s+'_Pinky1'].head_local-h).normalized()
    if n.dot(B[s+'_Thumb1'].tail_local-h)<0: n=-n
    return n
def _d(b): return (b.tail_local-b.head_local).normalized()
def finger_pose(B,curl=CURL,spread_k=1.0):
    P={}
    for s in 'LR':
        n=_palm(B,s)
        D={f:_d(B['%s_%s1'%(s,f)]) for f in FING}
        mid=sum(D.values(),Vector()).normalized()
        for f in FING:
            for i,c in enumerate(curl[f],1):
                b=B['%s_%s%d'%(s,f,i)]; Ri=b.matrix_local.to_3x3().inverted(); d=_d(b)
                q=Quaternion((Ri@d.cross(n)).normalized(),math.radians(c))
                if i==1:
                    dp=d-n*d.dot(n); mp=mid-n*mid.dot(n)
                    ang=math.atan2(dp.cross(mp).dot(n),dp.dot(mp))*spread_k
                    q=Quaternion((Ri@n).normalized(),ang)@q
                P[b.name]=q
    return P
def thumb_axes(B,s):
    n=_palm(B,s); b=B[s+'_Thumb1']; Ri=b.matrix_local.to_3x3().inverted(); d=_d(b)
    return (Ri@n).normalized(),(Ri@d.cross(n)).normalized()
def load(path=None):
    return json.load(open(path or os.path.join(HERE,'mano_relajada.json')))
def apply_to_action(act, P=None):
    P=P or load()
    a=bpy.data.actions[act] if isinstance(act,str) else act
    cb=a.layers[0].strips[0].channelbag(a.slots[0])
    f0,f1=a.frame_range
    for n,q in P.items():
        q=list(q)
        for fc in [fc for fc in cb.fcurves if fc.data_path.startswith('pose.bones["%s"].rotation'%n)]:
            cb.fcurves.remove(fc)
        dp='pose.bones["%s"].rotation_quaternion'%n
        for i in range(4):
            fc=cb.fcurves.new(dp,index=i,group_name=n)
            fc.keyframe_points.add(2)
            fc.keyframe_points[0].co=(f0,q[i]); fc.keyframe_points[1].co=(f1,q[i])
            for k in fc.keyframe_points: k.interpolation='LINEAR'
            fc.update()
    return a


def _cb(act):
    a=bpy.data.actions[act] if isinstance(act,str) else act
    return a,a.layers[0].strips[0].channelbag(a.slots[0])
def sample_rot(act,bone):
    a,cb=_cb(act)
    fcs=[cb.fcurves.find('pose.bones["%s"].rotation_quaternion'%bone,index=i) for i in range(4)]
    f0,f1=[int(round(x)) for x in a.frame_range]
    Q=[]
    for f in range(f0,f1+1):
        q=Quaternion([fc.evaluate(f) if fc else (1 if i==0 else 0) for i,fc in enumerate(fcs)]).normalized()
        if Q and Q[-1].dot(q)<0: q=-q
        Q.append(q)
    return f0,Q
def write_rot(act,bone,f0,Q):
    a,cb=_cb(act)
    for fc in [fc for fc in cb.fcurves if fc.data_path.startswith('pose.bones["%s"].rotation'%bone)]:
        cb.fcurves.remove(fc)
    dp='pose.bones["%s"].rotation_quaternion'%bone
    for i in range(4):
        fc=cb.fcurves.new(dp,index=i,group_name=bone)
        fc.keyframe_points.add(len(Q))
        for j,q in enumerate(Q):
            k=fc.keyframe_points[j]; k.co=(f0+j,q[i]); k.interpolation='BEZIER'; k.handle_left_type=k.handle_right_type='AUTO_CLAMPED'
        fc.update()
def calm(act,bone,k=0.0,radius=3,loop=True):
    """Keep the cycle-mean rotation; deviation smoothed (cyclic gaussian) and scaled by k. k=0 -> constant."""
    f0,Q=sample_rot(act,bone)
    U=Q[:-1] if loop and len(Q)>2 and Q[0].rotation_difference(Q[-1]).angle<0.02 else Q
    m=Quaternion((0,0,0,0)); 
    for q in U: m=Quaternion(tuple(m[i]+(q[i] if q.dot(U[0])>=0 else -q[i]) for i in range(4)))
    m.normalize()
    devs=[]
    for q in U:
        d=m.inverted()@q
        if d.w<0: d=-d
        ax,an=d.to_axis_angle(); devs.append(Vector(ax)*an)
    n=len(U); sm=[]
    w=[math.exp(-(i*i)/(2*(radius/2.0)**2)) for i in range(-radius,radius+1)]
    for j in range(n):
        acc=Vector((0,0,0)); tw=0
        for o,ww in zip(range(-radius,radius+1),w):
            jj=(j+o)%n if loop else min(max(j+o,0),n-1)
            acc+=devs[jj]*ww; tw+=ww
        sm.append(acc/tw*k)
    out=[m@Quaternion(v.normalized(),v.length) if v.length>1e-9 else m.copy() for v in sm]
    if len(U)<len(Q): out.append(out[0].copy())
    write_rot(act,bone,f0,out)
    return [round(math.degrees(v.length),1) for v in sm]
def clear_fingers(act):
    a,cb=_cb(act)
    for fc in [fc for fc in cb.fcurves if any('_%s'%f in fc.data_path for f in ('Thumb','Index','Mid','Ring','Pinky')) and '.rotation' in fc.data_path]:
        cb.fcurves.remove(fc)


ARM_BONES=('Upperarm','UpperarmTwist01','UpperarmTwist02','Forearm','ForearmTwist01','ForearmTwist02','Hand')
def _gauss_quats(Q, i, radius, cyclic):
    import math
    n=len(Q); c=Q[i]; acc=[0.0]*4; tw=0.0; s=radius/2.0 if radius else 1
    for o in range(-radius, radius+1):
        j=i+o
        if cyclic: j%=n
        else: j=min(max(j,0),n-1)
        q=Q[j]; sg=1 if q.dot(c)>=0 else -1; w=math.exp(-(o*o)/(2*s*s))
        for k in range(4): acc[k]+=q[k]*sg*w
        tw+=w
    return Quaternion([a/tw for a in acc]).normalized()
def fix_arm_flips(act, T=None, cyclic=False, radius=4, passes=2):
    """Smooth arm-chain rotations (kills IK pole/twist flips) and re-IK hands to original positions."""
    import cc_tools as C
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    a=bpy.data.actions[act] if isinstance(act,str) else act
    ad.action=a; ad.action_slot=a.slots[0]
    f0,f1=[int(round(x)) for x in a.frame_range]
    frames=list(range(f0,f1+1))
    D=[]; tgt=[]
    for f in frames:
        sc.frame_set(f+1); sc.frame_set(f)
        D.append({pb.name:(pb.rotation_quaternion.copy(),pb.location.copy()) for pb in T.pose.bones})
        tgt.append({s:(T.matrix_world@T.pose.bones[s+'_Hand'].head).copy() for s in 'LR'})
    U=frames[:-1] if cyclic else frames
    nU=len(U)
    for s in 'LR':
        for b in ARM_BONES:
            nm=s+'_'+b
            Q=[D[i][nm][0] for i in range(nU)]
            for _ in range(passes):
                Q=[_gauss_quats(Q,i,radius,cyclic) for i in range(nU)]
            for i in range(nU): D[i][nm]=(Q[i],D[i][nm][1])
    if cyclic: D[-1]=D[0]
    ad.action=None
    out={}
    for i,f in enumerate(frames):
        for nm,(q,l) in D[i].items():
            pb=T.pose.bones[nm]; pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=q; pb.location=l
        bpy.context.view_layer.update()
        for s in 'LR':
            pole=T.matrix_world@T.pose.bones[s+'_Forearm'].head
            hq=T.pose.bones[s+'_Hand'].rotation_quaternion.copy()
            C.two_bone_ik(s+'_Upperarm',s+'_Forearm',s+'_Hand',tgt[i][s],pole,T)
        for pb in T.pose.bones: pb.scale=(1,1,1)
        out[f]={s+'_'+b:T.pose.bones[s+'_'+b].rotation_quaternion.copy() for s in 'LR' for b in ARM_BONES}
    for s in 'LR':
        for b in ARM_BONES:
            nm=s+'_'+b; Q=[out[f][nm] for f in frames]
            for j in range(1,len(Q)):
                if Q[j].dot(Q[j-1])<0: Q[j]=-Q[j]
            write_rot(a,nm,f0,Q)
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
    return a
def arm_report(act, T=None, thr=(25,25,4)):
    import math
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    a=bpy.data.actions[act]; ad.action=a; ad.action_slot=a.slots[0]
    f0,f1=[int(round(x)) for x in a.frame_range]
    prev=None; ev=[]; mx={'ua':0,'ha':0,'el':0,'fa':0}
    for f in range(f0,f1+1):
        sc.frame_set(f+1); sc.frame_set(f)
        cur={s:(T.pose.bones[s+'_Forearm'].head.copy(),T.pose.bones[s+'_Upperarm'].matrix.to_quaternion(),T.pose.bones[s+'_Hand'].matrix.to_quaternion(),T.pose.bones[s+'_Forearm'].matrix.to_quaternion()) for s in 'LR'}
        if prev:
            for s in 'LR':
                de=(cur[s][0]-prev[s][0]).length*100
                def ang(x,y):
                    v=x.rotation_difference(y).angle; return math.degrees(min(v,2*math.pi-v))
                ua=ang(prev[s][1],cur[s][1]); ha=ang(prev[s][2],cur[s][2]); fa=ang(prev[s][3],cur[s][3])
                mx['ua']=max(mx['ua'],ua); mx['ha']=max(mx['ha'],ha); mx['el']=max(mx['el'],de); mx['fa']=max(mx['fa'],fa)
        prev=cur
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
    return {k:round(v,1) for k,v in mx.items()}


def fix_reach(src, out_act, T=None, dmin_k=0.62, out_k=0.22, down_k=0.40, radius=4, sides='LR'):
    """Rebuild arm IK for a non-cyclic clip: hand path pushed forward where it would pass through the
    shoulder (arm fully folded), stable elbow pole (out/down from the shoulder), smoothed wrist."""
    import math, cc_tools as C
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data; W=T.matrix_world
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    a=bpy.data.actions[src]; ad.action=a; ad.action_slot=a.slots[0]
    f0,f1=[int(round(x)) for x in a.frame_range]; frames=list(range(f0,f1+1)); n=len(frames)
    D=[]; tgt=[]; sh=[]; fwd=[]
    for f in frames:
        sc.frame_set(f+1); sc.frame_set(f)
        D.append({pb.name:(pb.rotation_quaternion.copy(),pb.location.copy()) for pb in T.pose.bones})
        tgt.append({s:(W@T.pose.bones[s+'_Hand'].head).copy() for s in 'LR'})
        sh.append({s:(W@T.pose.bones[s+'_Upperarm'].head).copy() for s in 'LR'})
        hipM=(W@T.pose.bones['Hip'].matrix).to_3x3()
        fw=Vector((0,-1,0)); fwd.append(fw)
    Lmax={s:((T.pose.bones[s+'_Forearm'].head-T.pose.bones[s+'_Upperarm'].head).length+(T.pose.bones[s+'_Hand'].head-T.pose.bones[s+'_Forearm'].head).length)*W.to_scale()[0] for s in 'LR'}
    newt=[dict(t) for t in tgt]
    for s in sides:
        dmin=dmin_k*Lmax[s]
        off=[]
        for i in range(n):
            t=tgt[i][s]; c=sh[i][s]; k=0.0
            while (t+fwd[i]*k-c).length<dmin and k<1.0: k+=0.005
            off.append(k)
        sm=[]
        for i in range(n):
            acc=0; tw=0
            for o in range(-radius*2,radius*2+1):
                j=min(max(i+o,0),n-1); w=math.exp(-(o*o)/(2*(radius)**2)); acc+=off[j]*w; tw+=w
            sm.append(max(acc/tw,off[i]))
        for i in range(n): newt[i][s]=tgt[i][s]+fwd[i]*sm[i]
    # smooth hand local rotations (wrist)
    for s in sides:
        nm=s+'_Hand'; Q=[D[i][nm][0] for i in range(n)]
        for _ in range(2): Q=[_gauss_quats(Q,i,radius,False) for i in range(n)]
        for i in range(n): D[i][nm]=(Q[i],D[i][nm][1])
    ad.action=None; res={}
    for i,f in enumerate(frames):
        for nm,(q,l) in D[i].items():
            pb=T.pose.bones[nm]; pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=q; pb.location=l
        bpy.context.view_layer.update()
        for s in sides:
            sg=1 if s=='L' else -1
            pole=sh[i][s]+Vector((sg*out_k,0.05,-down_k))
            C.two_bone_ik(s+'_Upperarm',s+'_Forearm',s+'_Hand',newt[i][s],pole,T)
        for pb in T.pose.bones: pb.scale=(1,1,1)
        res[f]={s+'_'+b:T.pose.bones[s+'_'+b].rotation_quaternion.copy() for s in sides for b in ('Upperarm','Forearm','Hand')}
    for s in sides:
        for b in ('Upperarm','Forearm','Hand'):
            nm=s+'_'+b; Q=[res[f][nm] for f in frames]
            for j in range(1,len(Q)):
                if Q[j].dot(Q[j-1])<0: Q[j]=-Q[j]
            write_rot(out_act,nm,f0,Q)
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
    return {s:round(Lmax[s],3) for s in 'LR'}


def joint_reach(src, dst, T=None, cyclic=False, thr=0.008, settle=0.004, sides='LR', bones=ARM_BONES):
    """Replace each hand 'travel' segment by a smooth joint-space blend between its still endpoints
    (no IK flips; the hand lands exactly where the original grip/rest pose is)."""
    import math
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data; W=T.matrix_world
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    a=bpy.data.actions[src]; ad.action=a; ad.action_slot=a.slots[0]
    f0,f1=[int(round(x)) for x in a.frame_range]; frames=list(range(f0,f1+1)); n=len(frames)
    D=[]; H=[]; root=[]
    for f in frames:
        sc.frame_set(f+1); sc.frame_set(f)
        D.append({pb.name:pb.rotation_quaternion.copy() for pb in T.pose.bones})
        # hand position relative to the chest (so body motion doesn't count as hand travel)
        Ms=(W@T.pose.bones['Spine02'].matrix).inverted()
        H.append({s:(Ms@(W@T.pose.bones[s+'_Hand'].head)).copy()*W.to_scale()[0] for s in 'LR'})
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
    m=n-1 if cyclic else n
    segs={}
    for s in sides:
        v=[(H[(i+1)%m if cyclic else min(i+1,n-1)][s]-H[i][s]).length for i in range(m)]
        mov=[x>thr for x in v]
        S=[]; i=0
        # walk (handle wrap for cyclic by starting at a still frame)
        start=0
        if cyclic:
            st=[k for k in range(m) if v[k]<settle]
            start=st[0] if st else 0
        visited=0; k=start
        while visited<m:
            if mov[k%m]:
                a_=k-1
                while a_>k-m and v[a_%m]>=settle: a_-=1
                b_=k
                while b_<k+m and v[b_%m]>=settle: b_+=1
                b_+=1
                S.append((a_,b_)); visited+=b_-k; k=b_
            else:
                k+=1; visited+=1
            if not cyclic and k>=m: break
        segs[s]=[(x,y) for x,y in S if y-x>=3]
    def ss(x): x=max(0.0,min(1.0,x)); return x*x*(3-2*x)
    out=[dict(d) for d in D]
    for s in sides:
        for a_,b_ in segs[s]:
            ia=a_%m if cyclic else max(a_,0); ib=b_%m if cyclic else min(b_,n-1)
            for k in range(a_+1,b_):
                ii=k%m if cyclic else k
                if not cyclic and (ii<0 or ii>=n): continue
                t=ss((k-a_)/(b_-a_))
                for b in bones:
                    nm=s+'_'+b; qa=D[ia][nm]; qb=D[ib][nm]
                    if qa.dot(qb)<0: qb=-qb
                    out[ii][nm]=qa.slerp(qb,t)
    if cyclic: out[-1]=out[0]
    dsta=bpy.data.actions[dst]
    for s in sides:
        for b in bones:
            nm=s+'_'+b; Q=[out[i][nm] for i in range(n)]
            for j in range(1,n):
                if Q[j].dot(Q[j-1])<0: Q[j]=-Q[j]
            write_rot(dsta,nm,f0,Q)
    return segs


def ik_reach(base_act, dst, segs, T=None, dmin=0.32, elbow_r=0.28):
    """For each (side,a,b): hand follows a forward-arcing curve (chest space) from its pose at a to b,
    elbow direction slerps between endpoints; starts from base_act pose (joint blend) for continuous twist."""
    import math, cc_tools as C
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data; W=T.matrix_world
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    A=bpy.data.actions[base_act]; ad.action=A; ad.action_slot=A.slots[0]
    f0,f1=[int(round(x)) for x in A.frame_range]; frames=list(range(f0,f1+1)); n=len(frames)
    D=[]; CH=[]; SH=[]; HA=[]; EL=[]
    for f in frames:
        sc.frame_set(f+1); sc.frame_set(f)
        D.append({pb.name:(pb.rotation_quaternion.copy(),pb.location.copy()) for pb in T.pose.bones})
        CH.append((W@T.pose.bones['Spine02'].matrix).copy())
        SH.append({s:(W@T.pose.bones[s+'_Upperarm'].head).copy() for s in 'LR'})
        HA.append({s:(W@T.pose.bones[s+'_Hand'].head).copy() for s in 'LR'})
        EL.append({s:(W@T.pose.bones[s+'_Forearm'].head).copy() for s in 'LR'})
    def ss(x): x=max(0.0,min(1.0,x)); return x*x*(3-2*x)
    tg={}; po={}
    for s,a,b in segs:
        ia=a-f0; ib=b-f0
        P0=CH[ia].inverted()@HA[ia][s]; P2=CH[ib].inverted()@HA[ib][s]
        S0=CH[ia].inverted()@SH[ia][s]
        fw=(CH[ia].inverted().to_3x3()@Vector((0,-1,0))).normalized()
        E0=(CH[ia].inverted()@EL[ia][s]-S0).normalized(); S2=CH[ib].inverted()@SH[ib][s]; E2=(CH[ib].inverted()@EL[ib][s]-S2).normalized()
        d=0.0
        while d<0.6:
            P1=(P0+P2)*0.5+fw*d
            mind=min((((1-t)**2)*P0+2*(1-t)*t*P1+(t**2)*P2 - S0).length for t in [i/20 for i in range(21)])
            if mind*W.to_scale()[0]>=dmin: break
            d+=0.01
        for k in range(a+1,b):
            t=ss((k-a)/(b-a)); i=k-f0
            Pc=((1-t)**2)*P0+2*(1-t)*t*P1+(t**2)*P2
            tg[(s,k)]=CH[i]@Pc
            ed=E0.slerp(E2,t) if hasattr(E0,'slerp') else (E0*(1-t)+E2*t).normalized()
            Sc=CH[i].inverted()@SH[i][s]
            po[(s,k)]=CH[i]@(Sc+ed*elbow_r)
    ad.action=None
    out={}
    for i,f in enumerate(frames):
        todo=[s for s in 'LR' if (s,f) in tg]
        if not todo: continue
        for nm,(q,l) in D[i].items():
            pb=T.pose.bones[nm]; pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=q; pb.location=l
        bpy.context.view_layer.update()
        for s in todo:
            C.two_bone_ik(s+'_Upperarm',s+'_Forearm',s+'_Hand',tg[(s,f)],po[(s,f)],T)
            out[(s,f)]={b:T.pose.bones[s+'_'+b].rotation_quaternion.copy() for b in ('Upperarm','Forearm')}
    dsta=bpy.data.actions[dst]
    for s in 'LR':
        for b in ('Upperarm','Forearm'):
            nm=s+'_'+b; Q=[out[(s,f)][b] if (s,f) in out else D[f-f0][nm][0] for f in frames]
            for j in range(1,n):
                if Q[j].dot(Q[j-1])<0: Q[j]=-Q[j]
            write_rot(dsta,nm,f0,Q)
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
    return True


def arm_events(act, T=None, thr=30, rel_v=0.12):
    """Per-frame jumps measured relative to the parent bone (independent of whole-body rotation)."""
    import math
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data; W=T.matrix_world
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    a=bpy.data.actions[act]; ad.action=a; ad.action_slot=a.slots[0]
    f0,f1=[int(round(x)) for x in a.frame_range]
    def rel(c,p): return (T.pose.bones[p].matrix.inverted()@T.pose.bones[c].matrix).to_quaternion()
    def ang(x,y):
        v=x.rotation_difference(y).angle; return math.degrees(min(v,2*math.pi-v))
    prev=None; ev=[]; mx=0
    for f in range(f0,f1+1):
        sc.frame_set(f+1); sc.frame_set(f)
        cur={}
        for s in 'LR':
            Ms=(W@T.pose.bones['Spine02'].matrix).inverted()
            cur[s]=(rel(s+'_Upperarm','Spine02'),rel(s+'_Forearm',s+'_Upperarm'),rel(s+'_Hand',s+'_Forearm'),(Ms@(W@T.pose.bones[s+'_Hand'].head)))
        if prev:
            for s in 'LR':
                u=ang(prev[s][0],cur[s][0]); fo=ang(prev[s][1],cur[s][1]); h=ang(prev[s][2],cur[s][2])
                v=(cur[s][3]-prev[s][3]).length*W.to_scale()[0]
                mx=max(mx,u,fo,h)
                if max(u,fo,h)>thr or v>rel_v: ev.append((f,s,round(u),round(fo),round(h),round(v*100,1)))
        prev=cur
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
    return round(mx,1),ev


def clean_arms(src, dst, T=None, contact_v=0.03, smooth_r=2, pole_r=3):
    """Rebuild both arms frame by frame, seeded from the previous solved frame (continuous twist):
    hands planted exactly where they touch something, free hands follow a smoothed path (chest space),
    elbow pole smoothed, forearm roll follows the hand so the wrist never twists."""
    import math, cc_tools as C
    T=T or bpy.data.objects['PJ01']; sc=bpy.context.scene; ad=T.animation_data; W=T.matrix_world; Wi=W.inverted()
    mut=[(t,t.mute) for t in ad.nla_tracks]
    for t,_ in mut: t.mute=True
    keep=(ad.action,ad.action_slot)
    A=bpy.data.actions[src]; ad.action=A; ad.action_slot=A.slots[0]
    f0,f1=[int(round(x)) for x in A.frame_range]; frames=list(range(f0,f1+1)); n=len(frames)
    D=[];CH=[];H={'L':[],'R':[]};E={'L':[],'R':[]};HR={'L':[],'R':[]}
    for f in frames:
        sc.frame_set(f+1); sc.frame_set(f)
        D.append({pb.name:(pb.rotation_quaternion.copy(),pb.location.copy()) for pb in T.pose.bones})
        ch=(W@T.pose.bones['Spine02'].matrix).copy(); CH.append(ch)
        for s in 'LR':
            H[s].append((W@T.pose.bones[s+'_Hand'].head).copy()); E[s].append((W@T.pose.bones[s+'_Forearm'].head).copy())
            HR[s].append((W.to_3x3()@T.pose.bones[s+'_Hand'].matrix.to_3x3()).normalized().to_quaternion())
    def gauss_vec(V,r):
        out=[]
        for i in range(len(V)):
            acc=Vector();tw=0
            for o in range(-r*2,r*2+1):
                j=min(max(i+o,0),len(V)-1); w=math.exp(-(o*o)/(2*r*r)); acc+=V[j]*w; tw+=w
            out.append(acc/tw)
        return out
    def gauss_q(Q,r):
        return [_gauss_quats(Q,i,r,False) for i in range(len(Q))]
    TG={};PO={};RO={}; CON={}
    for s in 'LR':
        sp=[0]+[(H[s][i]-H[s][i-1]).length for i in range(1,n)]
        still=[sp[i]<contact_v*W.to_scale()[0]**0 and sp[min(i+1,n-1)]<contact_v for i in range(n)]
        con=[False]*n
        i=0
        while i<n:
            if still[i]:
                j=i
                while j<n and still[j]: j+=1
                if j-i>=3:
                    for k in range(i,j): con[k]=True
                i=j
            else: i+=1
        CON[s]=con
        hc=[CH[i].inverted()@H[s][i] for i in range(n)]; hcs=gauss_vec(hc,smooth_r)
        ec=[CH[i].inverted()@E[s][i] for i in range(n)]; ecs=gauss_vec(ec,pole_r)
        rc=[(CH[i].to_3x3().normalized().to_quaternion().inverted()@HR[s][i]) for i in range(n)]
        for k in range(1,n):
            if rc[k].dot(rc[k-1])<0: rc[k]=-rc[k]
        rcs=gauss_q(rc,smooth_r)
        # gaps between two contacts: hand orientation (chest space) eases from one grip to the next
        def ss_(x): x=max(0.0,min(1.0,x)); return x*x*(3-2*x)
        k=0
        while k<n:
            if not con[k]:
                g0=k
                while k<n and not con[k]: k+=1
                g1=k
                if g0>0 and g1<n:
                    qa=rc[g0-1]; qb=rc[g1]
                    if qa.dot(qb)<0: qb=-qb
                    pa=hc[g0-1]; pb_=hc[g1]
                    for kk in range(g0,g1):
                        t=ss_((kk-g0+1)/(g1-g0+1)); rcs[kk]=qa.slerp(qb,t)
            else: k+=1
        # keep free-hand targets away from the shoulder (no folded-arm flips)
        Lr=((T.pose.bones[s+'_Forearm'].bone.head_local-T.pose.bones[s+'_Upperarm'].bone.head_local).length+(T.pose.bones[s+'_Hand'].bone.head_local-T.pose.bones[s+'_Forearm'].bone.head_local).length)
        shc=CH[0].inverted()@(W@T.pose.bones[s+'_Upperarm'].head)
        # contact weight (1 = planted), eased over 3 frames
        w=[1.0 if c else 0.0 for c in con]
        for _ in range(3): w=[max(w[i], (w[i-1]+w[min(i+1,n-1)])*0.5*0.9) if 0<i<n-1 else w[i] for i in range(n)]
        TG[s]=[H[s][i].lerp(CH[i]@hcs[i],1-w[i]) for i in range(n)]
        PO[s]=[CH[i]@ecs[i] for i in range(n)]
        RO[s]=[HR[s][i].slerp((CH[i].to_3x3().normalized().to_quaternion()@rcs[i]),1-w[i]) for i in range(n)]
    ad.action=None
    arm=[s+'_'+b for s in 'LR' for b in ('Upperarm','Forearm','Hand')]
    prev=None; out={nm:[] for nm in arm}
    for i,f in enumerate(frames):
        for nm,(q,l) in D[i].items():
            pb=T.pose.bones[nm]; pb.rotation_quaternion=q if (prev is None or nm not in arm) else prev[nm]; pb.location=l
        bpy.context.view_layer.update()
        for s in 'LR':
            C.two_bone_ik(s+'_Upperarm',s+'_Forearm',s+'_Hand',TG[s][i],PO[s][i],T)
            # forearm roll follows the hand: align forearm X with target hand X around the forearm axis
            fb=T.pose.bones[s+'_Forearm']; Mf=(W@fb.matrix); ax=(Mf.to_3x3().col[1]).normalized()
            hx=(RO[s][i].to_matrix().col[0]); fx=Mf.to_3x3().col[0]
            hx=(hx-ax*hx.dot(ax)); fx=(fx-ax*fx.dot(ax))
            if hx.length>1e-6 and fx.length>1e-6:
                hx.normalize(); fx.normalize()
                ang=math.atan2(fx.cross(hx).dot(ax),fx.dot(hx))
                C._rot_bone_world(T,s+'_Forearm',Matrix.Rotation(ang*0.6,3,ax),W@fb.head)
            C.set_bone_world_rot(s+'_Hand',RO[s][i].to_matrix(),T)
        for pb in T.pose.bones: pb.scale=(1,1,1)
        prev={nm:T.pose.bones[nm].rotation_quaternion.copy() for nm in arm}
        for nm in arm: out[nm].append(prev[nm])
    d=bpy.data.actions[dst]
    for nm,Q in out.items():
        for j in range(1,n):
            if Q[j].dot(Q[j-1])<0: Q[j]=-Q[j]
        write_rot(d,nm,f0,Q)
    ad.action=keep[0]
    if keep[0]: ad.action_slot=keep[1]
    for t,m in mut: t.mute=m
    return {s:''.join('#' if c else '.' for c in CON[s]) for s in 'LR'}


def relax_free_fingers(act, ref_frame=None, thr=12.0, full=4.0, r=2):
    """Where the fingers are still in the clip's default (Mixamo) hand, swap it for the relaxed hand;
    keep intentional grips/pushes. Smooth weights so nothing pops."""
    import math
    P={k:Quaternion(v) for k,v in load().items()}
    a=bpy.data.actions[act] if isinstance(act,str) else act
    out={}
    for s in 'LR':
        names=[k for k in P if k.startswith(s+'_')]
        S={nm:sample_rot(a,nm) for nm in names}
        f0=S[names[0]][0]; n=len(S[names[0]][1]); i0=(ref_frame-f0) if ref_frame else 0
        dev=[]
        for i in range(n):
            m=0
            for nm in names:
                x=S[nm][1][i].rotation_difference(S[nm][1][i0]).angle; m=max(m,math.degrees(min(x,2*math.pi-x)))
            dev.append(m)
        w=[1.0 if d<=full else (0.0 if d>=thr else 1-(d-full)/(thr-full)) for d in dev]
        ws=[]
        for i in range(n):
            acc=0;tw=0
            for o in range(-r*2,r*2+1):
                j=min(max(i+o,0),n-1); g=math.exp(-(o*o)/(2*r*r)); acc+=w[j]*g; tw+=g
            ws.append(min(w[i],acc/tw) if w[i]<1 else acc/tw)
        for nm in names:
            Q=S[nm][1]; off=Q[i0].inverted()@P[nm]
            R=[Q[i]@Quaternion().slerp(off,ws[i]) for i in range(n)]
            for j in range(1,n):
                if R[j].dot(R[j-1])<0: R[j]=-R[j]
            write_rot(a,nm,f0,R)
        out[s]=''.join('R' if x>0.95 else ('.' if x<0.05 else '~') for x in ws)
    return out
