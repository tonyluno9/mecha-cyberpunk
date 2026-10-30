using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-411 del creador de la ciudad de pandillas: el ServicioOpciones y la pantalla de opciones (UI/Prefabs/PantallaOpciones.prefab, que hace
    /// CreadorPantallaOpciones) bajo GTA_T411. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCiudadPandillas.T411
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT411 = "GTA_T411";
        public const string RutaPantallaOpciones = "Assets/_Proyecto/UI/Prefabs/PantallaOpciones.prefab";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-411 Opciones")]
        public static void T411()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T411Interno();
            Guardar(escena);
        }

        private static void T411Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT411);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT411);
            raiz.AddComponent<MechaCyberpunk.Progresion.Opciones.ServicioOpciones>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallaOpciones);
            if (prefab == null)
            {
                Debug.LogError("[Progresion] CreadorCiudadPandillas.T411: falta " + RutaPantallaOpciones + "; corre antes CreadorPantallaOpciones.Crear.");
                return;
            }
            GameObject pantalla = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
            pantalla.name = prefab.name;
            Debug.Log("[Progresion] CreadorCiudadPandillas.T411: opciones listas.");
        }
    }
}
