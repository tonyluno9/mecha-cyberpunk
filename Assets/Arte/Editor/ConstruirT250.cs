using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-250: regenera Builds/Provisional (las 9 escenas de la guía más Ciudad_Prologo, Clandestino y Pruebas_Assets al final)
    /// y Builds/Provisional_Assets (las mismas, arrancando en Ciudad_Prologo para ver el arte nuevo). No cambia los Build Settings.
    /// </summary>
    public static class ConstruirT250
    {
        private static readonly string[] EscenasGuia =
        {
            "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity",
            "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity",
            "Assets/_Proyecto/Escenas/Tony/Pruebas_Flashback.unity",
            "Assets/_Proyecto/Escenas/Pruebas_Tony.unity",
            "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity",
            "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity",
            "Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity",
            "Assets/_Proyecto/Escenas/Tony/Pruebas_Tutorial.unity",
            "Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity",
        };

        private static readonly string[] EscenasArte =
        {
            "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
            "Assets/_Proyecto/Escenas/Clandestino.unity",
            ArteT250.RutaEscenaPruebas,
        };

        /// <summary>Batchmode: los dos builds de Windows; sale con 1 si alguno falla.</summary>
        public static void Construir()
        {
            string[] todas = EscenasGuia.Concat(EscenasArte).ToArray();
            string[] desdeCiudad = new[] { EscenasArte[0] }.Concat(todas.Where(e => e != EscenasArte[0])).ToArray();
            bool bien = Uno(todas, "Builds/Provisional/MechaCyberpunk.exe") & Uno(desdeCiudad, "Builds/Provisional_Assets/MechaCyberpunk.exe");
            EditorApplication.Exit(bien ? 0 : 1);
        }

        private static bool Uno(string[] escenas, string salida)
        {
            BuildPlayerOptions opciones = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = salida,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
            Debug.Log("[Arte] Build " + salida + ": " + reporte.summary.result + ", " + reporte.summary.totalSize / (1024 * 1024) + " MB");
            return reporte.summary.result == BuildResult.Succeeded;
        }
    }
}
