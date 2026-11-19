using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1474: el sonido de una puerta sale de su nombre y, si no dice nada, de su forma.</summary>
    public class PuertasSonidoT1474Pruebas
    {
        [TestCase("Hoja_Reja_Taller", false, false, "INT_PUERTA_REJA")]
        [TestCase("Puerta_Cristal_Tienda", false, true, "INT_PUERTA_CRISTAL")]
        [TestCase("Cortina_Local", false, false, "INT_PUERTA_METAL")]
        [TestCase("Puerta_Madera_Casa", false, false, "INT_PUERTA_MADERA")]
        [TestCase("Hoja", true, false, "INT_PUERTA_MADERA")]
        [TestCase("Hoja", false, true, "INT_PUERTA_CRISTAL")]
        [TestCase("Hoja", false, false, "INT_PUERTA_METAL")]
        [TestCase(null, true, false, "INT_PUERTA_MADERA")]
        public void Id_SegunNombreYForma_DaElTipoCorrecto(string nombre, bool conBisagra, bool corrediza, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSonidoPuerta.Id(nombre, conBisagra, corrediza));
        }

        [Test]
        public void Id_NombreManda_SobreLaForma()
        {
            Assert.AreEqual("INT_PUERTA_REJA", ReglaSonidoPuerta.Id("Reja", true, true));
        }
    }
}
