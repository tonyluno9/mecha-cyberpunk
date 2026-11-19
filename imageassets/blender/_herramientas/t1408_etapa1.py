"""Etapa 1 (diagnostico): T-pose rigida de la malla de E16_b. blender -b --python t1408_etapa1.py -- <carpeta>"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1408_malla as M
import t1381_lib as L
import bpy

out = sys.argv[sys.argv.index('--') + 1]
os.makedirs(out, exist_ok=True)
me, arm = M.preparar()
for b in ('Hips', 'LeftArm', 'LeftForeArm', 'LeftHand', 'Head'):
    print("ORIG", b, [round(x, 3) for x in arm.data.bones[M.PRE + b].head_local])
info = M.brazos_a_tpose(me, arm)
print("INFO", info)
mn, mx = L.bbox(me)
print("BBOX", [round(x, 2) for x in mn], [round(x, 2) for x in mx])
arm.hide_render = True
L.vistas(me, arm, os.path.join(out, "T1408_e1"), ('frente', 'lado', '34'), esq=False, xray=False)
