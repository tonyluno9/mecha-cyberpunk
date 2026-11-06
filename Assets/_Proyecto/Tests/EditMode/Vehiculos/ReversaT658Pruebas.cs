using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-658: cambio de sentido sin tirones (espera y rampa del motor) y volante con velocidad limitada.</summary>
    public class ReversaT658Pruebas
    {
        private const float Dt = 0.02f;

        private static ParametrosManejo Parametros()
        {
            return new ParametrosManejo
            {
                Masa = 1100f,
                VelocidadMaxima = 16f,
                VelocidadReversa = 5f,
                UmbralReversa = 0.5f,
                FuerzaMotor = 6200f,
                FuerzaReversa = 3500f,
                FuerzaFreno = 9000f,
                FuerzaFrenoMano = 6000f,
                ResistenciaRodadura = 500f,
                EsperaCambioSentido = 0.25f,
                RampaMarcha = 0.5f,
            };
        }

        [Test]
        public void Factor_FrenandoContraElMovimiento_EsCeroYMarcaCambio()
        {
            var marcha = new CalculoMarcha();
            Assert.AreEqual(0f, marcha.Factor(Parametros(), 0f, 1f, 8f, Dt));
            Assert.IsTrue(marcha.EnCambio);
        }

        [Test]
        public void Factor_TrasFrenarYParar_EsperaYLuegoSubeSuave()
        {
            var marcha = new CalculoMarcha();
            ParametrosManejo p = Parametros();
            marcha.Factor(p, 0f, 1f, 8f, Dt);
            float t = 0f;
            while (t < p.EsperaCambioSentido - 0.05f)
            {
                Assert.AreEqual(0f, marcha.Factor(p, 0f, 1f, 0.1f, Dt), "Espera parado sin empujar");
                t += Dt;
            }
            float anterior = 0f;
            float maxSalto = 0f;
            for (int i = 0; i < 100; i++)
            {
                float f = marcha.Factor(p, 0f, 1f, -0.1f, Dt);
                maxSalto = System.Math.Max(maxSalto, f - anterior);
                Assert.GreaterOrEqual(f, anterior - 1e-6f, "Nunca baja");
                anterior = f;
            }
            Assert.AreEqual(1f, anterior, 1e-4f, "Llega a toda la fuerza");
            Assert.Less(maxSalto, 0.12f, "Sin saltos: sube de a poco en cada paso");
        }

        [Test]
        public void Factor_ArrancarAdelanteDesdeParado_EsUnoDeInmediato()
        {
            var marcha = new CalculoMarcha();
            Assert.AreEqual(1f, marcha.Factor(Parametros(), 1f, 0f, 0f, Dt));
        }

        [Test]
        public void Factor_ArrancarEnReversaDesdeParado_SubeSuaveSinEspera()
        {
            var marcha = new CalculoMarcha();
            ParametrosManejo p = Parametros();
            float primero = marcha.Factor(p, 0f, 1f, 0f, Dt);
            Assert.Greater(primero, 0f);
            Assert.Less(primero, 0.1f);
        }

        [Test]
        public void Factor_YaVaEnElSentidoPedido_EsUno()
        {
            var marcha = new CalculoMarcha();
            Assert.AreEqual(1f, marcha.Factor(Parametros(), 0f, 1f, -3f, Dt));
            Assert.AreEqual(1f, marcha.Factor(Parametros(), 1f, 0f, 5f, Dt));
        }

        [Test]
        public void Factor_ConParametrosEnCero_NoEsperaNiRampa()
        {
            var marcha = new CalculoMarcha();
            ParametrosManejo p = Parametros();
            p.EsperaCambioSentido = 0f;
            p.RampaMarcha = 0f;
            marcha.Factor(p, 0f, 1f, 8f, Dt);
            Assert.AreEqual(1f, marcha.Factor(p, 0f, 1f, 0f, Dt));
        }

        [Test]
        public void Factor_SoltarLasTeclas_ReiniciaElCambio()
        {
            var marcha = new CalculoMarcha();
            marcha.Factor(Parametros(), 0f, 1f, 8f, Dt);
            marcha.Factor(Parametros(), 0f, 0f, 3f, Dt);
            Assert.IsFalse(marcha.EnCambio);
        }

        [Test]
        public void FuerzaLongitudinal_ConFactorCero_ParadoNoDaReversaPeroFrenaSiSeMueve()
        {
            ParametrosManejo p = Parametros();
            Assert.AreEqual(0f, CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, 0f, Dt, 0f), 1e-3f, "Parado: sin empuje");
            float conRodando = CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, 0.3f, Dt, 0f);
            Assert.Less(conRodando, 0f, "Rodando hacia adelante: lo detiene");
            Assert.AreEqual(-p.FuerzaReversa * 0.5f, CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, 0f, Dt, 0.5f), 1f, "Con factor 0.5 da la mitad de la reversa");
        }

        [Test]
        public void FuerzaLongitudinal_SinFactor_NoCambiaElComportamientoDeAntes()
        {
            ParametrosManejo p = Parametros();
            Assert.AreEqual(-p.FuerzaReversa, CalculoManejo.FuerzaLongitudinal(p, 0f, 1f, 0f, 0f, Dt), p.FuerzaReversa * 0.01f);
            Assert.AreEqual(p.FuerzaMotor, CalculoManejo.FuerzaLongitudinal(p, 1f, 0f, 0f, 0f, Dt), p.FuerzaMotor * 0.01f);
        }

        [Test]
        public void Suavizar_NoPasaDeLaTasaYLlegaAlObjetivo()
        {
            float v = 0f;
            v = CalculoDireccion.Suavizar(v, 1f, Dt, 4f, 6f);
            Assert.AreEqual(0.08f, v, 1e-5f);
            for (int i = 0; i < 20; i++)
            {
                v = CalculoDireccion.Suavizar(v, 1f, Dt, 4f, 6f);
            }
            Assert.AreEqual(1f, v, 1e-5f);
        }

        [Test]
        public void Suavizar_CambiarDeLado_PasaPorElCentroSinSaltar()
        {
            float v = 1f;
            float anterior = v;
            for (int i = 0; i < 60; i++)
            {
                v = CalculoDireccion.Suavizar(v, -1f, Dt, 4f, 6f);
                Assert.LessOrEqual(System.Math.Abs(v - anterior), 6f * Dt + 1e-5f, "Paso limitado");
                anterior = v;
            }
            Assert.AreEqual(-1f, v, 1e-5f);
        }

        [Test]
        public void Suavizar_TasaCero_SaltaDirectoAlObjetivo()
        {
            Assert.AreEqual(-1f, CalculoDireccion.Suavizar(1f, -1f, Dt, 0f, 0f));
        }
    }
}
