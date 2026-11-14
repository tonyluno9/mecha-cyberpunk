using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-992: crea (o actualiza) <c>Datos/Resources/ConfigHabilidadesProtesis.asset</c> y lo enlaza al asset de entrada ControlesAPie. Se carga con
    /// Resources, así que el prefab del jugador no se toca. Idempotente. Batch: <c>-executeMethod MechaCyberpunk.Personaje.Editor.CreadorHabilidadesProtesis.Crear</c>.
    /// </summary>
    public static class CreadorHabilidadesProtesis
    {
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigHabilidadesProtesis.asset";
        private const string RutaEntrada = "Assets/_Proyecto/Personaje/Entrada/ControlesAPie.inputactions";

        [MenuItem("Mecha Cyberpunk/Personaje/Crear config de habilidades de prótesis")]
        public static void Crear()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaConfig));
            ConfigHabilidadesProtesis config = AssetDatabase.LoadAssetAtPath<ConfigHabilidadesProtesis>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigHabilidadesProtesis>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            InputActionAsset entrada = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaEntrada);
            if (entrada == null)
            {
                Debug.LogError("[Personaje] No encuentro " + RutaEntrada);
                return;
            }
            config.Llenar(entrada);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] ConfigHabilidadesProtesis lista en " + RutaConfig);
        }
    }
}
