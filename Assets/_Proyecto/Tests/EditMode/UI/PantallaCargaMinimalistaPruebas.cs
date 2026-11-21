using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Pantalla de carga minimalista (pedido de Tony "carga"): fotos sin deformar que llenan la pantalla y el logo del menú.</summary>
    public class PantallaCargaMinimalistaPruebas
    {
        private const string Carpeta = "Assets/_Proyecto/UI/Resources/Carga/";

        [Test]
        public void Cubierto_ConservaLaProporcion_YLlenaLaPantalla([Values(1920f, 1366f, 1280f, 1440f)] float anchoPantalla, [Values(1080f, 768f, 720f, 900f)] float altoPantalla)
        {
            Vector2 t = ReglaFotosCarga.TamanoCubierto(1376f, 768f, anchoPantalla, altoPantalla);
            Assert.AreEqual(1376f / 768f, t.x / t.y, 1e-3f, "nunca se deforma");
            Assert.GreaterOrEqual(t.x, anchoPantalla - 1e-3f, "llena el ancho");
            Assert.GreaterOrEqual(t.y, altoPantalla - 1e-3f, "llena el alto");
            Assert.IsTrue(Mathf.Abs(t.x - anchoPantalla) < 1e-3f || Mathf.Abs(t.y - altoPantalla) < 1e-3f, "recorta lo mínimo: un lado justo");
        }

        [Test]
        public void Cubierto_En16a9_RecortaMenosDel1PorCiento()
        {
            Vector2 t = ReglaFotosCarga.TamanoCubierto(1376f, 768f, 1920f, 1080f);
            Assert.Less((t.x - 1920f) / t.x, 0.01f);
        }

        [Test]
        public void Fotos_SeImportanSinEscalarAPotenciaDe2_NiMipmaps_NiCompresion([Values("L01_Carga_Protagonista", "L03_Carga_Doc", "L06_Carga_Techos")] string nombre)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(Carpeta + nombre + ".png");
            Assert.AreEqual(TextureImporterNPOTScale.None, imp.npotScale, "sin NPOT: si no, el ancho y alto cambian y la foto se deforma (corre ImportarFotosCarga.Aplicar)");
            Assert.IsFalse(imp.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, imp.GetPlatformTextureSettings("Standalone").textureCompression);
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(Carpeta + nombre + ".png");
            Assert.AreEqual(1376, t.width);
            Assert.AreEqual(768, t.height);
        }

        [Test]
        public void Logo_EsElMismoSpriteDelMenuPrincipal()
        {
            Assert.AreEqual("Inicio/Logo", PantallaCargando.RutaLogo);
            Assert.IsNotNull(Resources.Load<Sprite>(PantallaCargando.RutaLogo), "Resources/Inicio/Logo existe como Sprite");
        }
    }
}
