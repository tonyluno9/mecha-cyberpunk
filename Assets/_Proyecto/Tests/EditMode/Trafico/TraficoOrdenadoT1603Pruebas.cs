using MechaCyberpunk.Trafico.Calle;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1603: la regla de paso por cruce (ReservaCruces) y la detección de choques entre cajas de carros (EstadisticasTrafico.Solapan).</summary>
    public class TraficoOrdenadoT1603Pruebas
    {
        private const int Cruce = 7;

        [Test]
        public void Reserva_DosCarrosDeEjesCruzados_SoloPasaElPrimeroHastaQueSuelta()
        {
            ReservaCruces reservas = new ReservaCruces();
            Assert.IsTrue(reservas.Solicitar(Cruce, 1, 1f, 0f, false, 0f), "El primero pasa");
            Assert.IsFalse(reservas.Solicitar(Cruce, 2, 0f, 1f, false, 0.1f), "El de la otra calle espera");
            Assert.IsFalse(reservas.Solicitar(Cruce, 2, 0f, 1f, false, 0.2f), "Sigue esperando mientras el otro no suelte");
            reservas.Liberar(Cruce, 1);
            Assert.IsTrue(reservas.Solicitar(Cruce, 2, 0f, 1f, false, 0.3f), "Suelto el cruce, pasa");
        }

        [Test]
        public void Reserva_CarrosDelMismoEje_PasanJuntosAunqueVayanEnSentidoContrario()
        {
            ReservaCruces reservas = new ReservaCruces();
            Assert.IsTrue(reservas.Solicitar(Cruce, 1, 1f, 0f, false, 0f));
            Assert.IsTrue(reservas.Solicitar(Cruce, 2, -1f, 0f, false, 0.1f), "Sentido contrario, mismo eje");
            Assert.IsTrue(reservas.Solicitar(Cruce, 3, 1f, 0.1f, false, 0.2f), "Casi el mismo eje");
            Assert.AreEqual(3, reservas.Titulares(Cruce, 0.3f));
        }

        [Test]
        public void Reserva_GiroALaIzquierda_PasaSoloYNadieSeLeAdelanta()
        {
            ReservaCruces reservas = new ReservaCruces();
            Assert.IsTrue(reservas.Solicitar(Cruce, 1, 1f, 0f, false, 0f));
            Assert.IsFalse(reservas.Solicitar(Cruce, 2, -1f, 0f, true, 0.1f), "El que gira a la izquierda espera a que se vacíe");
            Assert.IsFalse(reservas.Solicitar(Cruce, 3, 1f, 0f, false, 0.2f), "Uno paralelo que llega después no se le adelanta");
            reservas.Liberar(Cruce, 1);
            Assert.IsTrue(reservas.Solicitar(Cruce, 2, -1f, 0f, true, 0.3f));
            Assert.IsFalse(reservas.Solicitar(Cruce, 3, 1f, 0f, false, 0.4f), "Con uno girando adentro, nadie más");
        }

        [Test]
        public void Reserva_LaQueNoSeSuelta_CaducaYElCruceNoSeCierraParaSiempre()
        {
            ReservaCruces reservas = new ReservaCruces();
            Assert.IsTrue(reservas.Solicitar(Cruce, 1, 1f, 0f, false, 0f));
            Assert.IsFalse(reservas.Solicitar(Cruce, 2, 0f, 1f, false, ReservaCruces.SegundosVigencia - 1f));
            Assert.IsTrue(reservas.Solicitar(Cruce, 2, 0f, 1f, false, ReservaCruces.SegundosVigencia + 0.1f), "Pasada la vigencia el cruce queda libre");
        }

        [Test]
        public void Reserva_QuienDejaDePedir_PierdeSuLugarEnLaFila()
        {
            ReservaCruces reservas = new ReservaCruces();
            Assert.IsTrue(reservas.Solicitar(Cruce, 1, 1f, 0f, false, 0f));
            Assert.IsFalse(reservas.Solicitar(Cruce, 2, 0f, 1f, false, 0.1f));
            reservas.Liberar(Cruce, 1);
            // el 2 se fue (se recicló): ya no estorba al 3 aunque llevara más tiempo
            Assert.IsTrue(reservas.Solicitar(Cruce, 3, 1f, 0f, false, 2f));
        }

        [Test]
        public void Solapan_DosCarrosEnFilaPegados_Si_YConSeisMetrosDeSeparacion_No()
        {
            Vector2 adelante = Vector2.right;
            Assert.IsTrue(EstadisticasTrafico.Solapan(Vector2.zero, adelante, new Vector2(3f, 0f), adelante, EstadisticasTrafico.MitadLargo, EstadisticasTrafico.MitadAncho));
            Assert.IsFalse(EstadisticasTrafico.Solapan(Vector2.zero, adelante, new Vector2(10.5f, 0f), adelante, EstadisticasTrafico.MitadLargo, EstadisticasTrafico.MitadAncho));
        }

        [Test]
        public void Solapan_CarrosDeCarrilesVecinos_No_PeroUnoAtravesadoEnElCarril_Si()
        {
            Vector2 adelante = Vector2.right;
            Assert.IsFalse(EstadisticasTrafico.Solapan(Vector2.zero, adelante, new Vector2(0f, 4.8f), -adelante, EstadisticasTrafico.MitadLargo, EstadisticasTrafico.MitadAncho), "Sentidos contrarios, 4.8 m de lado");
            Assert.IsTrue(EstadisticasTrafico.Solapan(Vector2.zero, adelante, new Vector2(0.5f, 1.5f), Vector2.up, EstadisticasTrafico.MitadLargo, EstadisticasTrafico.MitadAncho), "Uno cruzado encima");
        }
    }
}
