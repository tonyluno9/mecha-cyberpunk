using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    public class CalculoCamaraLibrePruebas
    {
        private const float Tolerancia = 0.001f;

        [Test]
        public void LimitarInclinacion_DentroDelLimite_SumaElDelta()
        {
            Assert.AreEqual(30f, CalculoCamaraLibre.LimitarInclinacion(20f, 10f, 85f), Tolerancia);
        }

        [Test]
        public void LimitarInclinacion_PasaElLimite_SeQuedaEnElLimite()
        {
            Assert.AreEqual(85f, CalculoCamaraLibre.LimitarInclinacion(80f, 30f, 85f), Tolerancia);
            Assert.AreEqual(-85f, CalculoCamaraLibre.LimitarInclinacion(-80f, -30f, 85f), Tolerancia);
        }

        [Test]
        public void Rapidez_SegunModo_ElijeLaCorrecta()
        {
            Assert.AreEqual(4.5f, CalculoCamaraLibre.Rapidez(false, false, 4.5f, 8f, 20f), Tolerancia);
            Assert.AreEqual(8f, CalculoCamaraLibre.Rapidez(false, true, 4.5f, 8f, 20f), Tolerancia);
            Assert.AreEqual(20f, CalculoCamaraLibre.Rapidez(true, true, 4.5f, 8f, 20f), Tolerancia);
        }

        [Test]
        public void DireccionHorizontal_ConYaw90_AdelanteVaAlEste()
        {
            Vector3 direccion = CalculoCamaraLibre.DireccionHorizontal(new Vector2(0f, 1f), 90f);
            Assert.AreEqual(1f, direccion.x, Tolerancia);
            Assert.AreEqual(0f, direccion.z, Tolerancia);
        }

        [Test]
        public void DireccionHorizontal_EnDiagonal_NoPasaDeUno()
        {
            Vector3 direccion = CalculoCamaraLibre.DireccionHorizontal(new Vector2(1f, 1f), 0f);
            Assert.AreEqual(1f, direccion.magnitude, Tolerancia);
        }

        [Test]
        public void VelocidadVertical_EnElAire_AcelerahaciaAbajo()
        {
            Assert.AreEqual(-2f, CalculoCamaraLibre.VelocidadVertical(0f, false, 20f, 0.1f, 2f), Tolerancia);
        }

        [Test]
        public void VelocidadVertical_EnElSueloCayendo_QuedaPegada()
        {
            Assert.AreEqual(-2f, CalculoCamaraLibre.VelocidadVertical(-9f, true, 20f, 0.1f, 2f), Tolerancia);
        }
    }
}
