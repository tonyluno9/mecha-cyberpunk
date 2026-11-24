using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1703 (T71): el cuerpo se limpia tras el mínimo solo fuera de la vista; a la vista espera hasta 120 s.</summary>
    public class ReglaLimpiezaCuerpoT71Pruebas
    {
        [Test]
        public void AntesDelMinimo_NuncaSeLimpia()
        {
            Assert.IsFalse(ReglaLimpiezaCuerpo.Debe(44f, 45f, true));
            Assert.IsFalse(ReglaLimpiezaCuerpo.Debe(0f, 45f, true));
        }

        [Test]
        public void PasadoElMinimo_FueraDeVistaSeLimpia_ALaVistaEspera()
        {
            Assert.IsTrue(ReglaLimpiezaCuerpo.Debe(45f, 45f, true));
            Assert.IsFalse(ReglaLimpiezaCuerpo.Debe(60f, 45f, false));
            Assert.IsFalse(ReglaLimpiezaCuerpo.Debe(119f, 45f, false));
        }

        [Test]
        public void ALos120s_SeLimpiaAunqueSeVea()
        {
            Assert.IsTrue(ReglaLimpiezaCuerpo.Debe(120f, 45f, false));
        }

        [Test]
        public void Fundido_AntesDe20s_Cero_LuegoSube_YA21y5sUno()
        {
            Assert.AreEqual(0f, ReglaLimpiezaCuerpo.Fundido(0f));
            Assert.AreEqual(0f, ReglaLimpiezaCuerpo.Fundido(20f));
            Assert.AreEqual(0.5f, ReglaLimpiezaCuerpo.Fundido(20.75f), 1e-4f);
            Assert.AreEqual(1f, ReglaLimpiezaCuerpo.Fundido(21.5f));
            Assert.AreEqual(1f, ReglaLimpiezaCuerpo.Fundido(500f));
        }

        [Test]
        public void MinimoMayorQueElMaximo_ElMaximoSubeAlMinimo()
        {
            Assert.IsFalse(ReglaLimpiezaCuerpo.Debe(130f, 200f, true));
            Assert.IsTrue(ReglaLimpiezaCuerpo.Debe(200f, 200f, false));
        }
    }
}
