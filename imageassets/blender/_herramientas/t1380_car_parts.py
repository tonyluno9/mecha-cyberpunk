# T-1380: agrega puertas y cofre (objetos aparte, pivote en la bisagra) a carros ya exportados que no los tienen,
# y ajusta ruedas. Trabaja sobre el FBX del carro tal cual esta (importa, edita, vuelve a exportar).
# uso: blender -b --python t1380_car_parts.py -- <carpeta_entrada> <carpeta_salida> COD [COD ...]
# Convencion del proyecto: frente -Y, metros, Z arriba (Blender), piso en z=0, hijos de una raiz vacia con el codigo.
#   Cofre: pivote en el borde de atras; abre girando NEGATIVO en X local.
#   Puerta_I (x>0): pivote en la bisagra delantera, abre girando NEGATIVO en Z local. Puerta_D (x<0): POSITIVO en Z local.
import bpy, bmesh, sys, os, math
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index('--') + 1:]
IN, OUT = argv[0], argv[1]
CODES = argv[2:]

# door = (yf, yr, zb, zt) de la puerta delantera, hood = (yf, yr) del cofre, kind: 'cut' (puerta pintada en la carroceria)
# o 'panel' (hueco abierto: se modela una lamina de puerta). wheel_k: escala de las ruedas (se bajan para tocar el piso)
SPEC = {
    'V02': dict(kind='cut', door=(-0.16, 0.07, 0.10, 0.30), hood=(-0.47, -0.22)),
    'V03': dict(kind='cut', door=(-0.155, 0.12, 0.075, 0.29), hood=(-0.47, -0.215)),
    'V04': dict(kind='cut', door=(-0.295, -0.075, 0.10, 0.30), hood=(-0.47, -0.30), arch_k=1.6),
    'V06': dict(kind='cut', door=(-0.19, 0.10, 0.10, 0.29), hood=(-0.47, -0.18)),
    'V09': dict(kind='cut', door=(-0.15, 0.085, 0.092, 0.257), hood=(-0.47, -0.13), wheel_k=0.88),
    'V10': dict(kind='cut', door=(-0.18, 0.054, 0.094, 0.244), hood=(-0.47, -0.22), wheel_k=0.88),
    'V22': dict(kind='cut', door=(-0.59, 0.50, 0.23, 0.83), recut=True),
    'V23': dict(kind='cut', door=(-0.60, 0.50, 0.23, 0.83), recut=True, remnant=True),
    'V01A': dict(kind='panel', door=(-0.14, 0.085, 0.10, 0.26), fix_motor=True),
    'V01B': dict(kind='panel', door=(-0.126, 0.10, 0.105, 0.255), fix_motor=True),
    'V05': dict(kind='panel', door=(-0.157, 0.086, 0.10, 0.265), fix_motor=True),
}


def mat(name, col, rough=0.7, metal=0.0):
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*col, 1); b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    m.diffuse_color = (*col, 1)
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
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    sel = set(idx)
    me2 = me.copy(); me2.name = name
    bm2 = bmesh.new(); bm2.from_mesh(me2); bm2.faces.ensure_lookup_table()
    bmesh.ops.delete(bm2, geom=[f for f in bm2.faces if f.index not in sel], context='FACES')
    for v in [v for v in bm2.verts if not v.link_faces]: bm2.verts.remove(v)
    bm2.to_mesh(me2); bm2.free()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index in sel], context='FACES')
    for v in [v for v in bm.verts if not v.link_faces]: bm.verts.remove(v)
    bm.to_mesh(me); bm.free(); me.update()
    o2 = bpy.data.objects.new(name, me2)
    for c in o.users_collection: c.objects.link(o2)
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


def box_mesh(name, x0, x1, y0, y1, z0, z1, m, inward=False):
    bm = bmesh.new()
    vs = [bm.verts.new(p) for p in [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                                    (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]]
    for f in [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]:
        bm.faces.new([vs[i] for i in f])
    if inward: bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(m)
    return me


def join_meshes(name, mes):
    bm = bmesh.new()
    mats = []
    for me in mes:
        for m in me.materials:
            if m not in mats: mats.append(m)
    for me in mes:
        mp = {i: mats.index(m) for i, m in enumerate(me.materials)}
        off = len(bm.verts)
        vm = {}
        for v in me.vertices: vm[v.index] = bm.verts.new(v.co)
        for p in me.polygons:
            f = bm.faces.new([vm[i] for i in p.vertices]); f.material_index = mp[p.material_index]
    out = bpy.data.meshes.new(name); bm.to_mesh(out); bm.free()
    for m in mats: out.materials.append(m)
    return out


def engine_bay(root, xh, yf, yr, zt, zb):
    dark = mat('Motor_Bahia', (0.05, 0.05, 0.06), 0.9)
    metal = mat('Motor_Metal', (0.35, 0.36, 0.38), 0.5, 0.8)
    block = mat('Motor', (0.12, 0.13, 0.14), 0.6, 0.5)
    L = yr - yf; W = 2 * xh; H = zt - zb
    parts = [box_mesh('b', -xh + 0.01, xh - 0.01, yf + 0.02, yr - 0.01, zb, zt, dark, inward=True)]
    bw = 0.42 * W; bl = 0.5 * L; bh = 0.5 * H; cy = yf + 0.55 * L
    parts.append(box_mesh('blk', -bw / 2, bw / 2, cy - bl / 2, cy + bl / 2, zb, zb + bh, block))
    parts.append(box_mesh('tapa', -bw * 0.45, bw * 0.45, cy - bl * 0.45, cy + bl * 0.45, zb + bh, zb + bh * 1.18, metal))
    parts.append(box_mesh('rad', -0.38 * W, 0.38 * W, yf + 0.03, yf + 0.045, zb + 0.05 * H, zt - 0.1 * H, metal))
    parts.append(box_mesh('bat', 0.22 * W, 0.4 * W, yr - 0.35 * L, yr - 0.1 * L, zb, zb + 0.4 * H, metal))
    parts.append(box_mesh('filtro', -0.4 * W, -0.22 * W, yf + 0.2 * L, yf + 0.4 * L, zb, zb + 0.55 * H, block))
    me = join_meshes('Motor', parts)
    o = bpy.data.objects.new('Motor', me)
    root.users_collection[0].objects.link(o)
    parent_keep(o, root)
    return o


def cut_hood(root, body, yf, yr):
    T = bvh(body)
    ym = (yf + yr) / 2
    zs = []
    for y in np.arange(yf + 0.02, yr - 0.01, 0.02):
        h = T.ray_cast(Vector((0, y, 5)), Vector((0, 0, -1)), 10)
        if h[0] is not None: zs.append(h[0].z)
    ztm = float(np.median(zs))
    hw = T.ray_cast(Vector((5, ym, ztm - 0.06)), Vector((-1, 0, 0)), 10)[0]
    hw = hw.x if hw else 0.25
    wheels = [o for o in root.children if o.name.startswith('Rueda')]
    inner = min(abs(o.matrix_world.translation.x) - 0.5 * (max(v.co.x for v in o.data.vertices) - min(v.co.x for v in o.data.vertices)) for o in wheels)
    xh = min(0.8 * hw, inner - 0.01)
    for co, no in (((0, yf, 0), (0, 1, 0)), ((0, yr, 0), (0, 1, 0)), ((xh, 0, 0), (1, 0, 0)), ((-xh, 0, 0), (1, 0, 0))):
        bisect(body, co, no)
    T = bvh(body)
    sel = []
    for p in body.data.polygons:
        c = p.center
        if not (yf < c.y < yr and abs(c.x) < xh and c.z > ztm - 0.09): continue
        h = T.ray_cast(Vector((c.x, c.y, c.z + 5)), Vector((0, 0, -1)), 10)
        if h[0] is not None and c.z > h[0].z - 0.012 and p.normal.z > -0.2: sel.append(p.index)
    hood = separate(body, sel, 'Cofre')
    with_backface(hood, (0, 0, -0.008))
    P = np.array([tuple(v.co) for v in hood.data.vertices])
    rear = P[P[:, 1] > yr - 0.03]
    hz = float(rear[:, 2].max()) if len(rear) else float(P[:, 2].max())
    set_origin(hood, (0, yr, hz))
    parent_keep(hood, root)
    zlow = float(P[:, 2].min())
    # fondo del cuerpo bajo el cofre (rayos hacia arriba) para que la bahia no asome por debajo
    bot = []
    for y in np.arange(yf + 0.03, yr - 0.02, 0.03):
        for x in (-xh * 0.6, 0, xh * 0.6):
            h = T.ray_cast(Vector((x, y, -5)), Vector((0, 0, 1)), 10)
            if h[0] is not None: bot.append(h[0].z)
    zfloor = max(zlow - 0.13, (float(np.percentile(bot, 80)) + 0.02) if bot else 0)
    engine_bay(root, xh, yf, yr, zlow - 0.01, min(zfloor, zlow - 0.05))
    return dict(n=len(sel), hinge=(0, round(yr, 3), round(hz, 3)), xh=round(xh, 3))


ARCH_K = 1.15
SC = 1.0  # tamano del carro respecto a 1 m (se calcula por carro)


def cut_door(root, body, glass, name, sx, yf, yr, zb, zt):
    for co, no in (((0, yf, 0), (0, 1, 0)), ((0, yr, 0), (0, 1, 0)), ((0, 0, zb), (0, 0, 1)), ((0, 0, zt), (0, 0, 1))):
        bisect(body, co, no)
    T = bvh(body)
    arcs = []
    for w in root.children:
        if w.name.startswith('Rueda') and sx * w.matrix_world.translation.x > 0:
            Pw = [v.co for v in w.data.vertices]
            r = (max(v.z for v in Pw) - min(v.z for v in Pw)) / 2
            arcs.append((w.matrix_world.translation.y, w.matrix_world.translation.z, r * ARCH_K))
    sel = []
    for p in body.data.polygons:
        c = p.center
        if not (yf < c.y < yr and zb < c.z < zt and sx * c.x > 0.05): continue
        if any((c.y - ay) ** 2 + (c.z - az) ** 2 < ar * ar for ay, az, ar in arcs): continue
        h = T.ray_cast(Vector((sx * 5.0, c.y, c.z)), Vector((-sx, 0, 0)), 10)
        if h[0] is not None and sx * c.x > sx * h[0].x - 0.025 * SC: sel.append(p.index)
    door = separate(body, sel, name)
    with_backface(door, (-sx * 0.01 * SC, 0, 0))
    P = np.array([tuple(v.co) for v in door.data.vertices])
    xo = float(P[:, 0].max()) if sx > 0 else float(P[:, 0].min())
    hinge = Vector((xo - sx * 0.02 * SC, yf, (zb + zt) / 2))
    set_origin(door, hinge)
    parent_keep(door, root)
    gl = None
    if glass is not None:
        gi = [p.index for p in glass.data.polygons if yf - 0.01 < p.center.y < yr + 0.01 and sx * p.center.x > 0.05]
        if gi:
            gl = separate(glass, gi, 'Cristal_' + name)
            set_origin(gl, hinge)
            parent_keep(gl, door)
    return door, dict(n=len(sel), hinge=[round(x, 3) for x in hinge], glass=0 if gl is None else len(gl.data.polygons))


def door_panel(root, body, name, sx, yf, yr, zb, zt):
    T = bvh(body)
    xs = []
    for y in list(np.arange(yr + 0.03, yr + 0.09, 0.02)) + list(np.arange(yf - 0.09, yf - 0.03, 0.02)):
        h = T.ray_cast(Vector((sx * 5.0, y, (zb + zt) / 2 - 0.02)), Vector((-sx, 0, 0)), 10)
        if h[0] is not None: xs.append(abs(h[0].x))
    xo = sx * float(np.median(xs)) if xs else sx * 0.22
    # color de pintura: de la carroceria justo detras de la puerta se toman varias muestras de textura y se usa
    # la UV cuyo pixel queda mas cerca del color medio (un solo punto de textura = color liso de la pintura)
    uvl = body.data.uv_layers.active
    img = None
    for m in body.data.materials:
        if m and m.use_nodes:
            for n in m.node_tree.nodes:
                if n.type == 'TEX_IMAGE' and n.image: img = n.image
    samples = []
    for y in np.arange(yr + 0.0, yr + 0.18, 0.015):
        for z in np.linspace(zb + 0.02, zt - 0.02, 5):
            h = T.ray_cast(Vector((sx * 5.0, y, z)), Vector((-sx, 0, 0)), 10)
            if h[0] is not None and abs(abs(h[0].x) - abs(xo)) < 0.04:
                p = body.data.polygons[h[2]]
                uvs = [uvl.data[li].uv.copy() for li in p.loop_indices]
                samples.append((sum(uvs, Vector((0, 0))) / len(uvs), p.material_index))
    sample = None; mi = 0
    if samples and img is not None:
        w, hh = img.size
        px = np.array(img.pixels[:], dtype=np.float32).reshape(hh, w, img.channels)
        cols = np.array([px[min(hh - 1, int(uv.y * hh)), min(w - 1, int(uv.x * w)), :3] for uv, _ in samples])
        med = np.median(cols, axis=0)
        k = int(np.argmin(((cols - med) ** 2).sum(1)))
        sample, mi = samples[k]
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new('UVMap')
    th = 0.018
    outline = [(yf, zb + 0.015), (yf + 0.015, zb), (yr - 0.015, zb), (yr, zb + 0.015), (yr, zt), (yf, zt)]
    xa, xb = (xo - th, xo) if sx > 0 else (xo, xo + th)
    def ring(x): return [bm.verts.new((x, y, z)) for (y, z) in outline]
    A = ring(xa); B = ring(xb)
    n = len(outline)
    fo = bm.faces.new(B if sx > 0 else B[::-1])
    fi = bm.faces.new(A[::-1] if sx > 0 else A)
    side = []
    for i in range(n):
        j = (i + 1) % n
        q = [A[i], A[j], B[j], B[i]]
        side.append(bm.faces.new(q if sx > 0 else q[::-1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for f in bm.faces:
        for l in f.loops: l[uv].uv = sample if sample is not None else (0.5, 0.5)
        f.material_index = 0
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(body.data.materials[mi] if sample is not None else mat('Puerta', (0.3, 0.3, 0.3)))
    o = bpy.data.objects.new(name, me)
    root.users_collection[0].objects.link(o)
    set_origin(o, (xo - sx * 0.01, yf, (zb + zt) / 2))
    parent_keep(o, root)
    return o, dict(xo=round(xo, 3), uv=None if sample is None else tuple(round(v, 3) for v in sample))


def merge_back(root, body, glass):
    """devuelve a la carroceria (y al cristal) las puertas de la ronda anterior, sin sus caras de espesor duplicadas"""
    def absorb(dst, src, half):
        bm = bmesh.new(); bm.from_mesh(dst.data)
        M = dst.matrix_world.inverted() @ src.matrix_world
        mats = [m for m in dst.data.materials]
        n = len(src.data.polygons) // 2 if half else len(src.data.polygons)
        for p in list(src.data.polygons)[:n]:
            vs = [bm.verts.new(M @ src.data.vertices[i].co) for i in p.vertices]
            f = bm.faces.new(vs)
            sm = src.data.materials[p.material_index] if p.material_index < len(src.data.materials) else None
            if sm is not None and sm in mats: f.material_index = mats.index(sm)
        bm.to_mesh(dst.data); bm.free(); dst.data.update()
    for d in [o for o in root.children if o.name.startswith('Puerta')]:
        for g in list(d.children):
            if glass is not None: absorb(glass, g, False)
            bpy.data.objects.remove(g, do_unlink=True)
        absorb(body, d, True)
        bpy.data.objects.remove(d, do_unlink=True)


def remove_remnants(root, body):
    """quita de la carroceria los restos de la llanta original (discos pegados al hueco)"""
    n = 0
    bm = bmesh.new(); bm.from_mesh(body.data); bm.faces.ensure_lookup_table()
    kill = []
    for w in [o for o in root.children if o.name.startswith('Rueda')]:
        Pw = [v.co for v in w.data.vertices]
        r = (max(v.z for v in Pw) - min(v.z for v in Pw)) / 2
        hw = (max(v.x for v in Pw) - min(v.x for v in Pw)) / 2
        wx, wy, wz = w.matrix_world.translation
        sx = 1 if wx > 0 else -1
        for f in bm.faces:
            c = f.calc_center_median()
            if sx * c.x < 0: continue
            if (c.y - wy) ** 2 + (c.z - wz) ** 2 < (r * 1.1) ** 2 and abs(c.x - wx) < hw + 0.12 * SC and abs(f.normal.x) > 0.6:
                kill.append(f)
    kill = list(set(kill))
    n = len(kill)
    bmesh.ops.delete(bm, geom=kill, context='FACES')
    for v in [v for v in bm.verts if not v.link_faces]: bm.verts.remove(v)
    bm.to_mesh(body.data); bm.free(); body.data.update()
    return n


def export(root, path):
    vl = bpy.context.view_layer
    for ob in vl.objects: ob.select_set(False)
    for ob in [root] + list(root.children_recursive): ob.select_set(True)
    vl.objects.active = root
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'}, global_scale=1.0,
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=False, add_leaf_bones=False, bake_anim=False, path_mode='COPY',
                             embed_textures=True, use_mesh_modifiers=True)


def flatten_import(code):
    """FBX importado: deja cada malla con su transformacion identica a la del archivo (origen donde estaba)"""
    bpy.ops.import_scene.fbx(filepath=os.path.join(IN, code + '.fbx'))
    root = [o for o in bpy.context.scene.objects if o.type == 'EMPTY' and o.name == code][0]
    return root


os.makedirs(OUT, exist_ok=True)
for code in CODES:
    sp = SPEC[code]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = flatten_import(code)
    ob = {o.name: o for o in root.children}
    body = ob['Carroceria']
    # el cuerpo debe tener origen en 0 y sin rotacion para trabajar en coordenadas del mundo
    assert body.matrix_world.translation.length < 1e-6, body.matrix_world.translation
    glass = ob.get('Cristal_Laterales')
    rep = {}
    Pb = np.array([tuple(v.co) for v in body.data.vertices])
    SC = float((Pb[:, 1].max() - Pb[:, 1].min()) / 0.98)
    if sp.get('recut'): merge_back(root, body, glass)
    if sp.get('remnant'): rep['restos_llanta_quitados'] = remove_remnants(root, body)
    if sp.get('wheel_k'):
        k = sp['wheel_k']
        for o in root.children:
            if o.name.startswith('Rueda'):
                c = o.matrix_world.translation.copy()
                o.data.transform(Matrix.Scale(k, 4))
                r0 = (max(v.co.z for v in o.data.vertices) - min(v.co.z for v in o.data.vertices)) / 2
                c.z = r0  # toca el piso
                o.matrix_world = Matrix.Translation(c)
        rep['ruedas'] = 'k=%.2f' % k
    if sp.get('fix_motor') and 'Motor' in ob:
        # la bahia del motor (de la ronda anterior) asomaba por debajo del carro: se sube el fondo hasta el fondo de la carroceria
        T0 = bvh(body); mo = ob['Motor']
        Pm = np.array([tuple(mo.matrix_world @ v.co) for v in mo.data.vertices])
        bot = []
        for y in np.arange(Pm[:, 1].min() + 0.02, Pm[:, 1].max() - 0.02, 0.03):
            for x in (-0.1, 0.0, 0.1):
                h = T0.ray_cast(Vector((x, y, -5)), Vector((0, 0, 1)), 10)
                if h[0] is not None: bot.append(h[0].z)
        zmin = float(np.percentile(bot, 80)) + 0.015 if bot else 0.07
        n = 0
        for v in mo.data.vertices:
            if v.co.z < zmin: v.co.z = zmin; n += 1
        mo.data.update()
        rep['motor_subido'] = (n, round(zmin, 3))
    ARCH_K = sp.get('arch_k', 1.15)
    yf, yr, zb, zt = sp['door']
    if sp['kind'] == 'cut' and 'hood' in sp and not sp.get('recut'):
        rep['cofre'] = cut_hood(root, body, *sp['hood'])
    for nm, sx in (('Puerta_I', 1), ('Puerta_D', -1)):
        if sp['kind'] == 'cut':
            d, info = cut_door(root, body, glass, nm, sx, yf, yr, zb, zt)
        else:
            d, info = door_panel(root, body, nm, sx, yf, yr, zb, zt)
        rep[nm] = info
    export(root, os.path.join(OUT, code + '.fbx'))
    print('CAR', code, rep)
