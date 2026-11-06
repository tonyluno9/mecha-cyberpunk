using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-683: la rodada más lenta y natural (0.8 s, 3.2 m, invulnerable 0.5 s) y que el muñeco se encoja al rodar.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarAgachadoT683.Ajustar
    /// </summary>
    public static class AjustarAgachadoT683
    {
        private const string RutaDatos = "Assets/_Proyecto/Personaje/Datos/Resources/DatosCubierta.asset";

        [MenuItem("Mecha Cyberpunk/Ajustar T-683 rodada")]
        public static void Ajustar()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(RutaDatos);
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("distanciaRodada").floatValue = 3.2f;
            so.FindProperty("duracionRodada").floatValue = 0.8f;
            so.FindProperty("invulnerabilidad").floatValue = 0.5f;
            so.FindProperty("escalaRodando").floatValue = 0.7f;
            so.FindProperty("fraccionGiroRodada").floatValue = 0.85f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
