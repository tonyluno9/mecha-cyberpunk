using MechaCyberpunk.Trafico.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1498: crea (o actualiza) Trafico/Resources/ConfigDronesCombate con las variantes de dron de combate. Idempotente.
    /// Variantes (mejor opción de cada modelo): DR01 policía (1+ estrellas), DR04_a explorador (2+), DR02_a pesado (4+), E13 asalto (5+).
    /// Ninguna es más débil que el DR01 (no se hace más fácil escapar): la cadencia y la vida son iguales o mejores, y el daño igual o mayor.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.Trafico.Editor.CrearDronesCombateT1498.Crear
    /// </summary>
    public static class CrearDronesCombateT1498
    {
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Resources/ConfigDronesCombate.asset";
        private const string CarpetaPrefabs = "Assets/Arte/Vehiculos/Drones/Prefabs/";

        [MenuItem("Mecha Cyberpunk/T1498/Crear config de drones de combate")]
        public static void Crear()
        {
            GameObject dr04 = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "Dron_DR04_a.prefab");
            GameObject dr02 = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "Dron_DR02_a.prefab");
            GameObject e13 = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "Dron_E13.prefab");
            if (dr04 == null || dr02 == null || e13 == null)
            {
                Debug.LogError("[T-1498] faltan prefabs Dron_DR04_a, Dron_DR02_a o Dron_E13");
                return;
            }
            ConfigDronesCombate config = AssetDatabase.LoadAssetAtPath<ConfigDronesCombate>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigDronesCombate>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            // nombre, modelo, estrellas, vida, danio, segundosEntreTiros, acierto, altura, escala, radioImpacto (0 = el de ConfigPersecucion)
            config.Configurar(new[]
            {
                new VarianteDronCombate("DR01_Policia", null, 1, 0f, 0f, 0f, 0f, 0f, 1f, 0f),
                new VarianteDronCombate("DR04_Explorador", dr04, 2, 40f, 4f, 1.2f, 0.7f, 6f, 1f, 0.7f),
                new VarianteDronCombate("DR02_Pesado", dr02, 4, 90f, 9f, 2f, 0.7f, 6.5f, 1f, 1.1f),
                new VarianteDronCombate("E13_Asalto", e13, 5, 55f, 6f, 1f, 0.7f, 5.5f, 1f, 0.8f),
            });
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1498] ConfigDronesCombate lista (DR01, DR04_a, DR02_a, E13)");
        }
    }
}
