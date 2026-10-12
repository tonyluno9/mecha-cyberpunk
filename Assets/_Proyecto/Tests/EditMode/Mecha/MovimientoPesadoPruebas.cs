using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mecha
{
    public class MovimientoPesadoPruebas
    {
        private const float VelocidadMaxima = 4f;
        private const float Aceleracion = 6f;
        private const float Desaceleracion = 10f;
        private const float Dt = 0.02f;

        private static Vector2 Paso(Vector2 entrada, Vector2 velocidad, float dt)
        {
            return MovimientoPesado.Calcular(entrada, velocidad, dt, VelocidadMaxima, Aceleracion, Desaceleracion);
        }

        [Test]
        public void Calcular_AdelanteUnFrame_AceleraGradualmente()
        {
            Vector2 v = Paso(Vector2.up, Vector2.zero, Dt);
            Assert.AreEqual(Aceleracion * Dt, v.y, 1e-5f);
            Assert.Less(v.y, VelocidadMaxima);
        }

        [Test]
        public void Calcular_AdelanteMuchoTiempo_NoPasaDeLaMaxima()
        {
            Vector2 v = Vector2.zero;
            for (int i = 0; i < 500; i++)
            {
                v = Paso(Vector2.up, v, Dt);
                Assert.LessOrEqual(v.magnitude, VelocidadMaxima + 1e-4f);
            }
            Assert.AreEqual(VelocidadMaxima, v.magnitude, 1e-3f);
        }

        [Test]
        public void Calcular_Diagonal_NoPasaDeLaMaxima()
        {
            Vector2 v = Vector2.zero;
            for (int i = 0; i < 500; i++)
            {
                v = Paso(new Vector2(1f, 1f), v, Dt);
                Assert.LessOrEqual(v.magnitude, VelocidadMaxima + 1e-4f);
            }
            Assert.AreEqual(VelocidadMaxima, v.magnitude, 1e-3f);
        }

        [Test]
        public void Calcular_SinEntrada_FrenaHastaCeroSinInvertir()
        {
            Vector2 v = new Vector2(0f, VelocidadMaxima);
            for (int i = 0; i < 100; i++)
            {
                v = Paso(Vector2.zero, v, Dt);
                Assert.GreaterOrEqual(v.y, 0f);
            }
            Assert.AreEqual(0f, v.magnitude);
        }

        [Test]
        public void Calcular_SinEntrada_NoFrenaEnSeco()
        {
            Vector2 v = Paso(Vector2.zero, new Vector2(0f, VelocidadMaxima), Dt);
            Assert.Greater(v.y, 0f);
            Assert.Less(v.y, VelocidadMaxima);
        }

        [Test]
        public void Calcular_ConDtCero_NoCambiaLaVelocidad()
        {
            Vector2 actual = new Vector2(1f, 2f);
            Assert.AreEqual(actual, Paso(Vector2.up, actual, 0f));
            Assert.AreEqual(actual, Paso(Vector2.zero, actual, 0f));
        }

        [Test]
        public void Calcular_VelocidadMayorQueLaMaxima_SeRecorta()
        {
            Vector2 v = Paso(Vector2.up, new Vector2(0f, 10f), Dt);
            Assert.LessOrEqual(v.magnitude, VelocidadMaxima + 1e-4f);
        }
    }
}
