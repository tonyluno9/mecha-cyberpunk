using MechaCyberpunk.Trafico.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1603: deja los números del tráfico ordenado en ConfigTraficoCalle_Trafico.asset (la que usa Ciudad_Prologo): 20 carros de día y 15 de noche (15 a 25 vivos alrededor del jugador),
    /// seguimiento a 6 m como mínimo (frena desde 16 m), y atorados que salen en otro punto a los 5 s si nadie los ve (7 s aunque se vean, para que ninguno pase de 8 s). No toca la escena: los campos nuevos de TraficoCalle
    /// (radioAlrededor, radioReciclar) ya salen con su valor por omisión. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.TraficoOrdenadoT1603.Aplicar
    /// </summary>
    public static class TraficoOrdenadoT1603
    {
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        public const int CarrosDeDia = 20;
        public const int CarrosDeNoche = 15;
        public const float DistanciaFrenado = 16f;
        public const float DistanciaParada = 6f;
        public const float Frenado = 10f;
        public const float SegundosParaDestrabar = 5f;
        public const float SegundosDestrabarForzado = 7f;

        [MenuItem("Mecha Cyberpunk/T-1603/Aplicar config del tráfico ordenado")]
        public static void Aplicar()
        {
            ConfigTraficoCalle config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            if (config == null)
            {
                Debug.LogError("[Trafico] T-1603: falta " + RutaConfig);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("carrosDeDia").intValue = CarrosDeDia;
            so.FindProperty("carrosDeNoche").intValue = CarrosDeNoche;
            so.FindProperty("distanciaFrenado").floatValue = DistanciaFrenado;
            so.FindProperty("distanciaParada").floatValue = DistanciaParada;
            so.FindProperty("frenado").floatValue = Frenado;
            so.FindProperty("segundosParaDestrabar").floatValue = SegundosParaDestrabar;
            so.FindProperty("segundosDestrabarForzado").floatValue = SegundosDestrabarForzado;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-1603: config del tráfico ordenado aplicada (" + CarrosDeDia + " de día, " + CarrosDeNoche + " de noche).");
        }
    }
}
