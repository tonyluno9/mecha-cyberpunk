
import bpy, math, os
from mathutils import Vector
import cc_tools as C

CREATED = []

def _rig(suffix, offset, meshes):
    if suffix is None:
        T = bpy.data.objects['PJ01']; T.location = Vector(offset)
        for m in meshes:
            o = bpy.data.objects[m]; o.hide_set(False); o.hide_viewport = False; o.hide_render = False
            if m != 'PJ01_Malla': CREATED.append(('show', m))
        return T
    objs = C.dup_rig(suffix, offset, meshes)
    for o in objs:
        o.hide_set(False); o.hide_viewport = False; o.hide_render = False
        CREATED.append(('obj', o.name))
    return objs[0]

def setup(specs, travel=0.0, cycles=1):
    """specs: [{'suffix':None|'REV_x','offset':(x,y,z),'meshes':[...],'actions':[...]}]"""
    end = 1
    for s in specs:
        T = _rig(s.get('suffix'), s.get('offset', (0,0,0)), s.get('meshes', ['PJ01_Malla']))
        names = list(s['actions']) * cycles
        end = max(end, C.nla_sequence(T, names))
        if travel:
            T.animation_data.action = None
            base = Vector(s.get('offset', (0,0,0)))
            L = int(bpy.data.actions[s['actions'][0]].frame_range[1]) - 1
            # keyframes on object location (constant steps)
            act = bpy.data.actions.new('REV_loc_' + T.name); act.use_fake_user = False
            slot = act.slots.new(id_type='OBJECT', name=T.name)
            T.animation_data.action = act; T.animation_data.action_slot = slot
            for c in range(cycles):
                T.location = base + Vector((0, -travel * c, 0))
                T.keyframe_insert('location', frame=1 + c * L)
            for l in act.layers:
                for st in l.strips:
                    for cb in st.channelbags:
                        for fc in cb.fcurves:
                            for k in fc.keyframe_points: k.interpolation = 'CONSTANT'
            CREATED.append(('locact', T.name, act.name))
    sc = bpy.context.scene; sc.frame_start = 1; sc.frame_end = end
    return end

def follow_cam(loc, target, lens, travel_total, f0, f1, name='Cam_Revision'):
    cam = C.camera(name, loc, target, lens)
    if travel_total:
        e = bpy.data.objects.get('REV_CamFollow')
        if e is None:
            e = bpy.data.objects.new('REV_CamFollow', None); bpy.context.scene.collection.objects.link(e)
        e.location = (0, 0, 0); e.keyframe_insert('location', frame=f0)
        e.location = (0, -travel_total, 0); e.keyframe_insert('location', frame=f1)
        ad = e.animation_data
        for l in ad.action.layers:
            for st in l.strips:
                for cb in st.channelbags:
                    for fc in cb.fcurves:
                        for k in fc.keyframe_points: k.interpolation = 'LINEAR'
        e.location = (0, 0, 0)
        cam.parent = e
        CREATED.append(('obj', e.name))
    CREATED.append(('obj', cam.name))
    return cam

def snap_cam(path, frame, res=(1200, 800), cam_name='Cam_Revision'):
    sc = bpy.context.scene; sc.camera = bpy.data.objects[cam_name]
    sc.frame_set(frame)
    r = sc.render; keep = (r.resolution_x, r.resolution_y, r.filepath)
    r.resolution_x, r.resolution_y = res; r.filepath = path
    win = bpy.context.window
    for area in win.screen.areas:
        if area.type == 'VIEW_3D':
            region = [x for x in area.regions if x.type == 'WINDOW'][0]
            with bpy.context.temp_override(window=win, area=area, region=region):
                bpy.ops.render.opengl(write_still=True, view_context=False)
            break
    r.resolution_x, r.resolution_y, r.filepath = keep
    return path

def finish(fname):
    fp = C.save_revision(fname)
    cleanup()
    return fp

def cleanup():
    T = bpy.data.objects['PJ01']
    for item in reversed(CREATED):
        if item[0] == 'obj':
            o = bpy.data.objects.get(item[1])
            if o: bpy.data.objects.remove(o, do_unlink=True)
        elif item[0] == 'show':
            o = bpy.data.objects.get(item[1])
            if o: o.hide_set(True); o.hide_render = True
        elif item[0] == 'locact':
            a = bpy.data.actions.get(item[2])
            if a: bpy.data.actions.remove(a)
    CREATED.clear()
    if T.animation_data:
        T.animation_data.action = None
        for t in list(T.animation_data.nla_tracks): T.animation_data.nla_tracks.remove(t)
    T.location = (0, 0, 0)
    for pb in T.pose.bones: pb.scale = (1, 1, 1)
    for a in list(bpy.data.actions):
        if a.users == 0 and a.name.startswith(('REV_', 'REV_loc_')): bpy.data.actions.remove(a)
    for m in list(bpy.data.meshes):
        if m.users == 0: bpy.data.meshes.remove(m)
    for c in list(bpy.data.cameras):
        if c.users == 0: bpy.data.cameras.remove(c)
