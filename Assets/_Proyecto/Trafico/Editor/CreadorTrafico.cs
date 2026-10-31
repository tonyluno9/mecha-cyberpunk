using System.IO;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-451: crea el prefab Trafico/Prefabs/CarroTrafico.prefab (un carro civil de cubos: carrocería, cabina, vidrios, ruedas y faros; cuerpo
    /// cinemático con un collider) y Trafico/Datos/ConfigTraficoCalle_Trafico.asset. Idempotente: los assets que ya existen se editan, no se borran.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorTrafico.Crear
    /// </summary>
    public static class CreadorTrafico
    {
        public const string RutaPrefab = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico.prefab";
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private const string CarpetaMateriales = "Assets/_Proyecto/Trafico/Materiales";

        [MenuItem("Mecha Cyberpunk/Crear T-451 tráfico de calle")]
        public static void Crear()
        {
            Directory.CreateDirectory(CarpetaMateriales);
            if (AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigTraficoCalle>(), RutaConfig);
            }
            Material carroceria = Mat("CarroTrafico_Carroceria", new Color(0.75f, 0.1f, 0.1f), 0.5f, default);
            Material vidrio = Mat("CarroTrafico_Vidrio", new Color(0.05f, 0.08f, 0.12f), 0.8f, default);
            Material llanta = Mat("CarroTrafico_Llanta", new Color(0.04f, 0.04f, 0.05f), 0.2f, default);
            Material faro = Mat("CarroTrafico_Faro", new Color(0.95f, 0.95f, 0.8f), 0.2f, new Color(2.5f, 2.5f, 2f));
            Material calavera = Mat("CarroTrafico_Calavera", new Color(0.8f, 0.05f, 0.05f), 0.2f, new Color(2f, 0.1f, 0.1f));

            GameObject raiz = new GameObject("CarroTrafico");
            try
            {
                Cubo(raiz.transform, "Carroceria", new Vector3(0f, -0.1f, 0f), new Vector3(1.9f, 0.55f, 4.4f), carroceria);
                Cubo(raiz.transform, "Cabina", new Vector3(0f, 0.38f, -0.2f), new Vector3(1.6f, 0.5f, 2.2f), carroceria);
                Cubo(raiz.transform, "Parabrisas", new Vector3(0f, 0.4f, 0.55f), new Vector3(1.52f, 0.42f, 0.08f), vidrio);
                Cubo(raiz.transform, "Medallon", new Vector3(0f, 0.4f, -0.95f), new Vector3(1.52f, 0.42f, 0.08f), vidrio);
                Cubo(raiz.transform, "FaroIzq", new Vector3(-0.6f, -0.05f, 2.2f), new Vector3(0.4f, 0.18f, 0.06f), faro);
                Cubo(raiz.transform, "FaroDer", new Vector3(0.6f, -0.05f, 2.2f), new Vector3(0.4f, 0.18f, 0.06f), faro);
                Cubo(raiz.transform, "CalaveraIzq", new Vector3(-0.6f, -0.05f, -2.2f), new Vector3(0.4f, 0.16f, 0.06f), calavera);
                Cubo(raiz.transform, "CalaveraDer", new Vector3(0.6f, -0.05f, -2.2f), new Vector3(0.4f, 0.16f, 0.06f), calavera);
                float[] xs = { -0.95f, 0.95f };
                float[] zs = { 1.4f, -1.4f };
                foreach (float x in xs)
                {
                    foreach (float z in zs)
                    {
                        GameObject rueda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        rueda.name = "Rueda";
                        Object.DestroyImmediate(rueda.GetComponent<Collider>());
                        rueda.transform.SetParent(raiz.transform, false);
                        rueda.transform.localPosition = new Vector3(x, -0.1f, z);
                        rueda.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        rueda.transform.localScale = new Vector3(0.7f, 0.12f, 0.7f);
                        rueda.GetComponent<Renderer>().sharedMaterial = llanta;
                    }
                }
                BoxCollider caja = raiz.AddComponent<BoxCollider>();
                caja.center = new Vector3(0f, 0.1f, 0f);
                caja.size = new Vector3(1.9f, 1.1f, 4.4f);
                Rigidbody cuerpo = raiz.AddComponent<Rigidbody>();
                cuerpo.isKinematic = true;
                raiz.AddComponent<CarroTrafico>();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-451: CarroTrafico.prefab y ConfigTraficoCalle_Trafico.asset listos.");
        }

        private static void Cubo(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(padre, false);
            g.transform.localPosition = posicion;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
