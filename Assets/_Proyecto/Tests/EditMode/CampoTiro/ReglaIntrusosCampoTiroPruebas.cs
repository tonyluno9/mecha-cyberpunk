using MechaCyberpunk.CampoTiro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>VR50: nadie de la ciudad (peatón, pandillero, policía, carro) puede estar en la sala del campo de tiro; el jugador, la sala y los objetivos sí.</summary>
    public class ReglaIntrusosCampoTiroPruebas
    {
        [Test]
        public void EsIntruso_PeatonPandilleroOCarro_Si()
        {
            Assert.IsTrue(ReglaIntrusosCampoTiro.EsIntruso(false, false, false, true, false, 0f, false), "con NavMeshAgent");
            Assert.IsTrue(ReglaIntrusosCampoTiro.EsIntruso(false, false, false, false, true, 0f, false), "con CharacterController");
            Assert.IsTrue(ReglaIntrusosCampoTiro.EsIntruso(false, false, false, false, false, ReglaIntrusosCampoTiro.MasaVehiculo, false), "carro");
            Assert.IsTrue(ReglaIntrusosCampoTiro.EsIntruso(false, false, false, false, false, 0f, true), "personaje que recibe daño");
        }

        [Test]
        public void EsIntruso_JugadorSalaUObjetivo_No()
        {
            Assert.IsFalse(ReglaIntrusosCampoTiro.EsIntruso(true, false, false, false, true, 80f, true));
            Assert.IsFalse(ReglaIntrusosCampoTiro.EsIntruso(false, true, false, false, false, 0f, false));
            Assert.IsFalse(ReglaIntrusosCampoTiro.EsIntruso(false, false, true, false, false, 0f, true));
        }

        [Test]
        public void EsIntruso_ObjetoLigeroSinVida_No()
        {
            Assert.IsFalse(ReglaIntrusosCampoTiro.EsIntruso(false, false, false, false, false, 0.5f, false), "un cargador o casquillo tirado no se apaga");
        }

        [Test]
        public void Nombre_SkullYPolicia_TienenNombreYVida()
        {
            Assert.AreEqual("SKULL", ReglaCampoTiro.Nombre(TipoObjetivoTiro.Skull));
            Assert.AreEqual("POLICÍA", ReglaCampoTiro.Nombre(TipoObjetivoTiro.Policia));
            Assert.Greater(ReglaCampoTiro.Vida(TipoObjetivoTiro.Policia), 0f);
        }
    }
}
