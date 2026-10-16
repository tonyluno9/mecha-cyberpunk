using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    public class ReglaAliasPruebas
    {
        private const string Respaldo = "NOVATO";

        [Test]
        public void Normalizar_ConEspaciosDeLosLados_LosQuita()
        {
            Assert.AreEqual("LA CHISPA", ReglaAlias.Normalizar("   LA CHISPA \t", Respaldo));
        }

        [Test]
        public void Normalizar_ConEspaciosAdentro_LosConserva()
        {
            Assert.AreEqual("EL  RAYO", ReglaAlias.Normalizar("EL  RAYO", Respaldo));
        }

        [Test]
        public void Normalizar_MasDe16Caracteres_CortaA16()
        {
            string resultado = ReglaAlias.Normalizar("ABCDEFGHIJKLMNOPQRSTUV", Respaldo);
            Assert.AreEqual("ABCDEFGHIJKLMNOP", resultado);
            Assert.AreEqual(ReglaAlias.LargoMaximo, resultado.Length);
        }

        [Test]
        public void Normalizar_Exactamente16_NoCambia()
        {
            Assert.AreEqual("ABCDEFGHIJKLMNOP", ReglaAlias.Normalizar("ABCDEFGHIJKLMNOP", Respaldo));
        }

        [Test]
        public void Normalizar_CortePartidoPorUnEspacio_NoDejaEspacioAlFinal()
        {
            Assert.AreEqual("ABCDEFGHIJKLMNO", ReglaAlias.Normalizar("ABCDEFGHIJKLMNO XYZ", Respaldo));
        }

        [Test]
        public void Normalizar_EspaciosAntesDelLargo_CuentaElLargoSinEllos()
        {
            Assert.AreEqual("ABCDEFGHIJKLMNOP", ReglaAlias.Normalizar("      ABCDEFGHIJKLMNOP      ", Respaldo));
        }

        [TestCase("")]
        [TestCase("    ")]
        [TestCase("\t \n")]
        [TestCase(null)]
        public void Normalizar_VacioSoloEspaciosONull_DevuelveElRespaldo(string entrada)
        {
            Assert.AreEqual(Respaldo, ReglaAlias.Normalizar(entrada, Respaldo));
        }
    }
}
