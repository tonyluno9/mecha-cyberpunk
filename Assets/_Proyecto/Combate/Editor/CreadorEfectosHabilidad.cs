using MechaCyberpunk.Combate.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Combate.Editor
{
    /// <summary>
    /// Crea ConfigEfectosHabilidad_Base.asset (T-175). Idempotente: no pisa el asset si ya existe.
    /// Batchmode: -executeMethod MechaCyberpunk.Combate.Editor.CreadorEfectosHabilidad.Crear
    /// </summary>
    public static class CreadorEfectosHabilidad
    {
        private const string Ruta = "Assets/_Proyecto/Combate/Datos/ConfigEfectosHabilidad_Base.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-175 config de efectos")]
        public static void Crear()
        {
            if (AssetDatabase.LoadAssetAtPath<ConfigEfectosHabilidad>(Ruta) != null)
            {
                return;
            }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigEfectosHabilidad>(), Ruta);
            AssetDatabase.SaveAssets();
            Debug.Log("[Combate] T-175 config de efectos creada.");
        }
    }
}
