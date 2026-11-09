using MechaCyberpunk.Trafico.Calle;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-705: tiempos y radios del conductor al que le roban el carro (sin Unity en juego).</summary>
    public class ConductorRobadoT705Pruebas
    {

        [Test]
        public void Tiempos_PrimeroLoSacanLuegoCaeYAlFinalSeLevanta()
        {
            Assert.Greater(ConductorRobado.SegundosSacar, 0f);
            Assert.Less(ConductorRobado.SegundosSacar, ConductorRobado.SegundosCaido, "Cae mientras lo sacan y sigue tirado un momento");
            Assert.Greater(ConductorRobado.SegundosLevantarse, 0.5f);
        }
    }
}
