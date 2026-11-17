using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ReglaIndicadorDanioT1328Pruebas
    {
        [TestCase(0f, 0f, -1f, 0f)]    // el golpe viaja hacia atrás: viene de enfrente
        [TestCase(1f, 0f, 0f, -90f)]   // viaja a la derecha: viene de la izquierda
        [TestCase(-1f, 0f, 0f, 90f)]   // viaja a la izquierda: viene de la derecha
        [TestCase(0f, 0f, 1f, 180f)]   // viaja hacia adelante: viene de atrás
        public void Angulo_MirandoAdelante_DaLaDireccionDeLaFuente(float dx, float dy, float dz, float esperado)
        {
            Assert.IsTrue(ReglaIndicadorDanio.Angulo(Vector3.forward, new Vector3(dx, dy, dz), out float grados));
            Assert.AreEqual(esperado, Mathf.Abs(esperado) == 180f ? Mathf.Abs(grados) : grados, 0.01f);
        }

        [Test]
        public void Angulo_SinDireccion_NoSePuede()
        {
            Assert.IsFalse(ReglaIndicadorDanio.Angulo(Vector3.forward, Vector3.up, out _));
        }

        [Test]
        public void Alfa_BajaDeUnoACero()
        {
            Assert.AreEqual(1f, ReglaIndicadorDanio.Alfa(0f, 1f), 0.001f);
            Assert.AreEqual(0.5f, ReglaIndicadorDanio.Alfa(0.5f, 1f), 0.001f);
            Assert.AreEqual(0f, ReglaIndicadorDanio.Alfa(1.5f, 1f), 0.001f);
        }
    }
}
