# T-1380: inventario de piezas de cada FBX (nombres, dims, origen vs centro)
# uso: blender -b --python t1380_survey.py -- <carpeta> <salida.json> COD1 COD2 ...
import bpy, sys, os, json
import numpy as np
argv = sys.argv[sys.argv.index('--') + 1:]
src = argv[0]; out = argv[1]; codes = argv[2:]
res = {}
for code in codes:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fp = os.path.join(src, code + '.fbx')
    bpy.ops.import_scene.fbx(filepath=fp)
    info = []
    for o in bpy.context.scene.objects:
        d = {'name': o.name, 'type': o.type, 'parent': o.parent.name if o.parent else None}
        if o.type == 'MESH':
            M = o.matrix_world
            P = np.array([tuple(M @ v.co) for v in o.data.vertices])
            if len(P):
                mn = P.min(0); mx = P.max(0); c = (mn + mx) / 2
                d.update(nv=len(P), npoly=len(o.data.polygons),
                         dims=[round(float(x), 3) for x in (mx - mn)],
                         org=[round(float(x), 3) for x in M.translation],
                         off=[round(float(x), 3) for x in (np.array(M.translation) - c)],
                         mats=[m.name for m in o.data.materials if m])
        else:
            d['org'] = [round(float(x), 3) for x in o.matrix_world.translation]
        info.append(d)
    res[code] = info
json.dump(res, open(out, 'w'), indent=0)
for code, info in res.items():
    print('==', code)
    for d in info:
        print(d['name'], d['type'], 'P=', d['parent'], 'dims', d.get('dims'), 'org', d.get('org'), 'off', d.get('off'), 'mats', d.get('mats'))
