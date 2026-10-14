using MechaCyberpunk.Mecha.Calor;
using MechaCyberpunk.Mecha.Llamado;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class CalorPorMechaPruebas
    {
        private const float PorSegundo = 2f;
        private const float AlLlamar = 10f;

        [Test]
        public void CalorPorFrame_DentroYEnCalle_Suma()
        {
            Assert.AreEqual(1f, ReglaCalorMecha.CalorPorFrame(true, true, EstadoLlamado.EnCalle, PorSegundo, 0.5f), 0.0001f);
        }

        [Test]
        public void CalorPorFrame_APie_NoSuma()
        {
            Assert.AreEqual(0f, ReglaCalorMecha.CalorPorFrame(true, false, EstadoLlamado.EnCalle, PorSegundo, 0.5f));
        }

        [TestCase(EstadoLlamado.EnTaller)]
        [TestCase(EstadoLlamado.Llegando)]
        [TestCase(EstadoLlamado.Regresando)]
        public void CalorPorFrame_EstadoQueNoEsEnCalle_NoSuma(EstadoLlamado estado)
        {
            Assert.AreEqual(0f, ReglaCalorMecha.CalorPorFrame(true, true, estado, PorSegundo, 0.5f));
        }

        [Test]
        public void CalorPorFrame_ArranqueDirecto_NuncaSuma()
        {
            Assert.AreEqual(0f, ReglaCalorMecha.CalorPorFrame(false, true, EstadoLlamado.EnCalle, PorSegundo, 0.5f));
        }

        [Test]
        public void CalorPorFrame_SinTiempo_NoSuma()
        {
            Assert.AreEqual(0f, ReglaCalorMecha.CalorPorFrame(true, true, EstadoLlamado.EnCalle, PorSegundo, 0f));
        }

        [Test]
        public void CalorAlLlamar_SoloSiLlega()
        {
            Assert.AreEqual(AlLlamar, ReglaCalorMecha.CalorAlLlamar(ResultadoLlamado.Llega, AlLlamar));
            Assert.AreEqual(0f, ReglaCalorMecha.CalorAlLlamar(ResultadoLlamado.ZonaProhibida, AlLlamar));
            Assert.AreEqual(0f, ReglaCalorMecha.CalorAlLlamar(ResultadoLlamado.YaViene, AlLlamar));
            Assert.AreEqual(0f, ReglaCalorMecha.CalorAlLlamar(ResultadoLlamado.Regresa, AlLlamar));
        }
    }
}
