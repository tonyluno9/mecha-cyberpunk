using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-509 (cierre del pulido visual). (1) Greybox que T-506 no atrapó: los puestos de tacos del callejón de El Mercado (malla puesto_tacos con trompo de bloques,
    /// mostrador, bancos y garrafón lisos) pasan al modelo de arte C12, y todo modelo del kit con material liso (tinacos, letreros, transformadores, mobiliario)
    /// recibe el material con textura de T-506. (2) Neón Alto verde ácido y magenta: grading local más verde, farolas verde frío, niebla, charcos, anuncios y luces.
    /// Idempotente: lo creado cuelga de <see cref="NombreRaiz"/> y de hijos Visual_T509.
    /// </summary>
    public static class ColocarT509
    {
        public const string NombreRaiz = "T509_Pulido";
        public const string NombreVisual = "Visual_T509";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaPerfilNeon = "Assets/Arte/Exteriores/PerfilNeonAlto.asset";
        private const string CarpetaMateriales = "Assets/Arte/Exteriores/Materiales";
        /// <summary>Caja de Neón Alto (x, z) para las luces y farolas verdes.</summary>
        public static readonly Vector2 ZonaMin = new Vector2(34f, 3f);
        public static readonly Vector2 ZonaMax = new Vector2(60f, 93f);
        public static readonly Color FarolaVerde = new Color(0.62f, 1f, 0.8f);

        private static readonly StringBuilder Informe = new StringBuilder();

        /// <summary>Renderer visible con material liso (sin textura, sin brillo propio): greybox de cualquier malla, no solo primitivas.</summary>
        public static bool EsSinArte(Renderer r)
        {
            if (!(r is MeshRenderer) || !r.enabled || !r.gameObject.activeInHierarchy || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) { return false; }
            Vector3 tam = r.bounds.size;
            if (Mathf.Min(tam.x, Mathf.Min(tam.y, tam.z)) < ColocarT506.GrosorDecal) { return false; }
            if (ColocarT506.RaicesExcluidas.Contains(r.transform.root.name)) { return false; }
            for (Transform p = r.transform; p != null; p = p.parent)
            {
                if (p.name == "VistaPreviaApariencia") { return false; }
            }
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) { return true; }
                if (ColocarT506.MaterialesDecal.Any(d => m.name.Contains(d))) { continue; }
                if (!ColocarT506.TieneTextura(m) && !ColocarT506.EsEfecto(m)) { return true; }
            }
            return false;
        }

        [MenuItem("Mecha Cyberpunk/T-509/Cierre del pulido")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null) { Object.DestroyImmediate(vieja); }
            Transform raiz = new GameObject(NombreRaiz).transform;

            PonerPuestosTacos();
            RetexturarModelos();
            NeonAltoVerde(raiz);

            int quedan = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Count(EsSinArte);
            Informe.AppendLine("Renderers sin arte que quedan: " + quedan);
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t509-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-509 listo:\n" + Informe);
        }

        // ------------------------------------------------------------------ puestos de tacos del callejón

        private static void PonerPuestosTacos()
        {
            ArteT250.Modelo modelo = ArteT250.Buscar("C12_PuestoTacos");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaPrefab);
            Vector3 propio = prefab.GetComponent<BoxCollider>().size;
            int n = 0;
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList())
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (r.name != "Modelo" || !r.enabled || r.transform.parent == null || mf == null || mf.sharedMesh == null || mf.sharedMesh.name != "puesto_tacos") { continue; }
                Transform puesto = r.transform.parent;
                Bounds b = r.bounds;
                GameObject contenedor = new GameObject(NombreVisual);
                contenedor.transform.SetParent(puesto, false);
                contenedor.transform.SetSiblingIndex(0);
                contenedor.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
                contenedor.transform.rotation = Quaternion.identity;
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contenedor.transform);
                Object.DestroyImmediate(instancia.GetComponent<BoxCollider>());
                instancia.transform.localScale = Vector3.one * (b.size.y / propio.y);
                instancia.transform.rotation = Quaternion.Euler(0f, puesto.eulerAngles.y + 180f, 0f);
                instancia.transform.position = contenedor.transform.position;
                r.enabled = false;
                n++;
            }
            Informe.AppendLine("Puestos de tacos cambiados por C12: " + n);
        }

        // ------------------------------------------------------------------ resto de modelos lisos del kit

        private static void RetexturarModelos()
        {
            int n = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!EsSinArte(r)) { continue; }
                Material[] mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || ColocarT506.TieneTextura(m) || ColocarT506.EsEfecto(m) || ColocarT506.MaterialesDecal.Any(d => m.name.Contains(d))) { continue; }
                    mats[i] = ColocarT506.ArteDe(m);
                    cambio = true;
                }
                if (cambio)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                    n++;
                }
            }
            Informe.AppendLine("Modelos del kit retexturizados: " + n);
        }

        // ------------------------------------------------------------------ Neón Alto verde ácido y magenta

        private static Material Mat(string nombre)
        {
            string guid = AssetDatabase.FindAssets(nombre + " t:Material", new[] { CarpetaMateriales }).FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == nombre);
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static bool EnZona(Vector3 p)
        {
            return p.x >= ZonaMin.x && p.x <= ZonaMax.x && p.z >= ZonaMin.y && p.z <= ZonaMax.y;
        }

        private static void NeonAltoVerde(Transform raiz)
        {
            // Perfil local: filtro verde, balance de blancos hacia verde, sombras verde azulado, luces altas magenta.
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNeon);
            foreach (Object viejo in AssetDatabase.LoadAllAssetsAtPath(RutaPerfilNeon))
            {
                if (viejo is VolumeComponent) { Object.DestroyImmediate(viejo, true); }
            }
            perfil.components.Clear();
            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.45f);
            ca.saturation.Override(26f);
            ca.contrast.Override(8f);
            ca.colorFilter.Override(new Color(0.55f, 1.15f, 0.72f));
            WhiteBalance wb = perfil.Add<WhiteBalance>(true);
            wb.temperature.Override(-18f);
            wb.tint.Override(-80f);
            ShadowsMidtonesHighlights smh = perfil.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.55f, 1.0f, 0.85f, -0.15f));
            smh.midtones.Override(new Vector4(0.8f, 1.1f, 0.85f, 0f));
            smh.highlights.Override(new Vector4(1.05f, 0.85f, 1.05f, 0f));
            Vignette vi = perfil.Add<Vignette>(true);
            vi.intensity.Override(0.24f);
            foreach (VolumeComponent c in new VolumeComponent[] { ca, wb, smh, vi }) { AssetDatabase.AddObjectToAsset(c, perfil); }
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            Informe.AppendLine("Perfil de Neón Alto: filtro verde, balance verde (tinte -80), sombras verde azulado, luces altas magenta");

            // Farolas de la zona: verde frío en vez de naranja.
            int farolas = 0;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.transform.parent != null && l.transform.parent.name.StartsWith("Farola_") && l.name == "Luz" && EnZona(l.transform.position))
                {
                    l.color = FarolaVerde;
                    EditorUtility.SetDirty(l);
                    farolas++;
                }
            }
            Informe.AppendLine("Farolas de Neón Alto en verde frío: " + farolas);

            // Niebla verde más visible.
            Material niebla = Mat("Niebla_Verde");
            if (niebla != null)
            {
                niebla.SetColor("_BaseColor", new Color(0.14f, 0.75f, 0.38f, 0.34f));
                EditorUtility.SetDirty(niebla);
            }

            // Charcos que reflejan verde (uno de cada tres magenta).
            int charcos = 0;
            for (int i = 0; i < 8; i++)
            {
                Material c = Mat("Charco_Neon_" + i);
                if (c == null) { continue; }
                bool magenta = i % 3 == 1;
                c.SetColor("_BaseColor", magenta ? new Color(0.9f, 0.25f, 0.8f, 0.85f) : new Color(0.3f, 1f, 0.5f, 0.9f));
                EditorUtility.SetDirty(c);
                charcos++;
            }
            Informe.AppendLine("Charcos teñidos: " + charcos);

            // Anuncios y pantallas con más verde en la emisión.
            foreach (string n in new[] { "Brillo_CY02_PantallaVertical", "Brillo_N08", "Brillo_E03_PantallaGigante" })
            {
                Material m = Mat(n);
                if (m == null) { continue; }
                m.SetColor("_EmissionColor", new Color(0.7f, 1.3f, 0.85f) * (n.Contains("E03") ? 1.3f : 2.2f));
                EditorUtility.SetDirty(m);
            }

            // Luces de color a lo largo de la calle (verde ácido y magenta), sin sombra.
            Transform g = new GameObject("Luces_Verde").transform;
            g.SetParent(raiz, false);
            float[] zs = { 10f, 18f, 26f, 34f, 42f, 50f, 58f, 66f, 74f, 82f };
            for (int i = 0; i < zs.Length; i++)
            {
                bool magenta = i % 4 == 3;
                Light l = new GameObject(magenta ? "Luz_Magenta_" + i : "Luz_Verde_" + i).AddComponent<Light>();
                l.transform.SetParent(g, false);
                l.transform.position = new Vector3(i % 2 == 0 ? 43.5f : 50.5f, 4.2f, zs[i]);
                l.type = LightType.Point;
                l.color = magenta ? new Color(1f, 0.15f, 0.8f) : new Color(0.35f, 1f, 0.45f);
                l.intensity = magenta ? 55f : 80f;
                l.range = 14f;
                l.shadows = LightShadows.None;
            }
            Informe.AppendLine("Luces verde/magenta de calle: " + zs.Length);
        }
    }
}
