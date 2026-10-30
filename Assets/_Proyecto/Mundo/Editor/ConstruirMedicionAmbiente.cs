using System.IO;
using MechaCyberpunk.Mundo.Pruebas;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Build de medición de la ambientación de noche: le agrega a Ciudad_Prologo (en una copia de respaldo del archivo)
    /// <see cref="MedicionAmbiente"/> con los tres puntos (calle, callejón y entrada del clandestino), construye
    /// Builds/Medicion/MechaCyberpunk.exe y devuelve el archivo de la escena como estaba (así conserva sus datos de APV).
    /// Uso: Builds/Medicion/MechaCyberpunk.exe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -medicionSalida &lt;carpeta&gt;
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.ConstruirMedicionAmbiente.Construir
    /// </summary>
    public static class ConstruirMedicionAmbiente
    {
        private const string RutaOrigen = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Builds/Medicion";
        private const string Ejecutable = "MechaCyberpunk.exe";

        // Pies del jugador y dirección de la mirada (m). La calle principal corre en X con z = 0.
        private static readonly (string nombre, Vector3 pies, Vector3 mira)[] Puntos =
        {
            ("calle", new Vector3(-30f, 0.2f, -2f), new Vector3(1f, -0.04f, 0.12f)),
            ("callejon", new Vector3(-53f, 0.2f, -59f), new Vector3(-1f, -0.02f, 0f)),
            ("clandestino", new Vector3(0f, 0.2f, -1.5f), new Vector3(0f, 0.08f, 1f)),
        };

        [MenuItem("Mecha Cyberpunk/Ambiente/Construir build de medición")]
        public static void Construir()
        {
            byte[] respaldo = File.ReadAllBytes(RutaOrigen);
            var escena = EditorSceneManager.OpenScene(RutaOrigen, OpenSceneMode.Single);
            MedicionAmbiente medicion = new GameObject("MedicionAmbiente").AddComponent<MedicionAmbiente>();
            SerializedObject so = new SerializedObject(medicion);
            SerializedProperty lista = so.FindProperty("puntos");
            lista.arraySize = Puntos.Length;
            for (int i = 0; i < Puntos.Length; i++)
            {
                SerializedProperty p = lista.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("nombre").stringValue = Puntos[i].nombre;
                p.FindPropertyRelative("pies").vector3Value = Puntos[i].pies;
                p.FindPropertyRelative("mira").vector3Value = Puntos[i].mira;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(escena);

            Directory.CreateDirectory(Carpeta);
            BuildPlayerOptions opciones = new BuildPlayerOptions
            {
                scenes = new[] { RutaOrigen },
                locationPathName = Path.Combine(Carpeta, Ejecutable),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            BuildSummary resumen;
            try
            {
                resumen = BuildPipeline.BuildPlayer(opciones).summary;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.WriteAllBytes(RutaOrigen, respaldo);
                AssetDatabase.ImportAsset(RutaOrigen, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("[Mundo] ConstruirMedicionAmbiente: " + resumen.result + ", " + resumen.totalErrors + " errores");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(resumen.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
