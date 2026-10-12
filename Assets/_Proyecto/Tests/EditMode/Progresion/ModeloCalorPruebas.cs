using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ModeloCalorPruebas
    {
        private DatosCalor datos;
        private ModeloCalor calor;
        private int avisosValor;
        private int avisosNivel;
        private int ultimoNivel;

        [SetUp]
        public void SetUp()
        {
            datos = ScriptableObject.CreateInstance<DatosCalor>();
            calor = new ModeloCalor(datos);
            avisosValor = 0;
            avisosNivel = 0;
            calor.ValorCambio += _ => avisosValor++;
            calor.NivelCambio += n => { avisosNivel++; ultimoNivel = n; };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(datos);
        }

        [Test]
        public void Sumar_VariasVeces_SubeYNoPasaDelMaximo()
        {
            calor.Sumar(30f, MotivoCalor.Pelea);
            Assert.AreEqual(30f, calor.Valor, 0.0001f);
            calor.Sumar(500f, MotivoCalor.Choque);
            Assert.AreEqual(calor.Maximo, calor.Valor, 0.0001f);
            Assert.AreEqual(100f, calor.Maximo, 0.0001f);
        }

        [TestCase(0f, 0)]
        [TestCase(24.9f, 0)]
        [TestCase(25f, 1)]
        [TestCase(50f, 2)]
        [TestCase(75f, 3)]
        public void Nivel_SegunValor_EsElEsperado(float valor, int esperado)
        {
            if (valor > 0f) calor.Sumar(valor, MotivoCalor.Otro);
            Assert.AreEqual(esperado, calor.Nivel);
        }

        [Test]
        public void NivelCambio_AlCruzarUmbral_AvisaUnaVezYNoSiNoCambia()
        {
            calor.Sumar(20f, MotivoCalor.Otro);
            Assert.AreEqual(0, avisosNivel);
            calor.Sumar(10f, MotivoCalor.Otro);
            Assert.AreEqual(1, avisosNivel);
            Assert.AreEqual(1, ultimoNivel);
            calor.Sumar(10f, MotivoCalor.Otro);
            Assert.AreEqual(1, avisosNivel);
            Assert.AreEqual(3, avisosValor);
        }

        [Test]
        public void Avanzar_AntesDelRetraso_NoEnfria()
        {
            calor.Sumar(60f, MotivoCalor.Otro);
            calor.Avanzar(9f);
            Assert.AreEqual(60f, calor.Valor, 0.0001f);
        }

        [Test]
        public void Avanzar_DespuesDelRetraso_EnfriaPorSegundoPorDt()
        {
            calor.Sumar(60f, MotivoCalor.Otro);
            calor.Avanzar(10f);
            calor.Avanzar(2f);
            Assert.AreEqual(50f, calor.Valor, 0.0001f);
        }

        [Test]
        public void Sumar_ReiniciaElRetraso()
        {
            calor.Sumar(60f, MotivoCalor.Otro);
            calor.Avanzar(15f);
            float tras = calor.Valor;
            calor.Sumar(10f, MotivoCalor.Otro);
            calor.Avanzar(5f);
            Assert.AreEqual(tras + 10f, calor.Valor, 0.0001f);
        }

        [Test]
        public void Avanzar_MuchoTiempo_NuncaBajaDeCero()
        {
            calor.Sumar(30f, MotivoCalor.Otro);
            calor.Avanzar(1000f);
            Assert.AreEqual(0f, calor.Valor, 0.0001f);
            Assert.AreEqual(0, calor.Nivel);
        }

        [Test]
        public void Enfriar_MasQueElValor_QuedaEnCero()
        {
            calor.Sumar(10f, MotivoCalor.Otro);
            calor.Enfriar(50f);
            Assert.AreEqual(0f, calor.Valor, 0.0001f);
        }

        [Test]
        public void Borrar_DejaCeroYAvisaUnaVezDeCadaEvento()
        {
            calor.Sumar(80f, MotivoCalor.Otro);
            avisosValor = 0;
            avisosNivel = 0;
            calor.Borrar();
            Assert.AreEqual(0f, calor.Valor, 0.0001f);
            Assert.AreEqual(0, calor.Nivel);
            Assert.AreEqual(1, avisosValor);
            Assert.AreEqual(1, avisosNivel);
            Assert.AreEqual(0, ultimoNivel);
        }

        [Test]
        public void Borrar_SinCalor_NoAvisa()
        {
            calor.Borrar();
            Assert.AreEqual(0, avisosValor);
            Assert.AreEqual(0, avisosNivel);
        }

        [Test]
        public void SumarYEnfriar_CantidadesNoPositivas_NoHacenNada()
        {
            calor.Sumar(40f, MotivoCalor.Otro);
            avisosValor = 0;
            calor.Sumar(0f, MotivoCalor.Otro);
            calor.Sumar(-5f, MotivoCalor.Otro);
            calor.Enfriar(0f);
            calor.Enfriar(-5f);
            Assert.AreEqual(40f, calor.Valor, 0.0001f);
            Assert.AreEqual(0, avisosValor);
        }
    }
}
