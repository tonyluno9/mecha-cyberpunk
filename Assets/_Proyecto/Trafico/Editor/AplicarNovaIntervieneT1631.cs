using MechaCyberpunk.Core;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1631 (T17): agrega al asset ConfigRespuesta los campos de "NOVA INTERVIENE" (estrellas del modo, tope de Nova vivos, bajas por nivel, nivel del dron de Nova
    /// y metros de retirada de la policía) con sus valores de omisión, sin tocar el resto de lo que Tony ajustó. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.AplicarNovaIntervieneT1631.Aplicar
    /// </summary>
    public static class AplicarNovaIntervieneT1631
    {
        [MenuItem("Mecha Cyberpunk/T-1631/Aplicar NOVA INTERVIENE a ConfigRespuesta")]
        public static void Aplicar()
        {
            ConfigRespuesta config = AssetDatabase.LoadAssetAtPath<ConfigRespuesta>(AplicarRespuestaT1620.Ruta);
            if (config == null)
            {
                AplicarRespuestaT1620.Aplicar();
                config = AssetDatabase.LoadAssetAtPath<ConfigRespuesta>(AplicarRespuestaT1620.Ruta);
            }
            ParametrosRespuesta omision = ParametrosRespuesta.PorOmision();
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("parametros.EstrellasModoNova").intValue = omision.EstrellasModoNova;
            so.FindProperty("parametros.TopeNovaVivos").intValue = omision.TopeNovaVivos;
            so.FindProperty("parametros.PerdidasNovaParaSubir").intValue = omision.PerdidasNovaParaSubir;
            so.FindProperty("parametros.NivelDronNova").intValue = omision.NivelDronNova;
            so.FindProperty("parametros.MetrosMinimosRetiroPolicia").floatValue = omision.MetrosMinimosRetiroPolicia;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Respuesta] ConfigRespuesta con NOVA INTERVIENE: modo " + config.Parametros.EstrellasModoNova + " estrellas, tope Nova " + config.Parametros.TopeNovaVivos);
        }
    }
}
