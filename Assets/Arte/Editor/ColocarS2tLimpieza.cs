using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-856 (playtest 6-nov tarde): fuera lo inventado con primitivos que Tony no hizo. Se apagan (no se borran: otros scripts pueden
    /// referenciarlos) los balcones, toldos, macetas y plantas de cubo de T508_CallesLlenas, los "tejados" (las Lamina cafés de las azoteas del
    /// barrio), los puentes de cubos de S2n_Puentes (T-857 pone el modelo de puente de Tony) y el set de cubos del hacker. Las banquetas pasan a la
    /// textura T01_Banqueta (imageassets/calle), con el tamaño de losa parejo en todas (un material por tamaño de banqueta).
    /// Lo que se queda: los edificios del barrio y sus fachadas del kit, y todo lo hecho con los assets de Tony. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2tLimpieza.Colocar
    /// </summary>
    public static class ColocarS2tLimpieza
    {
        /// <summary>Grupos de T508_CallesLlenas hechos con cubos y esferas (se apagan enteros).</summary>
        public static readonly string[] GruposT508 = { "Balcon", "Toldo_Lona", "Toldo_Lamina", "Planta", "Maceta" };
        public const string RaizT508 = "T508_CallesLlenas";
        public const string RaizPuentes = "S2n_Puentes";
        public const string RutaSetHacker = "GTA_T417/Apoyo_SERGIO/Set_Hacker";
        public const string NombreTejado = "Lamina";
        public const string MaterialBanqueta = "T506_Barrio_Banqueta";
        public const string OrigenBanqueta = "imageassets/calle/T01_Banqueta.png";
        public const string TexturaBanqueta = "Assets/Arte/Calle/Banqueta/T01_Banqueta.png";
        public const string CarpetaBanqueta = "Assets/Arte/Calle/Banqueta/";
        /// <summary>Lado de una losa de la textura T01 en el mundo (m).</summary>
        public const float LadoLosa = 2.5f;
        /// <summary>Tinte de la textura T01 (sale muy clara a pleno sol).</summary>
        public static readonly Color TonoBanqueta = new Color(0.5f, 0.5f, 0.52f);

        [MenuItem("Mecha Cyberpunk/S2t/4 Quitar lo inventado con primitivos")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            int apagados = 0;

            Transform t508 = ColocarS2.Buscar(RaizT508);
            if (t508 != null)
            {
                foreach (Transform g in t508.GetComponentsInChildren<Transform>(true).Where(t => GruposT508.Contains(t.name) && t.gameObject.activeSelf).ToArray())
                {
                    g.gameObject.SetActive(false);
                    apagados++;
                }
            }
            ColocarS2.Informe.AppendLine("T508: " + apagados + " grupos de balcones, toldos, macetas y plantas apagados");

            Transform barrio = ColocarS2.Buscar("Barrio");
            int tejados = 0;
            foreach (Transform l in barrio.GetComponentsInChildren<Transform>(true).Where(t => t.name == NombreTejado && t.parent != null && t.parent.parent != null
                && t.parent.parent.name == "Edificios" && t.gameObject.activeSelf).ToArray())
            {
                l.gameObject.SetActive(false);
                tejados++;
            }
            ColocarS2.Informe.AppendLine("tejados (Lamina) apagados: " + tejados);

            foreach (string r in new[] { RaizPuentes, RutaSetHacker })
            {
                Transform t = ColocarS2.Buscar(r);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
                ColocarS2.Informe.AppendLine((t != null ? "apagado " : "FALTA ") + r);
            }

            Banquetas();
            ColocarS2.Guardar("s2t-limpieza");
        }

        /// <summary>Pone la textura T01 a cada banqueta (las cajas con el material de banqueta), con su tiling según su tamaño.</summary>
        public static void Banquetas()
        {
            Directory.CreateDirectory(CarpetaBanqueta);
            if (!File.Exists(OrigenBanqueta))
            {
                ColocarS2.Informe.AppendLine("FALTA " + OrigenBanqueta + ": banquetas sin cambiar (pedirla al chat de arte)");
                return;
            }
            if (!File.Exists(TexturaBanqueta) || new FileInfo(OrigenBanqueta).Length != new FileInfo(TexturaBanqueta).Length)
            {
                File.Copy(OrigenBanqueta, TexturaBanqueta, true);
                AssetDatabase.ImportAsset(TexturaBanqueta);
            }
            TextureImporter ti = AssetImporter.GetAtPath(TexturaBanqueta) as TextureImporter;
            if (ti != null && (ti.wrapMode != TextureWrapMode.Repeat || ti.anisoLevel < 8))
            {
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.anisoLevel = 8;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaBanqueta);
            Material baseMat = null;
            Dictionary<string, Material> porTamano = new Dictionary<string, Material>();
            int puestas = 0;
            foreach (Renderer r in ColocarS2.Todos().Select(t => t.GetComponent<Renderer>()).Where(r => r != null && r.sharedMaterial != null
                && (r.sharedMaterial.name == MaterialBanqueta || r.sharedMaterial.name.StartsWith(MaterialBanqueta + "_T01"))))
            {
                if (baseMat == null)
                {
                    baseMat = r.sharedMaterial.name == MaterialBanqueta ? r.sharedMaterial : AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(r.sharedMaterial).Replace(Path.GetFileName(AssetDatabase.GetAssetPath(r.sharedMaterial)), MaterialBanqueta + ".mat")) ?? r.sharedMaterial;
                }
                Vector3 s = r.bounds.size;
                Vector2 tiling = new Vector2(Mathf.Max(1f, Mathf.Round(s.x / LadoLosa)), Mathf.Max(1f, Mathf.Round(s.z / LadoLosa)));
                string clave = tiling.x + "x" + tiling.y;
                if (!porTamano.TryGetValue(clave, out Material m))
                {
                    string ruta = CarpetaBanqueta + MaterialBanqueta + "_T01_" + clave + ".mat";
                    m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                    if (m == null)
                    {
                        m = new Material(baseMat) { name = MaterialBanqueta + "_T01_" + clave };
                        AssetDatabase.CreateAsset(m, ruta);
                    }
                    m.SetTexture("_BaseMap", tex);
                    m.SetTextureScale("_BaseMap", tiling);
                    m.SetColor("_BaseColor", TonoBanqueta);
                    EditorUtility.SetDirty(m);
                    porTamano[clave] = m;
                }
                r.sharedMaterial = m;
                EditorUtility.SetDirty(r);
                puestas++;
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("banquetas con T01: " + puestas + " (" + porTamano.Count + " tamaños)");
        }
    }
}
