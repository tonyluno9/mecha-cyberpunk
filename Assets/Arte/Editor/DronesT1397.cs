using System.IO;
using System.Linq;
using MechaCyberpunk.Trafico.Drones;
using MechaCyberpunk.Trafico.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1397: drones y naves de Blender (imageassets/blender/drones/*.fbx de T-1380).
    /// Aplicar: copia cada FBX a Assets/Arte/Vehiculos/Drones/&lt;código&gt;/, deja sus materiales URP/Lit, crea un prefab por modelo (con <see cref="GiroRotores"/>)
    /// y cambia el visual del dron de la policía (Trafico/Prefabs/Dron.prefab) por DR01, conservando DronMundo, colisión, luces y sonido.
    /// Fotos: docs/capturas/t-1397/&lt;código&gt;.png con los rotores girados.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.DronesT1397.Aplicar (y .Fotos).
    /// </summary>
    public static class DronesT1397
    {
        /// <summary>Código del modelo (nombre del FBX sin extensión).</summary>
        public static readonly string[] Modelos = { "DR01", "DR02_a", "DR02_b", "DR03_a", "DR04_a", "E13", "NV01", "NV03_a", "NV03_b" };

        public const string CarpetaOrigen = "imageassets/blender/drones/";
        public const string CarpetaDestino = "Assets/Arte/Vehiculos/Drones/";
        public const string CarpetaPrefabs = "Assets/Arte/Vehiculos/Drones/Prefabs/";
        public const string CarpetaCapturas = "docs/capturas/t-1397/";
        public const string ModeloDronPolicia = "DR01";
        public const string NombreVisualNuevo = "Visual_T1397";
        public const string NombreVisualNoche = "Visual_Noche";

        private const float GradosFoto = 30f;
        private const float VelocidadNave = 360f;
        private const float VelocidadDron = 1800f;

        public static string RutaFbx(string codigo) => CarpetaDestino + codigo + "/" + codigo + ".fbx";

        public static string RutaPrefab(string codigo) => CarpetaPrefabs + "Dron_" + codigo + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-1397/Importar drones y naves")]
        public static void Aplicar()
        {
            foreach (string codigo in Modelos)
            {
                CopiarFbx(codigo);
            }
            AssetDatabase.Refresh();
            foreach (string codigo in Modelos)
            {
                ConfigurarImportador(codigo);
            }
            AssetDatabase.Refresh();
            foreach (string codigo in Modelos)
            {
                ArreglarMateriales(codigo);
                CrearPrefab(codigo);
            }
            CambiarDronPolicia();
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T-1397: drones y naves importados, prefabs en " + CarpetaPrefabs + ", Dron.prefab con " + ModeloDronPolicia + ".");
        }

        private static void CopiarFbx(string codigo)
        {
            string origen = CarpetaOrigen + codigo + ".fbx";
            if (!File.Exists(origen))
            {
                Debug.LogError("[Arte] T-1397: falta " + origen);
                return;
            }
            Directory.CreateDirectory(CarpetaDestino + codigo);
            File.Copy(origen, RutaFbx(codigo), true);
        }

        // Materiales externos (se crean URP/Lit desde los del FBX), sin animación, cámaras ni luces.
        private static void ConfigurarImportador(string codigo)
        {
            ModelImporter importador = AssetImporter.GetAtPath(RutaFbx(codigo)) as ModelImporter;
            if (importador == null)
            {
                return;
            }
            importador.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importador.materialLocation = ModelImporterMaterialLocation.External;
            importador.importAnimation = false;
            importador.importCameras = false;
            importador.importLights = false;
            importador.SaveAndReimport();
        }

        // Si algún material no es URP/Lit lo pasa a URP/Lit conservando la textura base y el color.
        private static void ArreglarMateriales(string codigo)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            string carpeta = CarpetaDestino + codigo;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { carpeta }))
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null || lit == null || mat.shader == lit)
                {
                    continue;
                }
                Texture textura = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                mat.shader = lit;
                if (textura != null)
                {
                    mat.SetTexture("_BaseMap", textura);
                }
                mat.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(mat);
            }
        }

        private static void CrearPrefab(string codigo)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(codigo));
            if (fbx == null)
            {
                Debug.LogError("[Arte] T-1397: no se pudo cargar " + RutaFbx(codigo));
                return;
            }
            Directory.CreateDirectory(CarpetaPrefabs);
            GameObject raiz = new GameObject("Dron_" + codigo);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            modelo.name = codigo;
            modelo.transform.SetParent(raiz.transform, false);
            GiroRotores giro = raiz.AddComponent<GiroRotores>();
            SerializedObject so = new SerializedObject(giro);
            so.FindProperty("velocidad").floatValue = EsNave(codigo) ? VelocidadNave : VelocidadDron;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
            Object.DestroyImmediate(raiz);
        }

        private static bool EsNave(string codigo) => codigo.StartsWith("NV");

        /// <summary>
        /// Dron.prefab (el dron de la policía, T-341) conserva todo y solo cambia el visual: apaga los cubos greybox (Cuerpo y Rotor) y el Visual_Noche de CY07b,
        /// y cuelga el modelo DR01 con su GiroRotores. Correrlo otra vez deja lo mismo.
        /// </summary>
        public static void CambiarDronPolicia()
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(ModeloDronPolicia));
            if (modelo == null)
            {
                Debug.LogError("[Arte] T-1397: no hay " + RutaPrefab(ModeloDronPolicia));
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(CreadorPersecucion.RutaPrefabDron);
            try
            {
                Transform viejo = raiz.transform.Find(NombreVisualNuevo);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                foreach (Transform hijo in raiz.transform)
                {
                    bool greybox = hijo.name == "Cuerpo" || hijo.name == "Rotor" || hijo.name == NombreVisualNoche;
                    if (!greybox)
                    {
                        continue;
                    }
                    foreach (Renderer r in hijo.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled = false;
                    }
                }
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
                visual.name = NombreVisualNuevo;
                visual.transform.SetParent(raiz.transform, false);
                foreach (Collider c in visual.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(c);
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, CreadorPersecucion.RutaPrefabDron);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ fotos

        [MenuItem("Mecha Cyberpunk/T-1397/Fotos de drones y naves")]
        public static void Fotos()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            Light sol = new GameObject("Sol").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.3f;
            sol.transform.rotation = Quaternion.Euler(50f, 200f, 0f);
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            Directory.CreateDirectory(CarpetaCapturas);
            foreach (string codigo in Modelos)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
                if (prefab == null)
                {
                    Debug.LogWarning("[Arte] T-1397: no hay " + RutaPrefab(codigo));
                    continue;
                }
                GameObject dron = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                GiroRotores giro = dron.GetComponent<GiroRotores>();
                giro.Buscar();
                giro.Girar(GradosFoto / Mathf.Max(giro.Velocidad, 1f));
                Bounds b = LimitesDe(dron);
                Encuadrar(camara, b, new Vector3(-0.8f, 0.7f, 1f));
                Guardar(camara, Path.Combine(CarpetaCapturas, codigo + ".png"));
                Object.DestroyImmediate(dron);
            }
            Debug.Log("[Arte] T-1397: fotos en " + CarpetaCapturas);
        }

        private static Bounds LimitesDe(GameObject objeto)
        {
            Renderer[] rs = objeto.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static void Encuadrar(Camera camara, Bounds b, Vector3 direccion)
        {
            camara.fieldOfView = 40f;
            camara.nearClipPlane = 0.05f;
            camara.farClipPlane = 1000f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            float radio = Mathf.Max(b.extents.magnitude, 0.2f);
            float distancia = radio / Mathf.Sin(camara.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.85f;
            camara.transform.position = b.center + direccion.normalized * distancia;
            camara.transform.LookAt(b.center);
        }

        private static void Guardar(Camera camara, string ruta)
        {
            RenderTexture destino = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(destino.width, destino.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, destino.width, destino.height), 0, 0);
            imagen.Apply();
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            Object.DestroyImmediate(imagen);
            destino.Release();
            Object.DestroyImmediate(destino);
        }
    }
}
