using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-656: cuentas puras de acomodarse en la puerta del carro.</summary>
    public class ReglasEntradaCarroPruebas
    {
        [Test]
        public void SegundosAcercar_PegadoYLejos_SeAcotan()
        {
            Assert.AreEqual(ReglasEntradaCarro.SegundosMinimosAcercar, ReglasEntradaCarro.SegundosAcercar(0f), 1e-5f);
            Assert.AreEqual(ReglasEntradaCarro.SegundosMaximosAcercar, ReglasEntradaCarro.SegundosAcercar(50f), 1e-5f);
            Assert.AreEqual(0.2f, ReglasEntradaCarro.SegundosAcercar(0.2f * ReglasEntradaCarro.VelocidadAcercar), 1e-5f);
        }

        [Test]
        public void PuntoPuerta_ArrimaElPuntoHaciaElAsientoSinCambiarLaAltura()
        {
            Vector3 punto = new Vector3(-2f, 0f, 1f);
            Vector3 asiento = new Vector3(-0.4f, 0.8f, 1f);
            Vector3 puerta = ReglasEntradaCarro.PuntoPuerta(punto, asiento);
            Assert.AreEqual(punto.x + ReglasEntradaCarro.RetiroPuerta, puerta.x, 1e-4f);
            Assert.AreEqual(0f, puerta.y, 1e-5f);
            Assert.AreEqual(1f, puerta.z, 1e-4f);
        }

        [Test]
        public void PuntoPuerta_AsientoMuyCerca_NoPasaDelAsiento()
        {
            Vector3 puerta = ReglasEntradaCarro.PuntoPuerta(Vector3.zero, new Vector3(0.3f, 1f, 0f));
            Assert.AreEqual(0.3f, puerta.x, 1e-4f);
        }

        [Test]
        public void PuertaMasCercana_ElijeLaDelLadoDelJugador()
        {
            Vector3 izquierda = new Vector3(-2f, 0f, 0f);
            Vector3 derecha = new Vector3(2f, 3f, 0f);
            Assert.AreEqual(derecha, ReglasEntradaCarro.PuertaMasCercana(new Vector3(3f, 0f, 0f), izquierda, derecha));
            Assert.AreEqual(izquierda, ReglasEntradaCarro.PuertaMasCercana(new Vector3(-3f, 0f, 0f), izquierda, derecha));
        }
    }
}
