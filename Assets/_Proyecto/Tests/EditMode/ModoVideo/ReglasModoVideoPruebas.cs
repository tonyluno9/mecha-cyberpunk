using System;
using MechaCyberpunk.Herramientas.ModoVideo;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class ReglasModoVideoPruebas
    {
        [Test]
        public void AjustarVelocidad_RuedaArriba_Sube()
        {
            Assert.AreEqual(5f, ReglasModoVideo.AjustarVelocidad(4f, 120f), 0.001f);
        }

        [Test]
        public void AjustarVelocidad_RuedaAbajo_Baja()
        {
            Assert.AreEqual(3.2f, ReglasModoVideo.AjustarVelocidad(4f, -120f), 0.001f);
        }

        [Test]
        public void AjustarVelocidad_Extremos_SeLimita()
        {
            Assert.AreEqual(ReglasModoVideo.VelocidadMaxima, ReglasModoVideo.AjustarVelocidad(59f, 12000f), 0.001f);
            Assert.AreEqual(ReglasModoVideo.VelocidadMinima, ReglasModoVideo.AjustarVelocidad(1f, -12000f), 0.001f);
        }

        [Test]
        public void VelocidadEfectiva_Shift_Multiplica()
        {
            Assert.AreEqual(4f, ReglasModoVideo.VelocidadEfectiva(4f, false), 0.001f);
            Assert.AreEqual(16f, ReglasModoVideo.VelocidadEfectiva(4f, true), 0.001f);
        }

        [Test]
        public void NombreCaptura_Fecha_FormatoEsperado()
        {
            Assert.AreEqual("captura_20261031_142507.png", ReglasModoVideo.NombreCaptura(new DateTime(2026, 10, 31, 14, 25, 7)));
        }
    }
}
