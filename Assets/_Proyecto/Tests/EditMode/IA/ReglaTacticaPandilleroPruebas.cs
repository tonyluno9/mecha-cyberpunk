using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    public class ReglaTacticaPandilleroPruebas
    {
        private static ReglaTacticaPandillero Crear() => new ReglaTacticaPandillero(0.25f, 1f, 1f);

        [Test]
        public void Avanzar_VidaBaja_Huye()
        {
            ReglaTacticaPandillero regla = Crear();
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Huir, regla.Avanzar(0.1f, 0.2f, false, false, false, true));
            Assert.IsFalse(regla.PuedeDisparar);
        }

        [Test]
        public void Avanzar_HuyendoYAcorralado_Pelea()
        {
            ReglaTacticaPandillero regla = Crear();
            regla.Avanzar(0.1f, 0.2f, false, false, false, true);
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Rodear, regla.Avanzar(0.1f, 0.2f, false, true, false, true));
        }

        [Test]
        public void Avanzar_JefeNoPuedeHuir()
        {
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Rodear, Crear().Avanzar(0.1f, 0.05f, false, false, false, false));
        }

        [Test]
        public void Avanzar_DisparaConCubierta_SeCubreSeAsomaYVuelve()
        {
            ReglaTacticaPandillero regla = Crear();
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, regla.Avanzar(0.1f, 1f, true, false, true, true));
            Assert.IsFalse(regla.PuedeDisparar);
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, regla.Avanzar(0.5f, 1f, true, false, false, true));
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Asomado, regla.Avanzar(0.6f, 1f, true, false, false, true));
            Assert.IsTrue(regla.PuedeDisparar);
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Cubrirse, regla.Avanzar(0.1f, 1f, true, false, true, true));
        }

        [Test]
        public void Avanzar_DisparaSinCubierta_SigueRodeando()
        {
            Assert.AreEqual(ReglaTacticaPandillero.Modo.Rodear, Crear().Avanzar(0.1f, 1f, false, false, true, true));
        }

        [Test]
        public void AnguloRodeo_RepartePuestosEnElArco()
        {
            Assert.AreEqual(90f, ReglaTacticaPandillero.AnguloRodeo(0, 1, 90f, 240f), 0.001f);
            Assert.AreEqual(-30f, ReglaTacticaPandillero.AnguloRodeo(0, 3, 90f, 240f), 0.001f);
            Assert.AreEqual(90f, ReglaTacticaPandillero.AnguloRodeo(1, 3, 90f, 240f), 0.001f);
            Assert.AreEqual(210f, ReglaTacticaPandillero.AnguloRodeo(2, 3, 90f, 240f), 0.001f);
        }
    }
}
