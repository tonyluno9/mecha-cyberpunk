using System.IO;
using MechaCyberpunk.UI.Hud;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-700: importa los íconos de línea fina de UI/Iconos como sprites (el punteado de la ruta se repite) y arma UI/Resources/ConfigHudH10.asset
    /// con la fuente Rajdhani (UI/Fuentes) y esos sprites. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorHudH10.Crear
    /// </summary>
    public class CreadorHudH10 : AssetPostprocessor
    {
        public const string CarpetaIconos = "Assets/_Proyecto/UI/Iconos/";
        private const string CarpetaFuentes = "Assets/_Proyecto/UI/Fuentes/";
        private const string RutaConfig = "Assets/_Proyecto/UI/Resources/ConfigHudH10.asset";
        private static readonly string[] Partes = { "Cabeza", "Torso", "Brazos", "Piernas" };
        private static readonly string[] Ids = { "bebida_energetica", "papitas", "venda", "cafe", "bateria_escudo" };
        private static readonly string[] Archivos = { "Obj_BebidaEnergetica", "Obj_Papitas", "Obj_Venda", "Obj_Cafe", "Obj_Bateria" };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CarpetaIconos, System.StringComparison.Ordinal))
            {
                return;
            }
            TextureImporter importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.filterMode = FilterMode.Bilinear;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            bool repetir = Path.GetFileNameWithoutExtension(assetPath) == "Ruta_Punto";
            importador.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            TextureImporterSettings ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            importador.SetTextureSettings(ajustes);
        }

        [MenuItem("Mecha Cyberpunk/Crear T-700 (HUD H10)")]
        public static void Crear()
        {
            AssetDatabase.Refresh();
            ConfigHudH10 config = AssetDatabase.LoadAssetAtPath<ConfigHudH10>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigHudH10>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            Sprite[] linea = new Sprite[Partes.Length];
            Sprite[] relleno = new Sprite[Partes.Length];
            for (int i = 0; i < Partes.Length; i++)
            {
                linea[i] = Sprite_("Cuerpo_" + Partes[i] + "_Linea");
                relleno[i] = Sprite_("Cuerpo_" + Partes[i] + "_Relleno");
            }
            Sprite[] iconos = new Sprite[Archivos.Length];
            for (int i = 0; i < Archivos.Length; i++)
            {
                iconos[i] = Sprite_(Archivos[i]);
            }
            config.Llenar(
                AssetDatabase.LoadAssetAtPath<Font>(CarpetaFuentes + "Rajdhani-Regular.ttf"),
                AssetDatabase.LoadAssetAtPath<Font>(CarpetaFuentes + "Rajdhani-Medium.ttf"),
                Sprite_("Estrella_Contorno"), Sprite_("Estrella_Llena"), linea, relleno, Ids, iconos,
                Sprite_("Minimapa_Cono"), Sprite_("Ruta_Punto"));
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-700 creada: " + RutaConfig);
        }

        private static Sprite Sprite_(string nombre)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaIconos + nombre + ".png");
            if (sprite == null)
            {
                Debug.LogError("[UI] Falta el sprite " + nombre);
            }
            return sprite;
        }
    }
}
