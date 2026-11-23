using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Drones;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1665: el escudo holográfico de los drones de patrulla de Nova.</summary>
    public class ReglaEscudoDronPruebas
    {
        [TestCase(TipoDanio.Disparo)]
        [TestCase(TipoDanio.Golpe)]
        [TestCase(TipoDanio.Explosion)]
        public void Absorbe_CualquierTipoDeDanio_EsInmune(TipoDanio tipo)
        {
            Assert.IsTrue(ReglaEscudoDron.Absorbe(tipo));
        }

        [Test]
        public void PuedeMostrar_RespetaElIntervalo()
        {
            Assert.IsTrue(ReglaEscudoDron.PuedeMostrar(5f, -1f, 0.06f), "el primero siempre");
            Assert.IsFalse(ReglaEscudoDron.PuedeMostrar(5.03f, 5f, 0.06f));
            Assert.IsTrue(ReglaEscudoDron.PuedeMostrar(5.07f, 5f, 0.06f));
        }

        [Test]
        public void Destello_DuraTresDecimasYSeDesvanece()
        {
            Assert.AreEqual(0.3f, ReglaEscudoDron.SegundosDestello, 0.0001f);
            Assert.AreEqual(0f, ReglaEscudoDron.Progreso(0f), 0.0001f);
            Assert.AreEqual(1f, ReglaEscudoDron.Progreso(0.3f), 0.0001f);
            Assert.AreEqual(1f, ReglaEscudoDron.Progreso(5f), 0.0001f, "no pasa de 1");
            Assert.AreEqual(1f, ReglaEscudoDron.Alfa(0f), 0.0001f);
            Assert.AreEqual(0f, ReglaEscudoDron.Alfa(1f), 0.0001f);
            Assert.Greater(ReglaEscudoDron.Alfa(0.2f), ReglaEscudoDron.Alfa(0.6f));
        }

        [Test]
        public void Escala_CreceDeLaInicialALaFinal()
        {
            Assert.AreEqual(ReglaEscudoDron.EscalaInicial, ReglaEscudoDron.Escala(0f), 0.0001f);
            Assert.AreEqual(ReglaEscudoDron.EscalaFinal, ReglaEscudoDron.Escala(1f), 0.0001f);
            Assert.Greater(ReglaEscudoDron.Escala(0.5f), ReglaEscudoDron.Escala(0.2f));
        }
    }
}
