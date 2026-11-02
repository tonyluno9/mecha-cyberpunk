using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-513 (paso 5 del plan de personajes y carros): fondo lejano de la ciudad. Las pinturas de imageassets/fondos/ (fondo-suburbios,
    /// fondo-neon-alto, fondo-nova) van en un cilindro de <see cref="Radio"/> m alrededor de la ciudad, en 4 tramos de 90°: Nova al norte,
    /// Neón Alto al este y Suburbios al sur y al oeste (el oeste en espejo para que no se note la unión). Shader MechaCyberpunk/FondoCiudad: sin luces, sin sombras ni niebla del juego, arriba se funde con
    /// el cielo. Se apagan las 48 torres lejanas de T-501 (T501_Suburbios/Horizonte): el fondo las cubre; las 14 de Neón Alto (T-505) se quedan.
    /// Idempotente: todo cuelga de <see cref="NombreRaiz"/>. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT513.Colocar
    /// </summary>
    public static class ColocarT513
    {
        public const string NombreRaiz = "T513_Fondo";
        public const float Radio = 560f;
        public const int Tramos = 4;
        public const float GradosPorTramo = 360f / Tramos;
        private const float BaseY = -40f;
        private const int ColumnasPorTramo = 24;
        private const float AspectoPintura = 6336f / 2688f;

        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaFuente = "imageassets/fondos/";
        private const string Carpeta = "Assets/Arte/Fondos/";
        private const string RutaShader = "MechaCyberpunk/FondoCiudad";
        public static readonly string[] Pinturas = { "fondo-suburbios", "fondo-neon-alto", "fondo-nova" };

        /// <summary>Pintura de cada tramo (0 = norte, en el sentido del reloj visto desde arriba) y si va en espejo.</summary>
        public static readonly (string pintura, bool espejo)[] PorTramo =
        {
            ("fondo-nova", false),       // N
            ("fondo-neon-alto", false),  // E
            ("fondo-suburbios", false),  // S
            ("fondo-suburbios", true),   // O (espejo del sur: la unión no se nota)
        };

        // Valores del shader (con el posproceso de noche de T-507 la pintura necesita más brillo; de día se aclara y se mezcla con bruma).
        private const float BrilloNoche = 2f;
        private const float BrumaNoche = 0.1f;
        private const float BrilloDia = 0.7f;
        private static readonly Color ColorBrumaDia = new Color(0.36f, 0.45f, 0.62f);
        private const float BrumaDia = 0.45f;

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-513/Fondo de la ciudad")]
        public static void Colocar()
        {
            Informe.Clear();
            Directory.CreateDirectory(Carpeta + "Texturas");
            Directory.CreateDirectory(Carpeta + "Materiales");
            Directory.CreateDirectory(Carpeta + "Mallas");
            foreach (string p in Pinturas)
            {
                ImportarPintura(p);
            }

            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Transform raiz = new GameObject(NombreRaiz).transform;
            for (int i = 0; i < Tramos; i++)
            {
                (string pintura, bool espejo) = PorTramo[i];
                GameObject tramo = new GameObject("Tramo_" + i + "_" + pintura);
                tramo.transform.SetParent(raiz, false);
                tramo.AddComponent<MeshFilter>().sharedMesh = Malla(i, espejo);
                MeshRenderer r = tramo.AddComponent<MeshRenderer>();
                r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "Materiales/" + pintura + ".mat");
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                r.allowOcclusionWhenDynamic = false;
            }
            Informe.AppendLine("Fondo: " + Tramos + " tramos de " + GradosPorTramo + "° a " + Radio + " m (alto " + Alto().ToString("F0") + " m)");

            int torres = 0;
            Transform horizonte = GameObject.Find("T501_Suburbios")?.transform.Find("Horizonte");
            if (horizonte != null)
            {
                torres = horizonte.childCount;
                horizonte.gameObject.SetActive(false);
            }
            Informe.AppendLine("Torres lejanas de T-501 apagadas: " + torres);

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t513-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-513 listo:\n" + Informe);
        }

        /// <summary>Alto del cilindro (m): el de la pintura a lo ancho de un tramo, sin estirarla.</summary>
        public static float Alto()
        {
            return Radio * Mathf.Deg2Rad * GradosPorTramo / AspectoPintura;
        }

        private static void ImportarPintura(string nombre)
        {
            string destino = Carpeta + "Texturas/" + nombre + ".png";
            if (!File.Exists(destino))
            {
                File.Copy(CarpetaFuente + nombre + ".png", destino);
                AssetDatabase.ImportAsset(destino);
            }
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(destino);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 4096;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();

            string rutaMat = Carpeta + "Materiales/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (m == null)
            {
                m = new Material(Shader.Find(RutaShader));
                AssetDatabase.CreateAsset(m, rutaMat);
            }
            m.shader = Shader.Find(RutaShader);
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(destino));
            m.SetFloat("_BrilloNoche", BrilloNoche);
            m.SetFloat("_BrumaNoche", BrumaNoche);
            m.SetFloat("_BrilloDia", BrilloDia);
            m.SetColor("_ColorBrumaDia", ColorBrumaDia);
            m.SetFloat("_BrumaDia", BrumaDia);
            EditorUtility.SetDirty(m);
            Informe.AppendLine("Pintura " + nombre + ": " + destino);
        }

        private static Mesh Malla(int tramo, bool espejo)
        {
            string ruta = Carpeta + "Mallas/FondoCiudad_" + tramo + ".asset";
            Mesh malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (malla == null)
            {
                malla = new Mesh();
                AssetDatabase.CreateAsset(malla, ruta);
            }
            malla.Clear();
            malla.name = "FondoCiudad_" + tramo;
            float alto = Alto();
            int n = ColumnasPorTramo + 1;
            Vector3[] v = new Vector3[n * 2];
            Vector2[] uv = new Vector2[n * 2];
            Vector3[] normales = new Vector3[n * 2];
            float inicio = tramo * GradosPorTramo - GradosPorTramo * 0.5f;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)ColumnasPorTramo;
                float ang = (inicio + t * GradosPorTramo) * Mathf.Deg2Rad;
                // 0° = +Z (norte), 90° = +X (este).
                Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                float u = espejo ? 1f - t : t;
                v[k] = dir * Radio + Vector3.up * BaseY;
                v[k + n] = dir * Radio + Vector3.up * (BaseY + alto);
                uv[k] = new Vector2(u, 0f);
                uv[k + n] = new Vector2(u, 1f);
                normales[k] = -dir;
                normales[k + n] = -dir;
            }
            int[] tri = new int[ColumnasPorTramo * 6];
            for (int k = 0; k < ColumnasPorTramo; k++)
            {
                // Caras hacia adentro (el jugador mira desde el centro).
                tri[k * 6] = k;
                tri[k * 6 + 1] = k + n;
                tri[k * 6 + 2] = k + 1;
                tri[k * 6 + 3] = k + 1;
                tri[k * 6 + 4] = k + n;
                tri[k * 6 + 5] = k + n + 1;
            }
            malla.vertices = v;
            malla.uv = uv;
            malla.normals = normales;
            malla.triangles = tri;
            malla.RecalculateBounds();
            EditorUtility.SetDirty(malla);
            return malla;
        }
    }
}
