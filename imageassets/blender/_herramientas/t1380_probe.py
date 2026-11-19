# T-1380: sonda de rotores. Busca circulos (ductos) en la vista de planta de un dron Tripo y
# imprime perfiles radiales y de altura para decidir el recorte.
# uso: blender -b --python t1380_probe.py -- <fbx> [x0 y0 x1 y1 rmin rmax] ...   (cajas de busqueda en metros Blender)
import bpy, sys, os
import numpy as np

argv = sys.argv[sys.argv.index('--') + 1:]
fp = argv[0]
boxes = [tuple(float(v) for v in argv[1 + i * 6: 7 + i * 6]) for i in range((len(argv) - 1) // 6)]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fp)
P = []
for o in bpy.context.scene.objects:
    if o.type == 'MESH':
        P += [tuple(o.matrix_world @ v.co) for v in o.data.vertices]
P = np.array(P)
mn, mx = P.min(0), P.max(0)
print('BBOX', np.round(mn, 3), np.round(mx, 3), 'dims', np.round(mx - mn, 3), 'nverts', len(P))


def hough(Q, box, rmin, rmax):
    x0, y0, x1, y1 = box
    sel = Q[(Q[:, 0] > x0) & (Q[:, 0] < x1) & (Q[:, 1] > y0) & (Q[:, 1] < y1)]
    if len(sel) > 6000:
        sel = sel[np.random.RandomState(1).choice(len(sel), 6000, replace=False)]
    gx = np.arange(x0, x1, (x1 - x0) / 60); gy = np.arange(y0, y1, (y1 - y0) / 60)
    best = (-1, None)
    rs = np.linspace(rmin, rmax, 14)
    G = np.array([(a, b) for a in gx for b in gy])
    for r in rs:
        d = np.sqrt(((G[:, None, :] - sel[None, :, :2]) ** 2).sum(-1))
        sc = (np.abs(d - r) < (rmax - rmin) / 28).sum(1)
        i = sc.argmax()
        if sc[i] > best[0]: best = (sc[i], (G[i, 0], G[i, 1], r))
    return best


for box in boxes:
    x0, y0, x1, y1, rmin, rmax = box
    sc, (cx, cy, r) = hough(P, (x0, y0, x1, y1), rmin, rmax)
    # refinar con rejilla fina
    sel = P[(P[:, 0] > cx - r * 1.3) & (P[:, 0] < cx + r * 1.3) & (P[:, 1] > cy - r * 1.3) & (P[:, 1] < cy + r * 1.3)]
    print('ROTOR box', box[:4], 'hough', round(cx, 3), round(cy, 3), 'r', round(r, 3), 'score', sc)
    d = np.sqrt((sel[:, 0] - cx) ** 2 + (sel[:, 1] - cy) ** 2)
    h, e = np.histogram(d, bins=np.arange(0, r * 1.3, r / 20))
    print('  radial', list(h))
    inn = sel[d < r * 0.5]
    h2, e2 = np.histogram(inn[:, 2], bins=24)
    print('  z-hist (r<0.5R)', list(h2), 'z', round(e2[0], 3), round(e2[-1], 3))
    ring = sel[(d > r * 0.8) & (d < r * 1.2)]
    h3, e3 = np.histogram(ring[:, 2], bins=24)
    print('  z-hist ring', list(h3), 'z', round(e3[0], 3), round(e3[-1], 3))
