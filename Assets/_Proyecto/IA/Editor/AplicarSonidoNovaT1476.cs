using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1476: pone POL_NOVA_AVISO como sonido de llegada de Nova en ConfigRespuestaNova. Se puede correr varias veces.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.AplicarSonidoNovaT1476.Aplicar
    /// </summary>
    public static class AplicarSonidoNovaT1476
    {
        [MenuItem("Mecha Cyberpunk/T1476/Aplicar sonido de llegada de Nova")]
        public static void Aplicar()
        {
            ConfigRespuestaNova config = AssetDatabase.LoadAssetAtPath<ConfigRespuestaNova>(AplicarNovaT1323.RutaConfig);
            if (config == null)
            {
                Debug.LogError("[T-1476] falta " + AplicarNovaT1323.RutaConfig);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("sonidoLlegada").stringValue = "POL_NOVA_AVISO";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1476] ConfigRespuestaNova: sonido de llegada POL_NOVA_AVISO");
        }
    }
}
