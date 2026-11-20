using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1525: los costales de cobertura CB02 (T-1511) se veían "pintados". La textura que trae el modelo (Tripo) es un atlas suave, casi sin grano de tela,
    /// y el material los dejaba brillosos (_Smoothness 0.8 y _Metallic 1 con un mapa metal/suavidad del modelo). Aquí el material pasa a tela: sin metal, brillo
    /// muy bajo, y una textura de detalle de yute (albedo + normal, tejido simple que se repite) encima de la textura del modelo para que se lea como costal.
    /// Se puede repetir. Batchmode, con Unity cerrado:
    ///   Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.CostalesT1525.Diagnostico
    ///   Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.CostalesT1525.Aplicar
    /// </summary>
    public static class CostalesT1525
    {
        public const string Carpeta = "Assets/Arte/Zonas/CB/";
        public static readonly string[] Nombres = { "CB02_Costales_a", "CB02_Costales_b" };
        public const string RutaDetalle = Carpeta + "CB02_Tela_detalle.png";
        public const string RutaDetalleNormal = Carpeta + "CB02_Tela_detalle_normal.png";

        private const float Suavidad = 0.1f;
        private const float RepeticionesDetalle = 14f; // veces que el tejido (128 px) se repite por la textura del modelo
        private const float FuerzaNormalDetalle = 1f;
        private const int LadoDetalle = 128;
        private const int HilosPorLado = 16; // hilos por lado del tejido; 8 px por hilo

        [MenuItem("Mecha Cyberpunk/T-1525/Diagnostico costales CB02")]
        public static void Diagnostico()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string nombre in Nombres)
            {
                string rutaMat = Carpeta + nombre + "/" + nombre + ".mat";
                Material m = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
                if (m == null)
                {
                    sb.AppendLine(nombre + ": FALTA el material " + rutaMat);
                    continue;
                }
                sb.AppendLine(string.Format("{0}: shader {1}, BaseMap {2}, BumpMap {3}, MetalGloss {4}, Metallic {5}, Smoothness {6}, DetailAlbedo {7}",
                    nombre, m.shader.name, Nom(m.GetTexture("_BaseMap")), Nom(m.GetTexture("_BumpMap")), Nom(m.GetTexture("_MetallicGlossMap")),
                    m.GetFloat("_Metallic"), m.GetFloat("_Smoothness"), Nom(m.GetTexture("_DetailAlbedoMap"))));
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab");
                if (prefab == null)
                {
                    sb.AppendLine("  FALTA el prefab");
                    continue;
                }
                foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material rm in r.sharedMaterials)
                    {
                        sb.AppendLine("  " + r.name + " -> " + (rm != null ? AssetDatabase.GetAssetPath(rm) : "SIN MATERIAL") + (rm == m ? " (ok)" : " (DISTINTO)"));
                    }
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        sb.AppendLine("  malla " + mf.sharedMesh.name + ": uv0 " + (mf.sharedMesh.uv.Length > 0) + ", submallas " + mf.sharedMesh.subMeshCount);
                    }
                }
            }
            Debug.Log("[T-1525] Diagnostico costales:\n" + sb);
        }

        [MenuItem("Mecha Cyberpunk/T-1525/Aplicar tela a costales CB02")]
        public static void Aplicar()
        {
            Texture2D albedo = AsegurarDetalle(RutaDetalle, false);
            Texture2D normal = AsegurarDetalle(RutaDetalleNormal, true);
            int hechos = 0;
            foreach (string nombre in Nombres)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + nombre + "/" + nombre + ".mat");
                if (m == null)
                {
                    Debug.LogWarning("[T-1525] falta el material de " + nombre + ": corre antes CoberturaCbT1511.Importar");
                    continue;
                }
                AjustarMaterial(m, albedo, normal);
                AsegurarPrefab(nombre, m);
                hechos++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[T-1525] costales con tela: " + hechos + " materiales ajustados.");
        }

        /// <summary>Deja el material como tela. Lo llama también CoberturaCbT1511.CrearMaterial para que repetir la importación no lo revierta.</summary>
        public static void AjustarMaterial(Material m)
        {
            AjustarMaterial(m, AsegurarDetalle(RutaDetalle, false), AsegurarDetalle(RutaDetalleNormal, true));
        }

        private static void AjustarMaterial(Material m, Texture2D albedo, Texture2D normal)
        {
            m.SetFloat("_WorkflowMode", 1f);
            m.SetTexture("_MetallicGlossMap", null);
            m.DisableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", Suavidad);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.SetFloat("_SpecularHighlights", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.SetColor("_BaseColor", Color.white);
            if (albedo != null && normal != null)
            {
                Vector2 repetir = new Vector2(RepeticionesDetalle, RepeticionesDetalle);
                m.SetTexture("_DetailAlbedoMap", albedo);
                m.SetTextureScale("_DetailAlbedoMap", repetir);
                m.SetTexture("_DetailNormalMap", normal);
                m.SetTextureScale("_DetailNormalMap", repetir);
                m.SetFloat("_DetailAlbedoMapScale", 1f);
                m.SetFloat("_DetailNormalMapScale", FuerzaNormalDetalle);
                m.EnableKeyword("_DETAIL_MULX2");
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
        }

        // El prefab debe usar el material de su carpeta en todas las ranuras (si el modelo trajera más de una, quedaba la importada, de color plano).
        private static void AsegurarPrefab(string nombre, Material m)
        {
            string ruta = Carpeta + nombre + ".prefab";
            if (!File.Exists(ruta))
            {
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            bool cambio = false;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                Material[] actuales = r.sharedMaterials;
                Material[] nuevos = new Material[Mathf.Max(1, actuales.Length)];
                for (int i = 0; i < nuevos.Length; i++)
                {
                    nuevos[i] = m;
                    cambio |= i >= actuales.Length || actuales[i] != m;
                }
                if (cambio)
                {
                    r.sharedMaterials = nuevos;
                }
            }
            if (cambio)
            {
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            }
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        private static string Nom(Object o) => o != null ? o.name : "nada";

        // ------------------------------------------------------------------ textura de detalle (tejido de yute)

        private static Texture2D AsegurarDetalle(string ruta, bool esNormal)
        {
            if (!File.Exists(ruta))
            {
                File.WriteAllBytes(ruta, GenerarPng(esNormal));
                AssetDatabase.ImportAsset(ruta);
            }
            TextureImporter t = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (t != null)
            {
                bool cambio = t.wrapMode != TextureWrapMode.Repeat || t.mipmapEnabled != true;
                t.wrapMode = TextureWrapMode.Repeat;
                t.mipmapEnabled = true;
                t.filterMode = FilterMode.Trilinear;
                if (esNormal && t.textureType != TextureImporterType.NormalMap)
                {
                    t.textureType = TextureImporterType.NormalMap;
                    cambio = true;
                }
                if (!esNormal && (t.textureType != TextureImporterType.Default || !t.sRGBTexture))
                {
                    t.textureType = TextureImporterType.Default;
                    t.sRGBTexture = true;
                    cambio = true;
                }
                if (cambio)
                {
                    t.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        // Altura del tejido simple: cada hilo es un cilindro; sobre un cuadro pasa el horizontal, sobre el siguiente el vertical. Se repite sin costura.
        private static float Altura(int x, int y)
        {
            int paso = LadoDetalle / HilosPorLado;
            int hx = (x % LadoDetalle) / paso;
            int hy = (y % LadoDetalle) / paso;
            float px = ((x % paso) + 0.5f) / paso;
            float py = ((y % paso) + 0.5f) / paso;
            bool horizontalArriba = ((hx + hy) & 1) == 0;
            float perfil = horizontalArriba ? Mathf.Sin(py * Mathf.PI) : Mathf.Sin(px * Mathf.PI);
            float elevacion = horizontalArriba ? 1f : 0.55f;
            float ruido = Ruido(x, y) * 0.18f;
            return Mathf.Clamp01(perfil * 0.5f * elevacion + 0.25f + ruido);
        }

        private static float Ruido(int x, int y)
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 1023u) / 1023f - 0.5f;
        }

        private static byte[] GenerarPng(bool esNormal)
        {
            Texture2D t = new Texture2D(LadoDetalle, LadoDetalle, TextureFormat.RGBA32, false, esNormal);
            Color32[] pixeles = new Color32[LadoDetalle * LadoDetalle];
            for (int y = 0; y < LadoDetalle; y++)
            {
                for (int x = 0; x < LadoDetalle; x++)
                {
                    float h = Altura(x, y);
                    if (!esNormal)
                    {
                        // Gris medio = neutro (x2 en el shader): el hilo alto aclara y el hueco oscurece, sin teñir.
                        byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f + (h - 0.5f) * 0.9f) * 255f);
                        pixeles[y * LadoDetalle + x] = new Color32(g, g, g, 255);
                        continue;
                    }
                    float dx = Altura((x + 1) % LadoDetalle, y) - Altura((x + LadoDetalle - 1) % LadoDetalle, y);
                    float dy = Altura(x, (y + 1) % LadoDetalle) - Altura(x, (y + LadoDetalle - 1) % LadoDetalle);
                    Vector3 n = new Vector3(-dx * 3f, -dy * 3f, 1f).normalized;
                    pixeles[y * LadoDetalle + x] = new Color32(
                        (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 255);
                }
            }
            t.SetPixels32(pixeles);
            t.Apply();
            byte[] png = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return png;
        }
    }
}
