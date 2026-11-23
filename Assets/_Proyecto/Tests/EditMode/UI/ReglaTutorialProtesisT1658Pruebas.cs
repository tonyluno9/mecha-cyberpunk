using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    public class ReglaTutorialProtesisT1658Pruebas
    {
        [Test]
        public void Avanzar_AbrirMenu_PasaAPiernas()
        {
            var regla = new ReglaTutorialProtesis();
            Assert.IsFalse(regla.Avanzar(false, false, false, false, false, true));
            Assert.IsTrue(regla.Avanzar(true, false, false, false, false, true));
            Assert.AreEqual(PasoTutorialProtesis.Piernas, regla.Paso);
        }

        [Test]
        public void Avanzar_CorrerConPiernas_PasaABrazosYGolpearTermina()
        {
            var regla = new ReglaTutorialProtesis();
            regla.Avanzar(true, true, false, false, false, true);
            regla.Avanzar(false, true, true, false, false, true);
            Assert.AreEqual(PasoTutorialProtesis.Brazos, regla.Paso);
            regla.Avanzar(false, false, false, true, true, true);
            Assert.AreEqual(PasoTutorialProtesis.Hecho, regla.Paso);
        }

        [Test]
        public void Texto_Brazos_NoSaleSinImplante()
        {
            var regla = new ReglaTutorialProtesis();
            regla.Avanzar(true, false, false, false, false, false);
            Assert.AreEqual(PasoTutorialProtesis.Brazos, regla.Paso);
            Assert.AreEqual(string.Empty, regla.Texto("TAB", false, false));
            Assert.AreEqual(ReglaTutorialProtesis.TextoBrazos, regla.Texto("TAB", false, true));
        }

        [Test]
        public void PrimeraVisita_IniciarYTerminar_BloqueaLaCamillaYArrancaElTutorial()
        {
            PrimeraVisitaClinica.Fijar(false, false);
            PrimeraVisitaClinica.Terminar();
            Assert.IsFalse(PrimeraVisitaClinica.TutorialPendiente, "sin primera visita pendiente no hay tutorial");
            PrimeraVisitaClinica.Iniciar();
            Assert.IsTrue(PrimeraVisitaClinica.Pendiente);
            PrimeraVisitaClinica.Terminar();
            Assert.IsFalse(PrimeraVisitaClinica.Pendiente);
            Assert.IsTrue(PrimeraVisitaClinica.TutorialPendiente);
            PrimeraVisitaClinica.Fijar(false, false);
        }

        [Test]
        public void ConfigClinica_InstalacionLenta_SoloPiernasYBrazos()
        {
            Assert.IsTrue(MechaCyberpunk.UI.ConfigPantallaClinica.InstalacionLenta(MechaCyberpunk.Core.EspacioImplante.Piernas));
            Assert.IsTrue(MechaCyberpunk.UI.ConfigPantallaClinica.InstalacionLenta(MechaCyberpunk.Core.EspacioImplante.Brazos));
            Assert.IsFalse(MechaCyberpunk.UI.ConfigPantallaClinica.InstalacionLenta(MechaCyberpunk.Core.EspacioImplante.Ojos));
        }
    }
}
