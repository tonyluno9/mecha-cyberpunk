using MechaCyberpunk.Progresion.Partida;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-412 del creador de la ciudad de pandillas: el AUTOGUARDADO y la pantalla de ranuras (UI/Prefabs/PantallaRanuras.prefab, que hace
    /// CreadorPantallaRanuras) bajo GTA_T412. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCiudadPandillas.T412
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT412 = "GTA_T412";
        public const string RutaPantallaRanuras = "Assets/_Proyecto/UI/Prefabs/PantallaRanuras.prefab";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-412 Guardado")]
        public static void T412()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T412Interno();
            Guardar(escena);
        }

        private static void T412Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT412);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT412);
            raiz.AddComponent<Autoguardado>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallaRanuras);
            if (prefab == null)
            {
                Debug.LogError("[Progresion] CreadorCiudadPandillas.T412: falta " + RutaPantallaRanuras + "; corre antes CreadorPantallaRanuras.Crear.");
                return;
            }
            GameObject pantalla = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
            pantalla.name = prefab.name;
            Debug.Log("[Progresion] CreadorCiudadPandillas.T412: autoguardado y pantalla de ranuras listos.");
        }
    }
}
