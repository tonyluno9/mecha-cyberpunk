using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Misiones;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-186: los textos puros de las pantallas de misiones y de los avisos del HUD.</summary>
    public class FormatoMisionesPruebas
    {
        private ConfigPantallaMisiones config;
        private ConfigHudAPie configHud;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<ConfigPantallaMisiones>();
            configHud = ScriptableObject.CreateInstance<ConfigHudAPie>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(configHud);
        }

        [Test]
        public void Firmado_UsaMasYMenosTipografico()
        {
            Assert.AreEqual("+8", FormatoHud.Firmado(8));
            Assert.AreEqual("−5", FormatoHud.Firmado(-5));
            Assert.AreEqual("0", FormatoHud.Firmado(0));
        }

        [Test]
        public void TextoAvisoReputacion_UneLasDosPartes()
        {
            Assert.AreEqual("BARRIO −12 · CORPORACIONES +10", FormatoHud.TextoAvisoReputacion(-12, 10, configHud));
        }

        [Test]
        public void TextoAvisoReputacion_SoloLaQueCambio()
        {
            Assert.AreEqual("BARRIO +8", FormatoHud.TextoAvisoReputacion(8, 0, configHud));
            Assert.AreEqual("CORPORACIONES −3", FormatoHud.TextoAvisoReputacion(0, -3, configHud));
            Assert.AreEqual(string.Empty, FormatoHud.TextoAvisoReputacion(0, 0, configHud));
        }

        [Test]
        public void TextoNivel_DiceElNivelNuevoSegunLaReputacion()
        {
            Assert.AreEqual("AHORA ERES CONOCIDO EN EL BARRIO", FormatoHud.TextoNivel(Core.TipoReputacion.Barrio, "CONOCIDO", configHud));
            Assert.AreEqual("AHORA ERES SOCIO CON LAS CORPORACIONES", FormatoHud.TextoNivel(Core.TipoReputacion.Corporaciones, "SOCIO", configHud));
        }

        [Test]
        public void TextoMisionTerminada_Fallida_NoLlevaPago()
        {
            Assert.AreEqual("MISIÓN FALLIDA", FormatoHud.TextoMisionTerminada(false, default, configHud));
        }

        [Test]
        public void TextoMisionTerminada_Cumplida_LlevaElPago()
        {
            var resumen = new ResumenMision(50, null, 0, 8, 0, null, false, MotivoFalloMision.Ninguno, string.Empty);
            Assert.AreEqual("MISIÓN CUMPLIDA · +$50", FormatoHud.TextoMisionTerminada(true, resumen, configHud));
        }

        [Test]
        public void NombreMotivo_Mision_DiceMision()
        {
            Assert.AreEqual("MISIÓN", configHud.NombreMotivo(Core.MotivoDinero.Mision));
        }

        [Test]
        public void Tiempo_RedondeaHaciaArribaYNuncaEsNegativo()
        {
            Assert.AreEqual("02:30", FormatoMisiones.Tiempo(150f));
            Assert.AreEqual("02:30", FormatoMisiones.Tiempo(149.2f));
            Assert.AreEqual("00:00", FormatoMisiones.Tiempo(-3f));
        }

        [Test]
        public void PosicionBarra_CeroEstaAlMedio()
        {
            Assert.AreEqual(0.5f, FormatoMisiones.PosicionBarra(0, -100, 100), 0.0001f);
            Assert.AreEqual(0.44f, FormatoMisiones.PosicionBarra(-12, -100, 100), 0.0001f);
            Assert.AreEqual(1f, FormatoMisiones.PosicionBarra(500, -100, 100), 0.0001f);
        }

        [Test]
        public void Motivo_PorCadaRazon_DiceLasPalabrasDelDiseno()
        {
            DatosMision noche = DatosMision.Crear("m1", "UNA", Core.TipoMision.Ilegal, OrigenMision.Bar, FranjaMision.Noche, null);
            Assert.AreEqual("DE NOCHE", FormatoMisiones.Motivo(MotivoMision.FranjaEquivocada, noche, null, config));
            Assert.AreEqual("YA LO HICISTE HOY", FormatoMisiones.Motivo(MotivoMision.HechaHoy, noche, null, config));
            Assert.AreEqual("YA TIENES UNA MISIÓN", FormatoMisiones.Motivo(MotivoMision.OtraActiva, noche, null, config));
            Assert.AreEqual("NECESITAS MÁS REPUTACIÓN", FormatoMisiones.Motivo(MotivoMision.ReputacionBaja, noche, null, config));
        }

        [Test]
        public void Fila_LlevaPagoYReputacionConSigno()
        {
            DatosMision m = DatosMision.Crear("m3", "ENTREGA URGENTE", Core.TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia,
                null, pago: 400, deltaBarrio: -5, deltaCorporaciones: 8);
            Assert.AreEqual("ENTREGA URGENTE  ·  $400  ·  BARRIO −5 · CORP. +8", FormatoMisiones.Fila(m, config));
        }

        [Test]
        public void Fila_SinPagoNoMuestraPagoYMuestraCeros()
        {
            DatosMision m = DatosMision.Crear("m4", "LUZ", Core.TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera, null, deltaBarrio: 15);
            Assert.AreEqual("LUZ  ·  BARRIO +15 · CORP. 0", FormatoMisiones.Fila(m, config));
        }

        [Test]
        public void Motivo_FranjaDia_DiceDeDia()
        {
            DatosMision dia = DatosMision.Crear("m2", "DOS", Core.TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia, null);
            Assert.AreEqual("DE DÍA", FormatoMisiones.Motivo(MotivoMision.FranjaEquivocada, dia, null, config));
        }
    }
}
