using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1482: todas las rutas de los drones aéreos quedan dentro de los 4 barrios.</summary>
    public class DronesBarriosT1482Pruebas
    {
        [Test]
        public void Plan_TodasLasRutasQuedanDentroDeLosBarrios()
        {
            Assert.IsTrue(DronesZonasT1398.PlanDentroDeLosBarrios(), "alguna ruta sale de x +-" + DronesZonasT1398.LimiteX + ", z +-" + DronesZonasT1398.LimiteZ);
        }
    }
}
