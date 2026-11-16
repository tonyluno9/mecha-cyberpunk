using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-1011: importa como sprites los dibujos de línea de las prótesis de UI/Resources/Protesis (blancos con alfa, para teñirlos): piernas, brazos y
    /// cabeza I y II salen de imageassets/ui/menus/H41_IconosProtesis.png, y el láser y los misiles de los dibujos IMP04 e IMP05 de imageassets/modelos-nuevos.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.IconosProtesisT1011.Importar
    /// </summary>
    public static class IconosProtesisT1011
    {
        public const string Carpeta = "Assets/_Proyecto/UI/Resources/Protesis";

        [MenuItem("Mecha Cyberpunk/T-1011 Importar íconos de prótesis")]
        public static void Importar()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-1011: íconos de prótesis importados");
        }
    }
}
