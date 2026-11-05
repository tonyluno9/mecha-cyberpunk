using MechaCyberpunk.UI.Misiones;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-633: curva del deslizamiento del teléfono y hora en texto.</summary>
    public class CalculoTelefonoPruebas
    {
        [Test]
        public void Deslizamiento_EmpiezaEnCeroTerminaEnUnoYFrenaAlFinal()
        {
            Assert.AreEqual(0f, CalculoTelefono.Deslizamiento(0f), 0.0001f);
            Assert.AreEqual(1f, CalculoTelefono.Deslizamiento(1f), 0.0001f);
            Assert.AreEqual(1f, CalculoTelefono.Deslizamiento(5f), 0.0001f, "Se queda en 1 si se pasa");
            Assert.Greater(CalculoTelefono.Deslizamiento(0.5f), 0.5f, "Arranca rápido y frena al llegar");
            Assert.Greater(CalculoTelefono.Deslizamiento(0.6f), CalculoTelefono.Deslizamiento(0.4f), "Siempre sube");
        }

        [TestCase(0f, "00:00")]
        [TestCase(7.5f, "07:30")]
        [TestCase(13.25f, "13:15")]
        [TestCase(23.999f, "23:59")]
        [TestCase(24f, "00:00")]
        [TestCase(26.5f, "02:30")]
        [TestCase(-1f, "23:00")]
        public void FormatoHora_DaHoraYMinutosDe24Horas(float horas, string esperado)
        {
            Assert.AreEqual(esperado, CalculoTelefono.FormatoHora(horas));
        }
    }
}
