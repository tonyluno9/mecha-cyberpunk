using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-895: el holograma compartido del banco y la clínica resalta en turquesa claro en lugar de naranja.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.AplicarTurquesaMenusS2m.Aplicar
    /// </summary>
    public static class AplicarTurquesaMenusS2m
    {
        private const string RutaConfigArmas = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";
        private static readonly Color ResalteTurquesa = new Color(0.55f, 1f, 0.92f, 1f) * 1.5f;

        [MenuItem("Mecha Cyberpunk/S2m/2 Menús en turquesa")]
        public static void Aplicar()
        {
            ConfigMenuArmas armas = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfigArmas);
            Material holograma = armas.MaterialHolograma;
            holograma.SetColor("_ColorResalte", ResalteTurquesa);
            EditorUtility.SetDirty(holograma);
            AssetDatabase.SaveAssets();
        }
    }
}
