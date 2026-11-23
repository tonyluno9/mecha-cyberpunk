using MechaCyberpunk.UI.Hud;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T43: regla de visibilidad de las barras de vida de enemigos.</summary>
    public class ReglaBarraEnemigoPruebas
    {
        [Test]
        public void SinDanioNoSeVe()
        {
            float seg = ReglaBarraEnemigo.SegundosDesdeDanio(100f, -1f);
            Assert.IsFalse(ReglaBarraEnemigo.BarraPequenaVisible(false, seg, 5f, false));
        }

        [Test]
        public void ConDanioRecienteSeVeYLuegoSeOculta()
        {
            Assert.IsTrue(ReglaBarraEnemigo.BarraPequenaVisible(false, 1f, 10f, false));
            Assert.IsFalse(ReglaBarraEnemigo.BarraPequenaVisible(false, ReglaBarraEnemigo.SegundosVisibleSinDanio + 0.1f, 10f, false));
        }

        [Test]
        public void MuertoLejosOTapadoNoSeVe()
        {
            Assert.IsFalse(ReglaBarraEnemigo.BarraPequenaVisible(true, 0f, 5f, false));
            Assert.IsFalse(ReglaBarraEnemigo.BarraPequenaVisible(false, 0f, ReglaBarraEnemigo.DistanciaMaxima + 1f, false));
            Assert.IsFalse(ReglaBarraEnemigo.BarraPequenaVisible(false, 0f, 5f, true));
        }

        [Test]
        public void JefeGrandeSoloEnCombateYVivo()
        {
            Assert.IsTrue(ReglaBarraEnemigo.JefeEnCombate(false, 2f));
            Assert.IsFalse(ReglaBarraEnemigo.JefeEnCombate(true, 2f));
            Assert.IsFalse(ReglaBarraEnemigo.JefeEnCombate(false, ReglaBarraEnemigo.SegundosVisibleJefe + 1f));
        }

        [Test]
        public void ElegirJefeTomaElMasReciente()
        {
            Assert.AreEqual(1, ReglaBarraEnemigo.ElegirJefe(new[] { 5f, 2f, 1f }, new[] { true, true, false }));
            Assert.AreEqual(-1, ReglaBarraEnemigo.ElegirJefe(new[] { 5f }, new[] { false }));
        }

        [Test]
        public void EscalaConDistanciaAcotada()
        {
            Assert.AreEqual(1f, ReglaBarraEnemigo.Escala(ReglaBarraEnemigo.DistanciaEscalaBase), 0.001f);
            Assert.AreEqual(ReglaBarraEnemigo.EscalaMinima, ReglaBarraEnemigo.Escala(100f), 0.001f);
            Assert.AreEqual(ReglaBarraEnemigo.EscalaMaxima, ReglaBarraEnemigo.Escala(0.5f), 0.001f);
        }

        [Test]
        public void OpacidadSeDesvaneceAlFinal()
        {
            Assert.AreEqual(1f, ReglaBarraEnemigo.Opacidad(1f, 6f), 0.001f);
            Assert.AreEqual(0.5f, ReglaBarraEnemigo.Opacidad(5.5f, 6f), 0.001f);
            Assert.AreEqual(0f, ReglaBarraEnemigo.Opacidad(7f, 6f), 0.001f);
        }
    }
}
