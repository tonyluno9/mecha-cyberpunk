using MechaCyberpunk.Trafico.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1516: pone en la config de peatones de la ciudad cuántos platican (1 de cada 8) y qué parte de los que oyen disparos se agacha antes de huir.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.AplicarPlaticaT1516.Aplicar
    /// </summary>
    public static class AplicarPlaticaT1516
    {
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset";
        public const int UnoDeCadaPlatica = 8;
        public const float ProbabilidadAgacharse = 0.4f;
        public const float SegundosAgachado = 3f;

        [MenuItem("Mecha Cyberpunk/T-1516/Peatones: platicar y agacharse")]
        public static void Aplicar()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigPeatones>(RutaConfig);
            if (config == null)
            {
                Debug.LogError("[Trafico] T-1516: falta " + RutaConfig);
                return;
            }
            config.ConfigurarReaccion(UnoDeCadaPlatica, ProbabilidadAgacharse, SegundosAgachado);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-1516: peatones, 1 de cada " + UnoDeCadaPlatica + " platica y " + (ProbabilidadAgacharse * 100f) + " % se agacha al oír disparos.");
        }
    }
}
