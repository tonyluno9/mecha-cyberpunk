using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
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
        public const string NombreEjecutable = "MechaCyberpunk.exe";
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
            BuildPlayerOptions opciones = new BuildPlayerOptions
            {
                scenes = new[] { EscenaJuego },
                locationPathName = Path.Combine(CarpetaJuego, NombreEjecutable),
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
    }
}
