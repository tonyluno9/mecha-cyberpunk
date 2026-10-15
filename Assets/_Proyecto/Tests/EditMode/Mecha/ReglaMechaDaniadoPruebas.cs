using MechaCyberpunk.Mecha.Danio;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class ReglaMechaDaniadoPruebas
    {
        private const float Maxima = 100f;
        private const float Umbral = 0.3f;
        private const float Frecuencia = 2f;

        [Test]
        public void VidaBaja_JustoArribaDelUmbral_NoEsVidaBaja()
        {
            Assert.IsFalse(ReglaMechaDaniado.VidaBaja(31f, Maxima, Umbral));
        }

        [Test]
        public void VidaBaja_EnElUmbral_EsVidaBaja()
        {
            Assert.IsTrue(ReglaMechaDaniado.VidaBaja(30f, Maxima, Umbral));
        }

        [Test]
        public void VidaBaja_EnKO_NoEsVidaBaja()
        {
            Assert.IsFalse(ReglaMechaDaniado.VidaBaja(0f, Maxima, Umbral));
        }

        [Test]
        public void VidaBaja_MaximaCero_NoEsVidaBaja()
        {
            Assert.IsFalse(ReglaMechaDaniado.VidaBaja(10f, 0f, Umbral));
        }

        [Test]
        public void Factor_ConVidaBaja_DaElFactor()
        {
            Assert.AreEqual(0.7f, ReglaMechaDaniado.Factor(true, 0.7f), 0.0001f);
        }

        [Test]
        public void Factor_SinVidaBaja_DaUno()
        {
            Assert.AreEqual(1f, ReglaMechaDaniado.Factor(false, 0.7f), 0.0001f);
        }

        [Test]
        public void AlarmaPrendida_A2HzEn01Segundos_EstaPrendida()
        {
            Assert.IsTrue(ReglaMechaDaniado.AlarmaPrendida(0.1f, Frecuencia));
        }

        [Test]
        public void AlarmaPrendida_A2HzEn03Segundos_EstaApagada()
        {
            Assert.IsFalse(ReglaMechaDaniado.AlarmaPrendida(0.3f, Frecuencia));
        }

        [Test]
        public void AlarmaPrendida_A2HzEn06Segundos_EstaPrendida()
        {
            Assert.IsTrue(ReglaMechaDaniado.AlarmaPrendida(0.6f, Frecuencia));
        }

        [Test]
        public void AlarmaPrendida_FrecuenciaCero_NuncaPrende()
        {
            Assert.IsFalse(ReglaMechaDaniado.AlarmaPrendida(0.1f, 0f));
        }
    }
}
