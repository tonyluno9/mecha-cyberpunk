using MechaCyberpunk.Mecha.Cabina;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class CalculoSacudidaPruebas
    {
        private const float DanioReferencia = 30f;
        private const float Minima = 0.15f;
        private const float Maxima = 1f;
        private const float Duracion = 0.5f;
        private const float Dt = 0.02f;

        [Test]
        public void IntensidadInicial_MasDanio_DaMasIntensidad()
        {
            float chico = CalculoSacudida.IntensidadInicial(5f, DanioReferencia, Minima, Maxima);
            float medio = CalculoSacudida.IntensidadInicial(15f, DanioReferencia, Minima, Maxima);
            float grande = CalculoSacudida.IntensidadInicial(30f, DanioReferencia, Minima, Maxima);
            Assert.Less(chico, medio);
            Assert.Less(medio, grande);
            Assert.AreEqual(Maxima, grande, 1e-5f);
        }

        [Test]
        public void IntensidadInicial_DanioCero_DaLaMinima()
        {
            Assert.AreEqual(Minima, CalculoSacudida.IntensidadInicial(0f, DanioReferencia, Minima, Maxima), 1e-5f);
            Assert.Greater(CalculoSacudida.IntensidadInicial(0f, DanioReferencia, Minima, Maxima), 0f);
        }

        [Test]
        public void IntensidadInicial_DanioMuyGrande_NoPasaDeLaMaxima()
        {
            Assert.AreEqual(Maxima, CalculoSacudida.IntensidadInicial(10000f, DanioReferencia, Minima, Maxima), 1e-5f);
        }

        [Test]
        public void Decaer_ConElTiempo_LlegaACeroSinQuedarNegativa()
        {
            float intensidad = Maxima;
            float anterior = intensidad;
            for (int i = 0; i < 1000 && intensidad > 0f; i++)
            {
                intensidad = CalculoSacudida.Decaer(intensidad, Dt, Duracion, Maxima);
                Assert.LessOrEqual(intensidad, anterior);
                Assert.GreaterOrEqual(intensidad, 0f);
                anterior = intensidad;
            }
            Assert.AreEqual(0f, intensidad);
        }

        [Test]
        public void Decaer_LaMaximaTardaLaDuracion()
        {
            Assert.Greater(CalculoSacudida.Decaer(Maxima, Duracion * 0.9f, Duracion, Maxima), 0f);
            Assert.AreEqual(0f, CalculoSacudida.Decaer(Maxima, Duracion, Duracion, Maxima), 1e-5f);
        }

        [Test]
        public void Decaer_ConDtCero_NoCambia()
        {
            Assert.AreEqual(0.6f, CalculoSacudida.Decaer(0.6f, 0f, Duracion, Maxima));
        }

        [Test]
        public void Decaer_DtGigante_QuedaEnCero()
        {
            Assert.AreEqual(0f, CalculoSacudida.Decaer(0.6f, 100f, Duracion, Maxima));
        }
    }
}
