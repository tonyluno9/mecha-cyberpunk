using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace MechaCyberpunk.VR.Editor
{
    /// <summary>
    /// VR1: deja el proyecto listo para PCVR con OpenXR (Meta Quest 3 por Quest Link):
    /// XR Plugin Management con el loader de OpenXR en Standalone (sin arrancar solo: lo arranca SistemaVR),
    /// perfiles de controles Oculus Touch y Meta Quest Touch Plus, single pass instanced, y el asset RecursosVR.
    /// Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ConfigurarVR.Configurar
    /// </summary>
    public static class ConfigurarVR
    {
        public const string CarpetaSamples = "Assets/Samples/XR Interaction Toolkit/3.7.0";
        public const string RutaRecursos = "Assets/_Proyecto/VR/Resources/RecursosVR.asset";
        public const string CarpetaBuildVR = "Builds/VR";

        [MenuItem("Mecha Cyberpunk/VR/Configurar VR")]
        public static void Configurar()
        {
            ConfigurarXRManagement();
            ConfigurarOpenXR();
            CrearRecursos();
            AssetDatabase.SaveAssets();
            Debug.Log("[VR] Configuración lista.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void ConfigurarXRManagement()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget porPlataforma);
            if (porPlataforma == null)
            {
                Directory.CreateDirectory("Assets/XR");
                porPlataforma = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(porPlataforma, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, porPlataforma, true);
            }
            if (!porPlataforma.HasSettingsForBuildTarget(BuildTargetGroup.Standalone))
            {
                porPlataforma.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Standalone);
            }
            if (!porPlataforma.HasManagerSettingsForBuildTarget(BuildTargetGroup.Standalone))
            {
                porPlataforma.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
            }
            XRGeneralSettings generales = porPlataforma.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            generales.InitManagerOnStart = false;
            bool ok = XRPackageMetadataStore.AssignLoader(generales.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Standalone);
            EditorUtility.SetDirty(generales);
            EditorUtility.SetDirty(generales.Manager);
            EditorUtility.SetDirty(porPlataforma);
            Debug.Log("[VR] Loader OpenXR en Standalone: " + ok + " (loaders: " + generales.Manager.activeLoaders.Count + ")");
        }

        private static void ConfigurarOpenXR()
        {
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
            OpenXRSettings ajustes = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (ajustes == null)
            {
                Debug.LogError("[VR] No hay ajustes de OpenXR para Standalone.");
                return;
            }
            // VR13: Multi-pass. Con Single Pass Instanced varios shaders propios (neón, letreros, charcos) no se dibujaban en el ojo derecho.
            ajustes.renderMode = ModoRender;
            Activar<OculusTouchControllerProfile>(ajustes);
            Activar<MetaQuestTouchPlusControllerProfile>(ajustes);
            EditorUtility.SetDirty(ajustes);
        }

        private static void Activar<T>(OpenXRSettings ajustes) where T : OpenXRFeature
        {
            T f = ajustes.GetFeature<T>();
            if (f == null)
            {
                Debug.LogWarning("[VR] No encontré el perfil " + typeof(T).Name);
                return;
            }
            f.enabled = true;
            EditorUtility.SetDirty(f);
            Debug.Log("[VR] Perfil activo: " + typeof(T).Name);
        }

        private static void CrearRecursos()
        {
            RecursosVR r = AssetDatabase.LoadAssetAtPath<RecursosVR>(RutaRecursos);
            if (r == null)
            {
                r = ScriptableObject.CreateInstance<RecursosVR>();
                AssetDatabase.CreateAsset(r, RutaRecursos);
            }
            r.controlIzquierdo = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaSamples + "/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab");
            r.controlDerecho = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaSamples + "/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab");
            r.vinetaTunel = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaSamples + "/Starter Assets/TunnelingVignette/TunnelingVignette.prefab");
            r.simuladorXR = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaSamples + "/XR Device Simulator/XR Device Simulator.prefab");
            r.accionesXRI = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(CarpetaSamples + "/Starter Assets/XRI Default Input Actions.inputactions");
            EditorUtility.SetDirty(r);
            Debug.Log("[VR] RecursosVR: controles " + (r.controlIzquierdo != null) + "/" + (r.controlDerecho != null)
                + ", viñeta " + (r.vinetaTunel != null) + ", simulador " + (r.simuladorXR != null) + ", acciones " + (r.accionesXRI != null));
        }

        /// <summary>
        /// Build VR de PC: las mismas escenas que ConstruirJuego (Inicio, Menu_Calles, Ciudad_Prologo) en Builds/VR/CyberCity.exe.
        /// Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ConfigurarVR.ConstruirVR
        /// </summary>
        [MenuItem("Mecha Cyberpunk/VR/Construir build VR (Builds/VR)")]
        public static void ConstruirVR()
        {
            ConstruirVREn(CarpetaBuildVR);
        }

        /// <summary>Igual que <see cref="ConstruirVR"/> pero en Builds/VR2 (para no pisar el build que se está probando).
        /// Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ConfigurarVR.ConstruirVR2</summary>
        public static void ConstruirVR2()
        {
            ConstruirVREn("Builds/VR2");
        }

        /// <summary>Build VR en Builds/VR3 (tercera tanda; VR y VR2 se están probando).
        /// Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ConfigurarVR.ConstruirVR3</summary>
        public static void ConstruirVR3()
        {
            ConstruirVREn("Builds/VR3");
        }

        /// <summary>VR13: modo de dibujo estéreo de OpenXR en PC.</summary>
        public const OpenXRSettings.RenderMode ModoRender = OpenXRSettings.RenderMode.MultiPass;

        /// <summary>VR13: pone OpenXR (Standalone) en Multi-pass y guarda. Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ConfigurarVR.PonerMultiPass</summary>
        [MenuItem("Mecha Cyberpunk/VR/Poner Multi-pass (VR13)")]
        public static void PonerMultiPass()
        {
            OpenXRSettings ajustes = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (ajustes == null)
            {
                Debug.LogError("[VR] No hay ajustes de OpenXR para Standalone.");
                return;
            }
            if (ajustes.renderMode != ModoRender)
            {
                ajustes.renderMode = ModoRender;
                EditorUtility.SetDirty(ajustes);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("[VR] OpenXR Standalone renderMode = " + ajustes.renderMode);
        }

        /// <summary>Build VR en Builds/VR4 (cuarta tanda). Batchmode: -executeMethod MechaCyberpunk.VR.Editor.ConfigurarVR.ConstruirVR4</summary>
        public static void ConstruirVR4()
        {
            ConstruirVREn("Builds/VR4");
        }

        private static void ConstruirVREn(string carpeta)
        {
            PonerMultiPass();
            Directory.CreateDirectory(carpeta);
            System.Type tipo = typeof(MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo);
            MethodInfo preparar = tipo.GetMethod("PrepararBuildCyberfix", BindingFlags.NonPublic | BindingFlags.Static);
            preparar?.Invoke(null, new object[] { carpeta });
            var opciones = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.EscenaInicio,
                    MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.EscenaMenuCalles,
                    MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.EscenaJuego,
                },
                locationPathName = Path.Combine(carpeta, MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.NombreEjecutableJuego),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            BuildSummary resumen = BuildPipeline.BuildPlayer(opciones).summary;
            Debug.Log("[VR] ConstruirVR: " + resumen.result + ", " + resumen.totalErrors + " errores, "
                + (resumen.totalSize / (1024 * 1024)) + " MB en " + resumen.totalTime.TotalSeconds.ToString("F0") + " s");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(resumen.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
