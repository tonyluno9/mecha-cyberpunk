# T-1380: drones y naves Tripo -> FBX con Cuerpo + Rotor_1..N (pivote en el eje de giro).
# uso: blender -b --python t1380_drones.py -- <carpeta_salida> <carpeta_fuentes> COD [COD ...]
#   las fuentes son <carpeta_fuentes>/<COD>.fbx (con <COD>.fbm al lado)
# Convencion: frente = -Y de Blender (+Z Unity), metros, raiz vacia con el codigo en el centro del modelo.
# El eje de giro de cada rotor es el Z LOCAL de Blender (= Y local de Unity); su origen es el centro del giro.
import bpy, bmesh, sys, os, math
import numpy as np
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index('--') + 1:]
OUT, SRC = argv[0], argv[1]
CODES = argv[2:]

# nombre de salida, n de rotores, rango de radio del ducto (Tripo ~1 unidad), tamano objetivo (m) y eje que se mide,
# region opcional donde buscar rotores (x0,y0,x1,y1) para no confundir esferas del cuerpo con ductos
SPEC = {
    'DR01': dict(out='DR01', n=4, r=(0.10, 0.20), size=0.8, axis=0),
    'DR02_a': dict(out='DR02_a', n=4, r=(0.10, 0.20), size=1.8, axis=0, src='DR02_DronPesado_a'),
    'DR02_b': dict(out='DR02_b', n=4, r=(0.10, 0.20), size=1.8, axis=0, src='DR02_DronPesado_b'),
    'E13': dict(out='E13', n=4, r=(0.10, 0.20), size=1.1, axis=0, src='DR02_DronPesado_b'),
    'DR03_a': dict(out='DR03_a', n=6, r=(0.10, 0.18), size=2.4, axis=1, src='DR03_DronCarga_a'),
    'DR04_a': dict(out='DR04_a', n=1, r=(0.25, 0.46), size=0.7, axis=0, src='DR04_DronExplorador_a'),
    'NV01': dict(out='NV01', n=2, r=(0.10, 0.26), size=12.0, axis=0, src='NV01_NaveTransporte'),
    'NV03_a': dict(out='NV03_a', n=2, r=(0.10, 0.26), size=9.0, axis=0, src='NV03_NaveNova_a'),
    'NV03_b': dict(out='NV03_b', n=2, r=(0.10, 0.26), size=9.0, axis=0, src='NV03_NaveNova_b'),
}
SPEC['DR02_a']['src'] = 'DR02_DronPesado_a'
SPEC['DR04_a']['rc'] = 0.235
SPEC['DR04_a']['zmin'] = 0.665
for k in ('NV01', 'NV03_a', 'NV03_b'): SPEC[k]['rc'] = 0.074
for k in ('DR01', 'DR02_a', 'DR02_b', 'E13'): SPEC[k]['filt'] = lambda P: np.abs(P[:, 0]) > 0.2
SPEC['DR03_a']['filt'] = lambda P: np.abs(P[:, 0]) > 0.12
SPEC['DR04_a']['filt'] = lambda P: P[:, 2] > 0.5
for k in ('NV01', 'NV03_a', 'NV03_b'): SPEC[k]['filt'] = lambda P: np.abs(P[:, 0]) > 0.25


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_join(fp):
    bpy.ops.import_scene.fbx(filepath=fp)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in bpy.context.scene.objects: o.select_set(False)
    # aplanar: quitar padres/transformaciones
    for o in meshes:
        mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
        o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4)
    for o in bpy.context.scene.objects:
        if o.type != 'MESH': bpy.data.objects.remove(o, do_unlink=True)
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1: bpy.ops.object.join()
    body = bpy.context.view_layer.objects.active
    body.name = 'Cuerpo'; body.data.name = 'Cuerpo'
    return body


def mat_image(m):
    if not m or not m.use_nodes: return None
    for n in m.node_tree.nodes:
        if n.type == 'TEX_IMAGE' and n.image: return n.image
    return None


def atlas(body, cell=256):
    """Une los materiales de muchas piezas (DR01 trae 50) en un solo atlas con un solo material."""
    mats = list(body.data.materials)
    n = len(mats)
    cols = int(math.ceil(math.sqrt(n)))
    rows = int(math.ceil(n / cols))
    A = np.zeros((rows * cell, cols * cell, 4), dtype=np.float32); A[..., 3] = 1
    cellof = {}
    for i, m in enumerate(mats):
        im = mat_image(m)
        r, c = divmod(i, cols)
        cellof[i] = (r, c)
        if im is None:
            col = (0.5, 0.5, 0.5, 1)
            A[r * cell:(r + 1) * cell, c * cell:(c + 1) * cell] = col
            continue
        w, h = im.size
        px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, im.channels)
        if im.channels == 3:
            px = np.concatenate([px, np.ones((h, w, 1), np.float32)], 2)
        yi = (np.arange(cell) * h // cell); xi = (np.arange(cell) * w // cell)
        A[r * cell:(r + 1) * cell, c * cell:(c + 1) * cell] = px[np.ix_(yi, xi)][..., :4]
    img = bpy.data.images.new('atlas_' + body.name, cols * cell, rows * cell, alpha=False)
    img.pixels = A.ravel(); img.pack()
    me = body.data
    uv = me.uv_layers.active
    pad = 0.5 / cell
    for p in me.polygons:
        r, c = cellof[p.material_index]
        for li in p.loop_indices:
            u, v = uv.data[li].uv
            u = min(max(u, pad), 1 - pad); v = min(max(v, pad), 1 - pad)
            uv.data[li].uv = ((c + u) / cols, (r + v) / rows)
    mat = bpy.data.materials.new('Dron')
    mat.use_nodes = True
    nt = mat.node_tree
    for nd in list(nt.nodes):
        if nd.type != 'OUTPUT_MATERIAL' and nd.type != 'BSDF_PRINCIPLED': nt.nodes.remove(nd)
    bsdf = [nd for nd in nt.nodes if nd.type == 'BSDF_PRINCIPLED'][0]
    tex = nt.nodes.new('ShaderNodeTexImage'); tex.image = img
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Metallic'].default_value = 0.3; bsdf.inputs['Roughness'].default_value = 0.6
    me.materials.clear(); me.materials.append(mat)
    for p in me.polygons: p.material_index = 0
    return mat


def find_circles(P, n, rmin, rmax, cell=0.004, filt=None):
    pts = P[filt(P)][:, :2] if filt else P[:, :2]
    x0, y0 = pts.min(0) - 0.05; x1, y1 = pts.max(0) + 0.05
    nx = int((x1 - x0) / cell) + 1; ny = int((y1 - y0) / cell) + 1
    ang = np.linspace(0, 2 * math.pi, 40, endpoint=False)
    found = []
    alive = np.ones(len(pts), bool)
    for k in range(n):
        q = pts[alive]
        if len(q) > 20000: q = q[np.random.RandomState(k).choice(len(q), 20000, replace=False)]
        best = (-1, None)
        for r in np.arange(rmin, rmax + 1e-9, 0.01):
            cx = q[:, 0, None] - r * np.cos(ang)[None]
            cy = q[:, 1, None] - r * np.sin(ang)[None]
            H, _, _ = np.histogram2d(cx.ravel(), cy.ravel(), bins=[nx, ny], range=[[x0, x0 + nx * cell], [y0, y0 + ny * cell]])
            # suavizado 3x3
            S = H.copy()
            S[1:] += H[:-1]; S[:-1] += H[1:]
            T = S.copy(); T[:, 1:] += S[:, :-1]; T[:, :-1] += S[:, 1:]
            i = np.unravel_index(T.argmax(), T.shape)
            if T[i] > best[0]: best = (T[i], (x0 + (i[0] + .5) * cell, y0 + (i[1] + .5) * cell, r))
        cx, cy, r = best[1]
        found.append((cx, cy, r, best[0]))
        d = np.hypot(pts[:, 0] - cx, pts[:, 1] - cy)
        alive &= d > r * 1.6
    return found


def refine(P, cx, cy, r):
    """ajusta circulo (Kasa) a los vertices de la pared interior del ducto"""
    for _ in range(3):
        d = np.hypot(P[:, 0] - cx, P[:, 1] - cy)
        # pared interior: primer pico fuerte de la distribucion radial
        h, e = np.histogram(d, bins=np.arange(0.3 * r, 1.5 * r, 0.003))
        pk = h.argmax()
        ring = P[(d > e[pk] - 0.006) & (d < e[pk] + 0.009)]
        A = np.c_[2 * ring[:, 0], 2 * ring[:, 1], np.ones(len(ring))]
        b = (ring[:, :2] ** 2).sum(1)
        sol = np.linalg.lstsq(A, b, rcond=None)[0]
        cx, cy = sol[0], sol[1]
    return cx, cy


def cut_radius(P, cx, cy, r):
    d = np.hypot(P[:, 0] - cx, P[:, 1] - cy)
    bins = np.arange(0.2 * r, 1.6 * r, 0.004)
    h, e = np.histogram(d, bins=bins)
    # primer pico "fuerte" (pared interior del ducto): el primer bin con > 2.2 veces la mediana de los anteriores
    base = np.median(h[:max(3, int(len(h) * 0.25))]) + 1
    for i in range(3, len(h)):
        prev = np.median(h[max(0, i - 8):i]) + 1
        if h[i] > 2.5 * prev and h[i] > 25:
            return float(e[i]) - 0.004, h
    return r * 0.8, h


def separate(body, sel_idx, name):
    me = body.data
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    sel = set(sel_idx)
    me2 = me.copy(); me2.name = name
    bm2 = bmesh.new(); bm2.from_mesh(me2); bm2.faces.ensure_lookup_table()
    bmesh.ops.delete(bm2, geom=[f for f in bm2.faces if f.index not in sel], context='FACES')
    for v in [v for v in bm2.verts if not v.link_faces]: bm2.verts.remove(v)
    bm2.to_mesh(me2); bm2.free()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index in sel], context='FACES')
    for v in [v for v in bm.verts if not v.link_faces]: bm.verts.remove(v)
    bm.to_mesh(me); bm.free(); me.update()
    o2 = bpy.data.objects.new(name, me2)
    for c in body.users_collection: c.objects.link(o2)
    return o2


def set_origin(o, p):
    p = Vector(p)
    o.data.transform(Matrix.Translation(-p))
    o.matrix_world = Matrix.Translation(p)


def process(key):
    sp = SPEC[key]
    clear()
    src = sp.get('src', key)
    body = import_join(os.path.join(SRC, src + '.fbx'))
    if len(body.data.materials) > 6:
        atlas(body)
    P = np.array([tuple(v.co) for v in body.data.vertices])
    cs = find_circles(P, sp['n'], *sp['r'], filt=sp.get('filt'))
    rotors = []
    me = body.data
    centers = np.array([[p.center.x, p.center.y] for p in me.polygons])
    cz = np.array([p.center.z for p in me.polygons])
    zok = cz > sp.get('zmin', -9)
    taken = np.zeros(len(me.polygons), bool)
    info = []
    for (cx, cy, r, sc) in cs:
        cx, cy = refine(P, cx, cy, r)
        Rc, h = cut_radius(P, cx, cy, r)
        if sp.get('rc'): Rc = sp['rc']
        if os.environ.get('RAD'):
            dd = np.hypot(P[:, 0] - cx, P[:, 1] - cy); hh, ee = np.histogram(dd, bins=np.arange(0.0, 0.5, 0.01)); print('RAD', round(cx,3), round(cy,3), list(map(int, hh)))
        d = np.hypot(centers[:, 0] - cx, centers[:, 1] - cy)
        idx = np.where((d < Rc) & ~taken & zok)[0]
        taken[idx] = True
        info.append((cx, cy, Rc, len(idx))); print("CIRC", round(cx,3), round(cy,3), round(r,3), "Rc", round(Rc,3), "n", len(idx), "score", sc)
    # ordenar: primero los de adelante (-Y) y de la derecha (-X); numerar Rotor_1..N
    order = sorted(range(len(info)), key=lambda i: (round(info[i][1] / 0.12), info[i][0]))
    # separar en un solo paso (los indices de las caras cambian al borrar)
    groups = []
    for i in order:
        cx, cy, Rc, nf = info[i]
        d = np.hypot(centers[:, 0] - cx, centers[:, 1] - cy)
        groups.append((cx, cy, Rc, [int(j) for j in np.where((d < Rc) & zok)[0]]))
    # evitar solapes entre grupos
    used = set(); clean = []
    for cx, cy, Rc, idx in groups:
        idx2 = [j for j in idx if j not in used]; used.update(idx2); clean.append((cx, cy, Rc, idx2))
    # separar todos desde una copia fija de la malla original
    src_me = body.data.copy()
    parts = []
    for k, (cx, cy, Rc, idx) in enumerate(clean):
        tmp = bpy.data.objects.new('tmp', src_me.copy())
        bpy.context.scene.collection.objects.link(tmp)
        o = separate(tmp, idx, 'Rotor_%d' % (k + 1))
        bpy.data.objects.remove(tmp, do_unlink=True)
        parts.append((o, cx, cy))
    # quitar de Cuerpo todas las caras tomadas
    bm = bmesh.new(); bm.from_mesh(body.data); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.faces[j] for j in used], context='FACES')
    for v in [v for v in bm.verts if not v.link_faces]: bm.verts.remove(v)
    bm.to_mesh(body.data); bm.free()
    # recentrar y escalar
    allp = [tuple(v.co) for o in [body] + [p[0] for p in parts] for v in o.data.vertices]
    A = np.array(allp)
    mn, mx = A.min(0), A.max(0)
    ctr = (mn + mx) / 2
    s = sp['size'] / (mx - mn)[sp['axis']]
    for o in [body] + [p[0] for p in parts]:
        o.data.transform(Matrix.Scale(s, 4) @ Matrix.Translation(-Vector(ctr)))
    for o, cx, cy in parts:
        Q = np.array([tuple(v.co) for v in o.data.vertices])
        zc = (Q[:, 2].min() + Q[:, 2].max()) / 2
        hub = Vector(((cx - ctr[0]) * s, (cy - ctr[1]) * s, zc))
        off = ((Q[:, 0].min() + Q[:, 0].max()) / 2 - hub.x, (Q[:, 1].min() + Q[:, 1].max()) / 2 - hub.y)
        set_origin(o, hub)
        o['hub'] = tuple(hub)
        print('ROTOR', key, o.name, 'hub(m)', tuple(round(x, 3) for x in hub), 'dim', tuple(round(x, 3) for x in (Q.max(0) - Q.min(0))),
              'desfase bbox-hub', tuple(round(v, 4) for v in off))
    root = bpy.data.objects.new(sp['out'], None)
    bpy.context.scene.collection.objects.link(root)
    for o in [body] + [p[0] for p in parts]:
        if o.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(o)
        mw = o.matrix_world.copy(); o.parent = root; o.matrix_world = mw
    print('DRON', key, 'escala', round(s, 4), 'rotores', len(parts), 'dims(m)', tuple(round(x * s, 3) for x in (mx - mn)),
          'caras cuerpo', len(body.data.polygons))
    return root


def export(root, path):
    vl = bpy.context.view_layer
    for ob in vl.objects: ob.select_set(False)
    for ob in [root] + list(root.children_recursive):
        ob.select_set(True)
    vl.objects.active = root
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'}, global_scale=1.0,
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=False, add_leaf_bones=False, bake_anim=False, path_mode='COPY',
                             embed_textures=True, use_mesh_modifiers=True)


os.makedirs(OUT, exist_ok=True)
for key in CODES:
    root = process(key)
    export(root, os.path.join(OUT, SPEC[key]['out'] + '.fbx'))
    print('EXPORT', key)
