using System.IO;
using UnityEditor;

namespace MechaCyberpunk.UI.Creditos.Editor
{
    /// <summary>
    /// Genera Resources/Creditos.txt desde docs/creditos.md (T-471): se quita la nota de arriba (todo antes del primer "## ") y se pone
    /// el título "# CRÉDITOS". Batch: -executeMethod MechaCyberpunk.UI.Creditos.Editor.CreadorCreditos.Generar
    /// </summary>
    public static class CreadorCreditos
    {
        public const string RutaOrigen = "docs/creditos.md";
        public const string RutaDestino = "Assets/_Proyecto/UI/Creditos/Resources/Creditos.txt";

        [MenuItem("Mecha Cyberpunk/Créditos/Generar texto")]
        public static void Generar()
        {
            string md = File.ReadAllText(RutaOrigen).Replace("\r", string.Empty);
            int inicio = md.StartsWith("## ") ? 0 : md.IndexOf("\n## ") + 1;
            File.WriteAllText(RutaDestino, "# CRÉDITOS\n\n" + md.Substring(inicio));
            AssetDatabase.ImportAsset(RutaDestino);
            AssetDatabase.SaveAssets();
        }
    }
}
