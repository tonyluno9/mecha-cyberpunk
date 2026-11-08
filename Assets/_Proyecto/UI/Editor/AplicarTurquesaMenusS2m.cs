using MechaCyberpunk.UI.Clinica;
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
        private const string RutaConfigTienda = "Assets/_Proyecto/UI/Datos/ConfigPantallaTienda_Base.asset";
        private const string RutaConfigClinica = "Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset";
        private static readonly Color EspacioTurquesa = new Color(0.55f, 1f, 0.92f, 1f);
        private static readonly Color FondoTurquesa = new Color(0.008f, 0.05f, 0.06f, 1f);
        private static readonly Color ResalteTurquesa = new Color(0.55f, 1f, 0.92f, 1f) * 1.5f;

        [MenuItem("Mecha Cyberpunk/S2m/2 Menús en turquesa")]
        public static void Aplicar()
        {
            ConfigMenuArmas armas = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfigArmas);
            Material holograma = armas.MaterialHolograma;
            holograma.SetColor("_ColorResalte", ResalteTurquesa);
            EditorUtility.SetDirty(holograma);

            ConfigPantallaClinica clinica = AssetDatabase.LoadAssetAtPath<ConfigPantallaClinica>(RutaConfigClinica);
            SerializedObject so = new SerializedObject(clinica);
            so.FindProperty("colorEspacio").colorValue = EspacioTurquesa;
            so.FindProperty("colorFondoMenu").colorValue = FondoTurquesa;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clinica);

            ConfigPantallaTienda tienda = AssetDatabase.LoadAssetAtPath<ConfigPantallaTienda>(RutaConfigTienda);
            SerializedObject soTienda = new SerializedObject(tienda);
            soTienda.FindProperty("colorElegido").colorValue = EspacioTurquesa;
            soTienda.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tienda);
            AssetDatabase.SaveAssets();
        }
    }
}
