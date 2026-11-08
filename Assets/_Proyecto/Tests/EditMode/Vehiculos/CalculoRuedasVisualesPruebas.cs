using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-742: cuánto ruedan las llantas y cuánto doblan las delanteras.</summary>
    public class CalculoRuedasVisualesPruebas
    {
        private const float Margen = 0.01f;

        [Test]
        public void AvanceGrados_UnaVueltaDeLlanta_SonTrescientosSesentaGrados()
        {
            // Una llanta de 0.3 m de radio da una vuelta en 2 pi * 0.3 m.
            float perimetro = 2f * (float)System.Math.PI * 0.3f;
            Assert.AreEqual(360f, CalculoRuedasVisuales.AvanceGrados(perimetro, 0.3f, 1f), 0.1f);
        }

        [Test]
        public void AvanceGrados_EnReversa_EsNegativo()
        {
            Assert.Less(CalculoRuedasVisuales.AvanceGrados(-3f, 0.3f, 0.02f), 0f);
        }

        [Test]
        public void AvanceGrados_ParadoORadioCero_NoGira()
        {
            Assert.AreEqual(0f, CalculoRuedasVisuales.AvanceGrados(0f, 0.3f, 0.02f), Margen);
            Assert.AreEqual(0f, CalculoRuedasVisuales.AvanceGrados(10f, 0f, 0.02f), Margen);
        }

        [Test]
        public void AvanceGrados_MasRapido_GiraMasEnElMismoTiempo()
        {
            Assert.Greater(CalculoRuedasVisuales.AvanceGrados(12f, 0.3f, 0.02f), CalculoRuedasVisuales.AvanceGrados(4f, 0.3f, 0.02f));
        }

        [Test]
        public void AnguloPorGiro_ModeloDeBicicleta_CoincideConLaFormula()
        {
            // L = 2.5 m, v = 5 m/s, gira 20 grados por segundo: atan(2.5 * 0.349 / 5) = 9.9 grados.
            float angulo = CalculoRuedasVisuales.AnguloPorGiro(2.5f, 5f, 20f, 0.5f, 45f);
            Assert.AreEqual(9.9f, angulo, 0.1f);
        }

        [Test]
        public void AnguloPorGiro_GiroALaIzquierda_EsNegativo()
        {
            Assert.Less(CalculoRuedasVisuales.AnguloPorGiro(2.5f, 5f, -20f, 0.5f, 45f), 0f);
        }

        [Test]
        public void AnguloPorGiro_EnReversa_SeInvierteElSigno()
        {
            float adelante = CalculoRuedasVisuales.AnguloPorGiro(2.5f, 5f, 20f, 0.5f, 45f);
            float atras = CalculoRuedasVisuales.AnguloPorGiro(2.5f, -5f, 20f, 0.5f, 45f);
            Assert.AreEqual(-adelante, atras, Margen);
        }

        [Test]
        public void AnguloPorGiro_CasiParado_EsCero()
        {
            Assert.AreEqual(0f, CalculoRuedasVisuales.AnguloPorGiro(2.5f, 0.2f, 90f, 0.5f, 45f), Margen);
        }

        [Test]
        public void AnguloPorGiro_GiroEnorme_NoPasaDelMaximo()
        {
            Assert.AreEqual(32f, CalculoRuedasVisuales.AnguloPorGiro(2.5f, 1f, 720f, 0.5f, 32f), Margen);
            Assert.AreEqual(-32f, CalculoRuedasVisuales.AnguloPorGiro(2.5f, 1f, -720f, 0.5f, 32f), Margen);
        }

        [Test]
        public void AnguloPorVolante_ParadoYAFondo_EsElAnguloParado()
        {
            Assert.AreEqual(32f, CalculoRuedasVisuales.AnguloPorVolante(1f, 0f, 16f, 32f, 12f), Margen);
            Assert.AreEqual(-32f, CalculoRuedasVisuales.AnguloPorVolante(-1f, 0f, 16f, 32f, 12f), Margen);
        }

        [Test]
        public void AnguloPorVolante_ATodaVelocidad_EsElAnguloRapido()
        {
            Assert.AreEqual(12f, CalculoRuedasVisuales.AnguloPorVolante(1f, 16f, 16f, 32f, 12f), Margen);
            Assert.AreEqual(12f, CalculoRuedasVisuales.AnguloPorVolante(1f, 40f, 16f, 32f, 12f), Margen, "Más rápido que el máximo no dobla menos");
        }

        [Test]
        public void AnguloPorVolante_VolanteAlCentro_EsCero()
        {
            Assert.AreEqual(0f, CalculoRuedasVisuales.AnguloPorVolante(0f, 8f, 16f, 32f, 12f), Margen);
        }

        [Test]
        public void Acercar_NoPasaDeLaVelocidadYLlegaAlObjetivo()
        {
            Assert.AreEqual(4.8f, CalculoRuedasVisuales.Acercar(0f, 30f, 240f, 0.02f), Margen);
            Assert.AreEqual(30f, CalculoRuedasVisuales.Acercar(29f, 30f, 240f, 0.02f), Margen);
            Assert.AreEqual(-4.8f, CalculoRuedasVisuales.Acercar(0f, -30f, 240f, 0.02f), Margen);
        }
    }
}
