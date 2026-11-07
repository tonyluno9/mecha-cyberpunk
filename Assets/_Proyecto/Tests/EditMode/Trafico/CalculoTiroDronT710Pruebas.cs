using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-710: qué tan cerca pasa la bala del dron del jugador y cuándo silba.</summary>
    public class CalculoTiroDronT710Pruebas
    {
        [Test]
        public void BalaQuePasaAUnMetro_RozaYSuenaEnElPuntoMasCercano()
        {
            Vector3 origen = new Vector3(0f, 7f, 0f);
            Vector3 jugador = new Vector3(0f, 1.2f, 0f);
            Vector3 direccion = (jugador + Vector3.right - origen).normalized;
            float d = CalculoTiroDron.DistanciaALaBala(origen, direccion, jugador, out Vector3 masCercano);
            Assert.That(d, Is.InRange(0.8f, 1.0f));
            Assert.IsTrue(CalculoTiroDron.Roza(d, CalculoTiroDron.RadioRoce));
            Assert.Less(Vector3.Distance(masCercano, jugador), 1.01f);
        }

        [Test]
        public void BalaLejana_NoRoza()
        {
            float d = CalculoTiroDron.DistanciaALaBala(Vector3.zero, Vector3.forward, new Vector3(10f, 0f, 5f), out _);
            Assert.AreEqual(10f, d, 0.001f);
            Assert.IsFalse(CalculoTiroDron.Roza(d, CalculoTiroDron.RadioRoce));
        }

        [Test]
        public void BalaQueSaleParaOtroLado_NoRozaAlQueEstaDetras()
        {
            float d = CalculoTiroDron.DistanciaALaBala(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), out Vector3 masCercano);
            Assert.AreEqual(5f, d, 0.001f);
            Assert.AreEqual(Vector3.zero, masCercano);
        }
    }
}
