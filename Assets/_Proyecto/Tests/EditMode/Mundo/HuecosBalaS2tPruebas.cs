using System.IO;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-851 (playtest 6-nov tarde): los huecos de bala D01/D02 sin el halo gris (salía como un círculo alrededor) y con el cráter opaco
    /// (las astillas claras salían medio transparentes, "sin textura").
    /// </summary>
    public class HuecosBalaS2tPruebas
    {
        private const int Lado = 256;

        private static Texture2D Cargar(string archivo)
        {
            string ruta = ImportarS2nVfx.CarpetaDestino + archivo;
            Assert.IsTrue(File.Exists(ruta), "falta " + ruta);
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(ruta));
            return t;
        }

        // Alfa promedio en un anillo de radio r (fracción del lado) alrededor del centro.
        private static float AlfaEnAnillo(Texture2D t, float r)
        {
            float suma = 0f;
            const int Muestras = 72;
            for (int i = 0; i < Muestras; i++)
            {
                float a = i * Mathf.PI * 2f / Muestras;
                int x = Mathf.Clamp(Mathf.RoundToInt(Lado / 2f + Mathf.Cos(a) * r * Lado), 0, Lado - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(Lado / 2f + Mathf.Sin(a) * r * Lado), 0, Lado - 1);
                suma += t.GetPixel(x, y).a;
            }
            return suma / Muestras;
        }

        [TestCase("D01_HuecoConcreto.png")]
        [TestCase("D02_HuecoMetal.png")]
        public void Hueco_SinHaloYConElCraterOpaco(string archivo)
        {
            Texture2D t = Cargar(archivo);
            try
            {
                Assert.AreEqual(Lado, t.width);
                Assert.Greater(AlfaEnAnillo(t, 0.05f), 0.97f, "el centro del cráter es opaco");
                Assert.Greater(AlfaEnAnillo(t, 0.15f), 0.9f, "las astillas del cráter son opacas");
                Assert.Less(AlfaEnAnillo(t, 0.40f), 0.06f, "sin halo gris alrededor (el círculo)");
                Assert.Less(t.GetPixel(2, 2).a + t.GetPixel(Lado - 3, Lado - 3).a, 0.01f, "las esquinas del cuadro no se ven");
            }
            finally
            {
                Object.DestroyImmediate(t);
            }
        }

        [Test]
        public void Importar_DosVeces_NoCambiaLosHuecos()
        {
            ImportarS2nVfx.Importar();
            byte[] antes = File.ReadAllBytes(ImportarS2nVfx.CarpetaDestino + ImportarS2nVfx.Archivos[0]);
            ImportarS2nVfx.Importar();
            CollectionAssert.AreEqual(antes, File.ReadAllBytes(ImportarS2nVfx.CarpetaDestino + ImportarS2nVfx.Archivos[0]));
        }
    }
}
