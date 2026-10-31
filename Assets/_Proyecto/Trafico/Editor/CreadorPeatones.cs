using System.Collections.Generic;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// Crea los peatones de T-331: materiales, el prefab Peaton (cuerpo de cubos, chamarra con neón, sin colliders), ConfigPeatones_Trafico y, en
    /// <see cref="ColgarEnBarrio"/>, el grupo que cuelga de Barrio.prefab con una ruta por manzana.
    /// <see cref="CrearAssets"/> no toca el prefab del barrio. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPeatones.CrearAssets (o .Crear para todo).
    /// </summary>
    public static class CreadorPeatones
    {
        public const string RutaPrefabPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Trafico/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string NombreGrupo = "Peatones";
        private const string PrefijoManzana = "Manzana_";

        // Medidas del barrio de CreadorBarrio (m): manzana de 80 con banqueta de 3 m y 0.2 m de alto.
        private const float MitadManzana = 40f;
        private const float AltoBanqueta = 0.2f;

        private static readonly Color[] ColoresChamarra =
        {
            new Color(0.15f, 0.15f, 0.2f), new Color(0.35f, 0.12f, 0.12f), new Color(0.12f, 0.25f, 0.3f)
        };

        [MenuItem("Mecha Cyberpunk/Crear T-331")]
        public static void Crear()
        {
            CrearAssets();
            ColgarEnBarrio();
        }

        public static void CrearAssets()
        {
            CrearCarpeta("Materiales");
            CrearCarpeta("Prefabs");
            Material piel = Mat("Piel", new Color(0.55f, 0.4f, 0.3f), 0.1f, default);
            Material pantalon = Mat("Pantalon", new Color(0.12f, 0.13f, 0.18f), 0.1f, default);
            Material chamarra = Mat("Chamarra", ColoresChamarra[0], 0.2f, default);
            Material neon = Mat("Neon", new Color(0.1f, 0.8f, 0.9f), 0.2f, new Color(0.3f, 2.8f, 3.2f));

            GameObject raiz = new GameObject("Peaton");
            Cubo(raiz.transform, "PiernaIzquierda", new Vector3(-0.12f, 0.4f, 0f), new Vector3(0.2f, 0.8f, 0.22f), pantalon);
            Cubo(raiz.transform, "PiernaDerecha", new Vector3(0.12f, 0.4f, 0f), new Vector3(0.2f, 0.8f, 0.22f), pantalon);
            Cubo(raiz.transform, "Torso", new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.6f, 0.28f), chamarra);
            Cubo(raiz.transform, "Cabeza", new Vector3(0f, 1.57f, 0f), new Vector3(0.25f, 0.28f, 0.25f), piel);
            Cubo(raiz.transform, "NeonEspalda", new Vector3(0f, 1.15f, -0.15f), new Vector3(0.36f, 0.06f, 0.02f), neon);
            Cubo(raiz.transform, "NeonHombro", new Vector3(0.26f, 1.3f, 0f), new Vector3(0.04f, 0.3f, 0.2f), neon);
            raiz.AddComponent<PeatonMundo>();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPeaton);
            Object.DestroyImmediate(raiz);

            if (AssetDatabase.LoadAssetAtPath<ConfigPeatones>(RutaConfig) == null)
            {
                // T-447: más gente de día (14) que de noche (6).
                ConfigPeatones nuevo = ScriptableObject.CreateInstance<ConfigPeatones>();
                SerializedObject soConfig = new SerializedObject(nuevo);
                soConfig.FindProperty("peatonesDeDia").intValue = PeatonesDeDia;
                soConfig.FindProperty("peatonesDeNoche").intValue = PeatonesDeNoche;
                soConfig.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(nuevo, RutaConfig);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-331: prefab Peaton y ConfigPeatones_Trafico listos.");
        }

        public const int PeatonesDeDia = 14;
        public const int PeatonesDeNoche = 6;

        /// <summary>
        /// T-447: día y noche conectados: de día hay más peatones en las banquetas y de noche menos (más pandilleros). Cambia solo esos dos números
        /// del asset existente. Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPeatones.AjustarDiaNoche
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-447 Peatones de día y de noche")]
        public static void AjustarDiaNoche()
        {
            ConfigPeatones config = AssetDatabase.LoadAssetAtPath<ConfigPeatones>(RutaConfig);
            if (config == null)
            {
                Debug.LogError("[Trafico] T-447: no existe " + RutaConfig + "; corre antes la creación de peatones.");
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("peatonesDeDia").intValue = PeatonesDeDia;
            so.FindProperty("peatonesDeNoche").intValue = PeatonesDeNoche;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-447: peatones " + PeatonesDeDia + " de día y " + PeatonesDeNoche + " de noche.");
        }

        /// <summary>Cuelga de Barrio.prefab el grupo Peatones con una ruta por manzana. Reescribe el prefab del barrio: correrlo solo con el pipeline completo.</summary>
        public static void ColgarEnBarrio()
        {
            ConfigPeatones config = AssetDatabase.LoadAssetAtPath<ConfigPeatones>(RutaConfig);
            PeatonMundo prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPeaton).GetComponent<PeatonMundo>();
            GameObject barrio = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform viejo = barrio.transform.Find(NombreGrupo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            List<Vector3> centros = new List<Vector3>();
            foreach (Transform hijo in barrio.transform)
            {
                if (hijo.name.StartsWith(PrefijoManzana))
                {
                    centros.Add(hijo.localPosition);
                }
            }
            List<Vector3[]> rutas = RutasBanqueta.DeManzanas(centros, MitadManzana - config.MargenBanqueta, AltoBanqueta, config.RecorteCierres);
            GameObject grupo = new GameObject(NombreGrupo);
            grupo.transform.SetParent(barrio.transform, false);
            grupo.AddComponent<GrupoPeatones>().Configurar(config, prefab, rutas);
            PrefabUtility.SaveAsPrefabAsset(barrio, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(barrio);
            Debug.Log("[Trafico] T-331: grupo de peatones colgado de Barrio con " + rutas.Count + " rutas.");
        }

        private static void CrearCarpeta(string nombre)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Proyecto/Trafico/" + nombre))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Trafico", nombre);
            }
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
            string ruta = RutaMateriales + "/Peaton_" + nombre + ".mat";
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
