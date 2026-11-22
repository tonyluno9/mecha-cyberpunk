using MechaCyberpunk.UI.Intro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1646: la máquina de estados de la intro (Inactiva, Video, Terminada).</summary>
    public class ReglaIntroT1646Pruebas
    {
        private const float Espera = 5f;

        [Test]
        public void Nueva_EstaInactiva()
        {
            Assert.AreEqual(FaseIntro.Inactiva, new ReglaIntro(Espera).Fase);
        }

        [Test]
        public void Iniciar_SinVideo_TerminaDeInmediato()
        {
            ReglaIntro regla = new ReglaIntro(Espera);
            regla.Iniciar(false);
            Assert.AreEqual(FaseIntro.Terminada, regla.Fase);
            Assert.IsFalse(regla.Saltada);
        }

        [Test]
        public void Iniciar_ConVideo_EmpiezaEnVideo()
        {
            ReglaIntro regla = new ReglaIntro(Espera);
            regla.Iniciar(true);
            Assert.AreEqual(FaseIntro.Video, regla.Fase);
        }

        [Test]
        public void Saltar_EnVideo_TerminaYQuedaSaltada()
        {
            ReglaIntro regla = new ReglaIntro(Espera);
            regla.Iniciar(true);
            regla.Saltar();
            Assert.AreEqual(FaseIntro.Terminada, regla.Fase);
            Assert.IsTrue(regla.Saltada);
        }

        [Test]
        public void Saltar_Inactiva_NoHaceNada()
        {
            ReglaIntro regla = new ReglaIntro(Espera);
            regla.Saltar();
            Assert.AreEqual(FaseIntro.Inactiva, regla.Fase);
            Assert.IsFalse(regla.Saltada);
        }

        [Test]
        public void AlAcabarVideo_Termina_SinContarComoSaltada()
        {
            ReglaIntro regla = new ReglaIntro(Espera);
            regla.Iniciar(true);
            regla.AlAcabarVideo();
            Assert.AreEqual(FaseIntro.Terminada, regla.Fase);
            Assert.IsFalse(regla.Saltada);
        }

        [Test]
        public void Actualizar_VideoNoPreparadoMucho_Termina()
        {
            ReglaIntro regla = new ReglaIntro(Espera);
            regla.Iniciar(true);
            regla.Actualizar(Espera - 1f, false);
            Assert.AreEqual(FaseIntro.Video, regla.Fase);
            regla.Actualizar(1.5f, false);
            Assert.AreEqual(FaseIntro.Terminada, regla.Fase);
        }

        [Test]
        public void Actualizar_VideoPreparado_NoTermina()
        {
            ReglaIntro regla = new ReglaIntro(Espera);
            regla.Iniciar(true);
            regla.Actualizar(Espera * 10f, true);
            Assert.AreEqual(FaseIntro.Video, regla.Fase);
        }
    }
}
