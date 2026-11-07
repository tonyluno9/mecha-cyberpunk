using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-698: el juego se llama CYBERCITY (nombre y ejecutable; el Product Name de los builds sale de NombreJuego.Nombre).</summary>
    public class NombreJuegoCybercityPruebas
    {
        [Test]
        public void NombreJuego_EsCybercity()
        {
            Assert.AreEqual("CYBERCITY", NombreJuego.Nombre);
            Assert.AreEqual("CyberCity.exe", NombreJuego.Ejecutable);
        }
    }
}
