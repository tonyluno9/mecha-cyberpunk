using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-677: daño por zona. La cabeza (hueso o el 15 % de arriba del collider) hace x3.</summary>
    public class ReglaZonaDanioPruebas
    {
        [Test]
        public void MultiplicadorCabeza_EsTres_YCuerpoUno()
        {
            Assert.AreEqual(3f, ReglaZonaDanio.Multiplicador(true));
            Assert.AreEqual(1f, ReglaZonaDanio.Multiplicador(false));
        }

        [TestCase(1.8f, true)]
        [TestCase(1.6f, true)]
        [TestCase(1.5f, false)]
        [TestCase(1.2f, false)]
        [TestCase(0.2f, false)]
        public void PorAltura_ElQuinceDeArribaEsCabeza(float y, bool esperado)
        {
            Assert.AreEqual(esperado, ReglaZonaDanio.EsCabezaPorAltura(y, 0f, 1.8f));
        }

        [Test]
        public void PorAltura_SeAdaptaAlCuerpoAgachado()
        {
            Assert.IsTrue(ReglaZonaDanio.EsCabezaPorAltura(1.0f, 0f, 1.0f));
            Assert.IsFalse(ReglaZonaDanio.EsCabezaPorAltura(0.7f, 0f, 1.0f));
        }

        [Test]
        public void PorAltura_ColliderSinAlto_NoEsCabeza()
        {
            Assert.IsFalse(ReglaZonaDanio.EsCabezaPorAltura(1f, 1f, 1f));
        }

        [Test]
        public void PorHueso_DentroDelRadioEsCabeza_FueraNo()
        {
            Vector3 cabeza = new Vector3(0f, 1.6f, 0f);
            Assert.IsTrue(ReglaZonaDanio.EsCabezaPorHueso(cabeza + Vector3.right * 0.15f, cabeza));
            Assert.IsFalse(ReglaZonaDanio.EsCabezaPorHueso(cabeza + Vector3.right * 0.3f, cabeza));
        }
    }
}
