
import bpy, math, sys
from mathutils import Vector, Matrix
import cc_tools as C
P = dict(dz=0.15, dy=-0.05, lean=10.0, prot=6.0, wc=Vector((3.575,-0.578,0.80)), wr=0.168, ga=22.0,
         ankL=Vector((3.60,-0.56,0.235)), ankR=Vector((3.40,-0.56,0.235)), toe_dy=-0.155, toe_dz=-0.05,
         wrist_back=0.075, wrist_out=0.025, steer_k=1.5, steer_mean=4.0)
def W(T,b): return T.matrix_world @ T.pose.bones[b].head
def seat_pose(T, steer_deg=0.0, P=P, xoff=None):
    xo = T.matrix_world.translation.x - 3.5 if xoff is None else xoff
    off = Vector((xo,0,0))
    h = C.hip_world(T)
    C.move_hip_world(h + Vector((0, P['dy'], -P['dz'])), T)
    # lean torso forward about waist (negative = back); forward = +deg about X
    C.rot_world_about('Waist', P['lean'], 'X', None, T)
    # keep head level
    C.rot_world_about('NeckTwist01', -P['lean']*0.8, 'X', None, T)
    # protraction of clavicles (shoulders forward)
    C.rot_world_about('L_Clavicle', P['prot'], 'Z', None, T)
    C.rot_world_about('R_Clavicle', -P['prot'], 'Z', None, T)
    bpy.context.view_layer.update()
    # legs
    for s, ank in (('L', P['ankL']), ('R', P['ankR'])):
        a = ank + off
        pole = a + Vector((0.08 if s=='L' else -0.08, 0.25, 0.6))
        C.two_bone_ik(s+'_Thigh', s+'_Calf', s+'_Foot', a, pole, T)
        C.aim_bone(s+'_Foot', a + Vector((0, P['toe_dy'], P['toe_dz'])), T)
    # arms on wheel
    errs = {}
    for s, sgn in (('L', 1), ('R', -1)):
        ang = math.radians(90 - sgn*(90 - P['ga']) + steer_deg)
        # L at ga deg (right side +x), R at 180-ga
        ang = math.radians(P['ga'] + steer_deg) if s=='L' else math.radians(180 - P['ga'] + steer_deg)
        rad = Vector((math.cos(ang), 0, math.sin(ang)))
        rim = P['wc'] + off + rad * P['wr']
        wrist = rim + Vector((0, P['wrist_back'], 0)) + rad * P['wrist_out']
        sh = W(T, s+'_Upperarm')
        pole = sh + Vector((sgn*0.45, 0.1, -0.6))
        errs[s] = C.two_bone_ik(s+'_Upperarm', s+'_Forearm', s+'_Hand', wrist, pole, T)
        C.aim_bone(s+'_Hand', rim + rad*P['wrist_out'] + Vector((0,-0.04,0)), T)
    for pb in T.pose.bones: pb.scale = (1,1,1)
    bpy.context.view_layer.update()
    return errs
