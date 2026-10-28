using System;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>Reglas de la persecución de drones y patrullas (T-340): cantidades por nivel, avance y captura.</summary>
    public class CalculoPersecucionPruebas
    {
        private const float VelocidadDron = 10f;
        private const float VelocidadPatrulla = 15f;
        private const float Seguimiento = 6f;
        private const float Captura = 3f;
        private const float Tolerancia = 0.0001f;

        private static CalculoPersecucion Crear()
        {
            return new CalculoPersecucion(new[] { 1, 2, 2 }, new[] { 0, 0, 1 }, VelocidadDron, VelocidadPatrulla, Seguimiento, Captura);
        }

        [Test]
        public void Cantidades_PorNivel_SonLasDelDiseno()
        {
            CalculoPersecucion calculo = Crear();
            Assert.AreEqual(0, calculo.Drones(0));
            Assert.AreEqual(0, calculo.Patrullas(0));
            Assert.AreEqual(1, calculo.Drones(1));
            Assert.AreEqual(0, calculo.Patrullas(1));
            Assert.AreEqual(2, calculo.Drones(2));
            Assert.AreEqual(0, calculo.Patrullas(2));
            Assert.AreEqual(2, calculo.Drones(3));
            Assert.AreEqual(1, calculo.Patrullas(3));
        }

        [Test]
        public void Cantidades_ConNivelFueraDeRango_SeRecortan()
        {
            CalculoPersecucion calculo = Crear();
            Assert.AreEqual(0, calculo.Drones(-5));
            Assert.AreEqual(0, calculo.Patrullas(-1));
            Assert.AreEqual(calculo.Drones(3), calculo.Drones(99));
            Assert.AreEqual(calculo.Patrullas(3), calculo.Patrullas(99));
        }

        [Test]
        public void Paso_Lejos_AvanzaVelocidadPorTiempo()
        {
            CalculoPersecucion calculo = Crear();
            Assert.AreEqual(VelocidadDron * 0.5f, calculo.Paso(true, 100f, 0.5f), Tolerancia);
            Assert.AreEqual(VelocidadPatrulla * 0.5f, calculo.Paso(false, 100f, 0.5f), Tolerancia);
        }

        [Test]
        public void Paso_CercaDelSeguimiento_NoLoRebasa()
        {
            CalculoPersecucion calculo = Crear();
            Assert.AreEqual(1f, calculo.Paso(true, Seguimiento + 1f, 10f), Tolerancia, "Solo le faltan 1 m para quedarse a la distancia de seguimiento");
            Assert.AreEqual(0f, calculo.Paso(true, Seguimiento, 1f), Tolerancia);
            Assert.AreEqual(0f, calculo.Paso(true, 2f, 1f), Tolerancia, "Más cerca que el seguimiento no se acerca más");
            Assert.AreEqual(0f, calculo.Paso(true, 50f, 0f), Tolerancia);
            Assert.AreEqual(0f, calculo.Paso(true, 50f, -1f), Tolerancia);
        }

        [Test]
        public void Alcanzado_SoloDentroDeLaDistanciaDeCaptura()
        {
            CalculoPersecucion calculo = Crear();
            Assert.IsTrue(calculo.Alcanzado(Captura));
            Assert.IsTrue(calculo.Alcanzado(0f));
            Assert.IsFalse(calculo.Alcanzado(Captura + 0.01f));
        }

        [Test]
        public void Constructor_ConDatosInvalidos_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new CalculoPersecucion(new int[0], new int[0], 1f, 1f, 2f, 1f));
            Assert.Throws<ArgumentException>(() => new CalculoPersecucion(new[] { 1 }, new[] { 0, 0 }, 1f, 1f, 2f, 1f));
            Assert.Throws<ArgumentException>(() => new CalculoPersecucion(new[] { 1 }, new[] { 0 }, 0f, 1f, 2f, 1f));
            Assert.Throws<ArgumentException>(() => new CalculoPersecucion(new[] { 1 }, new[] { 0 }, 1f, 1f, 1f, 2f));
        }

        [Test]
        public void ConfigPersecucion_PorDefecto_DaUnDronEnNivel1YUnaPatrullaEnNivel3()
        {
            ConfigPersecucion config = ScriptableObject.CreateInstance<ConfigPersecucion>();
            try
            {
                CalculoPersecucion calculo = config.Crear();
                Assert.AreEqual(1, calculo.Drones(1));
                Assert.AreEqual(0, calculo.Patrullas(2));
                Assert.AreEqual(1, calculo.Patrullas(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
