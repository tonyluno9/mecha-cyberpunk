using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// T-507 (pulido visual, punto 2: iluminación y color). Idempotente: baja la luz ambiental, la luna y la exposición de noche, tiñe y ensucia las
    /// fachadas del kit con una paleta terracota/turquesa/crema (materiales T507_* con variación por edificio), convierte las farolas en focos naranja
    /// hacia abajo (con sombra solo en las cercanas a la ruta de la demo) y da color grading a cada zona con su Volume local (Suburbios cálido,
    /// callejón y Tianguis magenta, Neón Alto verde) con transición suave (blendDistance).
    /// </summary>
    public static class ColocarT507
    {
        public const string NombreRaiz = "T507_LuzColor";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaCiclo = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        private const string RutaPerfilNoche = "Assets/_Proyecto/Mundo/Datos/PerfilNoche.asset";
        private const string RutaPerfilSuburbios = "Assets/Arte/Suburbios/PerfilSuburbios.asset";
        private const string RutaPerfilCallejon = "Assets/Arte/Callejon/PerfilCallejon.asset";
        private const string RutaPerfilTianguis = "Assets/Arte/Tianguis/PerfilTianguis.asset";
        private const string RutaPerfilNeon = "Assets/Arte/Exteriores/PerfilNeonAlto.asset";
        private const string CarpetaMateriales = "Assets/Arte/Exteriores/Materiales";

        public const float ExposicionNoche = 2.8f;
        public const float IntensidadLuna = 1.0f;
        public const int SombrasMaximas = 14;
        public const float RadioRuta = 26f;
        public const float AlturaFoco = 6.8f;

        /// <summary>Puntos de la ruta de la demo (casa, callejón, mercado, tianguis, Neón Alto) en x, z.</summary>
        public static readonly Vector2[] Ruta =
        {
            new Vector2(-59f, -77f), new Vector2(-44f, -77f), new Vector2(-73f, -59f), new Vector2(-80f, 0f), new Vector2(-33f, 0f),
            new Vector2(0f, 0f), new Vector2(38f, 0.5f), new Vector2(-22f, 20f), new Vector2(-33f, 29f), new Vector2(-36f, 80f),
            new Vector2(47f, 12f), new Vector2(47f, 47f), new Vector2(74.5f, 47f),
        };

        /// <summary>Paleta de fachadas (tinte multiplicado sobre la textura crema): terracota, turquesa, crema sucio y ocre.</summary>
        public static readonly Color[] Paleta =
        {
            new Color(0.93f, 0.50f, 0.36f), new Color(0.38f, 0.76f, 0.74f), new Color(0.86f, 0.76f, 0.56f),
            new Color(0.80f, 0.40f, 0.30f), new Color(0.30f, 0.60f, 0.60f), new Color(0.90f, 0.66f, 0.32f),
        };

        private static readonly string[] BasesFachada =
        {
            "C01_FachadaLocal_cy", "C02_FachadaVivienda_cy", "C03_FachadaPisoAlto_cy", "C20a_PisoVentana_cy", "C20b_PisoDosVentanas_cy",
            "C21_PisoLiso_cy", "C22_Cornisa_cy", "C23_Franja", "Fachada_Alta",
            "T506_Barrio_Edif_A", "T506_Barrio_Edif_B", "T506_Barrio_Edif_C", "T506_Barrio_Edif_D", "T506_Barrio_Pobre_A", "T506_Barrio_Pobre_B",
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-507/Luz y color")]
        public static void Colocar()
        {
            Informe.Clear();
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            foreach (GameObject viejo in escena.GetRootGameObjects().Where(g => g.name == NombreRaiz).ToList())
            {
                Object.DestroyImmediate(viejo);
            }
            Transform raiz = new GameObject(NombreRaiz).transform;

            AjustarLuzGlobal();
            TenirFachadas();
            ConvertirFarolas();
            ArreglarCharcos();
            CambiarBarriles();
            Grading(raiz);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T-507:\n" + Informe);
        }

        // ------------------------------------------------------------------ luz global

        private static void AjustarLuzGlobal()
        {
            ScriptableObject ciclo = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaCiclo);
            Type tipo = ciclo.GetType();
            // Ambiente de noche (claves 0, 0.27, 0.82 y 0.88): mucho más oscuro y menos azul brillante.
            Gradient g = (Gradient)Campo(tipo, "colorAmbiente").GetValue(ciclo);
            GradientColorKey[] claves = g.colorKeys;
            for (int i = 0; i < claves.Length; i++)
            {
                float t = claves[i].time;
                if (t < 0.28f || t > 0.87f)
                {
                    claves[i].color = new Color(0.38f, 0.33f, 0.52f);
                }
            }
            g.SetKeys(claves, g.alphaKeys);
            Campo(tipo, "colorAmbiente").SetValue(ciclo, g);
            // Luna: baja la intensidad de noche (horas 0 a 6 y 19.5 a 24).
            AnimationCurve c = (AnimationCurve)Campo(tipo, "intensidadLuz").GetValue(ciclo);
            Keyframe[] k = c.keys;
            for (int i = 0; i < k.Length; i++)
            {
                if (k[i].time < 6.5f || k[i].time > 19.4f)
                {
                    k[i].value = IntensidadLuna;
                }
            }
            c.keys = k;
            Campo(tipo, "intensidadLuz").SetValue(ciclo, c);
            EditorUtility.SetDirty(ciclo);

            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilNoche);
            if (noche.TryGet(out ColorAdjustments ca))
            {
                ca.postExposure.Override(ExposicionNoche);
                ca.saturation.Override(22f);
                EditorUtility.SetDirty(ca);
            }
            EditorUtility.SetDirty(noche);
            Informe.AppendLine("Luz global: ambiente de noche 0.38/0.33/0.52, luna " + IntensidadLuna + ", exposición de noche " + ExposicionNoche);
        }

        private static FieldInfo Campo(Type t, string nombre)
        {
            return t.GetField(nombre, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // ------------------------------------------------------------------ fachadas

        private static string BaseDe(string nombre)
        {
            if (!nombre.StartsWith("T507_"))
            {
                return nombre;
            }
            string resto = nombre.Substring(5);
            int guion = resto.LastIndexOf('_');
            return guion < 0 ? nombre : resto.Substring(0, guion);
        }

        private static int Hash(string s)
        {
            int h = 17;
            foreach (char ch in s)
            {
                h = h * 31 + ch;
            }
            return h & 0x7fffffff;
        }

        /// <summary>El edificio al que pertenece un renderer (ancestro Edificio_*), o una celda de 24 m si no hay.</summary>
        private static string Edificio(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name.StartsWith("Edificio_"))
                {
                    return p.name;
                }
            }
            Vector3 pos = t.position;
            return "celda_" + Mathf.FloorToInt(pos.x / 24f) + "_" + Mathf.FloorToInt(pos.z / 24f);
        }

        private static void TenirFachadas()
        {
            System.IO.Directory.CreateDirectory(CarpetaMateriales);
            var originales = new Dictionary<string, Material>();
            foreach (string b in BasesFachada)
            {
                string guid = AssetDatabase.FindAssets("t:Material " + b).FirstOrDefault(x => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(x)) == b);
                if (guid != null)
                {
                    originales[b] = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                }
            }
            var variantes = new Dictionary<string, Material>();
            int cambiados = 0;
            var usados = new HashSet<int>();
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Material[] mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null)
                    {
                        continue;
                    }
                    string baseNombre = BaseDe(mats[i].name);
                    if (!originales.TryGetValue(baseNombre, out Material original))
                    {
                        continue;
                    }
                    string edificio = Edificio(r.transform);
                    int idx = Hash(edificio) % Paleta.Length;
                    usados.Add(idx);
                    string nombre = "T507_" + baseNombre + "_" + idx;
                    if (!variantes.TryGetValue(nombre, out Material variante))
                    {
                        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
                        variante = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                        if (variante == null)
                        {
                            variante = new Material(original) { name = nombre };
                            AssetDatabase.CreateAsset(variante, ruta);
                        }
                        Color orig = baseNombre.StartsWith("T506_Barrio_") ? Color.white : original.GetColor("_BaseColor");
                        variante.SetColor("_BaseColor", new Color(orig.r * Paleta[idx].r, orig.g * Paleta[idx].g, orig.b * Paleta[idx].b, orig.a));
                        if (variante.HasProperty("_Smoothness"))
                        {
                            variante.SetFloat("_Smoothness", Mathf.Min(0.12f, original.GetFloat("_Smoothness")));
                        }
                        EditorUtility.SetDirty(variante);
                        variantes[nombre] = variante;
                    }
                    if (mats[i] != variante)
                    {
                        mats[i] = variante;
                        cambio = true;
                    }
                }
                if (cambio)
                {
                    r.sharedMaterials = mats;
                    cambiados++;
                }
            }
            Informe.AppendLine("Fachadas teñidas: " + cambiados + " renderers, " + variantes.Count + " materiales T507_*, " + usados.Count + " colores usados");
        }

        // ------------------------------------------------------------------ farolas

        private static float DistRuta(Vector3 p)
        {
            float d = float.MaxValue;
            foreach (Vector2 v in Ruta)
            {
                d = Mathf.Min(d, Vector2.Distance(new Vector2(p.x, p.z), v));
            }
            return d;
        }

        private static void ConvertirFarolas()
        {
            var farolas = new List<(Light luz, float dist)>();
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.transform.parent != null && l.transform.parent.name.StartsWith("Farola_") && l.name == "Luz")
                {
                    farolas.Add((l, DistRuta(l.transform.position)));
                }
            }
            int sombras = 0;
            foreach (var f in farolas.OrderBy(x => x.dist))
            {
                Light l = f.luz;
                l.type = LightType.Spot;
                l.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                l.spotAngle = 130f;
                l.innerSpotAngle = 35f;
                l.range = 26f;
                l.color = new Color(1f, 0.6f, 0.2f);
                l.intensity = 700f;
                l.transform.position = new Vector3(l.transform.position.x, AlturaFoco, l.transform.position.z);
                l.shadowNearPlane = 0.3f;
                l.shadowBias = 0.02f;
                bool con = sombras < SombrasMaximas && f.dist <= RadioRuta;
                l.shadows = con ? LightShadows.Soft : LightShadows.None;
                l.shadowStrength = 0.95f;
                if (con)
                {
                    sombras++;
                    UniversalAdditionalLightData d = l.GetComponent<UniversalAdditionalLightData>() ?? l.gameObject.AddComponent<UniversalAdditionalLightData>();
                    SerializedObject sd = new SerializedObject(d); sd.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow; sd.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(l);
            }
            Informe.AppendLine("Farolas: " + farolas.Count + " focos naranja hacia abajo, " + sombras + " con sombra (cerca de la ruta)");
        }

        // ------------------------------------------------------------------ restos de T-506

        private const string RutaTexturaCharco = "Assets/Arte/Suburbios/Texturas/Charco_Redondo.png";

        /// <summary>Los charcos negros de la calle (cuadrados opacos, giran y se ven como planos inclinados) pasan a un óvalo suave transparente.</summary>
        private static void ArreglarCharcos()
        {
            string ruta = CarpetaMateriales + "/T507_Charco.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, ruta);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaCharco));
            mat.SetColor("_BaseColor", new Color(0.02f, 0.025f, 0.04f, 0.92f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(mat);
            int n = 0;
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r.sharedMaterial != null && (r.sharedMaterial.name == "Barrio_Charco" || r.sharedMaterial.name == "T507_Charco"))
                {
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    n++;
                }
            }
            Informe.AppendLine("Charcos negros con borde suave: " + n);
        }

        /// <summary>Los dos barriles lisos del callejón (cilindros de T-506) se cambian por el tambo C16 del kit.</summary>
        private static void CambiarBarriles()
        {
            string ruta = ImportarT500.RutasPrefab.FirstOrDefault(r => System.IO.Path.GetFileNameWithoutExtension(r) == "C16_Tambo");
            ruta = ruta ?? ArteNoche.Buscar("C16_Tambo").RutaPrefab;
            GameObject prefab = ruta != null ? AssetDatabase.LoadAssetAtPath<GameObject>(ruta) : null;
            if (prefab == null)
            {
                Informe.AppendLine("FALTA el prefab C16_Tambo");
                return;
            }
            int n = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.name.StartsWith("Barril") && x.GetComponent<MeshRenderer>() != null && x.GetComponent<MeshRenderer>().enabled && x.GetComponent<MeshFilter>().sharedMesh.name == "Cylinder").ToList())
            {
                GameObject tambo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t.parent);
                tambo.name = "T507_Tambo_" + t.name;
                tambo.transform.position = new Vector3(t.position.x, 0f, t.position.z);
                tambo.transform.rotation = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
                foreach (Collider c in tambo.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(c);
                }
                t.GetComponent<MeshRenderer>().enabled = false;
                n++;
            }
            Informe.AppendLine("Barriles lisos cambiados por tambo C16: " + n);
        }

        // ------------------------------------------------------------------ color grading por zona

        private static VolumeProfile Perfil(string ruta, float exposicion, float saturacion, Color filtro, float temperatura, float tinte, float vineta)
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, ruta);
            }
            foreach (Object viejo in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (viejo is VolumeComponent)
                {
                    Object.DestroyImmediate(viejo, true);
                }
            }
            perfil.components.Clear();
            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.postExposure.Override(exposicion);
            ca.saturation.Override(saturacion);
            ca.colorFilter.Override(filtro);
            WhiteBalance wb = perfil.Add<WhiteBalance>(true);
            wb.temperature.Override(temperatura);
            wb.tint.Override(tinte);
            Vignette vi = perfil.Add<Vignette>(true);
            vi.intensity.Override(vineta);
            AssetDatabase.AddObjectToAsset(ca, perfil);
            AssetDatabase.AddObjectToAsset(wb, perfil);
            AssetDatabase.AddObjectToAsset(vi, perfil);
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        private static void Grading(Transform raiz)
        {
            Perfil(RutaPerfilSuburbios, 0.25f, 18f, new Color(1.12f, 0.96f, 0.80f), 32f, 4f, 0.28f);
            Perfil(RutaPerfilCallejon, -0.3f, 16f, new Color(1.15f, 0.80f, 1.10f), 0f, 48f, 0.30f);
            Perfil(RutaPerfilNeon, 0.1f, 14f, new Color(0.78f, 1.12f, 0.88f), -8f, -50f, 0.24f);
            VolumeProfile tianguis = Perfil(RutaPerfilTianguis, 0.1f, 16f, new Color(1.08f, 0.90f, 1.06f), 8f, 30f, 0.26f);

            AjustarVolumen("VolumenSuburbios", 22f);
            AjustarVolumen("VolumenCallejon", 8f);
            AjustarVolumen("VolumenNeonAlto", 8f);

            GameObject go = new GameObject("VolumenTianguis");
            go.transform.SetParent(raiz, false);
            go.transform.position = new Vector3(0f, 12f, 0f);
            BoxCollider caja = go.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(74f, 30f, 22f);
            Volume v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = 16f;
            v.priority = 6f;
            v.sharedProfile = tianguis;
            Type tipo = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Ambiente.VolumenSigueNoche")).FirstOrDefault(t => t != null);
            if (tipo != null)
            {
                Component c = go.AddComponent(tipo);
                SerializedObject so = new SerializedObject(c);
                GameObject noche = GameObject.Find("PosprocesoNoche");
                so.FindProperty("origen").objectReferenceValue = noche != null ? noche.GetComponent<Volume>() : null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Informe.AppendLine("Color grading: Suburbios cálido, Callejón y Tianguis magenta, Neón Alto verde; blendDistance 22/8/16/8");
        }

        private static void AjustarVolumen(string nombre, float blend)
        {
            GameObject go = GameObject.Find(nombre);
            if (go == null)
            {
                Informe.AppendLine("FALTA " + nombre);
                return;
            }
            Volume v = go.GetComponent<Volume>();
            v.blendDistance = blend;
            EditorUtility.SetDirty(v);
        }
    }
}
