using MechaCyberpunk.UI.Hud;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-992: el paso del tutorial de las habilidades de prótesis nombra solo las que se tienen.</summary>
    public class ReglaTutorialHabilidadesPruebas
    {
        [Test]
        public void TextoHabilidades_SoloLasQueTienes()
        {
            Assert.AreEqual("[Q] PIERNAS · [C] BRAZOS · [X] CABEZA", ReglaTutorial.TextoHabilidades(null, true, true, true));
            Assert.AreEqual("[Q] PIERNAS · [X] CABEZA", ReglaTutorial.TextoHabilidades(null, true, false, true));
            Assert.AreEqual("[C] BRAZOS", ReglaTutorial.TextoHabilidades(null, false, true, false));
            Assert.AreEqual(string.Empty, ReglaTutorial.TextoHabilidades(null, false, false, false));
        }

        [Test]
        public void TextoHabilidades_UsaLaTeclaRealQueDaElInputSystem()
        {
            string texto = ReglaTutorial.TextoHabilidades(nombre => nombre == ReglaTutorial.TeclaPiernas ? "z" : "x", true, false, false);
            Assert.AreEqual("[Z] PIERNAS", texto);
        }

        [Test]
        public void Habilidades_SeOfreceDespuesDeMoverse_YSeDaPorVistaAlHacerla()
        {
            Assert.AreEqual(AccionTutorial.Moverse, ReglaTutorial.Requisito(AccionTutorial.Habilidades));
            ReglaTutorial regla = new ReglaTutorial(5f, 0f);
            regla.Hecha(AccionTutorial.Habilidades);
            Assert.IsTrue(regla.Vista(AccionTutorial.Habilidades));
        }
    }
}
