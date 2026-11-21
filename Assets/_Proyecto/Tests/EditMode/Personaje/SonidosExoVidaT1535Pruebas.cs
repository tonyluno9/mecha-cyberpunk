using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1535: los sonidos del exo (servos al activar, golpe del exo puesto) y de la vida (latido con vida baja).</summary>
    public class SonidosExoVidaT1535Pruebas
    {
        [Test]
        public void AlActivar_PiernasYBrazos_TienenSusServos_LoDemasNo()
        {
            Assert.AreEqual("EXO_SERVOS_CAMINAR", ReglaSonidoExo.AlActivar(HabilidadProtesis.Piernas));
            Assert.AreEqual("ROB_SERVOS", ReglaSonidoExo.AlActivar(HabilidadProtesis.Brazos));
            Assert.IsNull(ReglaSonidoExo.AlActivar(HabilidadProtesis.Cabeza));
            Assert.IsNull(ReglaSonidoExo.AlActivar(HabilidadProtesis.Laser));
            Assert.IsNull(ReglaSonidoExo.AlActivar(HabilidadProtesis.Misiles));
        }

        [Test]
        public void Pulso_SoloConPiernasOBrazosActivos_YCadaIntervalo()
        {
            Assert.IsFalse(ReglaSonidoExo.TocaPulso(false, false, 100f, 0f));
            Assert.IsTrue(ReglaSonidoExo.TocaPulso(true, false, 100f, 0f));
            Assert.IsTrue(ReglaSonidoExo.TocaPulso(false, true, 100f, 0f));
            Assert.IsFalse(ReglaSonidoExo.TocaPulso(true, true, 100f, 100f - ReglaSonidoExo.SegundosEntrePulsos + 0.1f));
            Assert.IsTrue(ReglaSonidoExo.TocaPulso(true, true, 100f, 100f - ReglaSonidoExo.SegundosEntrePulsos));
        }

        [Test]
        public void Latido_SoloVivoConVidaBaja_YCadaIntervalo()
        {
            const float baja = 0.4f;
            Assert.IsTrue(ReglaSonidoVida.TocaLatido(30f, 100f, baja, false, 100f, 0f));
            Assert.IsFalse(ReglaSonidoVida.TocaLatido(40f, 100f, baja, false, 100f, 0f));
            Assert.IsFalse(ReglaSonidoVida.TocaLatido(30f, 100f, baja, true, 100f, 0f));
            Assert.IsFalse(ReglaSonidoVida.TocaLatido(30f, 100f, baja, false, 101f, 100f));
            Assert.IsTrue(ReglaSonidoVida.TocaLatido(30f, 100f, baja, false, 104.6f, 100f));
            Assert.IsFalse(ReglaSonidoVida.TocaLatido(0f, 0f, baja, false, 100f, 0f));
        }
    }
}
