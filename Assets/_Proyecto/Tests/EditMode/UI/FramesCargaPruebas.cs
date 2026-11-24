using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.UI.Inicio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T100: la pantalla de carga usa solo los frames de las cinemáticas, uno al azar por carga y sin repetir el anterior.</summary>
    public class FramesCargaPruebas
    {
        [Test]
        public void ElegirSinRepetir_NuncaDevuelveElAnterior([Values(2, 3, 22)] int cantidad)
        {
            var azar = new System.Random(1234);
            int anterior = -1;
            for (int i = 0; i < 2000; i++)
            {
                int elegido = ReglaFotosCarga.ElegirSinRepetir(cantidad, anterior, azar);
                Assert.That(elegido, Is.InRange(0, cantidad - 1));
                Assert.AreNotEqual(anterior, elegido, "vuelta " + i);
                anterior = elegido;
            }
        }

        [Test]
        public void ElegirSinRepetir_SaleTodosLosFrames()
        {
            var azar = new System.Random(7);
            var vistos = new HashSet<int>();
            int anterior = -1;
            for (int i = 0; i < 2000; i++)
            {
                anterior = ReglaFotosCarga.ElegirSinRepetir(FramesCarga.Cantidad, anterior, azar);
                vistos.Add(anterior);
            }
            Assert.AreEqual(FramesCarga.Cantidad, vistos.Count, "todos salen alguna vez");
        }

        [Test]
        public void ElegirSinRepetir_CasosLimite()
        {
            var azar = new System.Random(3);
            Assert.AreEqual(-1, ReglaFotosCarga.ElegirSinRepetir(0, -1, azar), "sin frames");
            Assert.AreEqual(0, ReglaFotosCarga.ElegirSinRepetir(1, 0, azar), "con uno solo se repite a la fuerza");
            Assert.That(ReglaFotosCarga.ElegirSinRepetir(5, 99, azar), Is.InRange(0, 4), "anterior fuera de rango: cualquiera");
        }

        [Test]
        public void Frames_SonLos22DeLasCinematicas_YCoincidenConLaCarpeta()
        {
            Assert.AreEqual(22, FramesCarga.Cantidad);
            var enCarpeta = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FramesCarga.CarpetaAssets }))
            {
                enCarpeta.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
            }
            CollectionAssert.AreEquivalent(enCarpeta, FramesCarga.Nombres, "FramesCarga.Nombres = los PNG de " + FramesCarga.CarpetaAssets);
            foreach (string n in FramesCarga.Nombres)
            {
                StringAssert.DoesNotContain("REF", n, "sin los frames de referencia");
            }
        }

        [Test]
        public void Frames_SeCarganDesdeResources_Y_LaCargaYaNoUsaL01aL06()
        {
            Assert.AreEqual(FramesCarga.CarpetaRecursos, FotosCarga.CarpetaRecursos);
            for (int i = 0; i < FramesCarga.Cantidad; i++)
            {
                Assert.IsNotNull(Resources.Load<Texture2D>(FramesCarga.Ruta(i)), FramesCarga.Ruta(i));
            }
            Assert.IsNull(Resources.Load<Texture2D>(FramesCarga.CarpetaRecursos + "/L01_Carga_Protagonista"));
        }

        [Test]
        public void Frames_ImportadosComoSprite_SinMipmaps_HastaHD([Values(0, 4, 12, 21)] int indice)
        {
            string ruta = FramesCarga.CarpetaAssets + "/" + FramesCarga.Nombres[indice] + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            Assert.IsNotNull(imp, ruta);
            Assert.AreEqual(TextureImporterType.Sprite, imp.textureType, "corre ImportarFramesCarga.Aplicar");
            Assert.IsFalse(imp.mipmapEnabled);
            Assert.AreEqual(2048, imp.maxTextureSize);
            TextureImporterPlatformSettings pc = imp.GetPlatformTextureSettings("Standalone");
            Assert.IsTrue(pc.textureCompression == TextureImporterCompression.Uncompressed || pc.textureCompression == TextureImporterCompression.CompressedHQ,
                "sin compresión fuerte");
            Assert.IsFalse(pc.crunchedCompression);
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            Assert.AreEqual(1920, t.width, "escalados a 1920x1080 y sin reducir");
            Assert.AreEqual(1080, t.height);
        }

        [Test]
        public void Cubierto_ConFrames16a9_LlenaCualquierPantallaSinDeformar([Values(1920f, 2560f, 1280f, 1440f)] float ancho, [Values(1080f, 1440f, 1024f, 900f)] float alto)
        {
            Vector2 t = ReglaFotosCarga.TamanoCubierto(1920f, 1080f, ancho, alto);
            Assert.AreEqual(1920f / 1080f, t.x / t.y, 1e-3f, "nunca se deforma");
            Assert.GreaterOrEqual(t.x, ancho - 1e-3f);
            Assert.GreaterOrEqual(t.y, alto - 1e-3f);
        }
    }
}
