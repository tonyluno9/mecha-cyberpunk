using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-410 del creador de la ciudad de pandillas: la pausa (UI/Prefabs/PantallaPausa.prefab, que hace
    /// CreadorPantallaPausa) bajo GTA_T410. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCiudadPandillas.T410
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT410 = "GTA_T410";
        public const string RutaPantallaPausa = "Assets/_Proyecto/UI/Prefabs/PantallaPausa.prefab";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-410 Pausa")]
        public static void T410()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T410Interno();
            Guardar(escena);
        }

        private static void T410Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT410);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT410);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallaPausa);
            if (prefab == null)
            {
                Debug.LogError("[Progresion] CreadorCiudadPandillas.T410: falta " + RutaPantallaPausa + "; corre antes CreadorPantallaPausa.Crear.");
                return;
            }
            GameObject pantalla = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
            pantalla.name = prefab.name;
            Debug.Log("[Progresion] CreadorCiudadPandillas.T410: pausa lista.");
        }
    }
}
