using MechaCyberpunk.Core;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1620 (T8): crea el asset Core/Respuesta/Resources/ConfigRespuesta con los topes y tiempos del director de respuesta. Se puede repetir:
    /// si el asset ya existe lo deja como está (para no pisar lo que Tony ajustó).
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.AplicarRespuestaT1620.Aplicar
    /// </summary>
    public static class AplicarRespuestaT1620
    {
        public const string Carpeta = "Assets/_Proyecto/Core/Respuesta/Resources";
        public const string Ruta = Carpeta + "/ConfigRespuesta.asset";

        [MenuItem("Mecha Cyberpunk/T-1620/Crear ConfigRespuesta")]
        public static void Aplicar()
        {
            ConfigRespuesta config = AssetDatabase.LoadAssetAtPath<ConfigRespuesta>(Ruta);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigRespuesta>();
                config.Reiniciar();
                AssetDatabase.CreateAsset(config, Ruta);
                Debug.Log("[Respuesta] ConfigRespuesta creado en " + Ruta);
            }
            else
            {
                Debug.Log("[Respuesta] ConfigRespuesta ya existía: se deja igual.");
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
