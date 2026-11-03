using System.IO;
using MechaCyberpunk.Progresion.Partida;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-537 (plan de la noche 3-nov, paso 1): la cámara del título recorre despacio, a la altura de la calle, la calle principal (Suburbios,
    /// El Tianguis) y sube por Neón Alto, de ida y vuelta; el menú ya no tapa la ciudad con la imagen fija (PantallaTitulo.fondoAnimado).
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.ColocarT537.Colocar
    /// </summary>
    public static class ColocarT537
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const float VelocidadTitulo = 3f;
        public const float AlturaTitulo = 3.4f;

        /// <summary>Vuelta cerrada por el carril de ida y el de regreso (2.5 m a cada lado del centro de la calle).</summary>
        public static readonly Vector3[] Ruta =
        {
            new Vector3(-110f, 0f, -2.5f), new Vector3(-47f, 0f, -2.5f), new Vector3(0f, 0f, -2.5f), new Vector3(44.5f, 0f, -2.5f),
            new Vector3(44.5f, 0f, 40f), new Vector3(44.5f, 0f, 80f), new Vector3(49.5f, 0f, 80f), new Vector3(49.5f, 0f, 40f),
            new Vector3(49.5f, 0f, 2.5f), new Vector3(0f, 0f, 2.5f), new Vector3(-47f, 0f, 2.5f), new Vector3(-110f, 0f, 2.5f),
        };

        [MenuItem("Mecha Cyberpunk/T-537/Cámara del título por las calles")]
        public static void Colocar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            CamaraTitulo camara = Object.FindFirstObjectByType<CamaraTitulo>(FindObjectsInactive.Include);
            if (camara == null)
            {
                Debug.LogError("[Progresion] T-537: no hay CamaraTitulo en la escena.");
                return;
            }
            camara.Configurar(Ruta, VelocidadTitulo, AlturaTitulo);
            SerializedObject so = new SerializedObject(camara);
            so.FindProperty("mirarAdelante").floatValue = 18f;
            so.FindProperty("mirarAbajo").floatValue = 1.2f;
            so.FindProperty("suavidadGiro").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(camara);
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t537-colocar.txt", "Cámara del título: " + Ruta.Length + " puntos a " + AlturaTitulo + " m y " + VelocidadTitulo + " m/s");
            Debug.Log("[Progresion] T-537: cámara del título por las calles.");
        }
    }
}
