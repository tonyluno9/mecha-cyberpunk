using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    public class CalculoAmbienteSonoroPruebas
    {
        private static readonly NivelesAmbiente Niveles = new NivelesAmbiente(0.5f, 0.2f, 0.4f, 0.6f, 0.25f, 0.5f);

        [Test]
        public void Calcular_AfueraDeNocheLloviendo_SuenaLluviaNeonYTraficoReducidoSinMusica()
        {
            MezclaAmbiente m = CalculoAmbienteSonoro.Calcular(false, true, true, Niveles);
            Assert.AreEqual(0.5f, m.Lluvia, 0.0001f);
            Assert.AreEqual(0.2f, m.Neon, 0.0001f);
            Assert.AreEqual(0.2f, m.Trafico, 0.0001f);
            Assert.AreEqual(0f, m.Musica, 0.0001f);
        }

        [Test]
        public void Calcular_DentroDeUnLocal_SubeLaMusicaYBajaLaLluvia()
        {
            MezclaAmbiente afuera = CalculoAmbienteSonoro.Calcular(false, true, true, Niveles);
            MezclaAmbiente dentro = CalculoAmbienteSonoro.Calcular(true, true, true, Niveles);
            Assert.Greater(dentro.Musica, afuera.Musica);
            Assert.Less(dentro.Lluvia, afuera.Lluvia);
            Assert.AreEqual(0.125f, dentro.Lluvia, 0.0001f);
            Assert.AreEqual(0f, dentro.Neon, 0.0001f);
        }

        [Test]
        public void Calcular_DeDiaSinLluvia_SoloTraficoCompleto()
        {
            MezclaAmbiente m = CalculoAmbienteSonoro.Calcular(false, false, false, Niveles);
            Assert.AreEqual(0f, m.Lluvia, 0.0001f);
            Assert.AreEqual(0f, m.Neon, 0.0001f);
            Assert.AreEqual(0.4f, m.Trafico, 0.0001f);
        }

        [Test]
        public void Acercar_AvanzaALaVelocidadPedidaSinPasarseDelObjetivo()
        {
            Assert.AreEqual(0.5f, CalculoAmbienteSonoro.Acercar(0f, 1f, 0.5f, 1f), 0.0001f);
            Assert.AreEqual(1f, CalculoAmbienteSonoro.Acercar(0.9f, 1f, 0.5f, 1f), 0.0001f);
            Assert.AreEqual(0.2f, CalculoAmbienteSonoro.Acercar(0.7f, 0.2f, 1f, 1f), 0.0001f);
        }

        [Test]
        public void Acercar_ConTiempoDeCambioCero_SaltaAlObjetivo()
        {
            Assert.AreEqual(0.8f, CalculoAmbienteSonoro.Acercar(0f, 0.8f, 0.016f, 0f), 0.0001f);
        }
    }
}
