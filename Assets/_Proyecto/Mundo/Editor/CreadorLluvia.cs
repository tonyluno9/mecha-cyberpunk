using System.IO;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-096: crea ConfigLluvia_Mundo, los materiales de gota y niebla (URP Particles/Unlit transparente), la textura suave de la nube y
    /// pone el objeto Lluvia con LluviaSigueJugador en la raíz del prefab del barrio (lo heredan Ciudad y Pruebas_Alan).
    /// Corre después de VestirGrafitis y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorLluvia.Crear
    /// </summary>
    public static class CreadorLluvia
    {
        private const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigLluvia_Mundo.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaTextura = "Assets/_Proyecto/Mundo/Texturas/Niebla_Nube.png";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const int LadoTextura = 128;

        [MenuItem("Mecha Cyberpunk/Crear T-096")]
        public static void Crear()
        {
            ConfigLluvia config = AssetDatabase.LoadAssetAtPath<ConfigLluvia>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigLluvia>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            Texture2D nube = CrearTexturaNube();
            Material gota = CrearMaterial("Lluvia_Gota", null);
            Material niebla = CrearMaterial("Lluvia_Niebla", nube);
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform viejo = raiz.transform.Find("Lluvia");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject objeto = new GameObject("Lluvia");
            objeto.transform.SetParent(raiz.transform, false);
            LluviaSigueJugador lluvia = objeto.AddComponent<LluviaSigueJugador>();
            SerializedObject so = new SerializedObject(lluvia);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("materialGota").objectReferenceValue = gota;
            so.FindProperty("materialNiebla").objectReferenceValue = niebla;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-096 creada: lluvia y niebla baja que siguen al jugador.");
        }

        // Nube suave: un disco con borde que se desvanece (alfa = 1 - distancia al centro, al cuadrado).
        private static Texture2D CrearTexturaNube()
        {
            Texture2D textura = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false);
            Color[] pixeles = new Color[LadoTextura * LadoTextura];
            float mitad = LadoTextura / 2f;
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float d = Mathf.Clamp01(Mathf.Sqrt((x - mitad) * (x - mitad) + (y - mitad) * (y - mitad)) / mitad);
                    float alfa = (1f - d) * (1f - d);
                    pixeles[y * LadoTextura + x] = new Color(1f, 1f, 1f, alfa);
                }
            }
            textura.SetPixels(pixeles);
            byte[] bytes = textura.EncodeToPNG();
            Object.DestroyImmediate(textura);
            if (!File.Exists(RutaTextura) || !File.ReadAllBytes(RutaTextura).Length.Equals(bytes.Length))
            {
                File.WriteAllBytes(RutaTextura, bytes);
            }
            AssetDatabase.ImportAsset(RutaTextura, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(RutaTextura);
            if (!importador.alphaIsTransparency)
            {
                importador.alphaIsTransparency = true;
                importador.wrapMode = TextureWrapMode.Clamp;
                importador.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura);
        }

        private static Material CrearMaterial(string nombre, Texture2D textura)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetFloat("_Surface", 1f);                       // transparente
            material.SetFloat("_Blend", 0f);                         // alfa normal
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", Color.white);
            if (textura != null)
            {
                material.SetTexture("_BaseMap", textura);
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
