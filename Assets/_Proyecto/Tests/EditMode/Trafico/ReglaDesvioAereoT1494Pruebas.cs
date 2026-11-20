using System;
using MechaCyberpunk.Trafico.Drones;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1494: el desvío vertical con que un dron rodea un obstáculo.</summary>
    public class ReglaDesvioAereoT1494Pruebas
    {
        [Test]
        public void Subiendo_AvanzaALaVelocidadDeSubida()
        {
            ReglaDesvioAereo r = new ReglaDesvioAereo(8f, 3f, 40f);
            Assert.AreEqual(4f, r.DesvioSubiendo(0.5f), 0.0001f);
        }

        [Test]
        public void Subiendo_NoPasaDelMaximo()
        {
            ReglaDesvioAereo r = new ReglaDesvioAereo(8f, 3f, 10f);
            r.Fijar(9f);
            Assert.AreEqual(10f, r.DesvioSubiendo(5f), 0.0001f);
        }

        [Test]
        public void Bajando_NoPasaDeCero()
        {
            ReglaDesvioAereo r = new ReglaDesvioAereo(8f, 3f, 40f);
            r.Fijar(1f);
            Assert.AreEqual(0f, r.DesvioBajando(5f), 0.0001f);
            Assert.AreEqual(0.4f, r.DesvioBajando(0.2f), 0.0001f);
        }

        [Test]
        public void Fijar_RecortaAlRango()
        {
            ReglaDesvioAereo r = new ReglaDesvioAereo(8f, 3f, 10f);
            r.Fijar(99f);
            Assert.AreEqual(10f, r.Desvio, 0.0001f);
            r.Fijar(-5f);
            Assert.AreEqual(0f, r.Desvio, 0.0001f);
        }

        [Test]
        public void Constructor_VelocidadCero_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new ReglaDesvioAereo(0f, 3f, 10f));
        }
    }
}
