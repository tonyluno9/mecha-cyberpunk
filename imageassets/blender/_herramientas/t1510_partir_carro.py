# T-1510: parte un modelo Tripo de una sola malla (patrulla PA01, patrulla SUV PA02, superdeportivo V22) en las piezas del proyecto:
# Carroceria, Rueda_DI/DD/TI/TD (pivote en el centro del eje), Puerta_I/Puerta_D (pivote en la bisagra delantera), Cristal_* (parabrisas,
# medallon, laterales y Cristal_Puerta_I/D dentro de cada puerta) e Interior (fondo oscuro de los huecos de puerta).
# Convencion (la misma de t1380_car_parts.py): metros, Z arriba, frente a -Y, piso en z=0, hijos de una raiz vacia con el codigo.
# uso: blender -b --python t1510_partir_carro.py -- COD modelo.fbx salida.fbx carpeta_fbm [carpeta_debug]
#   carpeta_fbm: donde se dejan COD_basecolor.jpg y COD_normal.jpg (las texturas que arma Materiales() de Unity).
import bpy, bmesh, sys, os, math, glob, shutil
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index('--') + 1:]
CODE, SRC, OUT, FBM = argv[0], argv[1], argv[2], argv[3]
DBG = argv[4] if len(argv) > 4 else None

# rot: grados en Z para que el frente quede en -Y. largo: metros. rueda_r: radio de rueda en el modelo normalizado (largo 0.98).
# door = (yf, yr, zb, zt) de la puerta delantera. glass_*: poligonos de ventanas (ver mas abajo).
SPEC = {
    'PA01': dict(largo=4.8, rot=0, rueda_r=0.0885, door=(-0.15, 0.10, 0.095, 0.335),
                 glass_side=[[(-0.10, 0.268), (-0.04, 0.347), (0.235, 0.347), (0.255, 0.268)]],
                 glass_front=[(-0.135, 0.348), (0.135, 0.348), (0.172, 0.268), (-0.172, 0.268)],
                 glass_rear=[(-0.14, 0.348), (0.14, 0.348), (0.165, 0.266), (-0.165, 0.266)]),
    'PA02': dict(largo=5.0, rot=0, rueda_r=0.0885, door=(-0.152, 0.04, 0.10, 0.37),
                 glass_side=[[(-0.10, 0.275), (-0.065, 0.36), (0.02, 0.36), (0.02, 0.275)],
                             [(0.055, 0.275), (0.055, 0.36), (0.15, 0.36), (0.15, 0.275)],
                             [(0.21, 0.275), (0.21, 0.35), (0.43, 0.35), (0.43, 0.275)]],
                 glass_front=[(-0.16, 0.345), (0.165, 0.345), (0.19, 0.275), (-0.19, 0.275)],
                 glass_rear=[(-0.15, 0.345), (0.15, 0.345), (0.15, 0.28), (-0.15, 0.28)]),
    # V22: las 4 ruedas ya vienen como islas sueltas del Tripo ('islands'); el frente del modelo mira a -X y se gira 90 grados.
    'V22': dict(largo=4.6, rot=90, wheels='islands', door=(-0.14, 0.14, 0.12, 0.285),
                glass_side=[[(-0.19, 0.205), (-0.06, 0.285), (0.17, 0.285), (0.21, 0.235), (0.21, 0.205)]],
                glass_front=[(-0.135, 0.285), (0.135, 0.285), (0.185, 0.205), (-0.185, 0.205)],
                glass_rear=[(-0.09, 0.285), (0.09, 0.285), (0.09, 0.25), (-0.09, 0.25)]),
}
SP = SPEC[CODE]
ARCH_K = 1.15
SC = 1.0


def mat(name, col, rough=0.7, metal=0.0, alpha=1.0):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*col, 1)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    b.inputs['Alpha'].default_value = alpha
    m.diffuse_color = (*col, alpha)
    return m


def bvh(o):
    M = o.matrix_world
    return BVHTree.FromPolygons([M @ v.co for v in o.data.vertices], [list(p.vertices) for p in o.data.polygons])


def bisect(o, co, no):
    bm = bmesh.new(); bm.from_mesh(o.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=Vector(co), plane_no=Vector(no), dist=1e-6)
    bm.to_mesh(o.data); bm.free(); o.data.update()


def separate(o, idx, name):
    """saca las caras idx de o a un objeto nuevo (las quita de o)"""
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    sel = set(idx)
    me2 = me.copy(); me2.name = name
    bm2 = bmesh.new(); bm2.from_mesh(me2); bm2.faces.ensure_lookup_table()
    bmesh.ops.delete(bm2, geom=[f for f in bm2.faces if f.index not in sel], context='FACES')
    for v in [v for v in bm2.verts if not v.link_faces]:
        bm2.verts.remove(v)
    bm2.to_mesh(me2); bm2.free()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index in sel], context='FACES')
    for v in [v for v in bm.verts if not v.link_faces]:
        bm.verts.remove(v)
    bm.to_mesh(me); bm.free(); me.update()
    o2 = bpy.data.objects.new(name, me2)
    for c in o.users_collection:
        c.objects.link(o2)
    return o2


def copy_faces(o, idx, name, offset):
    """copia las caras idx de o a un objeto nuevo, corridas 'offset' hacia afuera por la normal de cada vertice (o se queda igual)"""
    me = o.data
    me.calc_loop_triangles()
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    sel = set(idx)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index not in sel], context='FACES')
    for v in [v for v in bm.verts if not v.link_faces]:
        bm.verts.remove(v)
    bm.normal_update()
    for v in bm.verts:
        v.co += v.normal * offset
    me2 = bpy.data.meshes.new(name); bm.to_mesh(me2); bm.free()
    o2 = bpy.data.objects.new(name, me2)
    for c in o.users_collection:
        c.objects.link(o2)
    return o2


def with_backface(o, vec):
    """copia invertida desplazada = espesor visible desde el otro lado"""
    bm = bmesh.new(); bm.from_mesh(o.data)
    d = bmesh.ops.duplicate(bm, geom=bm.faces[:])
    bmesh.ops.reverse_faces(bm, faces=[g for g in d['geom'] if isinstance(g, bmesh.types.BMFace)])
    bmesh.ops.translate(bm, verts=[g for g in d['geom'] if isinstance(g, bmesh.types.BMVert)], vec=vec)
    bm.to_mesh(o.data); bm.free()


def set_origin(o, p):
    p = Vector(p)
    o.data.transform(Matrix.Translation(-p))
    o.matrix_world = Matrix.Translation(p)


def parent_keep(o, root):
    mw = o.matrix_world.copy(); o.parent = root; o.matrix_world = mw


def verts_np(o):
    return np.array([tuple(v.co) for v in o.data.vertices])


# ---------------------------------------------------------------------------------------------------- normalizar
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
body = [o for o in bpy.data.objects if o.type == 'MESH'][0]
body.name = 'Carroceria'
body.data.name = 'Carroceria'
bpy.context.view_layer.update()
body.data.transform(body.matrix_world)  # hornea la transformacion que trae el FBX
body.matrix_world = Matrix.Identity(4)
if SP['rot']:
    body.data.transform(Matrix.Rotation(math.radians(SP['rot']), 4, 'Z'))
P = verts_np(body)
mn, mx = P.min(0), P.max(0)
body.data.transform(Matrix.Translation(Vector(((-(mn[0] + mx[0]) / 2), -(mn[1] + mx[1]) / 2, -mn[2]))))
P = verts_np(body)
LARGO0 = float(P[:, 1].max() - P[:, 1].min())
print('NORM largo', LARGO0, 'ancho', float(P[:, 0].max() - P[:, 0].min()), 'alto', float(P[:, 2].max()))
root = bpy.data.objects.new(CODE, None)
bpy.context.scene.collection.objects.link(root)
parent_keep(body, root)

# luminancia de la textura (para el cristal y el color del interior)
tex_mat = body.data.materials[0]
img = [n for n in tex_mat.node_tree.nodes if n.type == 'TEX_IMAGE'][0].image
w, h = img.size
px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(h, w, 4)
lum = px[..., :3] @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
uvl = body.data.uv_layers.active


def lum_cara(p):
    u = np.mean([uvl.data[l].uv for l in p.loop_indices], axis=0)
    return float(lum[min(h - 1, int(u[1] * h)), min(w - 1, int(u[0] * w))]), u


# ---------------------------------------------------------------------------------------------------- ruedas
R = SP.get('rueda_r', 0.09)
wheels = {}
if SP.get('wheels') == 'islands':
    # ruedas = islas sueltas de la malla (de 400 a 1500 vertices, bajas y hacia las esquinas)
    bm_ = bmesh.new(); bm_.from_mesh(body.data); bm_.verts.ensure_lookup_table()
    visto = set(); islas = []
    for v0 in bm_.verts:
        if v0.index in visto:
            continue
        pila = [v0]; visto.add(v0.index); isla = []
        while pila:
            a_ = pila.pop(); isla.append(a_.index)
            for e in a_.link_edges:
                b_ = e.other_vert(a_)
                if b_.index not in visto:
                    visto.add(b_.index); pila.append(b_)
        islas.append(isla)
    bm_.free()
    for isla in islas:
        if not (400 <= len(isla) <= 1500):
            continue
        Q = P[isla]
        c = (Q.min(0) + Q.max(0)) / 2
        if c[2] < 0.13 and abs(c[0]) > 0.15 and abs(c[1]) > 0.15:
            sx = 1 if c[0] > 0 else -1
            eje = 'D' if c[1] < 0 else 'T'
            wheels['Rueda_' + eje + ('I' if sx > 0 else 'D')] = dict(sx=sx, yc=float(c[1]), zc=float(c[2]), x0=float(np.abs(Q[:, 0]).min()), R=float((Q[:, 2].max() - Q[:, 2].min()) / 2), verts=set(isla))
            wheels['Rueda_' + eje + ('I' if sx > 0 else 'D')]['centros'] = set(tuple(round(float(t), 6) for t in p.center) for p in body.data.polygons if p.vertices[0] in set(isla))
    assert len(wheels) == 4, 'no se hallaron las 4 ruedas: %d' % len(wheels)
else:
    for sx, lado in ((1, 'I'), (-1, 'D')):
        for sy, eje in ((-1, 'D'), (1, 'T')):
            low = P[(P[:, 2] < 0.03) & (np.sign(P[:, 0]) == sx) & (np.sign(P[:, 1]) == sy) & (np.abs(P[:, 0]) > 0.1) & (np.abs(P[:, 1]) > 0.15)]
            yc = float((low[:, 1].min() + low[:, 1].max()) / 2)
            x0 = float(np.abs(low[:, 0]).min())
            zlow = float(low[:, 2].min())
            wheels['Rueda_' + eje + lado] = dict(sx=sx, yc=yc, zc=zlow + R, x0=x0, R=R)
    for eje in ('D', 'T'):
        # cada eje queda simetrico: la misma y y la misma altura en las dos ruedas
        a_, b_ = wheels['Rueda_' + eje + 'I'], wheels['Rueda_' + eje + 'D']
        yc_ = (a_['yc'] + b_['yc']) / 2
        zc_ = (a_['zc'] + b_['zc']) / 2
        a_['yc'] = b_['yc'] = yc_
        a_['zc'] = b_['zc'] = zc_
for k_, v_ in wheels.items():
    print('RUEDA', k_, {a: (round(b, 3) if isinstance(b, float) else b) for a, b in v_.items() if a not in ('verts', 'centros')})
wheel_objs = {}
# separate corta de a una rueda: los indices de las caras cambian al quitar, se recalculan en cada una
for k, wv in wheels.items():
    idx = []
    for p in body.data.polygons:
        c = p.center
        if 'centros' in wv:
            if tuple(round(float(t), 6) for t in c) in wv['centros']:
                idx.append(p.index)
        elif wv['sx'] * c.x >= wv['x0'] - 0.004 and (c.y - wv['yc']) ** 2 + (c.z - wv['zc']) ** 2 <= (wv['R'] * 1.01) ** 2:
            idx.append(p.index)
    wo = separate(body, idx, k)
    Pw = verts_np(wo)
    xm = float((Pw[:, 0].min() + Pw[:, 0].max()) / 2)
    wv['xm'] = xm
    set_origin(wo, (xm, wv['yc'], wv['zc']))
    parent_keep(wo, root)
    wheel_objs[k] = wo
    print('RUEDA_CORTE', k, len(idx), 'caras', 'x', Pw[:, 0].min().round(3), Pw[:, 0].max().round(3))

# ---------------------------------------------------------------------------------------------------- cristal
# Las ventanas son poligonos medidos en las vistas del modelo (lado: y,z; frente y atras: x,z). Cada cara de la carroceria que mira hacia afuera
# y cae dentro de un poligono pasa a una copia con el material de cristal, corrida 0.4 cm (modelo normalizado) hacia afuera. La carroceria las conserva
# (su textura oscura queda debajo como fondo, y el hueco de la puerta tiene su propia lamina oscura).


def dentro(poly, a, b):
    ok = False
    n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]; x2, y2 = poly[(i + 1) % n]
        if (y1 > b) != (y2 > b) and a < (x2 - x1) * (b - y1) / (y2 - y1) + x1:
            ok = not ok
    return ok


groups = {'Cristal_Parabrisas': [], 'Cristal_Medallon': [], 'Cristal_Laterales': []}
for p in body.data.polygons:
    c, n = p.center, p.normal
    if n.y < -0.25 and c.y < 0 and dentro(SP['glass_front'], c.x, c.z):
        groups['Cristal_Parabrisas'].append(p.index)
    elif n.y > 0.25 and c.y > 0 and dentro(SP['glass_rear'], c.x, c.z):
        groups['Cristal_Medallon'].append(p.index)
    elif abs(c.x) > 0.12 and abs(n.x) > 0.3 and n.x * c.x > 0 and any(dentro(q, c.y, c.z) for q in SP['glass_side']):
        groups['Cristal_Laterales'].append(p.index)
glass_idx = [i for v in groups.values() for i in v]
print('CRISTAL caras', {k: len(v) for k, v in groups.items()})
mat_cristal = mat('Cristal', (0.05, 0.08, 0.1), 0.05, 0.0, 0.35)
glass_objs = {}
dark_uv = None
for nombre, idx in groups.items():
    if not idx:
        continue
    go = copy_faces(body, idx, nombre, 0.004)
    go.data.materials.clear()
    go.data.materials.append(mat_cristal)
    parent_keep(go, root)
    glass_objs[nombre] = go
if glass_idx:
    dark_uv = min((lum_cara(body.data.polygons[i]) for i in glass_idx), key=lambda t: t[0])[1]
if dark_uv is None:
    dark_uv = np.array([0.5, 0.5])

# ---------------------------------------------------------------------------------------------------- puertas
yf, yr, zb, zt = SP['door']
glass_lat = glass_objs.get('Cristal_Laterales')


def cut_door(name, sx):
    for co, no in (((0, yf, 0), (0, 1, 0)), ((0, yr, 0), (0, 1, 0)), ((0, 0, zb), (0, 0, 1)), ((0, 0, zt), (0, 0, 1))):
        bisect(body, co, no)
    T = bvh(body)
    arcs = []
    for k, wv in wheels.items():
        if wv['sx'] == sx:
            arcs.append((wv['yc'], wv['zc'], wv['R'] * ARCH_K))
    sel = []
    for p in body.data.polygons:
        c = p.center
        if not (yf < c.y < yr and zb < c.z < zt and sx * c.x > 0.05):
            continue
        if any((c.y - ay) ** 2 + (c.z - az) ** 2 < ar * ar for ay, az, ar in arcs):
            continue
        hit = T.ray_cast(Vector((sx * 5.0, c.y, c.z)), Vector((-sx, 0, 0)), 10)
        if hit[0] is not None and sx * c.x > sx * hit[0].x - 0.025 * SC:
            sel.append(p.index)
    door = separate(body, sel, name)
    with_backface(door, (-sx * 0.01 * SC, 0, 0))
    P_ = verts_np(door)
    xo = float(P_[:, 0].max()) if sx > 0 else float(P_[:, 0].min())
    hinge = Vector((xo - sx * 0.02 * SC, yf, (zb + zt) / 2))
    set_origin(door, hinge)
    parent_keep(door, root)
    gl = None
    if glass_lat is not None:
        gi = [p.index for p in glass_lat.data.polygons if yf - 0.01 < p.center.y < yr + 0.01 and sx * p.center.x > 0.05 and zb < p.center.z < zt + 0.02]
        if gi:
            gl = separate(glass_lat, gi, 'Cristal_' + name)
            set_origin(gl, hinge)
            parent_keep(gl, door)
    xin = float(np.percentile(np.abs(P_[:, 0]), 5))  # parte mas interior del casco bajo la puerta (el techo se angosta)
    return door, (xin, P_), (0 if gl is None else len(gl.data.polygons)), len(sel), xo


door_info = {}
doors = {}
for nm, sx in (('Puerta_I', 1), ('Puerta_D', -1)):
    d, xin, ng, ns, xo = cut_door(nm, sx)
    doors[nm] = (d, xin)
    door_info[nm] = dict(caras=ns, cristal=ng, xo=round(xo, 3))
print('PUERTAS', door_info)

# interior: una lamina oscura detras de cada hueco de puerta (sin el, al abrir la puerta se ve el vacio del casco)
bm = bmesh.new()
uv_layer = bm.loops.layers.uv.new('UVMap')
for nm, sx in (('Puerta_I', 1), ('Puerta_D', -1)):
    xin, Pd = doors[nm][1]
    xi = sx * (xin - 0.02)
    # la lamina sigue la silueta de la puerta (el cofre y el parabrisas bajan por delante: un rectangulo asomaria)
    cols = []
    ys = np.linspace(yf, yr, 14)
    dy = (yr - yf) / 13
    ztop_prev = zt - 0.04
    for y in ys:
        zs = Pd[np.abs(Pd[:, 1] - y) < dy * 0.75][:, 2]
        ztop = (float(zs.max()) - 0.012) if len(zs) else ztop_prev
        ztop_prev = ztop
        cols.append((y, zb + 0.01, max(ztop, zb + 0.05)))
    vs = []
    for y, z0, z1 in cols:
        vs.append((bm.verts.new((xi, y, z0)), bm.verts.new((xi, y, z1))))
    for i in range(len(vs) - 1):
        q = [vs[i][0], vs[i + 1][0], vs[i + 1][1], vs[i][1]]
        f = bm.faces.new(q if sx > 0 else q[::-1])
        for l in f.loops:
            l[uv_layer].uv = (float(dark_uv[0]), float(dark_uv[1]))
# discos oscuros tras las ruedas
for k, wv in wheels.items():
    xi = wv['sx'] * (wv['x0'] - 0.006)
    ring = [bm.verts.new((xi, wv['yc'] + wv['R'] * math.cos(a), wv['zc'] + wv['R'] * math.sin(a))) for a in [i * math.pi / 12 for i in range(24)]]
    f = bm.faces.new(ring if wv['sx'] > 0 else ring[::-1])
    for l in f.loops:
        l[uv_layer].uv = (float(dark_uv[0]), float(dark_uv[1]))
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
# la normal de los discos y laminas debe mirar hacia afuera del carro (lado de su x)
for f in bm.faces:
    cx = sum(v.co.x for v in f.verts) / len(f.verts)
    if f.normal.x * cx < 0:
        f.normal_flip()
me_i = bpy.data.meshes.new('Interior'); bm.to_mesh(me_i); bm.free()
me_i.materials.append(tex_mat)
interior = bpy.data.objects.new('Interior', me_i)
bpy.context.scene.collection.objects.link(interior)
parent_keep(interior, root)

# ---------------------------------------------------------------------------------------------------- escala a metros
K = SP['largo'] / LARGO0
print('ESCALA', K)
partes = [o for o in bpy.data.objects if o.type == 'MESH']
padres = {o: o.parent for o in partes}
mundo = {o: o.matrix_world.copy() for o in partes}
for o in partes:
    o.parent = None
for o in partes:
    o.matrix_world = Matrix.Identity(4)
    o.data.transform(Matrix.Scale(K, 4))
    o.matrix_world = Matrix.Translation(mundo[o].translation * K)
for o in partes:
    if padres[o] is not None:
        parent_keep(o, padres[o])

# ---------------------------------------------------------------------------------------------------- texturas y exporte
os.makedirs(FBM, exist_ok=True)
srcdir = os.path.dirname(SRC)
base = glob.glob(os.path.join(srcdir, '*.fbm', '*_0_0.jpg'))
norm = glob.glob(os.path.join(srcdir, '*.fbm', '*_0_3.jpg'))
if base:
    shutil.copyfile(base[0], os.path.join(FBM, CODE + '_basecolor.jpg'))
    img.filepath = os.path.join(FBM, CODE + '_basecolor.jpg')
    img.name = CODE + '_basecolor.jpg'
if norm:
    shutil.copyfile(norm[0], os.path.join(FBM, CODE + '_normal.jpg'))

vl = bpy.context.view_layer
for ob in vl.objects:
    ob.select_set(False)
for ob in [root] + list(root.children_recursive):
    ob.select_set(True)
vl.objects.active = root
os.makedirs(os.path.dirname(OUT), exist_ok=True)
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, object_types={'EMPTY', 'MESH'}, global_scale=1.0,
                         apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                         bake_space_transform=False, add_leaf_bones=False, bake_anim=False, path_mode='COPY',
                         embed_textures=True, use_mesh_modifiers=True)
print('LISTO', CODE, OUT)
