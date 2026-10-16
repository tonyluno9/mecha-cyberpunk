using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-081: las texturas de ventanas generadas por código (pobre, media y alta) y sus mapas de emisión.</summary>
    public class FachadasTexturasPruebas
    {
        private const string RutaTexturas = "Assets/_Proyecto/Mundo/Texturas/Fachada_";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales/Fachada_";
        private const int LadoMinimo = 256;
        private const int CeldasPorLado = 4;
        private const float MinimoPrendidas = 0.15f;
        private const float MaximoPrendidas = 0.45f;
        private const float UmbralEmision = 0.15f;
        private static readonly string[] Variantes = { "Pobre", "Media", "Alta" };

        private static Texture2D Cargar(string variante, bool emision)
        {
            string ruta = RutaTexturas + variante + (emision ? "_Emision" : "") + ".png";
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            Assert.IsNotNull(textura, "Falta la textura " + ruta);
            return textura;
        }

        // Lee el PNG del disco: la textura importada no es legible.
        private static Texture2D Leer(string variante, bool emision)
        {
            Texture2D textura = new Texture2D(2, 2);
            Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(RutaTexturas + variante + (emision ? "_Emision" : "") + ".png")));
            return textura;
        }

        private static float Brillo(Color color)
        {
            return Mathf.Max(color.r, color.g, color.b);
        }

        [Test]
        public void Texturas_LasTresVariantesExisten_ConAlMenos256Pixeles_SinCompresion()
        {
            foreach (string variante in Variantes)
            {
                foreach (bool emision in new[] { false, true })
                {
                    Texture2D textura = Cargar(variante, emision);
                    Assert.GreaterOrEqual(textura.width, LadoMinimo, variante + " ancho");
                    Assert.GreaterOrEqual(textura.height, LadoMinimo, variante + " alto");
                    TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(textura));
                    Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression, variante + " no debe comprimirse");
                    Assert.AreEqual(TextureWrapMode.Repeat, importador.wrapMode, variante + " debe repetirse");
                }
            }
        }

        [Test]
        public void Emision_EntreElQuinceYElCuarentaYCincoPorCientoDeVentanasPrendidas()
        {
            foreach (string variante in Variantes)
            {
                Texture2D textura = Cargar(variante, true);
                Color[] pixeles = Leer(variante, true).GetPixels();
                int celda = textura.width / CeldasPorLado;
                int prendidas = 0;
                for (int cy = 0; cy < CeldasPorLado; cy++)
                {
                    for (int cx = 0; cx < CeldasPorLado; cx++)
                    {
                        Color centro = pixeles[(cy * celda + celda / 2) * textura.width + cx * celda + celda / 2];
                        if (Brillo(centro) > UmbralEmision)
                        {
                            prendidas++;
                        }
                    }
                }
                float porCeldas = prendidas / (float)(CeldasPorLado * CeldasPorLado);
                Assert.GreaterOrEqual(porCeldas, MinimoPrendidas, variante + ": pocas ventanas prendidas");
                Assert.LessOrEqual(porCeldas, MaximoPrendidas, variante + ": demasiadas ventanas prendidas");

                int pixelesPrendidos = 0;
                foreach (Color pixel in pixeles)
                {
                    if (Brillo(pixel) > UmbralEmision)
                    {
                        pixelesPrendidos++;
                    }
                }
                float porPixeles = pixelesPrendidos / (float)pixeles.Length;
                Assert.GreaterOrEqual(porPixeles, MinimoPrendidas, variante + ": poca área prendida");
                Assert.LessOrEqual(porPixeles, MaximoPrendidas, variante + ": demasiada área prendida");
            }
        }

        [Test]
        public void Emision_AlgunasVentanasSonCianOMagenta_YLaMayoriaCalida()
        {
            Texture2D textura = Cargar("Alta", true);
            Color[] pixeles = Leer("Alta", true).GetPixels();
            int calidos = 0;
            int frios = 0;
            foreach (Color pixel in pixeles)
            {
                if (Brillo(pixel) <= UmbralEmision)
                {
                    continue;
                }
                if (pixel.r > pixel.b)
                {
                    calidos++;
                }
                else
                {
                    frios++;
                }
            }
            Assert.Greater(calidos, frios, "La mayoría de las ventanas prendidas deben ser cálidas");
            Assert.Greater(frios, 0, "Debe haber ventanas cian o magenta");
        }

        [Test]
        public void Materiales_FachadaUsaUrpLitConEmisionYTexturas()
        {
            foreach (string variante in Variantes)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + variante + ".mat");
                Assert.IsNotNull(material, "Falta Fachada_" + variante);
                Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"), variante + " sin emisión");
                Assert.AreEqual(Cargar(variante, false), material.GetTexture("_BaseMap"));
                Assert.AreEqual(Cargar(variante, true), material.GetTexture("_EmissionMap"));
            }
        }
    }
}
