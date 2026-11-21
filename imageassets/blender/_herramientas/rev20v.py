
"""rev20v.py — revision de vehiculos 20-nov (solo diagnostico)"""
import bpy, bmesh, os, math, json
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
import rev20 as R
UV = r'C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk\Assets\Arte\Vehiculos'

TIPOS = {  # (largo, ancho, alto) rangos reales aproximados
 'auto': ((3.6, 5.2), (1.6, 2.05), (1.2, 1.9)),
 'pickup': ((4.8, 5.9), (1.8, 2.1), (1.6, 2.1)),
 'moto': ((1.8, 2.4), (0.6, 1.0), (1.0, 1.4)),
 'microbus': ((5.0, 7.5), (1.9, 2.4), (2.2, 3.0)),
 'camion': ((6.5, 10.0), (2.2, 2.6), (2.8, 3.8)),
 'trailer': ((12.0, 18.0), (2.4, 2.6), (3.5, 4.2)),
 'ambulancia': ((5.0, 6.5), (1.9, 2.3), (2.3, 2.9)),
 'dron': ((0.3, 3.0), (0.3, 3.0), (0.1, 1.5)),
 'nave': ((4.0, 25.0), (3.0, 25.0), (1.5, 8.0)),
}

def wv(o):
    n = len(o.data.vertices)
    if not n: return np.zeros((0, 3))
    co = np.empty(n * 3, np.float32); o.data.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
    M = np.array(o.matrix_world); return co @ M[:3, :3].T + M[:3, 3]

def imp(fp, cname):
    b = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fp)
    new = [o for o in bpy.data.objects if o not in b]
    c = R.coll(cname, R.coll('REVISION_VEHICULOS'))
    for o in new:
        for u in list(o.users_collection): u.objects.unlink(o)
        c.objects.link(o)
    return new

def bvh_of(o):
    P = wv(o)
    return BVHTree.FromPolygons([Vector(x) for x in P], [tuple(p.vertices) for p in o.data.polygons])

def islands(o):
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    seen = set(); isl = []
    for v in bm.verts:
        if v.index in seen: continue
        st = [v]; comp = []
        seen.add(v.index)
        while st:
            x = st.pop(); comp.append(x.index)
            for e in x.link_edges:
                y = e.other_vert(x)
                if y.index not in seen: seen.add(y.index); st.append(y)
        isl.append(comp)
    bm.free(); return isl

def floating(o, gap=0.03):
    """islas de la malla cuyo bbox no toca el de ninguna otra (piezas sueltas/flotando)"""
    P = wv(o); isl = islands(o)
    if len(isl) < 2: return 0, len(isl)
    bbs = [(P[i].min(0) - gap, P[i].max(0) + gap) for i in [np.array(c) for c in isl]]
    big = max(range(len(isl)), key=lambda k: len(isl[k]))
    fl = 0
    for k, (a, b) in enumerate(bbs):
        if k == big or len(isl[k]) < 8: continue
        touch = False
        for j, (c, d) in enumerate(bbs):
            if j == k: continue
            if np.all(a <= d) and np.all(c <= b): touch = True; break
        if not touch: fl += 1
    return fl, len(isl)

def glass_info(ms, body):
    gl = [o for k, o in ms.items() if k.startswith('Cristal')]
    trans = []
    for o in gl:
        t = False
        for m in o.data.materials:
            if not m: continue
            if m.diffuse_color[3] < 0.99: t = True
            try:
                if m.surface_render_method == 'BLENDED' or getattr(m, 'blend_method', 'OPAQUE') != 'OPAQUE': t = True
            except Exception: pass
            if m.node_tree:
                for n in m.node_tree.nodes:
                    if n.type == 'BSDF_PRINCIPLED':
                        a = n.inputs['Alpha']; tr = n.inputs.get('Transmission Weight') or n.inputs.get('Transmission')
                        if (not a.is_linked and a.default_value < 0.99) or (tr and tr.default_value > 0.3): t = True
        trans.append(t)
    # huecos de ventana: rayos desde centro del cristal hacia ambos lados; si la carroceria esta a < 4 cm detras/delante, hay cara tapando
    tapado = []
    if body is not None and gl:
        bv = bvh_of(body)
        for o in gl:
            me = o.data; W = o.matrix_world; N3 = W.to_3x3().inverted().transposed()
            hits = 0; tot = 0
            for p in [me.polygons[i] for i in range(0, len(me.polygons), max(1, len(me.polygons) // 40))]:
                c = W @ p.center; n = (N3 @ p.normal).normalized(); tot += 1
                for s in (1, -1):
                    h = bv.ray_cast(c + n * s * 0.002, n * s, 0.04)
                    if h[0] is not None: hits += 1; break
            tapado.append(round(hits / max(tot, 1), 2))
    return dict(n=len(gl), transparentes=sum(trans), cara_detras_pct=max(tapado) if tapado else None)

def hinge_err(o, kind):
    """distancia del pivote al borde de la bisagra esperada (puerta: borde delantero -Y; cofre: borde trasero +Y o delantero)"""
    P = wv(o); piv = o.matrix_world.translation
    if kind == 'puerta':
        return round(float(abs(piv.y - P[:, 1].min())), 3)
    return round(float(min(abs(piv.y - P[:, 1].max()), abs(piv.y - P[:, 1].min()))), 3)

def open_clip(o, body, ax, ang):
    """giro de prueba: vertices de la pieza que quedan dentro/atravesando la carroceria (cuenta caras que se cruzan)"""
    e = o.rotation_euler.copy(); e2 = e.copy(); e2.rotate_axis(ax, math.radians(ang)); o.rotation_euler = e2; bpy.context.view_layer.update()
    a = bvh_of(o); b = bvh_of(body)
    n = len(a.overlap(b))
    o.rotation_euler = e; bpy.context.view_layer.update()
    a0 = bvh_of(o); n0 = len(a0.overlap(b))
    return n0, n

def check(root, code, tipo):
    ms = {o.name.split('.')[0]: o for o in root.children_recursive if o.type == 'MESH'}
    P = np.concatenate([wv(o) for o in ms.values() if len(o.data.vertices)]); mn, mx = P.min(0), P.max(0)
    body = ms.get('Carroceria') or ms.get('Cuerpo') or (list(ms.values())[0] if len(ms) == 1 else None)
    r = dict(L=round(float(mx[1] - mn[1]), 2), W=round(float(mx[0] - mn[0]), 2), H=round(float(mx[2] - mn[2]), 2))
    r['pivote_raiz_cm'] = round(100 * float(max(abs((mn[0] + mx[0]) / 2), abs((mn[1] + mx[1]) / 2), abs(mn[2]))), 1)
    rg = TIPOS.get(tipo)
    if rg:
        bad = []
        for k, (lo, hi) in zip(('L', 'W', 'H'), rg):
            if not (lo * 0.92 <= r[k] <= hi * 1.08): bad.append('%s=%.2f (esperado %.1f-%.1f)' % (k, r[k], lo, hi))
        r['escala'] = 'OK' if not bad else '; '.join(bad)
    wl = [o for k, o in ms.items() if k.startswith('Rueda')]
    r['ruedas'] = len(wl)
    if wl:
        pe = []; zb = []; rd = []
        for o in wl:
            Q = wv(o); c = (Q.min(0) + Q.max(0)) / 2; t = o.matrix_world.translation
            pe.append(float(math.hypot(c[1] - t.y, c[2] - t.z))); zb.append(float(Q[:, 2].min()))
            d = Q.max(0) - Q.min(0); rd.append(float(abs(d[1] - d[2]) / max(d[1], d[2])))
        r['rueda_pivote_cm'] = round(100 * max(pe), 1); r['rueda_piso_cm'] = [round(100 * z, 1) for z in zb]
        r['rueda_desnivel_cm'] = round(100 * (max(zb) - min(zb)), 1); r['rueda_no_redonda_pct'] = round(100 * max(rd), 1)
    rot = [o for k, o in ms.items() if k.startswith(('Rotor', 'Helice', 'Propela'))]
    if rot:
        pe = []
        for o in rot:
            Q = wv(o); c = (Q.min(0) + Q.max(0)) / 2; t = o.matrix_world.translation
            pe.append(float(math.hypot(c[0] - t.x, c[1] - t.y)))
        r['rotores'] = len(rot); r['rotor_pivote_cm'] = round(100 * max(pe), 1)
    r['puertas'] = sorted(k for k in ms if k.startswith('Puerta')); r['cofre'] = 'Cofre' in ms
    if body is not None:
        for k in r['puertas']:
            r['bisagra_' + k + '_cm'] = round(100 * hinge_err(ms[k], 'puerta'), 1)
            sg = -1 if k.endswith('I') else 1
            try: r['choque_' + k] = open_clip(ms[k], body, 'Z', -65 if k.endswith('I') else 65)
            except Exception as e: r['choque_' + k] = str(e)[:40]
        if 'Cofre' in ms:
            r['bisagra_Cofre_cm'] = round(100 * hinge_err(ms['Cofre'], 'cofre'), 1)
            try: r['choque_Cofre'] = open_clip(ms['Cofre'], body, 'X', -50)
            except Exception as e: r['choque_Cofre'] = str(e)[:40]
        r['cristales'] = glass_info(ms, body)
        r['interior'] = any(k.startswith('Interior') for k in ms); r['motor'] = 'Motor' in ms
        r['invertidas_pct'] = R.mesh_issues(body)['winding_inv']
        bm = bmesh.new(); bm.from_mesh(body.data); r['bordes_abiertos'] = sum(1 for e in bm.edges if e.is_boundary); bm.free()
        fl, ni = floating(body); r['islas'] = ni; r['islas_flotando'] = fl
        r['vertices'] = len(body.data.vertices)
    r['piezas'] = sorted(ms.keys())
    return r

REF = None
def views(root, code, out, cname):
    allo = [root] + list(root.children_recursive)
    ms = {o.name.split('.')[0]: o for o in allo}
    P = np.concatenate([wv(o) for o in allo if o.type == 'MESH' and len(o.data.vertices)]); mn, mx = P.min(0), P.max(0)
    L = float(mx[1] - mn[1]); Wd = float(mx[0] - mn[0]); H = float(mx[2] - mn[2])
    c = Vector(((mn[0] + mx[0]) / 2, (mn[1] + mx[1]) / 2, max(H, 1.8) * 0.45))
    d = max(L, Wd, H * 1.4, 2.2) * 1.45
    sc = bpy.context.scene; sc.display.shading.show_backface_culling = False
    V = [('frente', Vector((0, -1, 0.08)), False), ('lado', Vector((1, 0, 0.05)), False), ('atras', Vector((0, 1, 0.1)), False),
         ('3/4', Vector((0.75, -0.75, 0.35)), False), ('3/4 abierto', Vector((-0.65, -0.75, 0.45)), True), ('abajo', Vector((0.25, -0.3, -1)), False)]
    paths = []
    for k, (nm, dv, opn) in enumerate(V):
        rot = {}
        if opn:
            for n, ax, a in (('Cofre', 'X', -50), ('Puerta_I', 'Z', -65), ('Puerta_D', 'Z', 65)):
                o = ms.get(n)
                if o: rot[n] = o.rotation_euler.copy(); e2 = o.rotation_euler.copy(); e2.rotate_axis(ax, math.radians(a)); o.rotation_euler = e2
            bpy.context.view_layer.update()
        tgt = c.copy()
        if nm == 'abajo': tgt.z = 0.2
        p = os.path.join(R.CAP, '_tmp_v_%s_%d.png' % (code, k))
        R.snapL(p, tuple(tgt + dv.normalized() * d), tuple(tgt), label=('%s  %s' % (code, nm)), lens=35, res=(480, 340)); paths.append(p)
        for n, e in rot.items(): ms[n].rotation_euler = e
    bpy.context.view_layer.update()
    return R.sheet([paths[:3], paths[3:]], out)


def solo_v(names):
    keep = set(names)
    for lc in bpy.context.view_layer.layer_collection.children:
        if lc.name == 'REVISION_VEHICULOS':
            lc.exclude = False
            for ch in lc.children: ch.exclude = ch.name not in keep

def proc(code, fp, tipo, slot):
    cname = 'REV_' + code
    if cname in bpy.data.collections: return None
    new = imp(fp, cname)
    solo_v([cname])
    roots = [o for o in new if o.parent is None]
    root = [o for o in roots if o.type == 'EMPTY']
    root = root[0] if root else roots[0]
    for o in roots:
        if o is not root and o.parent is None: o.parent = root; o.matrix_parent_inverse = root.matrix_world.inverted()
    bpy.context.view_layer.update()
    if root.type == 'MESH':
        class _W: pass
    r = check(root, code, tipo) if root.type == 'EMPTY' else check_single(root, code, tipo)
    ms = [o for o in new if o.type == 'MESH']
    P = np.concatenate([wv(o) for o in ms if len(o.data.vertices)]); mn, mx = P.min(0), P.max(0)
    refc = bpy.data.collections['_REFERENCIA']
    e = bpy.data.objects.new('ref_' + code, None); e.instance_type = 'COLLECTION'; e.instance_collection = refc
    e.location = (float(mx[0]) + 0.8, float((mn[1] + mx[1]) / 2), 0); bpy.data.collections[cname].objects.link(e)
    out = os.path.join(R.CAP, 'V_%s.png' % code)
    views(root, code, out, cname)
    # mover a su lugar
    e.parent = root; e.matrix_parent_inverse = root.matrix_world.inverted()
    root.location.x += slot
    r['tipo'] = tipo; r['fbx'] = fp; r['coleccion'] = cname
    return r

def check_single(o, code, tipo):
    """FBX viejo de una sola malla (sin piezas)"""
    P = wv(o); mn, mx = P.min(0), P.max(0)
    r = dict(L=round(float(mx[1] - mn[1]), 2), W=round(float(mx[0] - mn[0]), 2), H=round(float(mx[2] - mn[2]), 2))
    r['pivote_raiz_cm'] = round(100 * float(max(abs((mn[0] + mx[0]) / 2), abs((mn[1] + mx[1]) / 2), abs(mn[2]))), 1)
    rg = TIPOS.get(tipo); bad = []
    for k, (lo, hi) in zip(('L', 'W', 'H'), rg):
        if not (lo * 0.92 <= r[k] <= hi * 1.08): bad.append('%s=%.2f (esperado %.1f-%.1f)' % (k, r[k], lo, hi))
    r['escala'] = 'OK' if not bad else '; '.join(bad)
    r['ruedas'] = 0; r['puertas'] = []; r['cofre'] = False; r['piezas'] = [o.name]
    r['una_sola_malla'] = True; r['invertidas_pct'] = R.mesh_issues(o)['winding_inv']
    bm = bmesh.new(); bm.from_mesh(o.data); r['bordes_abiertos'] = sum(1 for e in bm.edges if e.is_boundary); bm.free()
    r['vertices'] = len(o.data.vertices)
    return r
