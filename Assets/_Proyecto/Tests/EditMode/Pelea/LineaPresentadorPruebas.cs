using MechaCyberpunk.Pelea.Flujo;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Pelea
{
    public class LineaPresentadorPruebas
    {
        [Test]
        public void Armar_ConLigaAliasYRival_CambiaLosTresYPasaAMayusculas()
        {
            string linea = LineaPresentador.Armar("Liga {liga}: {alias} contra {rival}", "Tony", "El Perro", "Sótano");
            Assert.AreEqual("LIGA SÓTANO: TONY CONTRA EL PERRO", linea);
        }

        [Test]
        public void Armar_SinLiga_DejaElHuecoVacio()
        {
            Assert.AreEqual("LIGA : X", LineaPresentador.Armar("LIGA {liga}: {rival}", "a", "x", string.Empty));
            Assert.AreEqual("LIGA : X", LineaPresentador.Armar("LIGA {liga}: {rival}", "a", "x", null));
        }

        [Test]
        public void Armar_PlantillaNull_DevuelveVacio()
        {
            Assert.AreEqual(string.Empty, LineaPresentador.Armar(null, "a", "b", "c"));
        }

        [Test]
        public void Armar_VariasLigas_CambiaTodas()
        {
            Assert.AreEqual("A-A", LineaPresentador.Armar("{liga}-{liga}", "", "", "a"));
        }
    }
}
