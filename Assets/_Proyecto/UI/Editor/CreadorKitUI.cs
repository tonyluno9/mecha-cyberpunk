using MechaCyberpunk.UI.Kit;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-408: crea UI/Datos/EstiloCyberfix_Base.asset (los valores de docs/diseno/ui-cyberfix.md) y rehace HudAPie.prefab con el kit.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorKitUI.Crear
    /// </summary>
    public static class CreadorKitUI
    {
        public const string RutaEstilo = "Assets/_Proyecto/UI/Datos/EstiloCyberfix_Base.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-408 Kit de UI")]
        public static void Crear()
        {
            AsegurarEstilo();
            CreadorHudAPie.Crear();
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-408 creada.");
        }

        /// <summary>Carga el estilo base o lo crea con los valores de la guía. La fuente queda vacía (= la integrada).</summary>
        public static EstiloCyberfix AsegurarEstilo()
        {
            EstiloCyberfix estilo = AssetDatabase.LoadAssetAtPath<EstiloCyberfix>(RutaEstilo);
            if (estilo == null)
            {
                estilo = ScriptableObject.CreateInstance<EstiloCyberfix>();
                AssetDatabase.CreateAsset(estilo, RutaEstilo);
                AssetDatabase.SaveAssets();
            }
            return estilo;
        }
    }
}
