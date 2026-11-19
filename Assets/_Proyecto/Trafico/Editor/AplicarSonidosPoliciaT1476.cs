using MechaCyberpunk.Trafico.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1476: pone en ConfigPersecucion los IDs de radio de la policía. Se puede correr varias veces.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.Trafico.Editor.AplicarSonidosPoliciaT1476.Aplicar
    /// </summary>
    public static class AplicarSonidosPoliciaT1476
    {
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";

        [MenuItem("Mecha Cyberpunk/T1476/Aplicar sonidos de policía (tráfico)")]
        public static void Aplicar()
        {
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            if (config == null)
            {
                Debug.LogError("[T-1476] falta " + RutaConfig);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("idSonidoRadio").stringValue = "POL_RADIO_AVISO";
            so.FindProperty("idSonidoDespacho").stringValue = "POL_RADIO_DESPACHO";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1476] ConfigPersecucion: radio POL_RADIO_AVISO, despacho POL_RADIO_DESPACHO");
        }
    }
}
