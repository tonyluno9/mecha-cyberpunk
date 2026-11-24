using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-historia-libre-23nov: los refuerzos del modo libre aparecen lejos y fuera de la vista (nunca a tus espaldas a menos de 25 m).</summary>
    public class ReglaPuntoRefuerzoPruebas
    {
        private const float Minimo = 25f;
        private const float Vista = 70f;

        [Test]
        public void Vale_AtrasACincoMetros_No()
        {
            Assert.IsFalse(ReglaPuntoRefuerzo.Vale(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), Minimo, Vista));
        }

        [Test]
        public void Vale_AtrasAVeinticuatroMetros_No()
        {
            Assert.IsFalse(ReglaPuntoRefuerzo.Vale(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -24f), Minimo, Vista));
        }

        [Test]
        public void Vale_AtrasATreintaMetros_Si()
        {
            Assert.IsTrue(ReglaPuntoRefuerzo.Vale(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -30f), Minimo, Vista));
        }

        [Test]
        public void Vale_EnfrenteLejos_NoPorqueSeVe()
        {
            Assert.IsFalse(ReglaPuntoRefuerzo.Vale(Vector3.zero, Vector3.forward, new Vector3(5f, 0f, 40f), Minimo, Vista));
        }

        [Test]
        public void Vale_DeLadoLejos_Si()
        {
            Assert.IsTrue(ReglaPuntoRefuerzo.Vale(Vector3.zero, Vector3.forward, new Vector3(30f, 0f, 0f), Minimo, Vista));
        }

        [Test]
        public void Vale_LaAlturaNoCuentaParaLaDistancia()
        {
            Assert.IsFalse(ReglaPuntoRefuerzo.Vale(Vector3.zero, Vector3.forward, new Vector3(0f, 40f, -10f), Minimo, Vista));
        }

        [Test]
        public void Vale_MirandoArriba_SoloCuentaElPlano()
        {
            Vector3 casiArriba = new Vector3(0f, 1f, 0.05f);
            Assert.IsFalse(ReglaPuntoRefuerzo.Vale(Vector3.zero, casiArriba, new Vector3(0f, 0f, 30f), Minimo, Vista), "el plano dice que mira hacia +z");
        }
    }
}
