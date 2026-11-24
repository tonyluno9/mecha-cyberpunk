using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>VR39: el golpe de un cuerpo contra el piso suena una vez (muerto), con enfriamiento y rapidez mínima, y nunca quieto.</summary>
    public class ReglaSonidoChoqueCuerpoPruebas
    {
        private const float Fuerte = 8f;

        [Test]
        public void DebeSonar_PrimerChoqueFuerte_Suena()
        {
            Assert.IsTrue(ReglaSonidoChoqueCuerpo.DebeSonar(Fuerte, 1f, -1f, 0, false, false));
            Assert.IsTrue(ReglaSonidoChoqueCuerpo.DebeSonar(Fuerte, 1f, -1f, 0, true, false));
        }

        [Test]
        public void DebeSonar_Derrotado_SoloUnaVez()
        {
            Assert.IsFalse(ReglaSonidoChoqueCuerpo.DebeSonar(Fuerte, 10f, 1f, 1, true, false), "Un cuerpo muerto ya sonó: nunca más");
        }

        [Test]
        public void DebeSonar_DentroDelEnfriamiento_NoSuena()
        {
            float casi = 1f + ReglaSonidoChoqueCuerpo.SegundosEnfriamiento * 0.5f;
            Assert.IsFalse(ReglaSonidoChoqueCuerpo.DebeSonar(Fuerte, casi, 1f, 1, false, false));
            Assert.IsTrue(ReglaSonidoChoqueCuerpo.DebeSonar(Fuerte, 1f + ReglaSonidoChoqueCuerpo.SegundosEnfriamiento, 1f, 1, false, false));
        }

        [Test]
        public void DebeSonar_ChoqueDebilOQuieto_NoSuena()
        {
            Assert.IsFalse(ReglaSonidoChoqueCuerpo.DebeSonar(ReglaSonidoChoqueCuerpo.RapidezMinima * 0.5f, 1f, -1f, 0, false, false));
            Assert.IsFalse(ReglaSonidoChoqueCuerpo.DebeSonar(Fuerte, 1f, -1f, 0, false, true));
        }

        [Test]
        public void DebeSonar_PasaElTopePorVuelo_NoSuena()
        {
            Assert.IsFalse(ReglaSonidoChoqueCuerpo.DebeSonar(Fuerte, 100f, 1f, ReglaSonidoChoqueCuerpo.MaximoPorVuelo, false, false));
        }
    }
}
