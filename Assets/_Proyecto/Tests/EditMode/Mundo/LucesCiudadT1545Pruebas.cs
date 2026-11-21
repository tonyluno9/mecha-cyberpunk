using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1545: reglas puras del presupuesto de luces, de los colores de farola y neón y del corte graduado del decorado.</summary>
    public class LucesCiudadT1545Pruebas
    {
        [Test]
        public void EsColorNeon_MagentaYTurquesa_Si_NaranjaYBlancoNo()
        {
            Assert.IsTrue(CalculoLucesCiudad.EsColorNeon(new Color(1f, 0.2f, 0.78f)), "magenta");
            Assert.IsTrue(CalculoLucesCiudad.EsColorNeon(new Color(0.1f, 0.95f, 0.88f)), "turquesa");
            Assert.IsFalse(CalculoLucesCiudad.EsColorNeon(new Color(1f, 0.6f, 0.22f)), "sodio");
            Assert.IsFalse(CalculoLucesCiudad.EsColorNeon(new Color(0.6f, 0.66f, 1f)), "luna azul");
            Assert.IsFalse(CalculoLucesCiudad.EsColorNeon(Color.white));
        }

        [Test]
        public void EsColorCalido_AmbarYNaranja_Si_AzulNo()
        {
            Assert.IsTrue(CalculoLucesCiudad.EsColorCalido(new Color(1f, 0.72f, 0.42f)));
            Assert.IsTrue(CalculoLucesCiudad.EsColorCalido(new Color(1f, 0.6f, 0.22f)));
            Assert.IsFalse(CalculoLucesCiudad.EsColorCalido(new Color(0.72f, 0.93f, 1f)));
            Assert.IsFalse(CalculoLucesCiudad.EsColorCalido(new Color(1f, 0.2f, 0.78f)));
        }

        [Test]
        public void Seleccionar_ConTope_DejaLasMasCercanas()
        {
            Vector3[] pos = { new Vector3(5f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(2f, 0f, 0f) };
            float[] radios = { 100f, 100f, 100f, 100f };
            bool[] ok = { true, true, true, true };
            bool[] estaba = new bool[4];
            bool[] prendida = new bool[4];
            int n = CalculoLucesCiudad.Seleccionar(Vector3.zero, pos, radios, ok, estaba, 4, 2, 0f, prendida, new float[4]);
            Assert.AreEqual(2, n);
            Assert.IsFalse(prendida[0]);
            Assert.IsTrue(prendida[1]);
            Assert.IsFalse(prendida[2]);
            Assert.IsTrue(prendida[3]);
        }

        [Test]
        public void Seleccionar_FueraDelRadio_SeApaga_YLaHisteresisLaSostiene()
        {
            Vector3[] pos = { new Vector3(52f, 0f, 0f) };
            float[] radios = { 50f };
            bool[] ok = { true };
            bool[] prendida = new bool[1];
            CalculoLucesCiudad.Seleccionar(Vector3.zero, pos, radios, ok, new[] { false }, 1, 10, 8f, prendida, new float[1]);
            Assert.IsFalse(prendida[0], "apagada y a 52 m con radio 50: sigue apagada");
            CalculoLucesCiudad.Seleccionar(Vector3.zero, pos, radios, ok, new[] { true }, 1, 10, 8f, prendida, new float[1]);
            Assert.IsTrue(prendida[0], "prendida y a 52 m con radio 50 y margen 8: sigue prendida");
        }

        [Test]
        public void Seleccionar_NoElegibles_NuncaSePrenden()
        {
            bool[] prendida = new bool[1];
            int n = CalculoLucesCiudad.Seleccionar(Vector3.zero, new[] { Vector3.zero }, new[] { 10f }, new[] { false }, new[] { false }, 1, 5, 0f, prendida, new float[1]);
            Assert.AreEqual(0, n);
            Assert.IsFalse(prendida[0]);
        }

        [Test]
        public void RadioPrendida_SumaElAlcanceYLaDistanciaExtra()
        {
            Assert.AreEqual(61f, CalculoLucesCiudad.RadioPrendida(16f, 45f), 0.001f);
            Assert.AreEqual(45f, CalculoLucesCiudad.RadioPrendida(-3f, 45f), 0.001f);
        }

        [Test]
        public void DistanciaPorTamanio_VaDelMinimoAlMaximo()
        {
            Assert.AreEqual(55f, CalculoCortePorDistancia.DistanciaPorTamanio(0f, 2.5f, 55f, 110f), 0.001f);
            Assert.AreEqual(110f, CalculoCortePorDistancia.DistanciaPorTamanio(2.5f, 2.5f, 55f, 110f), 0.001f);
            Assert.AreEqual(82.5f, CalculoCortePorDistancia.DistanciaPorTamanio(1.25f, 2.5f, 55f, 110f), 0.001f);
            Assert.AreEqual(110f, CalculoCortePorDistancia.DistanciaPorTamanio(9f, 2.5f, 55f, 110f), 0.001f);
        }
    }
}
