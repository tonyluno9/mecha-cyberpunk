using MechaCyberpunk.UI;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    public class CalculoPiezasPrimitivasPruebas
    {
        private static readonly Bounds Arma = new Bounds(new Vector3(1f, 2f, 3f), new Vector3(1f, 0.3f, 0.1f));

        [Test]
        public void Cargador_CuelgaDebajoDeLaZonaCentralYEsProporcionalAlLargo()
        {
            Bounds caja = CalculoPiezasPrimitivas.Cargador(Arma, 1f);
            Assert.AreEqual(CalculoPiezasPrimitivas.CargadorAlto, caja.size.y, 0.0001f);
            Assert.AreEqual(CalculoPiezasPrimitivas.CargadorLargo, caja.size.x, 0.0001f);
            Assert.Less(caja.min.y, Arma.min.y, "La caja cuelga bajo el arma");
            Assert.Greater(caja.max.y, Arma.min.y, "Pero se hunde un poco en el arma");
            Assert.AreEqual(Arma.center.z, caja.center.z, 0.0001f);
            Assert.Less(Mathf.Abs(caja.center.x - Arma.center.x), Arma.size.x * 0.25f, "Está en la zona central");

            Bounds doble = CalculoPiezasPrimitivas.Cargador(new Bounds(Arma.center, Arma.size * 2f), 1f);
            Assert.AreEqual(caja.size.y * 2f, doble.size.y, 0.0001f, "Escala con el largo");
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void Canon_TerminaEnLaBocaYSuEjeVaSobreElLargo(float signo)
        {
            Vector3 boca = new Vector3(Arma.center.x + signo * 0.5f, 2.1f, 3f);
            CalculoPiezasPrimitivas.Canon(Arma, boca, out Vector3 centro, out float radio, out float largo);
            Assert.AreEqual(CalculoPiezasPrimitivas.CanonLargo, largo, 0.0001f);
            Assert.AreEqual(CalculoPiezasPrimitivas.CanonRadio, radio, 0.0001f);
            Assert.AreEqual(boca.x, centro.x + signo * largo * 0.5f, 0.0001f, "La punta del cilindro es la boca");
            Assert.AreEqual(boca.y, centro.y, 0.0001f);
            Assert.AreEqual(signo, CalculoPiezasPrimitivas.SignoFrente(Arma, boca));
        }

        [Test]
        public void FactorSobrePedestal_LimitaLaBajadaYNoTocaLoQueSube()
        {
            Assert.AreEqual(1f, CalculoPiezasPrimitivas.FactorSobrePedestal(0.3f, -0.1f, -0.4f, 0.03f));
            Assert.AreEqual(1f, CalculoPiezasPrimitivas.FactorSobrePedestal(-0.1f, -0.1f, -0.4f, 0.03f), "Si cabe, baja completo");
            float factor = CalculoPiezasPrimitivas.FactorSobrePedestal(-0.55f, -0.1f, -0.4f, 0.03f);
            Assert.AreEqual(-0.1f - 0.55f * factor, -0.4f + 0.03f, 0.0001f, "Se queda justo sobre el pedestal más el margen");
            Assert.AreEqual(0f, CalculoPiezasPrimitivas.FactorSobrePedestal(-0.5f, -0.41f, -0.4f, 0.03f), "Si ya está bajo el techo no baja");
        }
    }
}
