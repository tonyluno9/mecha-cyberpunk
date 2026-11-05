using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-593: cada sonido de arma comparte una sola voz (lo nuevo corta lo anterior), con volumen y tono controlados.</summary>
    public class ReglaMezclaSonidosPruebas
    {
        [TestCase("ARM_PIS_DISPARO")]
        [TestCase("ARM_ESC_DISPARO")]
        [TestCase("ARM_FUS_DISPARO")]
        [TestCase("ARM_SNI_DISPARO")]
        [TestCase("ARM_PIS_RECARGA")]
        [TestCase("ARM_ESC_RECARGA_CARTUCHO")]
        [TestCase("ARM_FUS_RECARGA")]
        [TestCase("ARM_SNI_RECARGA")]
        [TestCase("ARM_FUS_RAFAGA_COLA")]
        [TestCase("ARM_CAMBIAR")]
        [TestCase("ARM_VACIO")]
        public void DisparoRecargaYCambioDeArma_ComparteUnaSolaVoz(string id)
        {
            PoliticaSonido p = ReglaMezclaSonidos.Para(id);
            Assert.AreEqual(ReglaMezclaSonidos.GrupoArma, p.Grupo, id);
            Assert.AreEqual(1, p.MaxVoces, id);
            Assert.That(p.Volumen, Is.InRange(0.3f, 0.95f), id + ": volumen controlado");
        }

        [Test]
        public void ElCasquilloSuenaBajoYElLejanoPermiteVariasVoces()
        {
            Assert.Less(ReglaMezclaSonidos.Para("ARM_CASQUILLO").Volumen, ReglaMezclaSonidos.Para("ARM_PIS_DISPARO").Volumen);
            Assert.Greater(ReglaMezclaSonidos.Para("ARM_DISPARO_LEJANO").MaxVoces, 1);
            Assert.Greater(ReglaMezclaSonidos.Para("ARM_CASQUILLO").SegundosMinimos, 0f);
        }

        [Test]
        public void UnIdSinReglaSuenaNormal()
        {
            PoliticaSonido p = ReglaMezclaSonidos.Para("UI_ACEPTAR");
            Assert.AreEqual(0, p.MaxVoces);
            Assert.AreEqual(1f, p.Volumen);
            Assert.AreEqual(0f, p.VariacionTono);
        }

        [Test]
        public void TonoVariaDentroDelRango()
        {
            Assert.AreEqual(0.97f, ReglaMezclaSonidos.Tono(0.03f, 0f), 1e-5f);
            Assert.AreEqual(1f, ReglaMezclaSonidos.Tono(0.03f, 0.5f), 1e-5f);
            Assert.AreEqual(1.03f, ReglaMezclaSonidos.Tono(0.03f, 1f), 1e-5f);
            Assert.AreEqual(1f, ReglaMezclaSonidos.Tono(0f, 0.9f), 1e-5f);
        }
    }
}
