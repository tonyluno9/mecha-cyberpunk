using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-714: el SKULL que se baja del carro de refuerzo. Crea Trafico/Resources/SkullRefuerzo.prefab como variante de IA/Prefabs/PandilleroArmado_Skulls.prefab con
    /// "siempreAlerta" (pelea contra el jugador sin necesitar un grupo con territorio). Solo por ruta de asset: Trafico no referencia a IA.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorSkullRefuerzoT714.Crear
    /// </summary>
    public static class CreadorSkullRefuerzoT714
    {
        public const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        public const string RutaRefuerzo = "Assets/_Proyecto/Trafico/Resources/SkullRefuerzo.prefab";

        [MenuItem("Mecha Cyberpunk/T-714/SKULL de refuerzo")]
        public static void Crear()
        {
            GameObject baseSkull = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull);
            if (baseSkull == null)
            {
                Debug.LogError("[Trafico] T-714: falta " + RutaSkull);
                return;
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(baseSkull);
            try
            {
                Component pandillero = instancia.GetComponents<MonoBehaviour>().FirstOrDefault(c => c != null && c.GetType().Name == "PandilleroAPie");
                if (pandillero == null)
                {
                    Debug.LogError("[Trafico] T-714: " + RutaSkull + " no trae PandilleroAPie.");
                    return;
                }
                SerializedObject so = new SerializedObject(pandillero);
                so.FindProperty("siempreAlerta").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                instancia.name = "SkullRefuerzo";
                PrefabUtility.SaveAsPrefabAssetAndConnect(instancia, RutaRefuerzo, InteractionMode.AutomatedAction);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-714: " + RutaRefuerzo + " creado (variante de " + RutaSkull + " con siempreAlerta).");
        }
    }
}
