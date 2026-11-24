using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-indicador-24nov: crea ConfigIndicadorDanio_Base en UI/Datos/Resources (el destello rojo en la orilla hacia donde vino el daño).
    /// Idempotente: si ya existe no la toca. Sin el asset el indicador usa los mismos valores por defecto.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.IndicadorDanioT24nov.Aplicar
    /// </summary>
    public static class IndicadorDanioT24nov
    {
        public const string Carpeta = "Assets/_Proyecto/UI/Datos/Resources";
        public const string Ruta = Carpeta + "/" + ConfigIndicadorDanio.NombreRecurso + ".asset";

        [MenuItem("Mecha Cyberpunk/T-indicador-24nov Config del indicador de daño")]
        public static void Aplicar()
        {
            Directory.CreateDirectory(Carpeta);
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<ConfigIndicadorDanio>(Ruta) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigIndicadorDanio>(), Ruta);
                AssetDatabase.SaveAssets();
                Debug.Log("[UI] T-indicador-24nov: creada " + Ruta);
            }
        }
    }
}
