using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-924 (playtest 9-nov): charcos de forma irregular y orilla suave; sin bloques planos de letrero brillando sobre las entradas.</summary>
    public class CharcosPuertasT924Pruebas
    {
        private static Texture2D Mascara(int forma)
        {
            Texture2D t = new Texture2D(2, 2);
            Assert.IsTrue(t.LoadImage(File.ReadAllBytes(KitS2c.Carpeta + "/Texturas/S2c_Charco_" + forma + ".png")));
            return t;
        }

        [Test]
        public void MascarasDeCharco_TienenOrillaSuaveYForma_NoSonManchaUniforme()
        {
            for (int forma = 0; forma < ColocarS2oCharcos.Formas; forma++)
            {
                Color[] px = Mascara(forma).GetPixels();
                float cubre = px.Count(c => c.r > 0.5f) / (float)px.Length;
                int dentro = px.Count(c => c.r > 0.02f);
                int orilla = px.Count(c => c.r > 0.1f && c.r < 0.9f);
                Assert.Greater(px.Max(c => c.r), 0.95f, "forma " + forma + ": el centro del charco llega a opaco");
                Assert.That(cubre, Is.InRange(0.05f, 0.6f), "forma " + forma + ": no es un cuadro lleno ni un punto");
                Assert.Greater(orilla / (float)dentro, 0.25f, "forma " + forma + ": la orilla suave es buena parte del charco (antes un corte casi seco)");
                // El borde de la textura queda vacío: el charco no se corta en cuadro.
                Assert.AreEqual(0f, px[0].r, 0.001f);
                Assert.AreEqual(0f, px[px.Length - 1].r, 0.001f);
            }
        }

        [Test]
        public void MascarasDeCharco_SonDistintasEntreSi()
        {
            Color[] a = Mascara(0).GetPixels();
            Color[] b = Mascara(1).GetPixels();
            float diferencia = a.Select((c, i) => Mathf.Abs(c.r - b[i].r)).Average();
            Assert.Greater(diferencia, 0.02f, "cada forma de charco es distinta");
        }

        [Test]
        public void Entradas_SinBloquesPlanosDeLetrero()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            Transform[] activos = ColocarT924.BloquesPlanos().Where(t => t.gameObject.activeInHierarchy).ToArray();
            Assert.AreEqual(0, activos.Length, "bloques planos que quedan: " + string.Join(", ", activos.Select(t => ColocarS2.Ruta(t))));
        }
    }
}
