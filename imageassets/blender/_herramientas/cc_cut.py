
import bpy, bmesh, math
from mathutils import Vector, Matrix

def corte_mat():
    cm = bpy.data.materials.get('Corte_Interior')
    if cm is None:
        cm = bpy.data.materials.new('Corte_Interior'); cm.use_nodes = True
        b = cm.node_tree.nodes.get('Principled BSDF')
        if b: b.inputs['Base Color'].default_value = (0.05, 0.05, 0.06, 1)
    return cm

INF = 1e9
def cut_box(src_obj, name, box):
    """box: dict xmin,xmax,zmin,zmax (y libre). Separa caras con centro dentro; tapa cortes en ambos lados."""
    me = src_obj.data
    cm = corte_mat()
    if 'Corte_Interior' not in [m.name for m in me.materials if m]: me.materials.append(cm)
    ci = [m.name if m else '' for m in me.materials].index('Corte_Interior')
    bm = bmesh.new(); bm.from_mesh(me)
    planes = []
    for k, ax, nrm in (('xmin', 0, (1, 0, 0)), ('xmax', 0, (1, 0, 0)), ('zmin', 2, (0, 0, 1)), ('zmax', 2, (0, 0, 1))):
        v = box.get(k)
        if v is None: continue
        co = Vector((0, 0, 0)); co[ax] = v
        bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=co, plane_no=Vector(nrm))
        planes.append((ax, v))
    def inside(c):
        return (box.get('xmin', -INF) < c.x < box.get('xmax', INF)) and (box.get('zmin', -INF) < c.z < box.get('zmax', INF))
    sel = [f for f in bm.faces if inside(f.calc_center_median())]
    onp = lambda v: any(abs(v.co[ax] - val) < 1e-5 for ax, val in planes)
    # piece mesh
    nb = bmesh.new(); vm = {}
    uvl = bm.loops.layers.uv.active; nuv = nb.loops.layers.uv.new(uvl.name) if uvl else None
    for f in sel:
        vs = []
        for v in f.verts:
            if v not in vm: vm[v] = nb.verts.new(v.co)
            vs.append(vm[v])
        try:
            nf = nb.faces.new(vs)
        except ValueError:
            continue
        nf.material_index = f.material_index; nf.smooth = f.smooth
        if uvl:
            for l, nl in zip(f.loops, nf.loops): nl[nuv].uv = l[uvl].uv
    bmesh.ops.delete(bm, geom=sel, context='FACES')
    def cap(b):
        b.edges.ensure_lookup_table()
        bnd = [e for e in b.edges if e.is_boundary and all(onp(v) for v in e.verts)]
        r = bmesh.ops.holes_fill(b, edges=bnd, sides=0)
        for f in r['faces']: f.material_index = ci
        return len(r['faces'])
    c1 = cap(bm); c2 = cap(nb)
    for b in (bm, nb):
        loose = [v for v in b.verts if not v.link_faces]; bmesh.ops.delete(b, geom=loose, context='VERTS')
    bm.to_mesh(me); bm.free(); me.update()
    pm = bpy.data.meshes.new(name); nb.to_mesh(pm); nb.free()
    for m in me.materials: pm.materials.append(m)
    po = bpy.data.objects.new(name, pm)
    for c in src_obj.users_collection: c.objects.link(po)
    po.matrix_world = src_obj.matrix_world.copy()
    return po, len(sel), (c1, c2)
