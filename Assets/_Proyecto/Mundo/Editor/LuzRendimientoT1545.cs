using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1545: luz como las referencias (K20 a K34) y 50 fps. Dos métodos, los dos idempotentes y con <c>Debug.Log("[T-1545] ...")</c>:
    /// <see cref="Diagnostico"/> solo mide y escribe la tabla de lo que hay (no cambia nada); <see cref="Aplicar"/> deja en los assets la niebla
    /// azul ligera, el piso mojado, las sombras y el SSAO baratos, y el GPU instancing de los materiales de props.
    /// Las luces (sodio, neón, presupuesto por cámara) y el corte del decorado van en código de ejecución (LucesCiudad, CulingDecorado): no tocan la escena.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.LuzRendimientoT1545.Aplicar
    /// </summary>
    public static class LuzRendimientoT1545
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaCiclo = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        private const string RutaUrpPc = "Assets/Settings/PC_RPAsset.asset";
        private const string RutaUrpMedio = "Assets/Settings/Medio_RPAsset.asset";
        private const string RutaRendererPc = "Assets/Settings/PC_Renderer.asset";
        private const string RutaRendererMedio = "Assets/Settings/Medio_Renderer.asset";

        // Niebla de noche: azul ligera como K20 a K31 (antes gris azulado 0.30, 0.40, 0.50 y densidad 0.009, que a 60 a 80 m ya tapaba el 30 a 50 %).
        private static readonly Color NieblaNoche = new Color(0.15f, 0.25f, 0.42f);
        private const float DensidadAntes = 0.009f;
        private const float DensidadNoche = 0.0055f;
        private static readonly string[] ClavesNochePorGradiente = { "key0", "key1", "key6", "key7" };

        // Piso mojado: asfalto con brillo alto (los reflejos salen de las sondas y los charcos planares que ya existen; URP 17.3 no trae SSR).
        private const float BrilloAsfaltoMojado = 0.8f;
        private const float BrilloBanquetaMojada = 0.5f;
        private static readonly string[] MaterialesAsfalto =
        {
            "Assets/Arte/Mundo/S2c/S2c_Asfalto.mat",
            "Assets/Arte/Exteriores/Materiales/T535_AsfaltoAfueras_120x14.mat",
            "Assets/Arte/Exteriores/Materiales/T535_AsfaltoAfueras_14x101.mat",
        };
        private const string CarpetaBanquetas = "Assets/Arte/Calle/Banqueta";

        // Sombras: la luna con dos cascadas y mapa de 2048; el resto de luces sin sombra.
        private const int CascadasLuna = 2;
        private const int ResolucionSombraPc = 2048;
        private const int CalidadSombraSuaveMedia = 2;

        private static readonly string[] CarpetasInstancing = { "Assets/_Proyecto/Mundo/Materiales", "Assets/Arte/Calle" };

        // ---------------------------------------------------------------- diagnóstico

        [MenuItem("Mecha Cyberpunk/T-1545/1 Diagnostico")]
        public static void Diagnostico()
        {
            if (!AbrirEscena())
            {
                return;
            }
            StringBuilder sb = new StringBuilder("[T-1545] DIAGNOSTICO\n");
            Luces(sb);
            Renderizadores(sb);
            Sondas(sb);
            Assets(sb);
            Debug.Log(sb.ToString());
        }

        private static bool AbrirEscena()
        {
            if (SceneManager.GetActiveScene().path == RutaEscena)
            {
                return true;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[T-1545] Cancelado: hay una escena sin guardar.");
                return false;
            }
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            return true;
        }

        private static void Luces(StringBuilder sb)
        {
            Light[] luces = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine("LUCES: " + luces.Length + " (" + luces.Count(l => l.enabled && l.gameObject.activeInHierarchy) + " prendidas)");
            foreach (LightType tipo in new[] { LightType.Directional, LightType.Spot, LightType.Point })
            {
                Light[] g = luces.Where(l => l.type == tipo).ToArray();
                if (g.Length > 0)
                {
                    sb.AppendLine("  " + tipo + ": " + g.Length + ", alcance " + g.Min(l => l.range).ToString("F1") + " a " + g.Max(l => l.range).ToString("F1")
                        + ", intensidad " + g.Min(l => l.intensity).ToString("F2") + " a " + g.Max(l => l.intensity).ToString("F2")
                        + ", con sombra " + g.Count(l => l.shadows != LightShadows.None) + ", mapa de luz " + g.Count(l => l.lightmapBakeType != LightmapBakeType.Realtime) + " horneadas");
                }
            }
            Light[] grandes = luces.Where(l => l.type == LightType.Point && l.range >= 12f).ToArray();
            sb.AppendLine("  puntuales de alcance 12 m o más: " + grandes.Length + " (rellenos de luna de azoteas: cada una cubre media cuadra)");
            sb.AppendLine("  colores más repetidos: " + string.Join(", ", luces.Where(l => l.type != LightType.Directional)
                .GroupBy(l => ColorUtility.ToHtmlStringRGB(l.color)).OrderByDescending(g => g.Count()).Take(6).Select(g => "#" + g.Key + "=" + g.Count())));
            sb.AppendLine("  nota: URP Forward+ pasa a lo más 256 luces por cámara; sin LucesCiudad se pierden las sobrantes sin criterio");
        }

        private static void Renderizadores(StringBuilder sb)
        {
            MeshRenderer[] mallas = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int sombra = 0;
            int estaticos = 0;
            int conLod = 0;
            int conSondas = 0;
            HashSet<Material> sinInstancing = new HashSet<Material>();
            HashSet<Material> todos = new HashSet<Material>();
            foreach (MeshRenderer r in mallas)
            {
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    sombra++;
                }
                if ((GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.BatchingStatic) != 0)
                {
                    estaticos++;
                }
                if (r.GetComponentInParent<LODGroup>(true) != null)
                {
                    conLod++;
                }
                if (r.reflectionProbeUsage != ReflectionProbeUsage.Off)
                {
                    conSondas++;
                }
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null)
                    {
                        continue;
                    }
                    todos.Add(m);
                    if (!m.enableInstancing)
                    {
                        sinInstancing.Add(m);
                    }
                }
            }
            sb.AppendLine("RENDERERS: " + mallas.Length + " MeshRenderer, " + sombra + " proyectan sombra, " + estaticos + " con static batching, " + conLod + " bajo un LODGroup, " + conSondas + " usan sondas");
            sb.AppendLine("  materiales distintos: " + todos.Count + ", sin GPU instancing: " + sinInstancing.Count + " (con SRP Batcher el instancing casi no cambia nada; ayuda si se activa GPU Resident Drawer)");
            sb.AppendLine("  LODGroups: " + Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length);
            int grandesSinLod = mallas.Count(r => r.bounds.size.magnitude > 30f && r.GetComponentInParent<LODGroup>(true) == null);
            sb.AppendLine("  edificios o piezas grandes (diagonal > 30 m) sin LOD: " + grandesSinLod);
        }

        private static void Sondas(StringBuilder sb)
        {
            ReflectionProbe[] sondas = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine("SONDAS DE REFLEJO: " + sondas.Length + " (" + sondas.Count(s => s.mode == ReflectionProbeMode.Realtime) + " en tiempo real)");
            sb.AppendLine("NIEBLA ahora en la escena: " + (RenderSettings.fog ? RenderSettings.fogMode + " densidad " + RenderSettings.fogDensity : "apagada") + ", color " + ColorUtility.ToHtmlStringRGB(RenderSettings.fogColor));
        }

        private static void Assets(StringBuilder sb)
        {
            ConfigCicloDiaVista(sb);
            foreach (string ruta in new[] { RutaUrpPc, RutaUrpMedio })
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                if (asset == null)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(asset);
                sb.AppendLine("URP " + ruta + ": cascadas " + so.FindProperty("m_ShadowCascadeCount").intValue + ", mapa de sol " + so.FindProperty("m_MainLightShadowmapResolution").intValue
                    + ", distancia " + so.FindProperty("m_ShadowDistance").floatValue + ", suave " + so.FindProperty("m_SoftShadowQuality").intValue
                    + ", luces por objeto " + so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue + ", GPU Resident Drawer " + so.FindProperty("m_GPUResidentDrawerMode").intValue);
            }
            foreach (string ruta in new[] { RutaRendererPc, RutaRendererMedio })
            {
                SerializedProperty abajo = BuscarSsao(ruta, out SerializedObject so2);
                sb.AppendLine("SSAO " + ruta + ": " + (abajo == null ? "no hay" : "mitad de resolución " + abajo.boolValue));
                so2?.Dispose();
            }
            foreach (string ruta in MaterialesAsfalto)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                sb.AppendLine("ASFALTO " + System.IO.Path.GetFileName(ruta) + ": brillo " + (m != null && m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness").ToString("F2") : "n/d"));
            }
        }

        private static void ConfigCicloDiaVista(StringBuilder sb)
        {
            Object ciclo = AssetDatabase.LoadAssetAtPath<Object>(RutaCiclo);
            if (ciclo == null)
            {
                return;
            }
            SerializedObject so = new SerializedObject(ciclo);
            AnimationCurve curva = so.FindProperty("densidadNiebla").animationCurveValue;
            sb.AppendLine("CICLO " + RutaCiclo + ": densidad de niebla 23 h " + curva.Evaluate(23f).ToString("F4") + ", 12 h " + curva.Evaluate(12f).ToString("F4")
                + ", color de noche #" + ColorUtility.ToHtmlStringRGB(so.FindProperty("colorNiebla.key0").colorValue));
        }

        // ---------------------------------------------------------------- aplicar

        [MenuItem("Mecha Cyberpunk/T-1545/2 Aplicar (niebla, piso mojado, sombras, SSAO, instancing)")]
        public static void Aplicar()
        {
            AplicarNiebla();
            AplicarPisoMojado();
            AplicarSombrasYSsao();
            AplicarInstancing();
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1545] Aplicar listo. Las luces (sodio, neón, presupuesto) y el corte del decorado actúan en ejecución; la escena no se toca.");
        }

        /// <summary>Niebla de noche azul y ligera: color de las claves de noche y densidad 0.009 a 0.0055.</summary>
        public static void AplicarNiebla()
        {
            Object ciclo = AssetDatabase.LoadAssetAtPath<Object>(RutaCiclo);
            if (ciclo == null)
            {
                Debug.LogError("[T-1545] Falta " + RutaCiclo);
                return;
            }
            SerializedObject so = new SerializedObject(ciclo);
            float antes = so.FindProperty("densidadNiebla").animationCurveValue.Evaluate(23f);
            string colorAntes = ColorUtility.ToHtmlStringRGB(so.FindProperty("colorNiebla.key0").colorValue);

            foreach (string clave in ClavesNochePorGradiente)
            {
                so.FindProperty("colorNiebla." + clave).colorValue = NieblaNoche;
            }
            AnimationCurve curva = so.FindProperty("densidadNiebla").animationCurveValue;
            Keyframe[] claves = curva.keys;
            for (int i = 0; i < claves.Length; i++)
            {
                if (Mathf.Abs(claves[i].value - DensidadAntes) < 0.0001f)
                {
                    claves[i].value = DensidadNoche;
                }
            }
            curva.keys = claves;
            so.FindProperty("densidadNiebla").animationCurveValue = curva;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ciclo);
            Debug.Log("[T-1545] Niebla de noche: densidad " + antes.ToString("F4") + " -> " + curva.Evaluate(23f).ToString("F4") + ", color #" + colorAntes + " -> #" + ColorUtility.ToHtmlStringRGB(NieblaNoche)
                + ". Visibilidad a 70 m: " + Visibilidad(antes, 70f).ToString("P0") + " -> " + Visibilidad(curva.Evaluate(23f), 70f).ToString("P0")
                + "; a 150 m: " + Visibilidad(antes, 150f).ToString("P0") + " -> " + Visibilidad(curva.Evaluate(23f), 150f).ToString("P0"));
        }

        // Fracción de la escena que sigue visible con niebla exponencial cuadrática.
        private static float Visibilidad(float densidad, float distancia)
        {
            float x = densidad * distancia;
            return Mathf.Exp(-x * x);
        }

        /// <summary>Asfalto con brillo alto y banquetas con brillo medio.</summary>
        public static void AplicarPisoMojado()
        {
            int cambiados = 0;
            foreach (string ruta in MaterialesAsfalto)
            {
                cambiados += PonerBrillo(AssetDatabase.LoadAssetAtPath<Material>(ruta), BrilloAsfaltoMojado);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { CarpetaBanquetas }))
            {
                cambiados += PonerBrillo(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)), BrilloBanquetaMojada, true);
            }
            Debug.Log("[T-1545] Piso mojado: " + cambiados + " materiales con más brillo (asfalto " + BrilloAsfaltoMojado + ", banqueta " + BrilloBanquetaMojada + "). Reflejos: sondas y charcos planares existentes; URP 17.3 no trae SSR.");
        }

        // soloSubir: las banquetas que ya brillan más no se bajan.
        private static int PonerBrillo(Material m, float brillo, bool soloSubir = false)
        {
            if (m == null || !m.HasProperty("_Smoothness"))
            {
                return 0;
            }
            float actual = m.GetFloat("_Smoothness");
            if (Mathf.Approximately(actual, brillo) || (soloSubir && actual >= brillo))
            {
                return 0;
            }
            m.SetFloat("_Smoothness", brillo);
            EditorUtility.SetDirty(m);
            Debug.Log("[T-1545]   " + m.name + ": brillo " + actual.ToString("F2") + " -> " + brillo.ToString("F2"));
            return 1;
        }

        /// <summary>Luna con 2 cascadas y mapa de 2048 en el asset PC, y SSAO a mitad de resolución en los dos renderers.</summary>
        public static void AplicarSombrasYSsao()
        {
            foreach (string ruta in new[] { RutaUrpPc, RutaUrpMedio })
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                if (asset == null)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(asset);
                string antes = so.FindProperty("m_ShadowCascadeCount").intValue + " cascadas, mapa " + so.FindProperty("m_MainLightShadowmapResolution").intValue + ", suave " + so.FindProperty("m_SoftShadowQuality").intValue;
                so.FindProperty("m_ShadowCascadeCount").intValue = CascadasLuna;
                if (so.FindProperty("m_MainLightShadowmapResolution").intValue > ResolucionSombraPc)
                {
                    so.FindProperty("m_MainLightShadowmapResolution").intValue = ResolucionSombraPc;
                }
                if (so.FindProperty("m_SoftShadowQuality").intValue > CalidadSombraSuaveMedia)
                {
                    so.FindProperty("m_SoftShadowQuality").intValue = CalidadSombraSuaveMedia;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                Debug.Log("[T-1545] URP " + ruta + ": " + antes + " -> " + CascadasLuna + " cascadas, mapa " + so.FindProperty("m_MainLightShadowmapResolution").intValue
                    + ", suave " + so.FindProperty("m_SoftShadowQuality").intValue);
            }
            foreach (string ruta in new[] { RutaRendererPc, RutaRendererMedio })
            {
                SerializedProperty mitad = BuscarSsao(ruta, out SerializedObject so);
                if (mitad == null)
                {
                    continue;
                }
                bool antes = mitad.boolValue;
                mitad.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(so.targetObject);
                AssetDatabase.SaveAssetIfDirty(so.targetObject);
                Debug.Log("[T-1545] SSAO " + ruta + ": mitad de resolución " + antes + " -> True");
                so.Dispose();
            }
        }

        // Propiedad Downsample del SSAO (sub-asset del renderer). El SerializedObject lo cierra quien llama.
        private static SerializedProperty BuscarSsao(string rutaRenderer, out SerializedObject so)
        {
            so = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(rutaRenderer))
            {
                if (o is ScriptableRendererFeature && o.name == "ScreenSpaceAmbientOcclusion")
                {
                    so = new SerializedObject(o);
                    return so.FindProperty("m_Settings.Downsample");
                }
            }
            return null;
        }

        /// <summary>GPU instancing en los materiales URP de los props de calle y del mundo.</summary>
        public static void AplicarInstancing()
        {
            int cambiados = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", CarpetasInstancing))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.enableInstancing || m.shader == null || !m.shader.name.StartsWith("Universal Render Pipeline/"))
                {
                    continue;
                }
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                cambiados++;
            }
            Debug.Log("[T-1545] GPU instancing activado en " + cambiados + " materiales URP de " + string.Join(", ", CarpetasInstancing));
        }
    }
}
