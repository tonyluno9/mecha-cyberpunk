using MechaCyberpunk.Core;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    public class TextoInteraccionPruebas
    {
        private ConfigHudAPie config;

        [SetUp]
        public void Setup()
        {
            config = ScriptableObject.CreateInstance<ConfigHudAPie>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Candidato_PuertaDelBar_DiceEAbrirBar()
        {
            Assert.AreEqual("E · Abrir Bar", TextoInteraccion.Candidato(TipoCandidatoInteraccion.Puerta, "Bar", config));
        }

        [Test]
        public void Candidato_PuertaSinNombre_DiceSoloEAbrir()
        {
            Assert.AreEqual("E · Abrir", TextoInteraccion.Candidato(TipoCandidatoInteraccion.Puerta, null, config));
        }

        [Test]
        public void Candidato_SinCandidato_EsVacio()
        {
            Assert.AreEqual(string.Empty, TextoInteraccion.Candidato(TipoCandidatoInteraccion.Ninguno, "Bar", config));
        }

        [Test]
        public void Candidato_MontableCarroDePrueba_DiceESubirCarroDePrueba()
        {
            Assert.AreEqual("E · Subir Carro de prueba",
                TextoInteraccion.Candidato(TipoCandidatoInteraccion.Montable, "Carro de prueba", config));
        }

        [Test]
        public void Candidato_MontableSinNombre_DiceSoloESubir()
        {
            Assert.AreEqual("E · Subir", TextoInteraccion.Candidato(TipoCandidatoInteraccion.Montable, null, config));
        }

        [Test]
        public void AvisoNoPuedeBajar_DiceFrenaParaBajar()
        {
            Assert.AreEqual("Frena para bajar", TextoInteraccion.AvisoNoPuedeBajar(config));
        }

        [Test]
        public void Aviso_CerradaPorHorarioDe20a06_DiceAbre2000()
        {
            string aviso = TextoInteraccion.Aviso(ResultadoPuerta.CerradaPorHorario, new HorarioLugar(20f, 6f), config);
            Assert.AreEqual("CERRADO · abre 20:00", aviso);
        }

        [Test]
        public void Aviso_CerradaPorHorarioConMinutos_DiceLaHoraExacta()
        {
            string aviso = TextoInteraccion.Aviso(ResultadoPuerta.CerradaPorHorario, new HorarioLugar(21.5f, 6f), config);
            Assert.AreEqual("CERRADO · abre 21:30", aviso);
        }

        [Test]
        public void Aviso_CerradaPorHorarioSinLugar_DiceProximamente()
        {
            Assert.AreEqual("CERRADO · PRÓXIMAMENTE", TextoInteraccion.Aviso(ResultadoPuerta.CerradaPorHorario, null, config));
        }

        [Test]
        public void Aviso_Bloqueada_DiceProximamente()
        {
            string aviso = TextoInteraccion.Aviso(ResultadoPuerta.Bloqueada, HorarioLugar.Siempre, config);
            Assert.AreEqual("CERRADO · PRÓXIMAMENTE", aviso);
        }

        [Test]
        public void Aviso_AbiertaYYaAbierta_NoAvisanNada()
        {
            Assert.AreEqual(string.Empty, TextoInteraccion.Aviso(ResultadoPuerta.Abierta, new HorarioLugar(20f, 6f), config));
            Assert.AreEqual(string.Empty, TextoInteraccion.Aviso(ResultadoPuerta.YaAbierta, new HorarioLugar(20f, 6f), config));
        }
    
        [Test]
        public void Interactuable_PuedeUsar_DiceEMasTexto()
        {
            Assert.AreEqual("E · Dormir", TextoInteraccion.Interactuable("Dormir", true, config));
        }

        [Test]
        public void Interactuable_NoPuedeUsar_DiceSoloElAviso()
        {
            Assert.AreEqual("TALLER BLOQUEADO", TextoInteraccion.Interactuable("TALLER BLOQUEADO", false, config));
        }

        [Test]
        public void Interactuable_SinTexto_EsVacio()
        {
            Assert.AreEqual(string.Empty, TextoInteraccion.Interactuable(string.Empty, true, config));
            Assert.AreEqual(string.Empty, TextoInteraccion.Interactuable(null, false, config));
        }
}
}
