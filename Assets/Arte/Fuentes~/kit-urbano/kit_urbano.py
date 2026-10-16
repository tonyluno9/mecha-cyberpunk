"""Kit de props urbanos cyberpunk low-poly (megaciudad mexicana).
Uso: blender -b -P kit_urbano.py -- [engine EEVEE|WORKBENCH] [solo1,solo2]
Metros, origen en la base, frente hacia -Y (Blender). Salida: .glb + .png en la carpeta del script."""
import bpy, bmesh, math, random, os, sys
from mathutils import Vector, Euler

OUT = os.path.dirname(os.path.abspath(__file__))
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ENGINE = ARGS[0] if ARGS else "EEVEE"
ONLY = ARGS[1].split(",") if len(ARGS) > 1 else None
R = random.Random(7)
BEV_MIN = 0.5

# ---------------------------------------------------------------- materiales
def mk_mat(name, col, rough=0.8, metal=0.0, emit=None, es=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (*col, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1)
        b.inputs["Emission Strength"].default_value = es
    m.diffuse_color = (*(emit or col), 1)
    return m

def make_mats():
    M = {}
    M["cian"] = mk_mat("Neon_Cian", (0, 0.05, 0.06), 0.3, 0, (0.0, 0.85, 1.0), 6)
    M["mag"] = mk_mat("Neon_Magenta", (0.06, 0, 0.04), 0.3, 0, (1.0, 0.0, 0.6), 6)
    M["nar"] = mk_mat("Neon_Naranja", (0.06, 0.02, 0), 0.3, 0, (1.0, 0.4, 0.02), 6)
    M["tur"] = mk_mat("Neon_Turquesa", (0, 0.06, 0.04), 0.3, 0, (0.0, 1.0, 0.65), 6)
    M["con"] = mk_mat("Concreto_Sucio", (0.30, 0.285, 0.26), 0.95)
    M["met"] = mk_mat("Metal_Oscuro", (0.13, 0.14, 0.17), 0.5, 0.6)
    # extras de apoyo
    M["conD"] = mk_mat("Concreto_Manchas", (0.13, 0.12, 0.11), 0.98)
    M["vid"] = mk_mat("Vidrio_Interior", (0.0, 0.05, 0.05), 0.2, 0, (0.0, 0.7, 0.55), 1.2)
    M["toA"] = mk_mat("Toldo_Rojo", (0.55, 0.05, 0.08), 0.9)
    M["toB"] = mk_mat("Toldo_Crema", (0.75, 0.68, 0.5), 0.9)
    M["car"] = mk_mat("Carne_Pastor", (0.55, 0.2, 0.05), 0.7)
    M["pin"] = mk_mat("Pina", (0.9, 0.7, 0.1), 0.6)
    M["sal"] = mk_mat("Salsa_Verde", (0.1, 0.5, 0.1), 0.5)
    M["sar"] = mk_mat("Salsa_Roja", (0.7, 0.05, 0.03), 0.5)
    M["gas"] = mk_mat("Tanque_Gas", (0.75, 0.75, 0.7), 0.5, 0.4)
    M["tin"] = mk_mat("Tinaco_Azul", (0.05, 0.15, 0.35), 0.6)
    M["gro"] = mk_mat("Hule_Negro", (0.02, 0.02, 0.02), 0.9)
    M["bas"] = mk_mat("Bolsa_Basura", (0.03, 0.03, 0.035), 0.3)
    M["bas2"] = mk_mat("Bolsa_Blanca", (0.6, 0.6, 0.62), 0.4)
    M["car2"] = mk_mat("Carton", (0.45, 0.3, 0.15), 0.9)
    M["bin"] = mk_mat("Contenedor_Verde", (0.04, 0.14, 0.1), 0.6, 0.5)
    M["gR"] = mk_mat("Graf_Rosa", (1.0, 0.1, 0.5), 0.5)
    M["gV"] = mk_mat("Graf_Verde", (0.2, 0.9, 0.1), 0.5)
    M["gA"] = mk_mat("Graf_Amarillo", (1.0, 0.85, 0.0), 0.5)
    M["gZ"] = mk_mat("Graf_Azul", (0.1, 0.3, 1.0), 0.5)
    M["gN"] = mk_mat("Graf_Negro", (0.0, 0.0, 0.0), 0.5)
    M["pap"] = mk_mat("Papel_Volante", (0.8, 0.78, 0.6), 0.9)
    M["por"] = mk_mat("Porcelana", (0.8, 0.8, 0.75), 0.3)
    M["mad"] = mk_mat("Madera_Vieja", (0.2, 0.12, 0.07), 0.9)
    M["roj"] = mk_mat("Luz_Roja", (0.1, 0, 0), 0.3, 0, (1.0, 0.05, 0.05), 5)
    return M

# ---------------------------------------------------------------- primitivas
class Prop:
    def __init__(self, name):
        self.name = name
        self.objs = []

def _link(o):
    bpy.context.scene.collection.objects.link(o)

def _from_bm(P, bm, mat, loc=(0, 0, 0), rot=(0, 0, 0)):
    me = bpy.data.meshes.new("m")
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new("p", me)
    _link(o)
    o.location = loc
    o.rotation_euler = Euler(rot)
    me.materials.append(mat)
    me.shade_flat() if hasattr(me, "shade_flat") else None
    P.objs.append(o)
    return o

def box(P, size, loc, mat, bevel=0.0, rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]; v.co.y *= size[1]; v.co.z *= size[2]
    if bevel > 0 and min(size) >= BEV_MIN:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=min(bevel, min(size) * 0.45),
                        offset_type='OFFSET', segments=1, affect='EDGES')
    return _from_bm(P, bm, mat, loc, rot)

def cyl(P, r, h, loc, mat, v=8, r2=None, rot=(0, 0, 0), caps=True):
    v = min(v, 8)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=caps, cap_tris=False, segments=v,
                          radius1=r, radius2=r if r2 is None else r2, depth=h)
    return _from_bm(P, bm, mat, loc, rot)

def sph(P, r, loc, mat, u=6, v=4, sc=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    for vv in bm.verts:
        vv.co.x *= sc[0]; vv.co.y *= sc[1]; vv.co.z *= sc[2]
    return _from_bm(P, bm, mat, loc)

def prism(P, w, h, t, loc, mat, rot=(0, 0, 0)):
    """prisma triangular: base w arriba, punta hacia abajo (-Z), grueso t en Y"""
    bm = bmesh.new()
    a = [bm.verts.new(p) for p in [(-w / 2, -t / 2, 0), (w / 2, -t / 2, 0), (0, -t / 2, -h)]]
    b = [bm.verts.new(p) for p in [(-w / 2, t / 2, 0), (w / 2, t / 2, 0), (0, t / 2, -h)]]
    bm.faces.new(a); bm.faces.new(b[::-1])
    for i in range(3):
        j = (i + 1) % 3
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return _from_bm(P, bm, mat, loc, rot)

def _curve_to_obj(P, cu, mat, loc=(0, 0, 0), rot=(0, 0, 0)):
    co = bpy.data.objects.new("c", cu)
    _link(co)
    co.location = loc
    co.rotation_euler = Euler(rot)
    bpy.context.view_layer.update()
    me = bpy.data.meshes.new_from_object(co)
    bpy.data.objects.remove(co)
    bpy.data.curves.remove(cu)
    o = bpy.data.objects.new("p", me)
    _link(o)
    o.location = loc
    o.rotation_euler = Euler(rot)
    me.materials.clear()
    me.materials.append(mat)
    P.objs.append(o)
    return o

def tube(P, pts, r, mat, res=0, cyclic=False, caps=False):
    cu = bpy.data.curves.new("t", 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = r
    cu.bevel_resolution = res
    cu.use_fill_caps = caps
    sp = cu.splines.new('POLY')
    sp.points.add(len(pts) - 1)
    for p, q in zip(sp.points, pts):
        p.co = (q[0], q[1], q[2], 1)
    sp.use_cyclic_u = cyclic
    return _curve_to_obj(P, cu, mat)

def sag(p0, p1, drop, n=6, jit=0.0):
    out = []
    for i in range(n + 1):
        t = i / n
        p = [p0[k] + (p1[k] - p0[k]) * t for k in range(3)]
        p[2] -= drop * 4 * t * (1 - t)
        if 0 < i < n and jit:
            p = [p[0] + R.uniform(-jit, jit), p[1] + R.uniform(-jit, jit), p[2] + R.uniform(-jit, jit)]
        out.append(tuple(p))
    return out

def cable(P, p0, p1, drop, mat, r=0.012, n=6, jit=0.0, res=0):
    return tube(P, sag(p0, p1, drop, n, jit), r, mat, res)

def text(P, s, size, loc, mat, rot=(math.radians(90), 0, 0), ext=0.03, ls=1.0, bold=True):
    cu = bpy.data.curves.new("tx", 'FONT')
    cu.body = s
    cu.size = size
    cu.extrude = ext
    cu.align_x = 'CENTER'
    cu.align_y = 'CENTER'
    cu.resolution_u = 1
    cu.space_line = ls
    if bold:
        cu.offset = 0.0
    return _curve_to_obj(P, cu, mat, loc, rot)

def rivets(P, pts, mat, r=0.018, rot=(0, 0, 0)):
    for p in pts:
        cyl(P, r, 0.02, p, mat, v=4, r2=r * 0.3, rot=rot, caps=False)

def finish(P):
    if os.environ.get("DBG"):
        L = sorted(((sum(len(p.vertices)-2 for p in o.data.polygons), tuple(round(x,2) for x in o.location)) for o in P.objs), reverse=True)
        print("TOP", P.name, L[:10], "N", len(P.objs))
    bpy.ops.object.select_all(action='DESELECT')
    for o in P.objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = P.objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = P.name
    # aplica transformaciones
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    me = o.data
    me.update()
    return o

def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

# ================================================================ 1) POSTE DE LUZ
def poste_luz(M):
    P = Prop("Poste_Luz")
    box(P, (0.55, 0.55, 0.4), (0, 0, 0.2), M["con"], 0.04)
    box(P, (0.38, 0.38, 0.04), (0, 0, 0.42), M["met"], 0.008)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl(P, 0.022, 0.09, (sx * 0.15, sy * 0.15, 0.5), M["met"], v=5)
    cyl(P, 0.17, 7.0, (0, 0, 3.94), M["met"], v=10, r2=0.085)
    for z, rr in ((1.2, 0.165), (2.6, 0.14), (4.2, 0.12), (5.6, 0.1)):
        cyl(P, rr + 0.015, 0.06, (0, 0, z), M["met"], v=10)
    # registro con puerta
    box(P, (0.22, 0.03, 0.55), (0, -0.165, 1.0), M["met"], 0.01)
    box(P, (0.16, 0.01, 0.44), (0, -0.185, 1.0), M["conD"], 0.005)
    rivets(P, [(sx * 0.085, -0.19, 1.0 + sz * 0.21) for sx in (-1, 1) for sz in (-1, 1)], M["met"], 0.012, (math.radians(90), 0, 0))
    box(P, (0.025, 0.02, 0.1), (0.07, -0.2, 1.0), M["nar"], 0.004)
    # grapas de escalera
    for i in range(9):
        z = 1.7 + i * 0.38
        side = 0.5 if i % 2 else -0.5
        box(P, (0.15, 0.04, 0.025), (0.15 * (1 if i % 2 else -1), 0.0, z), M["met"], 0.004)
    # tira neon en el poste
    box(P, (0.028, 0.02, 1.9), (0, -0.115, 3.3), M["cian"])
    box(P, (0.06, 0.03, 0.06), (0, -0.115, 2.3), M["met"])
    box(P, (0.06, 0.03, 0.06), (0, -0.115, 4.3), M["met"])
    # brazo curvo
    tube(P, [(0, 0, 6.5), (0, -0.05, 7.0), (0, -0.35, 7.35), (0, -0.85, 7.52), (0, -1.4, 7.5)], 0.045, M["met"], 2)
    box(P, (0.14, 0.14, 0.14), (0, 0, 6.6), M["met"], 0.02)
    # luminaria
    box(P, (0.42, 0.95, 0.13), (0, -1.55, 7.52), M["met"], 0.03)
    box(P, (0.34, 0.82, 0.03), (0, -1.55, 7.43), M["nar"])
    box(P, (0.46, 0.16, 0.16), (0, -1.05, 7.56), M["met"], 0.02)
    box(P, (0.46, 0.04, 0.12), (0, -2.04, 7.52), M["met"], 0.01)
    # contrabrazo con camara
    tube(P, [(0, 0, 6.2), (0.4, 0.2, 6.6), (0.7, 0.3, 6.5)], 0.03, M["met"], 1)
    box(P, (0.2, 0.38, 0.17), (0.78, 0.4, 6.45), M["met"], 0.025)
    # travesano con aisladores
    box(P, (1.7, 0.09, 0.09), (0, 0.0, 6.0), M["mad"], 0.01)
    for x in (-0.75, -0.25, 0.25, 0.75):
        cyl(P, 0.03, 0.08, (x, 0, 6.12), M["por"], v=5, r2=0.05)
        cyl(P, 0.05, 0.05, (x, 0, 6.18), M["por"], v=5, r2=0.03)
    tube(P, [(-0.7, 0, 6.0), (-0.3, 0.0, 5.7), (0, 0, 5.85)], 0.02, M["met"], 0)
    tube(P, [(0.7, 0, 6.0), (0.3, 0.0, 5.7), (0, 0, 5.85)], 0.02, M["met"], 0)
    # cables a ambos lados (hacia vecinos)
    for i, x in enumerate((-0.75, -0.25, 0.25, 0.75)):
        d = -1 if x < 0 else 1
        cable(P, (x, 0, 6.2), (d * 4.0, 0.1 * (i - 1.5), 6.4 - 0.05 * i), 0.7 + 0.1 * i, M["gro"], 0.014, 7, 0.0)
    # bajada hacia el registro y cables colgando sueltos
    cable(P, (0.25, 0, 6.2), (0.25, 0.3, 4.6), 0.0, M["gro"], 0.012, 5, 0.08)
    tube(P, [(0.75, 0, 6.2), (1.1, -0.1, 5.6), (1.2, -0.05, 4.8), (1.15, 0.0, 4.2)], 0.012, M["gro"], 0)
    tube(P, [(-0.5, 0, 6.0), (-0.9, 0.1, 5.2), (-0.95, 0.15, 4.5), (-0.8, 0.1, 4.0), (-0.85, 0.1, 3.6)], 0.012, M["gro"], 0)
    box(P, (0.05, 0.05, 0.1), (1.15, 0.0, 4.15), M["met"], 0.01)
    # volantes pegados
    box(P, (0.24, 0.005, 0.32), (-0.04, -0.19, 2.0), M["pap"])
    box(P, (0.2, 0.005, 0.26), (0.06, -0.19, 3.0), M["pap"], rot=(0, 0, 0))
    box(P, (0.1, 0.006, 0.14), (0.02, -0.195, 2.0), M["mag"])
    return [finish(P)], None

# ================================================================ 2) PUESTO DE TACOS
def puesto_tacos(M):
    P = Prop("Puesto_Tacos")
    # carrito
    box(P, (2.8, 1.1, 0.7), (0, 0, 0.7), M["met"], 0.03)
    for x in (-0.9, 0, 0.9):
        box(P, (0.8, 0.02, 0.5), (x, -0.56, 0.7), M["con"], 0.01)
        rivets(P, [(x + dx, -0.575, 0.7 + dz) for dx in (-0.35, 0.35) for dz in (-0.2, 0.2)], M["met"], 0.015, (math.radians(90), 0, 0))
    box(P, (2.7, 0.02, 0.045), (0, -0.57, 0.4), M["cian"])
    box(P, (3.0, 1.3, 0.06), (0, 0, 1.08), M["met"], 0.02)
    # ruedas
    for x in (-1.2, 1.2):
        cyl(P, 0.26, 0.09, (x, 0.0, 0.26), M["gro"], v=10, rot=(0, math.radians(90), 0))
    cyl(P, 0.03, 2.6, (0, 0, 0.26), M["met"], v=4, rot=(0, math.radians(90), 0))
    box(P, (0.1, 0.6, 0.05), (-1.3, -0.6, 0.38), M["met"], 0.01)  # patas
    # comal y trompo
    cyl(P, 0.42, 0.06, (-0.75, 0.05, 1.14), M["met"], v=12)
    cyl(P, 0.44, 0.03, (-0.75, 0.05, 1.12), M["nar"], v=12)
    for dx in (-0.2, 0, 0.2):
        box(P, (0.1, 0.1, 0.04), (-0.75 + dx, 0.05, 1.19), M["car"], 0.01)
    cyl(P, 0.02, 1.1, (0.55, 0.05, 1.6), M["met"], v=4)
    for i, (rr, hh) in enumerate(((0.14, 0.18), (0.2, 0.2), (0.22, 0.2), (0.2, 0.2), (0.15, 0.14))):
        cyl(P, rr, hh, (0.55, 0.05, 1.3 + i * 0.2), M["car"], v=6, r2=rr * 0.9)
    cyl(P, 0.07, 0.1, (0.55, 0.05, 2.32), M["pin"], v=6, r2=0.02)
    cyl(P, 0.2, 0.05, (0.55, 0.05, 1.14), M["met"], v=8)
    # tanque de gas
    cyl(P, 0.17, 0.62, (1.7, -0.2, 0.31), M["gas"], v=10)
    sph(P, 0.17, (1.7, -0.2, 0.62), M["gas"], 10, 3, (1, 1, 0.5))
    cyl(P, 0.05, 0.08, (1.7, -0.2, 0.74), M["met"], v=6)
    # salsas
    for i, mm in enumerate((M["sal"], M["sar"])):
        cyl(P, 0.09, 0.07, (0.0 + i * 0.28, -0.4, 1.14), mm, v=6, r2=0.075)
    box(P, (0.4, 0.25, 0.04), (-0.1, 0.4, 1.14), M["mad"], 0.005)
    # postes y toldo inclinado (frente mas bajo)
    for sx in (-1, 1):
        for sy in (-1, 1):
            h = 1.45 if sy < 0 else 1.7
            cyl(P, 0.035, h, (sx * 1.45, sy * 0.68, 1.1 + h / 2), M["met"], v=6)
    ang = math.atan2(0.25, 1.4)
    for i in range(8):
        x = -1.4 + i * 0.4
        box(P, (0.4, 1.6, 0.035), (x + 0.2 - 0.0, 0.0, 2.65), M["toA"] if i % 2 == 0 else M["toB"], 0.0, rot=(-ang * 0 + math.radians(9), 0, 0))
        prism(P, 0.4, 0.14, 0.03, (x + 0.2, -0.82, 2.58), M["toA"] if i % 2 == 0 else M["toB"], rot=(math.radians(9), 0, 0))
    box(P, (3.0, 0.05, 0.05), (0, 0.78, 2.78), M["met"], 0.01)
    box(P, (3.0, 0.05, 0.05), (0, -0.78, 2.55), M["met"], 0.01)
    # letrero encima
    box(P, (2.2, 0.07, 0.62), (0, 0.55, 3.15), M["met"], 0.02)
    for sx in (-0.95, 0.95):
        box(P, (0.06, 0.06, 0.45), (sx, 0.55, 2.85), M["met"], 0.01)
    text(P, "TACOS", 0.42, (0, 0.5, 3.15), M["mag"], ext=0.04)
    tube(P, [(-1.02, 0.5, 2.88), (1.02, 0.5, 2.88), (1.02, 0.5, 3.42), (-1.02, 0.5, 3.42)], 0.014, M["cian"], 0, True)
    rivets(P, [(sx, 0.51, sz) for sx in (-1.06, 1.06) for sz in (2.82, 3.48)], M["met"], 0.014, (math.radians(90), 0, 0))
    # foquitos colgando
    pts = [(-1.45, -0.68, 2.2), (1.45, -0.68, 2.2)]
    cable(P, pts[0], pts[1], 0.28, M["gro"], 0.008, 5)
    for i, p in enumerate(sag(pts[0], pts[1], 0.28, 5)[1:-1]):
        sph(P, 0.055, (p[0], p[1], p[2] - 0.07), M["nar"], 6, 3)
    # banquitos
    for x in (-0.5, 0.6):
        cyl(P, 0.19, 0.05, (x, -1.2, 0.62), M["toA"], v=8)
        cyl(P, 0.03, 0.6, (x, -1.2, 0.3), M["met"], v=4)
    # menu neon pequeno
    box(P, (0.7, 0.03, 0.36), (-0.9, -0.56, 1.6), M["met"], 0.01) if False else None
    box(P, (0.75, 0.03, 0.4), (-0.95, -0.63, 1.9), M["met"], 0.01, rot=(math.radians(-5), 0, 0))
    box(P, (0.6, 0.012, 0.05), (-0.95, -0.65, 2.0), M["nar"])
    box(P, (0.6, 0.012, 0.05), (-0.95, -0.65, 1.88), M["cian"])
    box(P, (0.6, 0.012, 0.05), (-0.95, -0.65, 1.76), M["tur"])
    return [finish(P)], None

# ================================================================ 3) LETREROS VERTICALES
def _letrero_base(P, M, w, h, frame):
    # caja metal + marco de tubo neon + soportes
    box(P, (w, 0.14, h), (0, 0, h / 2), M["met"], 0.025)
    box(P, (w - 0.1, 0.02, h - 0.1), (0, -0.075, h / 2), M["conD"], 0.01)
    tube(P, [(-w / 2 + 0.07, -0.1, 0.07), (w / 2 - 0.07, -0.1, 0.07), (w / 2 - 0.07, -0.1, h - 0.07), (-w / 2 + 0.07, -0.1, h - 0.07)],
         0.014, frame, 1, True)
    for dz in (0.35 * h, 0.8 * h):
        box(P, (0.1, 0.4, 0.07), (0, 0.27, dz), M["met"], 0.01)
        cyl(P, 0.03, 0.06, (0, 0.46, dz), M["met"], v=5, rot=(math.radians(90), 0, 0))
    rivets(P, [(sx * (w / 2 - 0.04), -0.075, sz) for sx in (-1, 1) for sz in (0.05, h - 0.05)], M["met"], 0.013, (math.radians(90), 0, 0))
    # cable de alimentacion
    tube(P, [(0, 0.3, 0.1), (0, 0.4, 0.0), (0.05, 0.2, 0.0)], 0.012, M["gro"], 0) if False else None
    cable(P, (0.0, 0.34, 0.35 * h), (0.1, 0.4, -0.0), 0.15, M["gro"], 0.012, 5)

def letrero_tacos(M):
    P = Prop("Letrero_TACOS")
    w, h = 0.8, 3.0
    _letrero_base(P, M, w, h, M["cian"])
    text(P, "T\nA\nC\nO\nS", 0.36, (0, -0.12, 1.55), M["mag"], ext=0.035, ls=0.8)
    # taco (media luna) arriba
    tube(P, [(math.cos(a) * 0.22, -0.11, 2.62 + math.sin(a) * 0.22) for a in [math.pi * i / 6 for i in range(7)]], 0.02, M["nar"], 1)
    tube(P, [(-0.22, -0.11, 2.62), (0.22, -0.11, 2.62)], 0.02, M["nar"], 1)
    for a in (0.5, 1.2, 1.9):
        sph(P, 0.04, (math.cos(a) * 0.15, -0.115, 2.7 + math.sin(a) * 0.1 - 0.08), M["tur"], 5, 3)
    # flecha neon abajo
    tube(P, [(0, -0.11, 0.35), (0, -0.11, 0.12)], 0.016, M["cian"], 0)
    tube(P, [(-0.1, -0.11, 0.22), (0, -0.11, 0.1), (0.1, -0.11, 0.22)], 0.016, M["cian"], 0)
    return [finish(P)], None

def letrero_farmacia(M):
    P = Prop("Letrero_FARMACIA")
    w, h = 0.9, 3.4
    _letrero_base(P, M, w, h, M["tur"])
    # cruz
    box(P, (0.14, 0.04, 0.5), (0, -0.115, 3.0), M["tur"], 0.01)
    box(P, (0.5, 0.04, 0.14), (0, -0.115, 3.0), M["tur"], 0.01)
    tube(P, [(-0.3, -0.105, 3.0), (-0.12, -0.105, 3.0), (-0.12, -0.105, 3.3), (0.12, -0.105, 3.3), (0.12, -0.105, 3.0), (0.3, -0.105, 3.0),
             (0.3, -0.105, 2.7), (0.12, -0.105, 2.7), (0.12, -0.105, 2.5), (-0.12, -0.105, 2.5), (-0.12, -0.105, 2.7), (-0.3, -0.105, 2.7)],
         0.011, M["cian"], 0, True)
    text(P, "F\nA\nR\nM\nA\nC\nI\nA", 0.26, (0, -0.12, 1.2), M["cian"], ext=0.03, ls=0.78)
    # pastilla / rombos
    for i, z in enumerate((0.3, 0.55)):
        box(P, (0.1, 0.03, 0.1), (0, -0.115, z), M["mag"], rot=(0, math.radians(45), 0) if False else (0, math.radians(45), 0))
    return [finish(P)], None

def letrero_cantina(M):
    P = Prop("Letrero_CANTINA")
    w, h = 0.85, 3.2
    _letrero_base(P, M, w, h, M["nar"])
    # copa de coctel arriba
    cyl(P, 0.24, 0.3, (0, -0.12, 2.8), M["mag"], v=6, r2=0.03, rot=(math.radians(90), 0, 0)) if False else None
    tube(P, [(-0.24, -0.11, 2.98), (0.24, -0.11, 2.98), (0, -0.11, 2.62), (-0.24, -0.11, 2.98)], 0.02, M["mag"], 1)
    tube(P, [(0, -0.11, 2.62), (0, -0.11, 2.38)], 0.02, M["mag"], 1)
    tube(P, [(-0.15, -0.11, 2.38), (0.15, -0.11, 2.38)], 0.02, M["mag"], 1)
    sph(P, 0.045, (0.12, -0.11, 3.03), M["tur"], 6, 3)
    tube(P, [(0.04, -0.11, 2.9), (0.2, -0.11, 3.15)], 0.012, M["nar"], 0)
    text(P, "C\nA\nN\nT\nI\nN\nA", 0.3, (0, -0.12, 1.28), M["nar"], ext=0.035, ls=0.78)
    # estrellitas
    for z in (0.2, 0.42):
        for sx in (-0.2, 0.2):
            box(P, (0.05, 0.02, 0.05), (sx, -0.115, z), M["cian"], rot=(0, math.radians(45), 0))
    return [finish(P)], None

# ================================================================ 4) AZOTEA: AC + TINACO
def azotea(M):
    P = Prop("Azotea_AC_Tinaco")
    box(P, (3.2, 3.0, 0.18), (0, 0, 0.09), M["con"], 0.015)
    # parapetos
    box(P, (3.2, 0.16, 0.55), (0, 1.42, 0.45), M["con"], 0.02)
    box(P, (0.16, 3.0, 0.55), (-1.52, 0, 0.45), M["con"], 0.02)
    box(P, (0.16, 3.0, 0.55), (1.52, 0, 0.45), M["con"], 0.02)
    box(P, (3.0, 0.07, 0.08), (0, -1.42, 0.22), M["conD"])
    # AC grande
    for sx in (-1, 1):
        box(P, (0.06, 0.55, 0.12), (-0.75 + sx * 0.4, -0.1, 0.24), M["met"], 0.01)
    box(P, (1.0, 0.62, 0.85), (-0.75, -0.1, 0.78), M["met"], 0.03)
    box(P, (1.04, 0.66, 0.06), (-0.75, -0.1, 1.23), M["con"], 0.01)
    # rejilla del ventilador (ring + aspas + barras)
    for rr in (0.17, 0.3):
        tube(P, [(-0.75 + math.cos(a) * rr, -0.435, 0.85 + math.sin(a) * rr) for a in [math.tau * i / 10 for i in range(10)]], 0.008, M["con"], 0, True)
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        tube(P, [(-0.75, -0.435, 0.85), (-0.75 + math.cos(a) * 0.32, -0.435, 0.85 + math.sin(a) * 0.32)], 0.008, M["con"], 0)
    cyl(P, 0.06, 0.05, (-0.75, -0.44, 0.85), M["con"], v=6, rot=(math.radians(90), 0, 0))
    # louvers laterales
    for i in range(7):
        box(P, (0.02, 0.4, 0.025), (-1.265, -0.1, 0.45 + i * 0.07), M["conD"], rot=(0, math.radians(-20), 0))
    # luz de estado + panel
    box(P, (0.08, 0.02, 0.04), (-0.4, -0.425, 0.42), M["tur"])
    box(P, (0.18, 0.02, 0.1), (-1.05, -0.425, 0.42), M["con"], 0.005)
    # tuberias aislantes
    tube(P, [(-0.45, 0.2, 0.5), (-0.1, 0.5, 0.35), (0.25, 1.0, 0.25), (0.25, 1.32, 0.3), (0.2, 1.4, 0.6)], 0.045, M["con"], 1)
    tube(P, [(-0.5, 0.2, 0.62), (-0.2, 0.5, 0.5), (0.35, 1.0, 0.4), (0.35, 1.32, 0.45)], 0.02, M["met"], 0)
    # mini split en el parapeto
    box(P, (0.85, 0.22, 0.3), (0.5, 1.2, 0.95), M["con"], 0.04)
    box(P, (0.75, 0.01, 0.04), (0.5, 1.085, 0.9), M["tur"])
    box(P, (0.7, 0.01, 0.07), (0.5, 1.085, 1.0), M["conD"])
    # tinaco
    box(P, (1.0, 1.0, 0.05), (0.75, -0.7, 0.62), M["met"], 0.01)
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(P, (0.06, 0.06, 0.45), (0.75 + sx * 0.45, -0.7 + sy * 0.45, 0.4), M["met"], 0.005)
        tube(P, [(0.75 + sx * 0.45, -1.15, 0.2), (0.75 + sx * 0.45, -0.25, 0.6)], 0.012, M["met"], 0)
    cyl(P, 0.44, 1.25, (0.75, -0.7, 1.28), M["tin"], v=14)
    for z in (0.85, 1.28, 1.7):
        cyl(P, 0.455, 0.04, (0.75, -0.7, z), M["tin"], v=14, r2=0.455)
    cyl(P, 0.45, 0.16, (0.75, -0.7, 1.99), M["tin"], v=14, r2=0.3)
    cyl(P, 0.2, 0.1, (0.75, -0.7, 2.1), M["tin"], v=10, r2=0.17)
    cyl(P, 0.05, 0.03, (0.75, -0.7, 2.17), M["met"], v=6)
    # tuberia salida
    tube(P, [(0.75, -0.7, 0.6), (0.75, -0.7, 0.35), (0.3, -0.7, 0.3), (-0.0, -0.4, 0.25), (-0.1, 0.4, 0.2)], 0.035, M["met"], 1)
    # indicador de nivel
    box(P, (0.035, 0.012, 1.0), (0.75, -1.14, 1.3), M["tur"])
    box(P, (0.07, 0.015, 0.06), (0.75, -1.14, 0.78), M["met"])
    box(P, (0.07, 0.015, 0.06), (0.75, -1.14, 1.82), M["met"])
    text(P, "AGUA", 0.16, (0.55, -1.15, 1.55), M["cian"], rot=(math.radians(90), 0, math.radians(15)), ext=0.015)
    # escalera
    for sx in (-0.1, 0.1):
        box(P, (0.025, 0.025, 1.7), (1.2 + sx, -0.2, 0.9), M["met"])
    for i in range(7):
        box(P, (0.2, 0.02, 0.02), (1.2, -0.2, 0.25 + i * 0.22), M["met"])
    # antena de tele
    tube(P, [(1.35, 1.2, 0.7), (1.35, 1.2, 2.3)], 0.015, M["met"], 0)
    for i, z in enumerate((1.7, 1.95, 2.2)):
        box(P, (0.5 - i * 0.1, 0.015, 0.015), (1.35, 1.2, z), M["met"])
    box(P, (0.04, 0.04, 0.04), (1.35, 1.2, 2.32), M["roj"])
    # cables del tinaco al AC
    cable(P, (1.35, 1.2, 0.8), (0.9, 0.4, 0.2), 0.1, M["gro"], 0.01, 6)
    cable(P, (-0.9, 0.2, 0.9), (-1.5, 1.0, 0.6), 0.3, M["gro"], 0.01, 6)
    return [finish(P)], None

# ================================================================ 5) TRANSFORMADOR + MARANA
def transformador(M):
    P = Prop("Transformador_Poste")
    box(P, (0.5, 0.5, 0.3), (0, 0, 0.15), M["con"], 0.03)
    cyl(P, 0.22, 7.0, (0, 0, 3.8), M["con"], v=8, r2=0.13)
    for z, rr in ((1.5, 0.2), (3.0, 0.17), (4.5, 0.15)):
        cyl(P, rr + 0.012, 0.05, (0, 0, z), M["met"], v=8)
    # travesano alto con aisladores
    box(P, (2.1, 0.1, 0.1), (0, 0, 6.9), M["mad"], 0.01)
    box(P, (1.4, 0.1, 0.1), (0, 0, 6.35), M["mad"], 0.01)
    for s in (-1, 1):
        tube(P, [(s * 0.1, 0, 6.2), (s * 0.55, 0, 6.8)], 0.025, M["met"], 0)
    for x in (-0.95, -0.55, -0.15, 0.15, 0.55, 0.95):
        cyl(P, 0.03, 0.08, (x, 0, 7.02), M["por"], v=5, r2=0.055)
        cyl(P, 0.055, 0.05, (x, 0, 7.08), M["por"], v=5, r2=0.035)
    for x in (-0.6, 0.6):
        cyl(P, 0.03, 0.08, (x, 0, 6.47), M["por"], v=5, r2=0.055)
    # soporte y transformadores
    box(P, (1.7, 0.1, 0.06), (0, -0.2, 5.45), M["met"], 0.01)
    box(P, (1.9, 0.06, 0.06), (0, -0.2, 4.6), M["met"], 0.01)
    for x in (-0.6, 0, 0.6):
        cyl(P, 0.25, 0.6, (x, -0.38, 5.0), M["met"], v=12)
        for dz in (-0.1, 0.1):
            cyl(P, 0.265, 0.035, (x, -0.38, 5.0 + dz), M["met"], v=12)
        cyl(P, 0.255, 0.1, (x, -0.38, 5.35), M["met"], v=12, r2=0.15)
        cyl(P, 0.04, 0.14, (x, -0.38, 5.46), M["por"], v=5, r2=0.06)
        box(P, (0.14, 0.015, 0.1), (x, -0.64, 5.1), M["nar"])
        rivets(P, [(x + dx, -0.625, 4.82) for dx in (-0.12, 0.12)], M["met"], 0.014, (math.radians(90), 0, 0))
    box(P, (0.36, 0.015, 0.12), (0, -0.64, 4.6), M["mag"])
    # caja de registro y conduit
    box(P, (0.35, 0.18, 0.5), (0, -0.22, 2.2), M["met"], 0.025)
    box(P, (0.26, 0.01, 0.4), (0, -0.315, 2.2), M["conD"])
    tube(P, [(0, -0.2, 2.45), (0, -0.22, 3.0), (0.0, -0.2, 4.4)], 0.035, M["met"], 0)
    tube(P, [(0.1, -0.25, 1.95), (0.1, -0.3, 1.0), (0.3, -0.5, 0.2), (0.6, -0.6, 0.02)], 0.03, M["met"], 0)
    for z in (3.2,):
        cyl(P, 0.05, 0.05, (0, -0.2, z), M["met"], v=6)
    # marana: muchos cables del travesano a todos lados
    ins = [(x, 0, 7.15) for x in (-0.95, -0.55, -0.15, 0.15, 0.55, 0.95)]
    for i, p in enumerate(ins):
        if i in (1, 2, 4):
            continue
        for k in range(2):
            d = -1 if p[0] < 0 else 1
            tgt = (d * R.uniform(3.2, 4.2), R.uniform(-1.5, 1.5), R.uniform(6.2, 7.2))
            cable(P, p, tgt, R.uniform(0.5, 1.2), M["gro"], 0.011, 4, 0.06)
    # bajadas a los transformadores
    for x in (-0.6, 0, 0.6):
        tube(P, [(x, -0.38, 5.58), (x + 0.1, -0.3, 6.0), (x * 0.9, -0.1, 6.5), (x, 0, 7.12)], 0.016, M["gro"], 0)
        tube(P, [(x, -0.38, 5.1), (x * 1.3, -0.5, 4.8), (x * 1.3, -0.3, 4.65)], 0.014, M["gro"], 0)
    # cables que cuelgan sueltos y madeja
    for x, z0 in ((-0.9, 6.9), (0.8, 6.3), (-0.4, 6.3)):
        tube(P, [(x, 0, z0), (x + 0.1, 0.15, z0 - 0.7), (x - 0.2, 0.25, z0 - 1.4), (x + 0.15, 0.2, z0 - 2.0), (x, 0.1, z0 - 2.4)], 0.012, M["gro"], 0)
    helix = []
    for i in range(12):
        a = i * 1.9
        helix.append((0.45 + math.cos(a) * 0.22, 0.3 + math.sin(a) * 0.22, 6.3 - 0.4 - i * 0.055))
    tube(P, [(0.55, 0, 6.35)] + helix + [(0.6, 0.3, 5.3)], 0.014, M["gro"], 0)
    # cables y bolsa atorada en las lineas
    # tenis colgando
    return [finish(P)], None

# ================================================================ 6) BASURERO CON GRAFITI
def basurero(M):
    P = Prop("Basurero_Grafiti")
    # ruedas y chasis
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl(P, 0.11, 0.07, (sx * 0.8, sy * 0.38, 0.11), M["gro"], v=8, rot=(0, math.radians(90), 0))
            box(P, (0.05, 0.1, 0.14), (sx * 0.8, sy * 0.38, 0.2), M["met"], 0.005)
    box(P, (1.84, 0.98, 0.1), (0, 0, 0.24), M["met"], 0.015)
    # cuerpo
    box(P, (1.8, 1.0, 0.8), (0, 0, 0.7), M["bin"], 0.03)
    # costillas
    for i in range(6):
        x = -0.75 + i * 0.3
        box(P, (0.07, 0.035, 0.7), (x, -0.515, 0.7), M["bin"], 0.01)
        box(P, (0.07, 0.035, 0.7), (x, 0.515, 0.7), M["bin"], 0.01)
    # marco superior
    box(P, (1.86, 0.06, 0.07), (0, -0.52, 1.1), M["met"], 0.01)
    box(P, (1.86, 0.06, 0.07), (0, 0.52, 1.1), M["met"], 0.01)
    box(P, (0.06, 1.04, 0.07), (-0.93, 0, 1.1), M["met"], 0.01)
    box(P, (0.06, 1.04, 0.07), (0.93, 0, 1.1), M["met"], 0.01)
    # tapas abiertas con basura
    box(P, (0.9, 1.0, 0.04), (-0.46, 0.55, 1.35), M["bin"], 0.01, rot=(math.radians(-55), 0, 0)) if False else None
    box(P, (0.88, 0.99, 0.05), (-0.45, 0.82, 1.28), M["bin"], 0.01, rot=(math.radians(-70), 0, 0))
    box(P, (0.88, 0.99, 0.05), (0.45, 0.0, 1.2), M["bin"], 0.01, rot=(math.radians(8), 0, 0))
    cyl(P, 0.025, 1.8, (0, 0.52, 1.14), M["met"], v=5, rot=(0, math.radians(90), 0))
    box(P, (0.4, 0.06, 0.05), (0.45, -0.52, 1.22), M["met"], 0.01)
    # bolsas y cajas desbordando
    for p, c, s in (((-0.5, -0.1, 1.15), "bas", 0.3), ((-0.1, 0.1, 1.2), "bas2", 0.26), ((-0.7, 0.2, 1.1), "bas", 0.27), ((-0.25, -0.2, 1.25), "bas", 0.22)):
        sph(P, s, p, M[c], 7, 4, (1.1, 0.9, 0.85))
    box(P, (0.4, 0.3, 0.12), (-0.55, -0.15, 1.38), M["car2"], 0.01, rot=(0.2, 0.3, 0.5))
    cyl(P, 0.04, 0.2, (-0.25, 0.05, 1.45), M["tur"], v=5, rot=(0.9, 0, 0.3))
    box(P, (0.2, 0.2, 0.25), (-0.9, -0.6, 0.35), M["bas"], 0.02) if False else None
    sph(P, 0.15, (-0.95, -0.7, 0.15), M["bas2"], 6, 3, (1, 1, 0.8))
    # bolsa tirada
    box(P, (0.4, 0.2, 0.05), (0.9, -0.8, 0.03), M["car2"], 0.01, rot=(0, 0, 0.4))
    # grafiti frente
    f = -0.54
    box(P, (1.2, 0.01, 0.62), (0, f, 0.72), M["gN"], 0.04)
    text(P, "XOLO", 0.42, (0, f - 0.01, 0.74), M["gN"], ext=0.06)
    text(P, "XOLO", 0.4, (-0.01, f - 0.04, 0.76), M["gR"], ext=0.03)
    # chorreadas
    for x, h in ((-0.42, 0.18), (-0.12, 0.28), (0.2, 0.15), (0.48, 0.24)):
        box(P, (0.03, 0.012, h), (x, f - 0.02, 0.5 - h / 2), M["gR"], 0.005)
    # estrella de spray y corona
    cyl(P, 0.14, 0.012, (0.62, f - 0.01, 0.94), M["gA"], v=8, r2=0.14, rot=(math.radians(90), 0, 0))
    prism(P, 0.26, 0.16, 0.014, (0.62, f - 0.02, 1.0), M["gV"], rot=(math.pi, 0, 0))
    for dx in (-0.1, 0.0, 0.1):
        cyl(P, 0.025, 0.014, (0.62 + dx, f - 0.02, 1.05), M["gV"], v=5, rot=(math.radians(90), 0, 0))
    cyl(P, 0.12, 0.012, (-0.7, f - 0.01, 0.48), M["gZ"], v=7, rot=(math.radians(90), 0, 0))
    text(P, "13", 0.12, (-0.7, f - 0.03, 0.48), M["gA"], ext=0.015)
    # tag neon en un costado + pegatina
    text(P, "CDMX", 0.16, (0.905, -0.1, 0.8), M["cian"], rot=(math.radians(90), 0, math.radians(90)), ext=0.02)
    box(P, (0.01, 0.2, 0.2), (0.905, 0.3, 0.55), M["mag"], rot=(0.0, 0, 0.0))
    box(P, (0.012, 0.12, 0.12), (0.91, 0.3, 0.55), M["nar"], rot=(math.radians(45), 0, 0))
    box(P, (0.25, 0.012, 0.16), (0.45, 0.515, 0.8), M["pap"])
    # tubo escape lateral/asa
    tube(P, [(-0.93, -0.4, 0.6), (-1.05, -0.4, 0.6), (-1.05, 0.4, 0.6), (-0.93, 0.4, 0.6)], 0.022, M["met"], 0)
    return [finish(P)], None

# ================================================================ 7) FACHADA MODULAR 4 m
def fachada(M):
    P = Prop("Fachada_Modular_4m")
    W, D, H = 4.0, 0.4, 6.0
    box(P, (W, D, H), (0, D / 2, H / 2), M["con"], 0.02)
    # zocalo sucio y manchas
    box(P, (W + 0.04, 0.05, 0.55), (0, -0.01, 0.275), M["conD"], 0.01)
    box(P, (0.5, 0.01, 1.4), (1.4, -0.005, 4.4), M["conD"])
    box(P, (0.9, 0.01, 0.7), (-0.9, -0.005, 3.7), M["conD"])
    # pilastras y cornisa
    for sx in (-1, 1):
        box(P, (0.14, 0.07, H - 0.2), (sx * (W / 2 - 0.07), -0.03, H / 2), M["con"], 0.015)
    box(P, (W + 0.12, 0.34, 0.24), (0, -0.0, H - 0.1), M["con"], 0.03)
    box(P, (W, 0.08, 0.12), (0, -0.04, 3.06), M["con"], 0.01)
    # planta baja: hueco oscuro + cortina
    box(P, (2.7, 0.04, 2.5), (-0.45, -0.005, 1.45), M["conD"])
    box(P, (2.9, 0.1, 0.1), (-0.45, -0.05, 2.78), M["met"], 0.015)
    cyl(P, 0.12, 2.7, (-0.45, -0.1, 2.62), M["met"], v=8, rot=(0, math.radians(90), 0))
    for sx in (-1, 1):
        box(P, (0.08, 0.1, 2.5), (-0.45 + sx * 1.38, -0.04, 1.45), M["met"], 0.01)
    for i in range(9):
        box(P, (2.62, 0.045, 0.1), (-0.45, -0.07, 0.36 + i * 0.18), M["met"], 0.008)
    box(P, (2.66, 0.07, 0.1), (-0.45, -0.075, 0.2), M["met"], 0.015)
    box(P, (0.28, 0.06, 0.05), (-0.45, -0.13, 0.28), M["nar"], 0.01)
    rivets(P, [(-0.45 + sx * 1.38, -0.095, z) for sx in (-1, 1) for z in (0.5, 1.1, 1.7, 2.3)], M["met"], 0.018, (math.radians(90), 0, 0))
    # candado
    box(P, (0.08, 0.04, 0.1), (-1.2, -0.12, 0.25), M["met"], 0.01)
    # puerta lateral
    box(P, (0.95, 0.05, 2.25), (1.45, -0.02, 1.2), M["met"], 0.02)
    box(P, (0.68, 0.02, 0.8), (1.45, -0.06, 1.85), M["vid"])
    for dx in (-0.2, 0, 0.2):
        box(P, (0.02, 0.025, 0.8), (1.45 + dx, -0.08, 1.85), M["met"])
    box(P, (0.68, 0.025, 0.02), (1.45, -0.08, 1.85), M["met"])
    box(P, (0.06, 0.06, 0.2), (1.78, -0.08, 1.15), M["con"], 0.01)
    # medidor
    box(P, (0.4, 0.12, 0.5), (-1.9, -0.08, 1.9), M["met"], 0.015) if False else None
    box(P, (0.3, 0.12, 0.4), (-1.82, -0.08, 2.1), M["met"], 0.015)
    cyl(P, 0.09, 0.04, (-1.82, -0.16, 2.1), M["con"], v=8, rot=(math.radians(90), 0, 0))
    box(P, (0.04, 0.01, 0.04), (-1.72, -0.15, 2.28), M["roj"])
    # anuncio
    box(P, (2.9, 0.22, 0.75), (0, -0.18, 3.55), M["met"], 0.03)
    box(P, (2.74, 0.02, 0.6), (0, -0.3, 3.55), M["conD"])
    text(P, "TIENDA", 0.42, (-0.0, -0.33, 3.56), M["nar"], ext=0.04)
    tube(P, [(-1.38, -0.32, 3.28), (1.38, -0.32, 3.28), (1.38, -0.32, 3.82), (-1.38, -0.32, 3.82)], 0.013, M["cian"], 1, True)
    for sx in (-1.3, 1.3):
        box(P, (0.07, 0.3, 0.07), (sx, -0.05, 3.9), M["met"], 0.01, rot=(math.radians(-40), 0, 0)) if False else None
    # letrero vertical lateral pequeno (blade)
    box(P, (0.1, 0.55, 0.06), (1.6, -0.4, 3.9), M["met"], 0.01)
    box(P, (0.08, 0.1, 1.0), (1.6, -0.62, 3.4), M["met"], 0.015)
    box(P, (0.02, 0.02, 0.85), (1.6, -0.69, 3.4), M["mag"])
    # ventanas con reja
    for cx in (-1.0, 1.0):
        zc = 4.7
        for dz in (-0.65, 0.65):
            box(P, (1.05, 0.12, 0.1), (cx, -0.06, zc + dz), M["con"], 0.015)
        for dx in (-0.5, 0.5):
            box(P, (0.1, 0.12, 1.3), (cx + dx, -0.06, zc), M["con"], 0.015)
        box(P, (0.9, 0.02, 1.2), (cx, 0.0, zc), M["vid"])
        box(P, (1.25, 0.2, 0.06), (cx, -0.12, zc - 0.7), M["con"], 0.01)  # repisa
        for i in range(4):
            box(P, (0.025, 0.035, 1.15), (cx - 0.27 + i * 0.18, -0.15, zc), M["met"])
        for dz in (-0.2, 0.2):
            box(P, (0.9, 0.03, 0.025), (cx, -0.15, zc + dz), M["met"])
        # adorno en reja
        box(P, (0.2, 0.03, 0.2), (cx, -0.17, zc), M["met"], rot=(0, math.radians(45), 0))
        rivets(P, [(cx + dx, -0.14, zc + dz) for dx in (-0.44, 0.44) for dz in (-0.55, 0.55)], M["met"], 0.014, (math.radians(90), 0, 0))
    box(P, (0.5, 0.04, 0.08), (-1.0, -0.2, 5.6), M["nar"]) if False else None
    # cortina de la ventana derecha: persiana a medio abrir (cian de dentro)
    box(P, (0.88, 0.03, 0.45), (1.0, -0.03, 4.9), M["conD"])
    # aire acondicionado de ventana
    box(P, (0.65, 0.3, 0.38), (0.0, -0.2, 4.35), M["met"], 0.03)
    box(P, (0.06, 0.012, 0.03), (0.2, -0.355, 4.5), M["tur"])
    # tuberia y conduit
    cyl(P, 0.035, H - 0.7, (1.86, -0.08, (H - 0.7) / 2 + 0.2), M["met"], v=6)
    for z in (1.2, 3.0, 5.2):
        cyl(P, 0.05, 0.05, (1.86, -0.08, z), M["met"], v=6)
    # cables cruzando la fachada
    cable(P, (-2.0, -0.2, 5.6), (1.86, -0.12, 4.9), 0.5, M["gro"], 0.012, 6)
    cable(P, (-2.0, -0.25, 5.8), (2.0, -0.25, 5.65), 0.55, M["gro"], 0.012, 5)
    # volantes y poster
    box(P, (0.4, 0.008, 0.55), (-1.8, -0.01, 1.2), M["pap"], 0.0, rot=(0, 0, 0)) if False else None
    box(P, (0.38, 0.008, 0.52), (0.95, -0.035, 1.8), M["pap"]) if False else None
    box(P, (0.6, 0.012, 0.8), (-1.75, -0.025, 1.0) if False else (-1.78, -0.015, 4.7), M["mag"]) if False else None
    box(P, (0.35, 0.01, 0.5), (-0.3, 0.0, 2.9), M["pap"]) if False else None
    box(P, (0.36, 0.01, 0.5), (-1.83, -0.006, 3.1), M["pap"])
    # luz sobre el anuncio
    return [finish(P)], None

# ================================================================ render / exportar
def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    for coll in (bpy.data.meshes, bpy.data.curves, bpy.data.lights, bpy.data.cameras):
        for d in list(coll):
            coll.remove(d)

def set_engine():
    sc = bpy.context.scene
    ok = False
    if ENGINE.upper().startswith("EEVEE"):
        for e in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
            try:
                sc.render.engine = e
                ok = True
                break
            except TypeError:
                continue
    if not ok:
        sc.render.engine = 'BLENDER_WORKBENCH'
        sc.display.shading.light = 'STUDIO'
        sc.display.shading.color_type = 'MATERIAL'
    print("ENGINE:", sc.render.engine)

def bbox_world(objs):
    mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
    return mn, mx

def render(name, objs, W=640, Hh=640, view=(0.55, -1, 0.35)):
    sc = bpy.context.scene
    set_engine()
    sc.render.resolution_x = W; sc.render.resolution_y = Hh
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    if hasattr(sc, "eevee"):
        try:
            sc.eevee.taa_render_samples = 24
        except Exception:
            pass
    mn, mx = bbox_world(objs)
    c = (mn + mx) / 2
    size = max((mx - mn).x, (mx - mn).z * (W / Hh) , (mx - mn).y)
    # suelo
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size * 3.0; v.co.y *= size * 3.0; v.co.z *= 0.02
    me = bpy.data.meshes.new("g"); bm.to_mesh(me); bm.free()
    g = bpy.data.objects.new("ground", me); _link(g); g.location = (c.x, c.y, mn.z - 0.01)
    gm = mk_mat("suelo", (0.05, 0.055, 0.07), 0.5, 0.3)
    me.materials.append(gm)
    # mundo
    w = bpy.data.worlds.new("w") if not sc.world else sc.world
    sc.world = w
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = (0.03, 0.04, 0.09, 1)
    bg.inputs["Strength"].default_value = 0.6
    sc.view_settings.view_transform = "Standard"
    # camara
    cam = bpy.data.cameras.new("cam"); cam.lens = 50
    co = bpy.data.objects.new("cam", cam); _link(co)
    d = Vector(view).normalized()
    dist = size * 1.5 + 0.8
    co.location = c + d * dist
    tr = co.constraints.new('TRACK_TO'); tr.target = g
    g.location = c  # target at center
    tr.track_axis = 'TRACK_NEGATIVE_Z'; tr.up_axis = 'UP_Y'
    sc.camera = co
    g.location = (c.x, c.y, mn.z - 0.01)
    emp = bpy.data.objects.new("tgt", None); _link(emp); emp.location = c
    tr.target = emp
    # luces
    def light(kind, loc, en, col, size_=3.0):
        L = bpy.data.lights.new("l", kind); L.energy = en; L.color = col
        if kind == 'AREA': L.size = size_
        o = bpy.data.objects.new("l", L); _link(o); o.location = loc
        t = o.constraints.new('TRACK_TO'); t.target = emp
        t.track_axis = 'TRACK_NEGATIVE_Z'; t.up_axis = 'UP_Y'
    light('AREA', c + Vector((-size * 1.2, -size * 1.0, size * 0.8)), 420 * size, (0.3, 0.85, 1.0), size)
    light('AREA', c + Vector((size * 1.3, -size * 0.6, size * 0.6)), 320 * size, (1.0, 0.25, 0.7), size)
    light('AREA', c + Vector((0, -size * 1.6, size * 1.3)), 180 * size, (1.0, 0.95, 0.9), size * 1.5)
    light('AREA', c + Vector((0, size * 1.5, size * 0.8)), 140 * size, (0.5, 0.5, 1.0), size)
    sc.render.filepath = os.path.join(OUT, name + ".png")
    sc.render.image_settings.file_format = 'PNG'
    bpy.ops.render.render(write_still=True)

def export(o, name):
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, name + ".glb"), export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True)

def main():
    clear_scene()
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)
    M = make_mats()
    jobs = [
        ("poste_luz", poste_luz, (640, 800), (0.9, -1, 0.15)),
        ("puesto_tacos", puesto_tacos, (640, 640), (0.55, -1, 0.3)),
        ("letreros", None, (640, 640), (0.2, -1, 0.1)),
        ("azotea_ac_tinaco", azotea, (640, 640), (0.7, -1, 0.5)),
        ("transformador_poste", transformador, (640, 800), (0.5, -1, 0.15)),
        ("basurero_grafiti", basurero, (640, 640), (0.5, -1, 0.35)),
        ("fachada_modular", fachada, (640, 800), (0.3, -1, 0.15)),
    ]
    report = []
    for name, fn, (W, Hh), view in jobs:
        if ONLY and name not in ONLY:
            continue
        clear_scene()
        if fn is None:
            objs = []
            for f in (letrero_tacos, letrero_farmacia, letrero_cantina):
                o = f(M)[0][0]
                objs.append(o)
            for o in objs:
                export(o, o.name.lower())
                report.append((o.name.lower(), tris(o)))
            for o, x in zip(objs, (-1.3, 0, 1.3)):
                o.location.x = x
            bpy.context.view_layer.update()
            render(name, objs, W, Hh, view)
        else:
            o = fn(M)[0][0]
            export(o, name)
            report.append((name, tris(o)))
            render(name, [o], W, Hh, view)
    # guardar blend
    for n, t in report:
        print("TRIS", n, t)
    print("DONE")

main()
