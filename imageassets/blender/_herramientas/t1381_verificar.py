"""T-1381: reimporta los FBX exportados y reporta altura, huesos, frente y texturas."""
import bpy, sys, os, glob
from mathutils import Vector
d = sys.argv[sys.argv.index('--') + 1]
for fp in sorted(glob.glob(os.path.join(d, "*.fbx"))):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fp, ignore_leaf_bones=False)
    ms = [o for o in bpy.data.objects if o.type == 'MESH']; ar = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in ms:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            for i in range(3): mn[i] = min(mn[i], w[i]); mx[i] = max(mx[i], w[i])
    a = ar[0]; bn = a.data.bones
    fr = "?"
    for n in ('mixamorig:LeftFoot', 'mixamorig:LeftToeBase'):
        pass
    if 'mixamorig:LeftToeBase' in bn:
        fr = "-Y" if bn['mixamorig:LeftToeBase'].tail_local.y < bn['mixamorig:LeftFoot'].head_local.y else "+Y"
    imgs = [i.name for i in bpy.data.images if i.type == 'IMAGE' and (i.packed_file or i.size[0] > 0)]
    print("VER", os.path.basename(fp), "h=%.2f" % (mx.z - mn.z), "zmin=%.3f" % mn.z, "huesos", len(bn), "frente", fr,
          "vgrupos", len(ms[0].vertex_groups), "imgs", len(imgs), "escalaObj", tuple(round(x, 2) for x in ms[0].scale))
