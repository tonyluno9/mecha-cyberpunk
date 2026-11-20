using System.Linq;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1516: qué peatones platican y dónde se paran en su corro.</summary>
    public class ReglaPlaticaT1516Pruebas
    {
        [Test]
        public void CuantosPlatican_UnoDeCadaOcho_DeVeinticuatro_SonTres()
        {
            Assert.AreEqual(3, ReglaPlatica.CuantosPlatican(24, 8));
        }

        [Test]
        public void CuantosPlatican_SinConfigurar_Nadie()
        {
            Assert.AreEqual(0, ReglaPlatica.CuantosPlatican(24, 0));
        }

        [Test]
        public void CuantosPlatican_PocaGente_NoPasaDeLosQueHay()
        {
            Assert.AreEqual(0, ReglaPlatica.CuantosPlatican(4, 8), "no hay lugar para un corro");
            Assert.AreEqual(2, ReglaPlatica.CuantosPlatican(6, 2), "nunca más de los que quedan desde el índice inicial");
        }

        [TestCase(24, 8, 1)]
        [TestCase(16, 8, 1)]
        [TestCase(48, 8, 3)]
        public void Corros_SonParejasYUnTrioSiSobraUno(int total, int unoDeCada, int corrosEsperados)
        {
            var corros = ReglaPlatica.Corros(total, unoDeCada);
            Assert.AreEqual(corrosEsperados, corros.Count);
            Assert.AreEqual(ReglaPlatica.CuantosPlatican(total, unoDeCada), corros.Sum(c => c.Tamano));
            Assert.IsTrue(corros.All(c => c.Tamano == 2 || c.Tamano == 3));
            Assert.AreEqual(ReglaPlatica.IndiceInicial, corros[0].Primero);
        }

        [Test]
        public void Corros_NoSeEmpalman()
        {
            var corros = ReglaPlatica.Corros(48, 8);
            for (int i = 1; i < corros.Count; i++)
            {
                Assert.AreEqual(corros[i - 1].Primero + corros[i - 1].Tamano, corros[i].Primero);
            }
        }

        [Test]
        public void Lugar_DeUnaPareja_EstaFrenteAFrenteAlRadioDelCorro()
        {
            Vector3 a = ReglaPlatica.Lugar(0, 2);
            Vector3 b = ReglaPlatica.Lugar(1, 2);
            Assert.AreEqual(ReglaPlatica.RadioCorro, a.magnitude, 1e-4f);
            Assert.AreEqual(ReglaPlatica.RadioCorro * 2f, Vector3.Distance(a, b), 1e-4f);
        }

        [Test]
        public void Lugar_DeUnTrio_ReparteTresPuntosAlMismoRadio()
        {
            Vector3[] lugares = { ReglaPlatica.Lugar(0, 3), ReglaPlatica.Lugar(1, 3), ReglaPlatica.Lugar(2, 3) };
            foreach (Vector3 l in lugares)
            {
                Assert.AreEqual(ReglaPlatica.RadioCorro, l.magnitude, 1e-4f);
            }
            Assert.AreEqual(Vector3.Distance(lugares[0], lugares[1]), Vector3.Distance(lugares[1], lugares[2]), 1e-4f);
        }
    }
}
