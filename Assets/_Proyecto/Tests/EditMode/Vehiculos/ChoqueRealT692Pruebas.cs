using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-692: solo cuentan los choques reales; una rapidez imposible (glitch de física) no daña y ninguno pasa del tope por choque.</summary>
    public class ChoqueRealT692Pruebas
    {
        private const float Umbral = 6f;
        private const float PorMetro = 7f;
        private const float Vida = 250f;

        [Test]
        public void ChoqueNormal_HaceElDanioDeSiempre()
        {
            Assert.AreEqual((12f - Umbral) * PorMetro, DanioCarro.DanioDeChoque(12f, Umbral, PorMetro, Vida), 0.01f);
        }

        [Test]
        public void BajoElUmbral_NoHaceDanio()
        {
            Assert.AreEqual(0f, DanioCarro.DanioDeChoque(5f, Umbral, PorMetro, Vida), 0.0001f);
        }

        [Test]
        public void RapidezImposible_NoHaceDanio()
        {
            Assert.AreEqual(0f, DanioCarro.DanioDeChoque(300f, Umbral, PorMetro, Vida), 0.0001f, "un carro lanzado a 300 m/s es un glitch, no un choque");
        }

        [Test]
        public void UnSoloChoque_NuncaPasaDel30PorCientoDeLaVida()
        {
            Assert.LessOrEqual(DanioCarro.DanioDeChoque(44f, Umbral, PorMetro, Vida), Vida * 0.3f + 0.01f);
        }
    }
}
