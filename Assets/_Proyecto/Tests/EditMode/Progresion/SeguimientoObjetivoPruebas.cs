using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    public class SeguimientoObjetivoPruebas
    {
        // Radio 4 m, alejarse a 25 m más que la mejor, volver a 10 m, 1.5 s mínimo.
        private const float Radio = 4f;
        private const float MargenAlejarse = 25f;
        private const float MargenVolver = 10f;
        private const float TiempoMinimo = 1.5f;

        private SeguimientoObjetivo seguimiento;
        private int avisosAleja;
        private int avisosLlegada;
        private bool ultimoSeAleja;

        [SetUp]
        public void SetUp()
        {
            seguimiento = new SeguimientoObjetivo(Radio, MargenAlejarse, MargenVolver, TiempoMinimo);
            avisosAleja = 0;
            avisosLlegada = 0;
            ultimoSeAleja = false;
            seguimiento.SeAlejaCambio += valor => { avisosAleja++; ultimoSeAleja = valor; };
            seguimiento.Llegada += () => avisosLlegada++;
            seguimiento.FijarPosicion(Vector3.zero);
        }

        private static Vector3 A(float x) => new Vector3(x, 0f, 0f);

        [Test]
        public void Avanzar_DistanciaHorizontal_IgnoraLaAltura()
        {
            seguimiento.Avanzar(0.1f, new Vector3(30f, 50f, 40f));
            Assert.AreEqual(50f, seguimiento.Distancia, 0.001f);
        }

        [Test]
        public void Avanzar_MejorDistancia_SoloBaja()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(0.1f, A(30f));
            seguimiento.Avanzar(0.1f, A(60f));
            Assert.AreEqual(30f, seguimiento.MejorDistancia, 0.001f);
            Assert.AreEqual(60f, seguimiento.Distancia, 0.001f);
        }

        [Test]
        public void Avanzar_PasaElMargenPeroMenosDelTiempoMinimo_NoSeAleja()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(1.0f, A(80f));
            Assert.IsFalse(seguimiento.SeAleja);
            Assert.AreEqual(0, avisosAleja);
        }

        [Test]
        public void Avanzar_PasaElMargenElTiempoMinimo_SeAlejaYAvisaUnaVez()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(1.0f, A(80f));
            seguimiento.Avanzar(0.6f, A(80f));
            seguimiento.Avanzar(0.6f, A(85f));
            Assert.IsTrue(seguimiento.SeAleja);
            Assert.AreEqual(1, avisosAleja);
            Assert.IsTrue(ultimoSeAleja);
        }

        [Test]
        public void Avanzar_VuelveDentroDelMargenAntesDelTiempo_ReiniciaLaCuenta()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(1.0f, A(80f));
            seguimiento.Avanzar(0.1f, A(60f));
            seguimiento.Avanzar(1.0f, A(80f));
            Assert.IsFalse(seguimiento.SeAleja);
        }

        [Test]
        public void Avanzar_SeAlejaYVuelveEntreLosDosMargenes_SigueAlejandose()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(2f, A(80f));
            Assert.IsTrue(seguimiento.SeAleja);
            // 62 m: dentro del margen de alejarse (75) pero fuera del de volver (60): no parpadea.
            seguimiento.Avanzar(0.1f, A(62f));
            Assert.IsTrue(seguimiento.SeAleja);
            Assert.AreEqual(1, avisosAleja);
        }

        [Test]
        public void Avanzar_SeAlejaYVuelveDentroDelMargenMenor_SeApagaYAvisaUnaVez()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(2f, A(80f));
            seguimiento.Avanzar(0.1f, A(58f));
            Assert.IsFalse(seguimiento.SeAleja);
            Assert.AreEqual(2, avisosAleja);
            Assert.IsFalse(ultimoSeAleja);
        }

        [Test]
        public void Avanzar_DentroDelRadio_LlegadaAvisaUnaSolaVez()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(0.1f, A(3f));
            seguimiento.Avanzar(0.1f, A(2f));
            seguimiento.Avanzar(0.1f, A(10f));
            seguimiento.Avanzar(0.1f, A(1f));
            Assert.IsTrue(seguimiento.Llego);
            Assert.AreEqual(1, avisosLlegada);
        }

        [Test]
        public void Avanzar_FueraDelRadio_NoLlega()
        {
            seguimiento.Avanzar(0.1f, A(4.5f));
            Assert.IsFalse(seguimiento.Llego);
            Assert.AreEqual(0, avisosLlegada);
        }

        [Test]
        public void MarcarLlegada_LejosDelObjetivo_LlegaYAvisaUnaVez()
        {
            seguimiento.Avanzar(0.1f, A(100f));
            seguimiento.MarcarLlegada();
            seguimiento.MarcarLlegada();
            Assert.IsTrue(seguimiento.Llego);
            Assert.AreEqual(1, avisosLlegada);
        }

        [Test]
        public void Reiniciar_DespuesDeAlejarseYLlegar_LimpiaTodo()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.Avanzar(2f, A(80f));
            seguimiento.MarcarLlegada();
            seguimiento.Reiniciar();
            Assert.IsFalse(seguimiento.SeAleja);
            Assert.IsFalse(seguimiento.Llego);
            Assert.IsFalse(seguimiento.PosicionResuelta);
            Assert.AreEqual(0f, seguimiento.Distancia);
            Assert.AreEqual(0f, seguimiento.MejorDistancia);
            Assert.IsFalse(ultimoSeAleja);
        }

        [Test]
        public void Reiniciar_ConObjetivoNuevo_PuedeAvisarLlegadaOtraVez()
        {
            seguimiento.Avanzar(0.1f, A(1f));
            seguimiento.Reiniciar();
            seguimiento.FijarPosicion(A(100f));
            seguimiento.Avanzar(0.1f, A(101f));
            Assert.AreEqual(2, avisosLlegada);
            Assert.AreEqual(1f, seguimiento.MejorDistancia, 0.001f);
        }

        [Test]
        public void Avanzar_SinPosicionResuelta_NoAvisaNiMide()
        {
            seguimiento.Reiniciar();
            seguimiento.Avanzar(5f, A(1f));
            seguimiento.Avanzar(5f, A(500f));
            Assert.AreEqual(0, avisosLlegada);
            Assert.AreEqual(0, avisosAleja);
            Assert.AreEqual(0f, seguimiento.Distancia);
            Assert.IsFalse(seguimiento.Llego);
        }

        [Test]
        public void PerderPosicion_DejaDeAvanzar()
        {
            seguimiento.Avanzar(0.1f, A(50f));
            seguimiento.PerderPosicion();
            seguimiento.Avanzar(0.1f, A(1f));
            Assert.IsFalse(seguimiento.Llego);
            Assert.AreEqual(50f, seguimiento.Distancia, 0.001f);
        }
    }
}
