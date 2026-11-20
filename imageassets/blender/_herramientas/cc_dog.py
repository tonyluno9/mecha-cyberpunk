
"""cc_dog.py - animacion procedural del perro robot E17 (rig propio de 19 huesos). Frente = -Y."""
import bpy, math
from mathutils import Vector, Matrix, Quaternion
import cc_tools as C
UP=Vector((0,0,1)); D=Vector((0,-1,0))
LEGS={'DI':('Pata_DI_1','Pata_DI_2','Pata_DI_3'),'DD':('Pata_DD_1','Pata_DD_2','Pata_DD_3'),
      'TI':('Pata_TI_1','Pata_TI_2','Pata_TI_3'),'TD':('Pata_TD_1','Pata_TD_2','Pata_TD_3')}
def ss(x): x=min(max(x,0.0),1.0); return x*x*(3-2*x)
def W(A): return A.matrix_world
def wp(A,b,tail=False):
    pb=A.pose.bones[b]; return W(A)@(pb.tail if tail else pb.head)
def wr(A,b): return (W(A)@A.pose.bones[b].matrix).to_3x3().normalized()
def rot(A,b,R,piv=None):
    C._rot_bone_world(A,b,R,wp(A,b) if piv is None else Vector(piv))
def move(A,b,d):
    pb=A.pose.bones[b]; M=W(A)@pb.matrix
    pb.matrix=W(A).inverted()@(Matrix.Translation(Vector(d))@M); bpy.context.view_layer.update()
def setrot(A,b,R3):
    pb=A.pose.bones[b]; Mw=W(A)@pb.matrix; h=Mw.to_translation()
    pb.matrix=W(A).inverted()@(Matrix.Translation(h)@R3.to_4x4()); pb.scale=(1,1,1); bpy.context.view_layer.update()
def reset(A):
    for pb in A.pose.bones:
        pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0); pb.scale=(1,1,1)
    bpy.context.view_layer.update()
def capture(A):
    if A.animation_data: A.animation_data.action=None
    reset(A)
    return {'R':{b.name:wr(A,b.name) for b in A.pose.bones},'foot':{k:wp(A,v[2]).copy() for k,v in LEGS.items()},
            'root':wp(A,'Raiz').copy(),'hip':wp(A,'Cadera').copy()}
def new_action(A,name):
    a=bpy.data.actions.get(name)
    if a: bpy.data.actions.remove(a)
    a=bpy.data.actions.new(name); a.use_fake_user=True; s=a.slots.new(id_type='OBJECT',name=A.name)
    if A.animation_data is None: A.animation_data_create()
    A.animation_data.action=a; A.animation_data.action_slot=s
    return a
def key(A,f):
    for pb in A.pose.bones:
        pb.keyframe_insert('rotation_quaternion',frame=f,group=pb.name); pb.keyframe_insert('location',frame=f,group=pb.name)
def legs_to(A,ref,targets,pitch=None):
    """targets: dict leg->world pos of paw-joint (cabeza del hueso 3)"""
    err=0
    for k,(b1,b2,b3) in LEGS.items():
        t=targets[k]
        pole=wp(A,b1)+Vector((0,0.6,-0.1))
        err=max(err,C.two_bone_ik(b1,b2,b3,t,pole,A))
        p=(pitch or {}).get(k,0.0)
        setrot(A,b3,Matrix.Rotation(math.radians(p),3,'X')@ref['R'][b3])
    return err
def body(A,ref,root_off,bob=0.0,pitch=0.0,roll=0.0,yaw=0.0,spine=0.0,neck=0.0,head=0.0,head_yaw=0.0,head_roll=0.0,tail=0.0,tail_yaw=0.0):
    move(A,'Raiz',root_off)
    move(A,'Cadera',(0,0,bob))
    rot(A,'Cadera',Matrix.Rotation(math.radians(yaw),3,'Z'),piv=wp(A,'Columna'))
    rot(A,'Cadera',Matrix.Rotation(math.radians(roll),3,'Y'),piv=wp(A,'Columna'))
    rot(A,'Cadera',Matrix.Rotation(math.radians(pitch),3,'X'),piv=wp(A,'Columna'))
    rot(A,'Columna',Matrix.Rotation(math.radians(spine),3,'X'))
    rot(A,'Cuello',Matrix.Rotation(math.radians(neck),3,'X'))
    rot(A,'Cabeza',Matrix.Rotation(math.radians(head_yaw),3,'Z')@Matrix.Rotation(math.radians(head_roll),3,'Y')@Matrix.Rotation(math.radians(head),3,'X'))
    rot(A,'Cola',Matrix.Rotation(math.radians(tail_yaw),3,'Z')@Matrix.Rotation(math.radians(tail),3,'X'))
    rot(A,'Cola2',Matrix.Rotation(math.radians(tail*0.8),3,'X'))
def gait(A,ref,name,v,N,offs,duty,lift,stride_k=1.0,h=-0.06,bob=0.02,spine_amp=0.0,pitch_amp=0.0,head_amp=2.0,tail_amp=8.0,land=0.0,bob_ph=0.0):
    new_action(A,name); Tc=N/30.0; L=v*duty*Tc*stride_k; err=0
    for f in range(N+1):
        t=f/30.0; ph=f/N
        reset(A)
        b=h-bob*math.cos(4*math.pi*(ph-bob_ph))
        body(A,ref,D*(v*t),bob=b,spine=spine_amp*math.sin(2*math.pi*ph),pitch=pitch_amp*math.sin(2*math.pi*(ph+0.25)),
             head=head_amp*math.cos(4*math.pi*ph),tail=tail_amp*math.sin(2*math.pi*ph),tail_yaw=tail_amp*math.cos(2*math.pi*ph))
        tg={}; pit={}
        for k,o in offs.items():
            s=(ph-o)%1.0; base=ref['foot'][k]+D*(v*t)+D*land
            if s<duty:
                u=s/duty; rel=D*(L/2-L*u); tg[k]=base+rel; pit[k]=0.0
            else:
                u=(s-duty)/(1-duty); rel=D*(-L/2+L*ss(u)); tg[k]=base+rel+UP*(lift*math.sin(math.pi*u)); pit[k]=-25*math.sin(math.pi*u)
        err=max(err,legs_to(A,ref,tg,pit))
        key(A,f+1)
    return dict(name=name,frames=(1,N+1),mps=v,err=round(err,4))
def keyed(A,ref,name,keys,N,legfn):
    """keys: lista (f, dict de canales de body + 'ox','oy','oz' desplazamiento raiz). legfn(f,p)->(targets,pitch)"""
    import cc_keys as K
    new_action(A,name); err=0
    chans=keys[0][1].keys()
    for f in range(N+1):
        p={c:K.sample(keys,f,c) for c in chans}
        reset(A)
        body(A,ref,Vector((p.get('ox',0),p.get('oy',0),p.get('oz',0))),**{k:v for k,v in p.items() if k not in ('ox','oy','oz') and not k.startswith('_')})
        tg,pit=legfn(f,p)
        err=max(err,legs_to(A,ref,tg,pit)); key(A,f+1)
    return dict(name=name,frames=(1,N+1),err=round(err,4))
