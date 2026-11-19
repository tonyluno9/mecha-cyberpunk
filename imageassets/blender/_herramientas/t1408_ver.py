"""T-1408: renders de diagnostico (reposo con esqueleto) de E16_a, E16_b original (zip) y E16_b exportado.
blender -b --python t1408_ver.py -- <carpeta_salida>"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1381_lib as L
import bpy

out = sys.argv[sys.argv.index('--') + 1]
os.makedirs(out, exist_ok=True)


def render_fbx(fp, pref):
    L.reset()
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fp, ignore_leaf_bones=True, automatic_bone_orientation=False)
    new = [o for o in bpy.data.objects if o not in before]
    me = max([o for o in new if o.type == 'MESH'], key=lambda o: len(o.data.vertices))
    ar = [o for o in new if o.type == 'ARMATURE']
    mn, mx = L.bbox(me)
    print("DIAG", pref, "bbox", [round(x, 2) for x in mn], [round(x, 2) for x in mx], "arm", bool(ar), "escala", tuple(me.scale), tuple(ar[0].scale) if ar else None)
    L.vistas(me, ar[0] if ar else None, os.path.join(out, pref), ('frente', 'lado'), esq=bool(ar), xray=True)


render_fbx(L.extraer("E16_RobotPesadoNova_b"), "T1408_diag_zip_b")
render_fbx(os.path.join(L.OUT, "E16_RobotPesadoNova_b.fbx"), "T1408_diag_exp_b")
render_fbx(os.path.join(L.OUT, "E16_RobotPesadoNova_a.fbx"), "T1408_diag_exp_a")
