using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    public class CalculoCamaraCarroPruebas
    {
        private const float LimiteLados = 110f;
        private const float LimiteVertical = 60f;
        private const float Espera = 1.5f;
        private const float VelocidadRegreso = 150f;

        [Test]
        public void LimitarMirada_PasaDelLimite_SeQuedaEnElLimite()
        {
            Assert.AreEqual(LimiteLados, CalculoCamaraCarro.LimitarMirada(100f, 50f, LimiteLados));
            Assert.AreEqual(-LimiteLados, CalculoCamaraCarro.LimitarMirada(-100f, -50f, LimiteLados));
            Assert.AreEqual(-LimiteVertical, CalculoCamaraCarro.LimitarMirada(0f, -200f, LimiteVertical));
        }

        [Test]
        public void LimitarMirada_DentroDelLimite_Suma()
        {
            Assert.AreEqual(30f, CalculoCamaraCarro.LimitarMirada(10f, 20f, LimiteLados), 0.0001f);
        }

        [Test]
        public void Regreso_ConPocoTiempoSinMover_NoSeMueve()
        {
            Assert.AreEqual(80f, CalculoCamaraCarro.Regreso(80f, 1.4f, Espera, VelocidadRegreso, 0.1f));
        }

        [Test]
        public void Regreso_PasadaLaEspera_AcercaAlFrenteSinPasarse()
        {
            Assert.AreEqual(65f, CalculoCamaraCarro.Regreso(80f, 1.5f, Espera, VelocidadRegreso, 0.1f), 0.0001f);
            Assert.AreEqual(-65f, CalculoCamaraCarro.Regreso(-80f, 2f, Espera, VelocidadRegreso, 0.1f), 0.0001f);
            Assert.AreEqual(0f, CalculoCamaraCarro.Regreso(5f, 2f, Espera, VelocidadRegreso, 0.1f));
        }

        [Test]
        public void PosicionTercera_DetrasYArribaDelFoco()
        {
            Vector3 posicion = CalculoCamaraCarro.PosicionTercera(new Vector3(0f, 1f, 0f), new Vector3(0f, 0.3f, 2f), 6f, 2.5f);
            Assert.AreEqual(0f, posicion.x, 0.0001f);
            Assert.AreEqual(3.5f, posicion.y, 0.0001f);
            Assert.AreEqual(-6f, posicion.z, 0.0001f);
        }

        [Test]
        public void DistanciaLibre_SinMuro_EsLaIdeal()
        {
            Assert.AreEqual(6.5f, CalculoCamaraCarro.DistanciaLibre(6.5f, float.MaxValue, 0.3f, 1.2f));
            Assert.AreEqual(6.5f, CalculoCamaraCarro.DistanciaLibre(6.5f, 6.5f, 0.3f, 1.2f));
        }

        [Test]
        public void DistanciaLibre_ConMuro_SeQuedaAntesDelMuroSinBajarDelMinimo()
        {
            Assert.AreEqual(3.7f, CalculoCamaraCarro.DistanciaLibre(6.5f, 4f, 0.3f, 1.2f), 0.0001f);
            Assert.AreEqual(1.2f, CalculoCamaraCarro.DistanciaLibre(6.5f, 0.5f, 0.3f, 1.2f), 0.0001f);
        }
    }
}
