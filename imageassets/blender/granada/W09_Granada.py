"""T-1014: granada de mano low-poly (piña) para W09_Granada.fbx. Medidas en metros: ~0.10 m de alto, ~0.065 m de ancho.
Cuerpo (verde oliva, con relieve de piña), cuello, anillo y palanca (metal) en dos objetos: Cuerpo y Metal.
Uso: blender -b --python W09_Granada.py -- <ruta_salida.fbx>
Origen del modelo: el centro del cuerpo (donde queda la palma); arriba = +Z de Blender (= +Y en Unity)."""
import bpy, bmesh, math, sys
from mathutils import Vector

salida = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)

RADIO_XY = 0.032
RADIO_Z = 0.04
SEGMENTOS = 10
ANILLOS = 6
RELIEVE = 0.1


def material(nombre, color, metal, rugoso):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = color
    b.inputs["Metallic"].default_value = metal
    b.inputs["Roughness"].default_value = rugoso
    return m


def objeto(nombre, bm, mat):
    malla = bpy.data.meshes.new(nombre)
    bm.to_mesh(malla)
    bm.free()
    o = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(o)
    o.data.materials.append(mat)
    for p in o.data.polygons:
        p.use_smooth = False
    return o


# Cuerpo: elipsoide con relieve alternado (casillas de la piña) y el cuello arriba.
bm = bmesh.new()
bmesh.ops.create_uvsphere(bm, u_segments=SEGMENTOS, v_segments=ANILLOS, radius=1.0)
for v in bm.verts:
    u = math.atan2(v.co.y, v.co.x)
    columna = int(round((u / (2 * math.pi)) * SEGMENTOS)) % SEGMENTOS
    fila = int(round((math.acos(max(-1.0, min(1.0, v.co.z))) / math.pi) * ANILLOS))
    sube = 1.0 + (RELIEVE if (columna + fila) % 2 == 0 else 0.0)
    v.co = Vector((v.co.x * RADIO_XY * sube, v.co.y * RADIO_XY * sube, v.co.z * RADIO_Z * sube))
res = bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.011, radius2=0.011, depth=0.014,
                            matrix=__import__("mathutils").Matrix.Translation((0, 0, RADIO_Z + 0.004)))
cuerpo = objeto("Cuerpo", bm, material("W09_Cuerpo", (0.22, 0.27, 0.12, 1), 0.35, 0.55))

# Metal: anillo (toro) a un lado del cuello y palanca curva sobre el cuerpo.
bm = bmesh.new()
z_cuello = RADIO_Z + 0.004
bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.013, radius2=0.013, depth=0.006,
                      matrix=__import__("mathutils").Matrix.Translation((0, 0, z_cuello + 0.009)))
# Anillo del seguro: toro delgado en el plano XZ, al lado del cuello.
n = 12
r_mayor, r_menor = 0.014, 0.0024
anillo = []
for i in range(n):
    a = 2 * math.pi * i / n
    centro = Vector((0.020 + math.cos(a) * r_mayor, 0.0, z_cuello + 0.003 + math.sin(a) * r_mayor))
    ring = []
    for j in range(4):
        b = 2 * math.pi * j / 4
        dirn = Vector((math.cos(a), 0, math.sin(a)))
        ring.append(bm.verts.new(centro + dirn * (math.cos(b) * r_menor) + Vector((0, 1, 0)) * (math.sin(b) * r_menor)))
    anillo.append(ring)
for i in range(n):
    for j in range(4):
        bm.faces.new((anillo[i][j], anillo[i][(j + 1) % 4], anillo[(i + 1) % n][(j + 1) % 4], anillo[(i + 1) % n][j]))
# Palanca: tira curva pegada al cuerpo por el lado +X, del cuello hacia abajo.
pasos = 6
ancho = 0.010
grosor = 0.003
arriba, abajo = [], []
for i in range(pasos + 1):
    t = i / pasos
    ang = math.radians(8 + t * 62)
    r = 1.0 + 0.04
    x = math.sin(ang) * RADIO_XY * 1.12
    z = math.cos(ang) * RADIO_Z * 1.12 + 0.002
    fila = []
    for dy in (-ancho / 2, ancho / 2):
        fila.append((bm.verts.new((x, dy, z)), bm.verts.new((x + grosor * math.cos(ang), dy, z - grosor * math.sin(ang)))))
    arriba.append(fila)
for i in range(pasos):
    a, b = arriba[i], arriba[i + 1]
    # capas exterior e interior de la tira y sus bordes
    bm.faces.new((a[0][0], a[1][0], b[1][0], b[0][0]))
    bm.faces.new((a[0][1], b[0][1], b[1][1], a[1][1]))
    bm.faces.new((a[0][0], b[0][0], b[0][1], a[0][1]))
    bm.faces.new((a[1][0], a[1][1], b[1][1], b[1][0]))
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
metal = objeto("Metal", bm, material("W09_Metal", (0.55, 0.55, 0.5, 1), 0.9, 0.35))

for o in (cuerpo, metal):
    o.select_set(True)
bpy.ops.export_scene.fbx(filepath=salida, use_selection=True, apply_unit_scale=True, global_scale=1.0,
                         object_types={'MESH'}, mesh_smooth_type='FACE', add_leaf_bones=False)
alto = max(v.co.z for o in (cuerpo, metal) for v in o.data.vertices) - min(v.co.z for o in (cuerpo, metal) for v in o.data.vertices)
print("GRANADA alto", round(alto, 4), "verts", sum(len(o.data.vertices) for o in (cuerpo, metal)))
