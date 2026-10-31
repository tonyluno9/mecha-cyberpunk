using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-451 del creador de la ciudad de pandillas: el TraficoCalle (carros civiles que frenan y pitan) en la raíz GTA_T451, con el prefab y la
    /// config de Trafico. Se busca el tipo por nombre para no referenciar el ensamblado de Trafico desde Progresion. Idempotente: rehace solo esa raíz.
    /// Antes hay que correr CreadorTrafico.Crear (el prefab y la config).
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT451 = "GTA_T451";
        private const string RutaPrefabCarroTrafico = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico.prefab";
        private const string RutaConfigTrafico = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-451 Tráfico de calle")]
        public static void T451()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T451Interno();
            Guardar(escena);
        }

        private static void T451Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT451);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            System.Type tipoTrafico = System.Type.GetType("MechaCyberpunk.Trafico.Calle.TraficoCalle, MechaCyberpunk.Trafico");
            System.Type tipoCarro = System.Type.GetType("MechaCyberpunk.Trafico.Calle.CarroTrafico, MechaCyberpunk.Trafico");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCarroTrafico);
            Object config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaConfigTrafico);
            if (tipoTrafico == null || tipoCarro == null || prefab == null || config == null)
            {
                Debug.LogError("[T-451] Falta el tipo, el prefab o la config del tráfico: corre antes CreadorTrafico.Crear.");
                return;
            }
            GameObject raiz = new GameObject(RaizT451);
            Component trafico = raiz.AddComponent(tipoTrafico);
            SerializedObject so = new SerializedObject(trafico);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("prefabCarro").objectReferenceValue = prefab.GetComponent(tipoCarro);
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[T-451] TraficoCalle creado.");
        }
    }
}
