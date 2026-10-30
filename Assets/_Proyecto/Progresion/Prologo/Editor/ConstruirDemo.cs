using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using MechaCyberpunk.Progresion.Editor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Build de Windows de la demo (T-137): pone en Build Settings las escenas de DatosEscenas_Demo en orden (título, ciudad,
    /// flashback), fija el nombre del producto y la pantalla, y construye Builds/Demo/MechaCyberpunk.exe (64 bits).
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.Construir
    /// Solo Build Settings: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.AplicarEscenas
    /// </summary>
    public static class ConstruirDemo
    {
        public const string RutaDatosEscenasDemo = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";
        public const string CarpetaBuild = "Builds/Demo";
        public const string NombreEjecutable = "MechaCyberpunk.exe";   // del Construir() viejo del mecha
        public const string NombreEjecutableJuego = NombreJuego.Ejecutable;
        private const string ExeViejo = "MechaCyberpunk.exe";
        private const string DatosViejo = "MechaCyberpunk_Data";
        public const string NombreProducto = "Mecha Cyberpunk (demo)";

        /// <summary>Las escenas de la demo en el orden de Build Settings: la de índice 0 es con la que arranca el build.</summary>
        public static string[] EscenasDemo()
        {
            DatosEscenas datos = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenasDemo);
            if (datos == null)
            {
                throw new FileNotFoundException("No está DatosEscenas_Demo", RutaDatosEscenasDemo);
            }
            return new[] { datos.RutaTitulo, datos.RutaCiudad, datos.RutaFlashback };
        }

        [MenuItem("Mecha Cyberpunk/Demo/Aplicar Build Settings")]
        public static void AplicarEscenas()
        {
            string[] rutas = EscenasDemo();
            foreach (string ruta in rutas)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null)
                {
                    throw new FileNotFoundException("Falta una escena de la demo", ruta);
                }
            }
            EditorBuildSettings.scenes = rutas.Select(ruta => new EditorBuildSettingsScene(ruta, true)).ToArray();
            AplicarPantalla();
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] ConstruirDemo: Build Settings con " + string.Join(", ", rutas));
        }

        // Pantalla completa a la resolución nativa; Alt+Enter pasa a ventana (T-137, punto 2).
        private static void AplicarPantalla()
        {
            PlayerSettings.productName = NombreProducto;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
        }

        [MenuItem("Mecha Cyberpunk/Demo/Construir build de Windows")]
        public static void Construir()
        {
            AplicarEscenas();
            Directory.CreateDirectory(CarpetaBuild);
            BuildPlayerOptions opciones = new BuildPlayerOptions
            {
                scenes = EscenasDemo(),
                locationPathName = Path.Combine(CarpetaBuild, NombreEjecutable),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
            BuildSummary resumen = reporte.summary;
            Debug.Log("[Progresion] ConstruirDemo: " + resumen.result + ", " + resumen.totalErrors + " errores, "
                + (resumen.totalSize / (1024 * 1024)) + " MB en " + resumen.totalTime.TotalSeconds.ToString("F0") + " s");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(resumen.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        // T-407: nombre del producto, catálogo de sonidos al día (entran los WAV nuevos) y fuera el .exe viejo de esa carpeta.
        private static void PrepararBuildCyberfix(string carpeta)
        {
            PlayerSettings.productName = NombreJuego.Nombre;
            CreadorCatalogoSonidos.Crear();
            string exeViejo = Path.Combine(carpeta, ExeViejo);
            if (File.Exists(exeViejo)) File.Delete(exeViejo);
            string datosViejo = Path.Combine(carpeta, DatosViejo);
            if (Directory.Exists(datosViejo)) Directory.Delete(datosViejo, true);
            foreach (string viejo in Directory.GetDirectories(carpeta, "*_BurstDebugInformation_DoNotShip"))
            {
                Directory.Delete(viejo, true);
            }
        }

        public const string CarpetaJuego = "Builds/Juego";
        public const string EscenaJuego = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        /// <summary>
        /// Build del juego de pandillas (T-400, D-tony-207): solo Ciudad_Prologo como escena 0, en Builds/Juego/MechaCyberpunk.exe.
        /// No cambia EditorBuildSettings ni PlayerSettings. En batchmode sale con 0 si el build salió bien.
        /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Demo/Construir juego de pandillas (Builds/Juego)")]
        public static void ConstruirJuego()
        {
            Directory.CreateDirectory(CarpetaJuego);
            PrepararBuildCyberfix(CarpetaJuego);
            BuildPlayerOptions opciones = new BuildPlayerOptions
            {
                scenes = new[] { EscenaJuego },
                locationPathName = Path.Combine(CarpetaJuego, NombreEjecutableJuego),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            BuildSummary resumen = BuildPipeline.BuildPlayer(opciones).summary;
            Debug.Log("[Progresion] ConstruirJuego: " + resumen.result + ", " + resumen.totalErrors + " errores, "
                + (resumen.totalSize / (1024 * 1024)) + " MB en " + resumen.totalTime.TotalSeconds.ToString("F0") + " s");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(resumen.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        /// <summary>
        /// Build final de la demo de pandillas (T-406): como ConstruirJuego pero en Builds/Demo y con el símbolo DEMO_FINAL, que apaga F6, F7 y las teclas
        /// de prueba. No cambia EditorBuildSettings ni PlayerSettings. Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirDemoPandillas
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Demo/Construir demo final de pandillas (Builds/Demo)")]
        public static void ConstruirDemoPandillas()
        {
            Directory.CreateDirectory(CarpetaBuild);
            PrepararBuildCyberfix(CarpetaBuild);
            BuildPlayerOptions opciones = new BuildPlayerOptions
            {
                scenes = new[] { EscenaJuego },
                locationPathName = Path.Combine(CarpetaBuild, NombreEjecutableJuego),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "DEMO_FINAL" },
            };
            BuildSummary resumen = BuildPipeline.BuildPlayer(opciones).summary;
            Debug.Log("[Progresion] ConstruirDemoPandillas: " + resumen.result + ", " + resumen.totalErrors + " errores, "
                + (resumen.totalSize / (1024 * 1024)) + " MB en " + resumen.totalTime.TotalSeconds.ToString("F0") + " s");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(resumen.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
