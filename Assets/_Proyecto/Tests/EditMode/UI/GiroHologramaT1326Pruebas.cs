using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1326: las reglas del giro del arma 3D con teclas (Q y E dan el mismo arrastre que el ratón).</summary>
    public class GiroHologramaT1326Pruebas
    {
        [Test]
        public void Direccion_SoloDerecha_EsUno() => Assert.AreEqual(1f, ReglaGiroHolograma.Direccion(false, true));

        [Test]
        public void Direccion_SoloIzquierda_EsMenosUno() => Assert.AreEqual(-1f, ReglaGiroHolograma.Direccion(true, false));

        [Test]
        public void Direccion_LasDosONinguna_SeAnula()
        {
            Assert.AreEqual(0f, ReglaGiroHolograma.Direccion(true, true));
            Assert.AreEqual(0f, ReglaGiroHolograma.Direccion(false, false));
        }

        [Test]
        public void ArrastrePorTeclas_EsProporcionalAlTiempo()
        {
            Vector2 a = ReglaGiroHolograma.ArrastrePorTeclas(1f, 0.5f);
            Assert.AreEqual(ReglaGiroHolograma.PxPorSegundo * 0.5f, a.x, 0.001f);
            Assert.AreEqual(0f, a.y);
            Assert.AreEqual(-a.x, ReglaGiroHolograma.ArrastrePorTeclas(-1f, 0.5f).x, 0.001f);
        }

        [Test]
        public void ArrastrePorTeclas_RecortaDireccionYTiempoNegativo()
        {
            Assert.AreEqual(ReglaGiroHolograma.PxPorSegundo, ReglaGiroHolograma.ArrastrePorTeclas(5f, 1f).x, 0.001f);
            Assert.AreEqual(0f, ReglaGiroHolograma.ArrastrePorTeclas(1f, -1f).x);
        }
    }
}
