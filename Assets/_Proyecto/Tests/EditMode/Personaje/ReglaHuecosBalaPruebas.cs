using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-808: metal o concreto y cómo se apaga un hueco de bala.</summary>
    public class ReglaHuecosBalaPruebas
    {
        private static readonly string[] Palabras = { "carro", "metal", "patrulla" };

        [Test]
        public void EsMetal_NombreConCarroOPatrulla_EsMetal()
        {
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Puerta", "V03_CarroTaxi" }, false, false, Palabras));
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "PATRULLA_01" }, false, false, Palabras), "sin distinguir mayúsculas");
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Lamina_Metal" }, false, false, Palabras));
        }

        [Test]
        public void EsMetal_CuerpoRigidoOMontable_EsMetal()
        {
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Caja" }, true, false, Palabras));
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Caja" }, false, true, Palabras));
        }

        [Test]
        public void EsMetal_ParedComun_EsConcreto()
        {
            Assert.IsFalse(ReglaHuecosBala.EsMetal(new[] { "Pared", "Edificio_03", "Greybox_Concreto" }, false, false, Palabras));
            Assert.IsFalse(ReglaHuecosBala.EsMetal(null, false, false, Palabras));
        }

        [Test]
        public void Alfa_ConElTiempo_SeVeYLuegoSeApaga()
        {
            Assert.AreEqual(1f, ReglaHuecosBala.Alfa(5f, 10f, 2f));
            Assert.AreEqual(0.5f, ReglaHuecosBala.Alfa(11f, 10f, 2f), 0.0001f);
            Assert.AreEqual(0f, ReglaHuecosBala.Alfa(13f, 10f, 2f));
        }
    }
}
