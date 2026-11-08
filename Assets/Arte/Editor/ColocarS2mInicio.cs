using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-893 (C1): fotos de las calles reales para el fondo del menú de inicio y el logo LG04 con transparencia, como sprites en Resources.
    /// Los personajes se apagan para la foto (en el editor salen en pose T); la escena no se guarda.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2mInicio.Capturar
    /// </summary>
    public static class ColocarS2mInicio
    {
        public const string Carpeta = "Assets/_Proyecto/UI/Resources/Inicio";
        private const string LogoOrigen = "imageassets/logo/LG04_Barrio.png";
        private const float CampoFoto = 66f;
        private const float BrilloParaOpaco = 2.2f;
        private const float AlfaMinimaRecorte = 0.08f;

        [MenuItem("Mecha Cyberpunk/S2m/1 Fondo y logo del menú de inicio")]
        public static void Capturar()
        {
            Directory.CreateDirectory(Carpeta);
            ColocarS2.Abrir();
            foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                a.gameObject.SetActive(false);
            }
            (Vector3 desde, Vector3 hacia)[] tomas =
            {
                (ColocarS2cCalle.CamaraFoto + Vector3.up * 0.2f, ColocarS2cCalle.MiraFoto + Vector3.up * 0.2f),
                (ColocarS2cMercado.Tomas[0].desde, ColocarS2cMercado.Tomas[0].hacia),
                (ColocarS2cNeonAlto.Tomas[0].desde, ColocarS2cNeonAlto.Tomas[0].hacia),
                (new Vector3(58f, 2.6f, ColocarS2cNova.ZCentro - 0.5f), new Vector3(130f, 5f, ColocarS2cNova.ZCentro)),
            };
            for (int i = 0; i < tomas.Length; i++)
            {
                ColocarS2cLook.Foto(tomas[i].desde, tomas[i].hacia, Carpeta + "/FondoCalle_" + i + ".png", CampoFoto);
                Importar(Carpeta + "/FondoCalle_" + i + ".png", false);
            }
            Logo();
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }

        /// <summary>El logo viene sobre negro: el brillo se vuelve transparencia y se recorta a lo que se ve.</summary>
        private static void Logo()
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(LogoOrigen));
            Color[] p = t.GetPixels();
            int x0 = t.width, x1 = 0, y0 = t.height, y1 = 0;
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    Color c = p[y * t.width + x];
                    float alfa = Mathf.Clamp01(Mathf.Max(c.r, c.g, c.b) * BrilloParaOpaco);
                    p[y * t.width + x] = new Color(c.r, c.g, c.b, alfa);
                    if (alfa > AlfaMinimaRecorte)
                    {
                        x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                    }
                }
            }
            int ancho = x1 - x0 + 1, alto = y1 - y0 + 1;
            Texture2D r = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    r.SetPixel(x, y, p[(y + y0) * t.width + x + x0]);
                }
            }
            r.Apply();
            string ruta = Carpeta + "/Logo.png";
            File.WriteAllBytes(ruta, r.EncodeToPNG());
            Object.DestroyImmediate(t);
            Object.DestroyImmediate(r);
            Importar(ruta, true);
        }

        private static void Importar(string ruta, bool alfa)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = alfa;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
    }
}
