"""Etapa 2 (diagnostico): manos de b tras la T-pose rigida. blender -b --python t1408_etapa2.py -- <carpeta>"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import t1408_malla as M
import t1381_lib as L
import bpy
from mathutils import Vector

out = sys.argv[sys.argv.index('--') + 1]
me, arm = M.preparar()
Aw = arm.matrix_world
orig = {b.name: (Aw @ b.head_local, Aw @ b.tail_local) for b in arm.data.bones}
info = M.brazos_a_tpose(me, arm)
# bones originales rotados con el mismo giro
for lado, sx in (('Left', 1), ('Right', -1)):
    h = info[lado]['pivote']
    cur = (orig[M.PRE + lado + 'Hand'][0] - h).normalized()
    R = cur.rotation_difference(Vector((sx, 0, 0))).to_matrix()
    for n in ('Arm', 'ForeArm', 'Hand', 'HandIndex1', 'HandIndex3', 'HandThumb1', 'HandPinky1'):
        a, b = orig[M.PRE + lado + n]
        a2 = h + R @ (a - h); b2 = h + R @ (b - h)
        print("ROT", lado, n, [round(x, 3) for x in a2], "->", [round(x, 3) for x in b2])
    # centroides de los grupos mano/dedos por pesos
    ws = M.pesos_grupo(me, [M.PRE + lado + 'Hand'] + [M.PRE + lado + 'Hand' + d + str(i) for d in M.DEDOS for i in (1, 2, 3)])
    pts = [v.co for v, w in zip(me.data.vertices, ws) if w > 0.5]
    c = sum(pts, Vector()) / len(pts)
    print("CENT mano", lado, [round(x, 3) for x in c], "n", len(pts), "xmin", round(min(p.x for p in pts), 2), "xmax", round(max(p.x for p in pts), 2), "zmin", round(min(p.z for p in pts), 2), "zmax", round(max(p.z for p in pts), 2))
    wf = M.pesos_grupo(me, [M.PRE + lado + 'ForeArm'])
    pf = [v.co for v, w in zip(me.data.vertices, wf) if w > 0.5]
    print("CENT fore", lado, [round(x, 3) for x in sum(pf, Vector()) / len(pf)], "xmin", round(min(abs(p.x) for p in pf), 2), "xmax", round(max(abs(p.x) for p in pf), 2), "zmin", round(min(p.z for p in pf), 2), "zmax", round(max(p.z for p in pf), 2))
arm.hide_render = True
for lado, cx in (('L', 1.35), ('R', -1.35)):
    sc = L.escena_render()
    sc.display.shading.show_xray = False
    for nom, d in (('frente', (0, -1, 0)), ('arriba', (0, 0, 1))):
        L.camara(sc, (cx, 0, 2.0), 1.4, d)
        L.renderizar(sc, os.path.join(out, "T1408_e2_%s_%s.png" % (lado, nom)))
