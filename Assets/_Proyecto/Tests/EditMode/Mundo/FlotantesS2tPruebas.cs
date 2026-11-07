using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-859 (playtest 6-nov tarde): ninguna pieza del kit flota en la ciudad (ni en el piso ni pegada a una pared).</summary>
    public class FlotantesS2tPruebas
    {
        [Test]
        public void Ciudad_SinPiezasFlotando()
        {
            var flotan = FlotantesS2t.Buscar();
            CollectionAssert.IsEmpty(flotan.Select(f => f.pieza.name + " en " + f.limites.center).ToArray());
        }
    }
}
