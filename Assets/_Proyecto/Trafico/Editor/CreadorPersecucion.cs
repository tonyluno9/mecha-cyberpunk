using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// Crea los perseguidores de T-341: materiales, prefabs Dron (cuerpo, rotores y foco hacia abajo) y Patrulla (carro con barra de luces roja y azul, sin colliders),
    /// ConfigPersecucion_Trafico y, en <see cref="ColgarEnBarrio"/>, el GestorPersecucion que cuelga de Barrio.prefab.
    /// <see cref="CrearAssets"/> no toca el prefab del barrio. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPersecucion.CrearAssets (o .Crear para todo).
    /// </summary>
    public static class CreadorPersecucion
    {
        public const string RutaPrefabDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        public const string RutaPrefabPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Trafico/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string NombreGestor = "Persecucion";

        [MenuItem("Mecha Cyberpunk/Crear T-341")]
        public static void Crear()
        {
            CrearAssets();
            ColgarEnBarrio();
        }

        public static void CrearAssets()
        {
            CrearCarpeta("Materiales");
            CrearCarpeta("Prefabs");
            Material casco = Mat("Casco", new Color(0.1f, 0.11f, 0.13f), 0.5f, default);
            Material luzFoco = Mat("Foco", new Color(0.9f, 0.95f, 1f), 0.2f, new Color(3f, 3.4f, 3.6f));
            Material carroceria = Mat("Carroceria", new Color(0.9f, 0.9f, 0.92f), 0.4f, default);
            Material vidrio = Mat("Vidrio", new Color(0.05f, 0.08f, 0.12f), 0.8f, default);
            Material rojo = Mat("SirenaRoja", new Color(0.9f, 0.05f, 0.05f), 0.2f, new Color(4f, 0.1f, 0.1f));
            Material azul = Mat("SirenaAzul", new Color(0.05f, 0.1f, 0.95f), 0.2f, new Color(0.1f, 0.4f, 4f));

            GameObject dron = new GameObject("Dron");
            Cubo(dron.transform, "Cuerpo", new Vector3(0f, 0f, 0f), new Vector3(0.8f, 0.25f, 0.8f), casco);
            foreach (Vector3 esquina in new[] { new Vector3(0.6f, 0.05f, 0.6f), new Vector3(-0.6f, 0.05f, 0.6f), new Vector3(0.6f, 0.05f, -0.6f), new Vector3(-0.6f, 0.05f, -0.6f) })
            {
                Cubo(dron.transform, "Rotor", esquina, new Vector3(0.5f, 0.04f, 0.5f), casco);
            }
            Cubo(dron.transform, "Lente", new Vector3(0f, -0.17f, 0f), new Vector3(0.25f, 0.1f, 0.25f), luzFoco);
            GameObject foco = new GameObject("Foco");
            foco.transform.SetParent(dron.transform, false);
            foco.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            foco.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light luz = foco.AddComponent<Light>();
            luz.type = LightType.Spot;
            luz.spotAngle = 50f;
            luz.range = 20f;
            luz.intensity = 40f;
            luz.color = new Color(0.9f, 0.95f, 1f);
            luz.shadows = LightShadows.None;
            dron.AddComponent<DronMundo>();
            PrefabUtility.SaveAsPrefabAsset(dron, RutaPrefabDron);
            Object.DestroyImmediate(dron);

            GameObject patrulla = new GameObject("Patrulla");
            Cubo(patrulla.transform, "Carroceria", new Vector3(0f, 0.6f, 0f), new Vector3(1.9f, 0.7f, 4.4f), carroceria);
            Cubo(patrulla.transform, "Cabina", new Vector3(0f, 1.2f, -0.2f), new Vector3(1.6f, 0.55f, 2.2f), vidrio);
            Cubo(patrulla.transform, "BarraRoja", new Vector3(-0.35f, 1.55f, -0.2f), new Vector3(0.6f, 0.15f, 0.3f), rojo);
            Cubo(patrulla.transform, "BarraAzul", new Vector3(0.35f, 1.55f, -0.2f), new Vector3(0.6f, 0.15f, 0.3f), azul);
            Light luzRoja = PuntoDeLuz(patrulla.transform, "LuzRoja", new Vector3(-0.35f, 1.9f, -0.2f), new Color(1f, 0.1f, 0.1f));
            Light luzAzul = PuntoDeLuz(patrulla.transform, "LuzAzul", new Vector3(0.35f, 1.9f, -0.2f), new Color(0.1f, 0.2f, 1f));
            PatrullaMundo componente = patrulla.AddComponent<PatrullaMundo>();
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty("luzRoja").objectReferenceValue = luzRoja;
            so.FindProperty("luzAzul").objectReferenceValue = luzAzul;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(patrulla, RutaPrefabPatrulla);
            Object.DestroyImmediate(patrulla);

            if (AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigPersecucion>(), RutaConfig);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-341: prefabs Dron y Patrulla y ConfigPersecucion_Trafico listos.");
        }

        /// <summary>Cuelga de Barrio.prefab el GestorPersecucion. Reescribe el prefab del barrio: correrlo solo con el pipeline completo.</summary>
        public static void ColgarEnBarrio()
        {
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            DronMundo dron = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabDron).GetComponent<DronMundo>();
            PatrullaMundo patrulla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPatrulla).GetComponent<PatrullaMundo>();
            GameObject barrio = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform viejo = barrio.transform.Find(NombreGestor);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject gestor = new GameObject(NombreGestor);
            gestor.transform.SetParent(barrio.transform, false);
            gestor.AddComponent<GestorPersecucion>().Configurar(config, dron, patrulla);
            PrefabUtility.SaveAsPrefabAsset(barrio, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(barrio);
            Debug.Log("[Trafico] T-341: GestorPersecucion colgado de Barrio.");
        }

        private static void CrearCarpeta(string nombre)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Proyecto/Trafico/" + nombre))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Trafico", nombre);
            }
        }

        private static Light PuntoDeLuz(Transform padre, string nombre, Vector3 posicion, Color color)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            Light luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.intensity = 12f;
            luz.range = 10f;
            luz.shadows = LightShadows.None;
            return luz;
        }

        private static GameObject Cubo(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = RutaMateriales + "/Persecucion_" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
