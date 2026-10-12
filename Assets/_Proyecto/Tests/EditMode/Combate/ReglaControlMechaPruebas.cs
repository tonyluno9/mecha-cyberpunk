using MechaCyberpunk.Combate.Peleador;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Combate
{
    public class ReglaControlMechaPruebas
    {
        [TestCase(true, true, false, true)]
        [TestCase(false, true, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, true, false)]
        [TestCase(false, false, true, false)]
        public void Calcular_ConPermisosYKO_DaElControlEfectivo(bool pelea, bool piloto, bool ko, bool esperado)
        {
            Assert.AreEqual(esperado, ReglaControlMecha.Calcular(pelea, piloto, ko));
        }
    }
}
