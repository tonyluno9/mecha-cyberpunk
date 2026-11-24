using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using Fase = MechaCyberpunk.Core.FaseJuego.Fase;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-musica-24nov y T-volumen-ajustes-24nov: pista por fase, nunca hacia atrás, factores de interior y narración, decibeles del deslizador.</summary>
    public class ReglaMusicaFasesPruebas
    {
        [TestCase(Fase.Guiada, ReglaMusicaFases.PianoIntro)]
        [TestCase(Fase.CampoTiro, ReglaMusicaFases.CampoTiro)]
        [TestCase(Fase.Skulls, ReglaMusicaFases.Skulls)]
        [TestCase(Fase.Policia, ReglaMusicaFases.Policia)]
        [TestCase(Fase.Nova, ReglaMusicaFases.Nova)]
        [TestCase(Fase.Muerto, ReglaMusicaFases.PianoIntro)]
        public void PistaDe_CadaFase_SuPista(Fase fase, string pista)
        {
            Assert.AreEqual(pista, ReglaMusicaFases.PistaDe(fase));
        }

        [Test]
        public void Elegir_FaseAnteriorEstandoVivo_SigueLaActual()
        {
            Assert.AreEqual(Fase.Nova, ReglaMusicaFases.Elegir(Fase.Nova, Fase.Policia));
            Assert.AreEqual(Fase.Policia, ReglaMusicaFases.Elegir(Fase.Policia, Fase.Skulls));
            Assert.AreEqual(Fase.Skulls, ReglaMusicaFases.Elegir(Fase.Skulls, Fase.Guiada));
        }

        [Test]
        public void Elegir_FaseSiguiente_Avanza()
        {
            Assert.AreEqual(Fase.Policia, ReglaMusicaFases.Elegir(Fase.Skulls, Fase.Policia));
            Assert.AreEqual(Fase.Muerto, ReglaMusicaFases.Elegir(Fase.Nova, Fase.Muerto));
        }

        [Test]
        public void Elegir_ReintentarDesdeMuerto_VuelveASkulls()
        {
            Assert.AreEqual(Fase.Skulls, ReglaMusicaFases.Elegir(Fase.Muerto, Fase.Skulls));
        }

        [Test]
        public void Elegir_SalirDelCampoDeTiro_VuelveAlPiano()
        {
            Assert.AreEqual(Fase.Guiada, ReglaMusicaFases.Elegir(Fase.CampoTiro, Fase.Guiada));
        }

        [Test]
        public void FactorInterior_ModoLibreEnInterior_Baja()
        {
            Assert.Less(ReglaMusicaFases.FactorInterior(IdLugar.Clinica, Fase.Skulls), 0.5f);
            Assert.AreEqual(1f, ReglaMusicaFases.FactorInterior(IdLugar.Calle, Fase.Skulls));
            Assert.AreEqual(1f, ReglaMusicaFases.FactorInterior(IdLugar.Clinica, Fase.Guiada), "el piano de la parte guiada sigue con Doc y Tino");
        }

        [Test]
        public void FactorNarracion_Narrando_MenosDiezDecibeles()
        {
            Assert.AreEqual(-10f, 20f * (float)System.Math.Log10(ReglaMusicaFases.FactorNarracion(true)), 0.01f);
            Assert.AreEqual(1f, ReglaMusicaFases.FactorNarracion(false));
        }

        [Test]
        public void Decibeles_Deslizador_CurvaLogaritmicaYSilencio()
        {
            Assert.AreEqual(0f, MezclaAudio.Decibeles(1f), 0.001f);
            Assert.AreEqual(-6.02f, MezclaAudio.Decibeles(0.5f), 0.01f);
            Assert.AreEqual(MezclaAudio.Silencio, MezclaAudio.Decibeles(0f));
        }

        [Test]
        public void SeccionDe_Pistas_VanAMusica()
        {
            foreach (string id in ReglaMusicaFases.Pistas)
            {
                Assert.AreEqual(SeccionSonido.Musica, ReglaCatalogoSonidos.SeccionDe(id), id);
            }
        }
    }
}
