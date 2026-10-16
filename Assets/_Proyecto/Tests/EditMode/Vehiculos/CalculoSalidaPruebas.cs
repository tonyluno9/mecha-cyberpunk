using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    public class CalculoSalidaPruebas
    {
        [Test]
        public void Elegir_ConductorLibre_EligeConductor()
        {
            Assert.AreEqual(LadoSalida.Conductor, CalculoSalida.Elegir(true, true));
            Assert.AreEqual(LadoSalida.Conductor, CalculoSalida.Elegir(true, false));
        }

        [Test]
        public void Elegir_ConductorBloqueadoYCopilotoLibre_EligeCopiloto()
        {
            Assert.AreEqual(LadoSalida.Copiloto, CalculoSalida.Elegir(false, true));
        }

        [Test]
        public void Elegir_AmbasBloqueadas_NoElige()
        {
            Assert.AreEqual(LadoSalida.Ninguno, CalculoSalida.Elegir(false, false));
        }

        [Test]
        public void VaLentoParaBajar_ComparaConElMaximo()
        {
            Assert.IsTrue(CalculoSalida.VaLentoParaBajar(0.49f, 0.5f));
            Assert.IsFalse(CalculoSalida.VaLentoParaBajar(0.5f, 0.5f));
            Assert.IsFalse(CalculoSalida.VaLentoParaBajar(8f, 0.5f));
        }

        [Test]
        public void EnRangoDeSubida_CercaYMirando_EsTrue()
        {
            Assert.IsTrue(CalculoCamaraLibre.EnRangoDeSubida(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 1.5f), 2.5f, 60f));
        }

        [Test]
        public void EnRangoDeSubida_LejosODeEspaldas_EsFalse()
        {
            Assert.IsFalse(CalculoCamaraLibre.EnRangoDeSubida(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 3f), 2.5f, 60f));
            Assert.IsFalse(CalculoCamaraLibre.EnRangoDeSubida(Vector3.zero, Vector3.back, new Vector3(0f, 0f, 1.5f), 2.5f, 60f));
        }

        [Test]
        public void EnRangoDeSubida_MirarArribaOAbajo_NoCambiaLaRespuesta()
        {
            Vector3 mirandoAbajo = new Vector3(0f, -0.9f, 1f);
            Assert.IsTrue(CalculoCamaraLibre.EnRangoDeSubida(new Vector3(0f, 1.6f, 0f), mirandoAbajo, new Vector3(0f, 0f, 1.5f), 2.5f, 60f));
        }
    }
}
