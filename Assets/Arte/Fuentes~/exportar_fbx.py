"""Convierte los .glb de kit-urbano/ y bar/ a FBX para Unity (T-076).
Uso: blender -b -P exportar_fbx.py
Salida (desde la raiz del repo):
  Assets/Arte/Mundo/KitUrbano/<nombre>.fbx, Assets/Arte/Mundo/Bar/<nombre>.fbx
  Assets/Arte/Materiales/materiales.json   (color base, rugosidad, metalico, emision)
  Assets/Arte/Mundo/dimensiones.json       (ancho, alto, fondo en metros, ejes de Unity)
Reproducible: correrlo dos veces da los mismos archivos."""
import bpy, os, re, json, glob, math
from mathutils import Matrix

AQUI = os.path.dirname(os.path.abspath(__file__))
ARTE = os.path.abspath(os.path.join(AQUI, ".."))
GRUPOS = [("kit-urbano", "KitUrbano"), ("bar", "Bar"), ("letreros", "Letreros")]  # el kit urbano va primero: gana en materiales repetidos
NOTAS = []


def limpiar_escena():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def valor(b, nombre, defecto):
    s = b.inputs.get(nombre)
    return s.default_value if s is not None else defecto


def leer_material(m):
    b = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None) if m.use_nodes else None
    if b is None:
        return None
    base = [round(float(c), 4) for c in valor(b, "Base Color", (0.8, 0.8, 0.8, 1))]
    em = [round(float(c), 4) for c in valor(b, "Emission Color", (0, 0, 0, 1))][:3]
    fuerza = round(float(valor(b, "Emission Strength", 0.0)), 4)
    if fuerza <= 0 or max(em) <= 0:
        em, fuerza = [0.0, 0.0, 0.0], 0.0
    return {
        "color_base": base[:3],
        "alfa": round(float(valor(b, "Alpha", 1.0)), 4),
        "rugosidad": round(float(valor(b, "Roughness", 0.5)), 4),
        "metalico": round(float(valor(b, "Metallic", 0.0)), 4),
        "emision_color": em,
        "emision_fuerza": fuerza,
    }


def main():
    materiales, dimensiones = {}, {}
    for carpeta, destino in GRUPOS:
        os.makedirs(os.path.join(ARTE, "Mundo", destino), exist_ok=True)
        for glb in sorted(glob.glob(os.path.join(AQUI, carpeta, "*.glb"))):
            nombre = os.path.splitext(os.path.basename(glb))[0]
            limpiar_escena()
            bpy.ops.import_scene.gltf(filepath=glb)
            mallas = [o for o in bpy.context.scene.objects if o.type == "MESH"]
            for o in list(bpy.context.scene.objects):
                if o.type != "MESH":
                    bpy.data.objects.remove(o)
            # Unir en una sola malla con transformaciones aplicadas
            bpy.ops.object.select_all(action="DESELECT")
            for o in mallas:
                o.select_set(True)
            bpy.context.view_layer.objects.active = mallas[0]
            for o in mallas:
                o.parent = None
            bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
            if len(mallas) > 1:
                bpy.ops.object.join()
            o = bpy.context.view_layer.objects.active
            o.name = nombre
            # El frente en Blender es -Y; Unity lo importa como -Z. Se gira 180 grados sobre Z para que el frente quede en +Z.
            o.data.transform(Matrix.Rotation(math.pi, 4, "Z"))
            o.data.name = nombre
            # Materiales: nombres sin sufijo .001
            for slot in o.material_slots:
                m = slot.material
                if m is None:
                    continue
                limpio = re.sub(r"\.\d{3}$", "", m.name)
                info = leer_material(m)
                if info is None:
                    continue
                if limpio in materiales and materiales[limpio] != info:
                    NOTAS.append("Material %s distinto en %s: gana el primero (kit urbano)" % (limpio, nombre))
                    info = materiales[limpio]
                materiales.setdefault(limpio, info)
                m.name = limpio + "__" + nombre  # evita choques al renombrar
                m.name = limpio
            bpy.ops.object.select_all(action="DESELECT")
            o.select_set(True)
            bpy.context.view_layer.objects.active = o
            # Dimensiones en ejes de Unity: x ancho, z (Blender y) fondo, y (Blender z) alto
            esq = [o.matrix_world @ v.co for v in o.data.vertices]
            mn = [min(v[i] for v in esq) for i in range(3)]
            mx = [max(v[i] for v in esq) for i in range(3)]
            dimensiones[nombre] = {
                "grupo": destino,
                "ancho": round(mx[0] - mn[0], 4),
                "alto": round(mx[2] - mn[2], 4),
                "fondo": round(mx[1] - mn[1], 4),
            }
            salida = os.path.join(ARTE, "Mundo", destino, nombre + ".fbx")
            bpy.ops.export_scene.fbx(
                filepath=salida, use_selection=True, object_types={"MESH"},
                global_scale=1.0, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                bake_space_transform=True, axis_forward="-Z", axis_up="Y",
                use_mesh_modifiers=True, mesh_smooth_type="FACE", add_leaf_bones=False,
                bake_anim=False, path_mode="AUTO", embed_textures=False)
            print("FBX:", salida)
    os.makedirs(os.path.join(ARTE, "Materiales"), exist_ok=True)
    with open(os.path.join(ARTE, "Materiales", "materiales.json"), "w", newline="\n") as f:
        json.dump({"materiales": [dict(nombre=k, **v) for k, v in sorted(materiales.items())], "notas": NOTAS}, f, indent=2, sort_keys=True)
        f.write("\n")
    with open(os.path.join(ARTE, "Mundo", "dimensiones.json"), "w", newline="\n") as f:
        json.dump({"piezas": [dict(nombre=k, **v) for k, v in sorted(dimensiones.items())]}, f, indent=2, sort_keys=True)
        f.write("\n")
    print("Piezas:", len(dimensiones), "Materiales:", len(materiales), "Notas:", NOTAS)


main()
