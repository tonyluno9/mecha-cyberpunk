using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-535 (plan de la noche 3-nov, paso 1: luces, lluvia y asfalto). Idempotente sobre Ciudad_Prologo:
    /// - Asfalto mojado con detalle (grano, grietas, parches de bacheo, manchas de aceite y zonas lisas que brillan; normal map): texturas
    ///   T535_Asfalto_* de Assets/Arte/Exteriores/Fuentes~/generar_asfalto.py en Barrio_Asfalto (8 m por mosaico) y en las calles de las afueras.
    /// - Farolas: la luz sale del foco (se busca en la malla del poste la punta del brazo), no de un lado.
    /// - El Tianguis: cada puesto con su propia luz chiquita del color de lo que vende (solo de noche).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT535.Colocar
    /// </summary>
    public static class ColocarT535
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaAsfalto = "Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat";
        private const string Texturas = "Assets/Arte/Exteriores/Texturas/";
        private const string Materiales = "Assets/Arte/Exteriores/Materiales/";
        private const float MetrosPorMosaico = 8f;
        private const string NombreLuzPuesto = "Luz_Puesto_T535";

        public static readonly Color LuzTacos = new Color(1f, 0.55f, 0.22f);
        public static readonly Color LuzMagenta = new Color(1f, 0.3f, 0.8f);
        public static readonly Color LuzAmbar = new Color(1f, 0.75f, 0.38f);
        public static readonly Color LuzCiber = new Color(0.3f, 0.9f, 1f);
        public static readonly Color LuzMesa = new Color(1f, 0.82f, 0.55f);

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-535/Luces, lluvia y asfalto")]
        public static void Colocar()
        {
            Informe.Clear();
            Material asfalto = Asfalto();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            AsfaltoAfueras(asfalto);
            Farolas();
            LucesPuestos();
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t535-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-535 listo:\n" + Informe);
        }

        // ---------------------------------------------------------------- Asfalto

        private static Material Asfalto()
        {
            AssetDatabase.ImportAsset(Texturas + "T535_Asfalto_Color.png");
            AssetDatabase.ImportAsset(Texturas + "T535_Asfalto_Normal.png");
            TextureImporter color = (TextureImporter)AssetImporter.GetAtPath(Texturas + "T535_Asfalto_Color.png");
            color.sRGBTexture = true;
            color.alphaSource = TextureImporterAlphaSource.FromInput;
            color.alphaIsTransparency = false;
            color.wrapMode = TextureWrapMode.Repeat;
            color.mipmapEnabled = true;
            color.anisoLevel = 8;
            color.maxTextureSize = 1024;
            color.SaveAndReimport();
            TextureImporter normal = (TextureImporter)AssetImporter.GetAtPath(Texturas + "T535_Asfalto_Normal.png");
            normal.textureType = TextureImporterType.NormalMap;
            normal.wrapMode = TextureWrapMode.Repeat;
            normal.anisoLevel = 8;
            normal.maxTextureSize = 1024;
            normal.SaveAndReimport();

            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaAsfalto);
            Texture2D tc = AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + "T535_Asfalto_Color.png");
            Texture2D tn = AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + "T535_Asfalto_Normal.png");
            Configurar(m, tc, tn, new Vector2(272f / MetrosPorMosaico, 178f / MetrosPorMosaico));
            Informe.AppendLine("Asfalto: " + m.shader.name + " con color, normal y lo mojado en el alfa (" + MetrosPorMosaico + " m por mosaico)");
            return m;
        }

        private static void Configurar(Material m, Texture2D color, Texture2D normal, Vector2 repeticion)
        {
            m.SetTexture("_BaseMap", color);
            m.SetTexture("_MainTex", color);
            m.SetTextureScale("_BaseMap", repeticion);
            m.SetTextureScale("_MainTex", repeticion);
            m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 0.8f);
                m.EnableKeyword("_NORMALMAP");
            }
            if (m.HasProperty("_Smoothness"))
            {
                // Lo mojado (alfa del color) brilla con los neones; lo seco no.
                m.SetFloat("_Smoothness", 0.8f);
                m.SetFloat("_SmoothnessTextureChannel", 1f);
                m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            }
            if (m.HasProperty("_Metallic"))
            {
                m.SetFloat("_Metallic", 0f);
            }
            EditorUtility.SetDirty(m);
        }

        // Las calles de las afueras (T-530) son cubos: cada una con su copia del material, con el mosaico de 8 m a su medida.
        private static void AsfaltoAfueras(Material asfalto)
        {
            Transform afueras = GameObject.Find("T530_Afueras")?.transform;
            if (afueras == null)
            {
                return;
            }
            int n = 0;
            foreach (MeshRenderer r in afueras.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Calle" || r.name == "CalleCruzada"))
            {
                Vector3 e = r.transform.localScale;
                string ruta = Materiales + "T535_AsfaltoAfueras_" + Mathf.RoundToInt(e.x) + "x" + Mathf.RoundToInt(e.z) + ".mat";
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null)
                {
                    m = new Material(asfalto);
                    AssetDatabase.CreateAsset(m, ruta);
                }
                m.CopyPropertiesFromMaterial(asfalto);
                Configurar(m, (Texture2D)asfalto.GetTexture("_BaseMap"), asfalto.HasProperty("_BumpMap") ? (Texture2D)asfalto.GetTexture("_BumpMap") : null,
                    new Vector2(e.x / MetrosPorMosaico, e.z / MetrosPorMosaico));
                r.sharedMaterial = m;
                n++;
            }
            Informe.AppendLine("Calles de las afueras con el asfalto nuevo: " + n);
        }

        // ---------------------------------------------------------------- Farolas

        private static void Farolas()
        {
            Transform farolas = GameObject.Find("Barrio")?.transform.Find("Farolas");
            if (farolas == null)
            {
                return;
            }
            int n = 0;
            foreach (Transform farola in farolas)
            {
                Light luz = farola.GetComponentsInChildren<Light>(true).FirstOrDefault();
                MeshFilter malla = farola.GetComponentsInChildren<MeshFilter>()
                    .Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null && f.GetComponent<MeshRenderer>().enabled)
                    .OrderByDescending(f => f.GetComponent<MeshRenderer>().bounds.size.y).FirstOrDefault();
                if (luz == null || malla == null || !Foco(malla, out Vector3 foco))
                {
                    continue;
                }
                luz.transform.position = foco;
                n++;
            }
            Informe.AppendLine("Farolas con la luz en el foco: " + n);
        }

        /// <summary>
        /// La punta del brazo del poste: de los vértices del último metro y medio de alto, los más lejos del eje del poste (que sale de los
        /// vértices del pie). La luz va 25 cm debajo de su centro.
        /// </summary>
        public static bool Foco(MeshFilter malla, out Vector3 foco)
        {
            foco = Vector3.zero;
            Mesh m = malla.sharedMesh;
            if (!m.isReadable)
            {
                Bounds b = malla.GetComponent<Renderer>().bounds;
                foco = new Vector3(b.center.x, b.max.y - 0.4f, b.center.z);
                return true;
            }
            Vector3[] v = m.vertices.Select(p => malla.transform.TransformPoint(p)).ToArray();
            float minY = v.Min(p => p.y);
            float maxY = v.Max(p => p.y);
            Vector3[] pie = v.Where(p => p.y < minY + 1f).ToArray();
            Vector3 eje = new Vector3(pie.Average(p => p.x), 0f, pie.Average(p => p.z));
            Vector3[] arriba = v.Where(p => p.y > maxY - 1.5f).ToArray();
            Func<Vector3, float> lejos = p => new Vector2(p.x - eje.x, p.z - eje.z).magnitude;
            float maxLejos = arriba.Max(lejos);
            if (maxLejos < 0.3f)
            {
                foco = new Vector3(eje.x, maxY - 0.3f, eje.z);
                return true;
            }
            Vector3[] punta = arriba.Where(p => lejos(p) > maxLejos * 0.75f).ToArray();
            foco = new Vector3(punta.Average(p => p.x), punta.Min(p => p.y) - 0.25f, punta.Average(p => p.z));
            return true;
        }

        // ---------------------------------------------------------------- Puestos del Tianguis

        private static void LucesPuestos()
        {
            foreach (Light vieja in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(l => l.name == NombreLuzPuesto).ToList())
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Type tipoLuzNocturna = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Reloj.LuzNocturna")).FirstOrDefault(t => t != null);
            int n = 0;
            foreach (Transform puesto in Puestos())
            {
                Renderer[] rs = puesto.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
                if (rs.Length == 0)
                {
                    continue;
                }
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                GameObject go = new GameObject(NombreLuzPuesto);
                go.transform.SetParent(puesto, true);
                go.transform.position = new Vector3(b.center.x, b.min.y + Mathf.Min(2.1f, b.size.y * 0.75f), b.center.z);
                Light l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = ColorDe(puesto.name);
                l.intensity = 2.5f;
                l.range = 4.5f;
                l.shadows = LightShadows.None;
                if (tipoLuzNocturna != null)
                {
                    Component c = go.AddComponent(tipoLuzNocturna);
                    SerializedObject so = new SerializedObject(c);
                    so.FindProperty("luz").objectReferenceValue = l;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                n++;
            }
            Informe.AppendLine("Puestos del Tianguis con su propia luz: " + n);
        }

        private static Transform[] Puestos()
        {
            Transform t512 = GameObject.Find("T512_LuzZonas")?.transform.Find("Tianguis");
            Transform t503 = GameObject.Find("T503_Tianguis")?.transform.Find("Puestos");
            Transform t407 = GameObject.Find("GTA_T407")?.transform.Find("Tianguis/Puestos");
            Transform noche = GameObject.Find("Barrio")?.transform.Find("ArteNoche/Tianguis");
            return new[] { t512, t503, t407 }.Where(t => t != null).SelectMany(t => t.Cast<Transform>())
                .Concat(noche != null ? noche.Cast<Transform>().Where(t => t.name.StartsWith("C13")) : Enumerable.Empty<Transform>())
                .Where(t => t.gameObject.activeInHierarchy && !t.name.StartsWith("CY06") && !t.name.StartsWith("CY01")).ToArray();
        }

        public static Color ColorDe(string nombre)
        {
            if (nombre.StartsWith("C12"))
            {
                return LuzTacos;
            }
            if (nombre.StartsWith("C13"))
            {
                return LuzCiber;
            }
            if (nombre.StartsWith("N03a"))
            {
                return LuzMagenta;
            }
            if (nombre.StartsWith("N03b"))
            {
                return LuzAmbar;
            }
            return LuzMesa;
        }
    }
}
