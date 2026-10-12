using System;
using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Core
{
    public class ModeloVidaPruebas
    {
        [Test]
        public void Aplicar_DanioMenorQueVida_BajaLaVidaYDevuelveLoRestado()
        {
            var vida = new ModeloVida(100f);

            float restado = vida.Aplicar(30f);

            Assert.AreEqual(30f, restado);
            Assert.AreEqual(70f, vida.Actual);
            Assert.IsFalse(vida.EstaKO);
        }

        [Test]
        public void Aplicar_DanioMayorQueVida_NoBajaDeCeroYDevuelveLoQueRestoDeVerdad()
        {
            var vida = new ModeloVida(50f);
            vida.Aplicar(40f);

            float restado = vida.Aplicar(100f);

            Assert.AreEqual(10f, restado);
            Assert.AreEqual(0f, vida.Actual);
            Assert.IsTrue(vida.EstaKO);
        }

        [Test]
        public void Aplicar_DanioQueLlegaACero_AvisaKOUnaSolaVezAunqueSigaElDanio()
        {
            var vida = new ModeloVida(20f);
            int avisosKO = 0;
            vida.KO += () => avisosKO++;

            vida.Aplicar(20f);
            vida.Aplicar(10f);
            vida.Aplicar(10f);

            Assert.AreEqual(1, avisosKO);
        }

        [Test]
        public void Aplicar_DespuesDelKO_DevuelveCeroYNoAvisaCambio()
        {
            var vida = new ModeloVida(10f);
            vida.Aplicar(10f);
            int avisosCambio = 0;
            vida.Cambio += () => avisosCambio++;

            float restado = vida.Aplicar(5f);

            Assert.AreEqual(0f, restado);
            Assert.AreEqual(0, avisosCambio);
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        public void Aplicar_CantidadCeroONegativa_SeIgnora(float cantidad)
        {
            var vida = new ModeloVida(100f);
            int avisosCambio = 0;
            vida.Cambio += () => avisosCambio++;

            float restado = vida.Aplicar(cantidad);

            Assert.AreEqual(0f, restado);
            Assert.AreEqual(100f, vida.Actual);
            Assert.AreEqual(0, avisosCambio);
        }

        [Test]
        public void Aplicar_DanioMortal_EstaKOYaEsVerdadEnLosAvisos()
        {
            var vida = new ModeloVida(10f);
            bool koEnCambio = false;
            bool koEnKO = false;
            vida.Cambio += () => koEnCambio = vida.EstaKO;
            vida.KO += () => koEnKO = vida.EstaKO;

            vida.Aplicar(10f);

            Assert.IsTrue(koEnCambio);
            Assert.IsTrue(koEnKO);
        }

        [Test]
        public void Aplicar_DanioValido_AvisaCambio()
        {
            var vida = new ModeloVida(100f);
            int avisosCambio = 0;
            vida.Cambio += () => avisosCambio++;

            vida.Aplicar(10f);

            Assert.AreEqual(1, avisosCambio);
        }

        [Test]
        public void Restaurar_DespuesDelKO_VuelveAlMaximoYPermiteOtroKO()
        {
            var vida = new ModeloVida(30f);
            int avisosKO = 0;
            int avisosCambio = 0;
            vida.KO += () => avisosKO++;
            vida.Aplicar(30f);
            vida.Cambio += () => avisosCambio++;

            vida.Restaurar();

            Assert.AreEqual(30f, vida.Actual);
            Assert.IsFalse(vida.EstaKO);
            Assert.AreEqual(1, avisosCambio);

            vida.Aplicar(30f);

            Assert.IsTrue(vida.EstaKO);
            Assert.AreEqual(2, avisosKO);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Constructor_MaximoCeroONegativo_LanzaExcepcion(float maxima)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ModeloVida(maxima));
        }
    }
}
