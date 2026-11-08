using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-895/T-896: corta las hojas H30, H33 y H34 (casillas con fondo negro) en sprites sueltos con transparencia, en Resources/Menus.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CortarIconosMenusS2m.Cortar
    /// </summary>
    public static class CortarIconosMenusS2m
    {
        public const string Carpeta = "Assets/_Proyecto/UI/Resources/Menus";
        private const string Hojas = "imageassets/ui/menus/";
        private const int Lado = 256;
        private const int MargenCasilla = 10;
        private const float BrilloParaOpaco = 3f;

        private static readonly RectInt[] CasillasDeSeis =
        {
            new RectInt(23, 27, 382, 380), new RectInt(441, 27, 382, 380), new RectInt(859, 27, 382, 380),
            new RectInt(23, 442, 382, 380), new RectInt(441, 442, 382, 380), new RectInt(859, 442, 382, 380),
        };

        private static readonly RectInt[] CasillasDeTres =
        {
            new RectInt(35, 40, 484, 593), new RectInt(550, 40, 484, 593), new RectInt(1065, 40, 484, 593),
        };

        [MenuItem("Mecha Cyberpunk/S2m/3 Cortar íconos de menús")]
        public static void Cortar()
        {
            Directory.CreateDirectory(Carpeta);
            Hoja("H34_PiezasArma.png", CasillasDeSeis, (0, "Pieza_Cargador"), (2, "Pieza_Canon"), (3, "Pieza_Mira"));
            Hoja("H30_IconosImplantes.png", CasillasDeSeis, (0, "Implante_Piernas_1"), (2, "Implante_Brazos_1"), (4, "Implante_Ojos_1"));
            Hoja("H33_Implantes_Nivel2.png", CasillasDeTres, (0, "Implante_Piernas_2"), (1, "Implante_Brazos_2"), (2, "Implante_Ojos_2"));
            AssetDatabase.SaveAssets();
        }

        private static void Hoja(string archivo, RectInt[] casillas, params (int casilla, string nombre)[] cortes)
        {
            Texture2D hoja = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            hoja.LoadImage(File.ReadAllBytes(Hojas + archivo));
            foreach ((int casilla, string nombre) in cortes)
            {
                string ruta = Carpeta + "/" + nombre + ".png";
                File.WriteAllBytes(ruta, Recortar(hoja, casillas[casilla]).EncodeToPNG());
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            Object.DestroyImmediate(hoja);
        }

        // La casilla (y desde arriba) se centra en un cuadrado de Lado px; el negro del fondo se vuelve transparencia.
        private static Texture2D Recortar(Texture2D hoja, RectInt casilla)
        {
            int x0 = casilla.x + MargenCasilla;
            int yArriba = casilla.y + MargenCasilla;
            int ancho = casilla.width - 2 * MargenCasilla;
            int alto = casilla.height - 2 * MargenCasilla;
            int lado = Mathf.Max(ancho, alto);
            Texture2D r = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    int sx = x0 + (ancho - lado) / 2 + x * lado / Lado;
                    int sy = hoja.height - 1 - (yArriba + (alto - lado) / 2 + (Lado - 1 - y) * lado / Lado);
                    bool dentro = sx >= x0 && sx < x0 + ancho && sy >= hoja.height - yArriba - alto && sy < hoja.height - yArriba;
                    Color c = dentro ? hoja.GetPixel(sx, sy) : Color.black;
                    c.a = Mathf.Clamp01(Mathf.Max(c.r, c.g, c.b) * BrilloParaOpaco);
                    r.SetPixel(x, y, c);
                }
            }
            r.Apply();
            return r;
        }
    }
}
