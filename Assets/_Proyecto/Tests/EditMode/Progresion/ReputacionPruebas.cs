using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ModeloReputacion y ReglaReputacion (T-182): recorte, niveles, nombres, avisos y multiplicador de calor.</summary>
    public class ReputacionPruebas
    {
        private DatosReputacion datos;
        private ModeloReputacion modelo;

        [SetUp]
        public void SetUp()
        {
            datos = ScriptableObject.CreateInstance<DatosReputacion>();
            modelo = new ModeloReputacion(datos);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(datos);

        [Test]
        public void Sumar_PasaDelMaximoOMinimo_SeRecortaA100()
        {
            modelo.Sumar(TipoReputacion.Barrio, 250);
            modelo.Sumar(TipoReputacion.Corporaciones, -250);
            Assert.AreEqual(100, modelo.Valor(TipoReputacion.Barrio));
            Assert.AreEqual(-100, modelo.Valor(TipoReputacion.Corporaciones));
        }

        [TestCase(0, 0)]
        [TestCase(14, 0)]
        [TestCase(15, 1)]
        [TestCase(49, 1)]
        [TestCase(50, 2)]
        [TestCase(100, 2)]
        [TestCase(-14, 0)]
        [TestCase(-15, -1)]
        [TestCase(-49, -1)]
        [TestCase(-50, -2)]
        public void Nivel_SegunElValor_RespetaLosUmbrales(int valor, int esperado)
        {
            modelo.Restablecer(valor, valor);
            Assert.AreEqual(esperado, modelo.Nivel(TipoReputacion.Barrio));
            Assert.AreEqual(esperado, modelo.Nivel(TipoReputacion.Corporaciones));
        }

        [Test]
        public void NombreNivel_PorTipoYNivel_DaLosNombresDelDiseno()
        {
            string[] barrio = { "TRAIDOR", "MAL VISTO", "DESCONOCIDO", "CONOCIDO", "JUSTICIERO" };
            string[] corp = { "ENEMIGO", "FICHADO", "SIN FICHA", "CONTRATISTA", "SOCIO" };
            int[] valores = { -60, -20, 0, 20, 60 };
            for (int i = 0; i < valores.Length; i++)
            {
                modelo.Restablecer(valores[i], valores[i]);
                Assert.AreEqual(barrio[i], modelo.NombreNivel(TipoReputacion.Barrio));
                Assert.AreEqual(corp[i], modelo.NombreNivel(TipoReputacion.Corporaciones));
            }
        }

        [Test]
        public void NivelCambio_SoloAlCruzarUnUmbral_Avisa()
        {
            int avisos = 0;
            TipoReputacion tipoAvisado = TipoReputacion.Corporaciones;
            int nivelAvisado = 0;
            modelo.NivelCambio += (t, n) => { avisos++; tipoAvisado = t; nivelAvisado = n; };

            modelo.Sumar(TipoReputacion.Barrio, 10);
            Assert.AreEqual(0, avisos, "10 no cruza el umbral de 15");
            modelo.Sumar(TipoReputacion.Barrio, 5);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(TipoReputacion.Barrio, tipoAvisado);
            Assert.AreEqual(1, nivelAvisado);
            modelo.Sumar(TipoReputacion.Barrio, 10);
            Assert.AreEqual(1, avisos, "25 sigue en nivel 1");
            modelo.Sumar(TipoReputacion.Barrio, 25);
            Assert.AreEqual(2, avisos);
            Assert.AreEqual(2, nivelAvisado);
        }

        [Test]
        public void Cambio_AvisaSoloSiElValorCambia()
        {
            int cambios = 0;
            modelo.Cambio += () => cambios++;
            modelo.Sumar(TipoReputacion.Barrio, 0);
            Assert.AreEqual(0, cambios);
            modelo.Sumar(TipoReputacion.Barrio, 3);
            Assert.AreEqual(1, cambios);
        }

        [Test]
        public void Restablecer_CambiaValoresYAvisaCambioPeroNoNivelCambio()
        {
            int cambios = 0;
            int niveles = 0;
            modelo.Cambio += () => cambios++;
            modelo.NivelCambio += (_, __) => niveles++;

            modelo.Restablecer(60, -60);

            Assert.AreEqual(60, modelo.Valor(TipoReputacion.Barrio));
            Assert.AreEqual(-60, modelo.Valor(TipoReputacion.Corporaciones));
            Assert.AreEqual(2, modelo.Nivel(TipoReputacion.Barrio));
            Assert.AreEqual(-2, modelo.Nivel(TipoReputacion.Corporaciones));
            Assert.AreEqual(1, cambios);
            Assert.AreEqual(0, niveles);
        }

        [TestCase(-2, 1.3f)]
        [TestCase(-1, 1.15f)]
        [TestCase(0, 1f)]
        [TestCase(1, 1f)]
        [TestCase(2, 1f)]
        [TestCase(7, 1f)]
        public void MultiplicadorCalor_PorNivelDeCorporaciones_SigueLaTabla(int nivel, float esperado)
        {
            Assert.AreEqual(esperado, ReglaReputacion.MultiplicadorCalor(datos, nivel), 0.0001f);
        }

        [Test]
        public void MultiplicadorCalor_SinDatos_EsUno()
        {
            Assert.AreEqual(1f, ReglaReputacion.MultiplicadorCalor(null, -2));
        }
    }
}
