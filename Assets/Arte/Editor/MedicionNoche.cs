using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-419: build de medición de noche con los lugares del paso 4 (calle principal, callejón, El Tianguis, Neón Alto, VivaCoin,
    /// El Mercado, clandestino, casa y taller). Igual que ConstruirMedicionAmbiente de Mundo (copia de la escena, MedicionAmbiente
    /// con sus puntos y la escena devuelta byte a byte), pero además quita CamaraTitulo de la copia para que el build no arranque
    /// en el título (T-413). Los tipos se buscan por nombre para no referenciar Mundo ni Progresion desde Arte.Editor.
    /// Uso: Builds/Medicion/&lt;exe&gt; -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -medicionSalida &lt;carpeta&gt;
    /// </summary>
    public static class MedicionNoche
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Builds/Medicion";
        private const string Ejecutable = "MechaCyberpunk.exe";
        private const string TipoMedicion = "MechaCyberpunk.Mundo.Pruebas.MedicionAmbiente";
        private const string TipoTitulo = "CamaraTitulo";

        /// <summary>Pies del jugador y hacia dónde mira (m). La calle principal corre en X con z = 0; Suburbios x &lt; -47, Neón x &gt; 47.</summary>
        public static readonly (string nombre, Vector3 pies, Vector3 mira)[] Puntos =
        {
            ("calle", new Vector3(-30f, 0.2f, -2f), new Vector3(1f, -0.04f, 0.12f)),
            ("callejon", new Vector3(-53f, 0.2f, -59f), new Vector3(-1f, -0.02f, 0f)),
            ("tianguis", new Vector3(38f, 0.2f, 0.5f), new Vector3(-1f, -0.05f, 0f)),
            ("neon", new Vector3(50f, 0.2f, -4f), new Vector3(1f, 0.02f, 0.25f)),
            ("vivacoin", new Vector3(44f, 0.2f, 36f), new Vector3(0.75f, 0.05f, 0.65f)),
            ("mercado", new Vector3(-22f, 0.2f, 11f), new Vector3(-0.15f, 0.02f, 1f)),
            ("clandestino", new Vector3(0f, 0.2f, -1.5f), new Vector3(0f, 0.08f, 1f)),
            ("casa", new Vector3(-61.8f, 0.2f, -77.6f), new Vector3(-1f, -0.25f, -0.75f)),
            ("taller", new Vector3(-61.8f, 0.2f, -76.4f), new Vector3(-1f, -0.2f, 0.6f)),
            ("suburbios", new Vector3(-80f, 0.2f, 3f), new Vector3(-1f, -0.02f, -0.15f)),
        };

        [MenuItem("Mecha Cyberpunk/T-419/Construir build de medición de noche")]
        public static void Construir()
        {
            byte[] respaldo = File.ReadAllBytes(RutaEscena);
            BuildSummary resumen = default;
            bool bien = false;
            try
            {
                var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
                Type tipoMedicion = BuscarTipo(TipoMedicion);
                foreach (MonoBehaviour titulo in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(m => m != null && m.GetType().Name == TipoTitulo).ToList())
                {
                    UnityEngine.Object.DestroyImmediate(titulo);
                }
                Component medicion = new GameObject("MedicionNoche").AddComponent(tipoMedicion);
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
                so.FindProperty("segundosCalentar").floatValue = 3f;
                so.FindProperty("segundosMedir").floatValue = 6f;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(escena);

                Directory.CreateDirectory(Carpeta);
                BuildPlayerOptions opciones = new BuildPlayerOptions
                {
                    scenes = new[] { RutaEscena },
                    locationPathName = Path.Combine(Carpeta, Ejecutable),
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None,
                };
                resumen = BuildPipeline.BuildPlayer(opciones).summary;
                bien = resumen.result == BuildResult.Succeeded;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.WriteAllBytes(RutaEscena, respaldo);
                AssetDatabase.ImportAsset(RutaEscena, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("[Arte] MedicionNoche: " + resumen.result + ", " + resumen.totalErrors + " errores");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(bien ? 0 : 1);
            }
        }

        private static Type BuscarTipo(string nombre)
        {
            foreach (var ensamblado in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = ensamblado.GetType(nombre);
                if (t != null)
                {
                    return t;
                }
            }
            throw new InvalidOperationException("No encuentro el tipo " + nombre);
        }
    }
}
