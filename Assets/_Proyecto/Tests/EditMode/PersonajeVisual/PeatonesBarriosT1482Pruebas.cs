using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1482: los NPC que salían en las zonas quitadas (Malecón, Puerto, Cerro, Autopista) salen en las zonas de los 4 barrios.</summary>
    public class PeatonesBarriosT1482Pruebas
    {
        private static readonly string[] CodigosPedidos = { "NP07", "NP08", "NP09", "NP10", "NP11", "NP12", "NP13", "NP14" };

        [Test]
        public void Diseno_SoloTieneZonasDeLosBarrios()
        {
            string[] zonas = PeatonesBarriosT1482.Diseno.Select(d => d.zona).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Avenida", "NeonAlto", "LasLomas", "Deshuesadero" }, zonas);
        }

        [Test]
        public void Diseno_CadaNpcDeZonaQuitadaSaleEnAlgunaZonaDeLosBarrios()
        {
            foreach (string codigo in CodigosPedidos)
            {
                bool sale = PeatonesBarriosT1482.Diseno.Any(d => PeatonesBarriosT1482.VariantesDe(d.zona).Any(v => v.variante.StartsWith(codigo + "_") && v.peso > 0f));
                Assert.IsTrue(sale, codigo + " no sale en ningun barrio");
            }
        }

        [Test]
        public void Diseno_RepartidorYOficinistaSalenEnTodasLasZonas()
        {
            foreach ((string zona, string[] _) in PeatonesBarriosT1482.Diseno)
            {
                var variantes = PeatonesBarriosT1482.VariantesDe(zona);
                Assert.IsTrue(variantes.Any(v => v.variante.StartsWith("NP13_")), zona);
                Assert.IsTrue(variantes.Any(v => v.variante.StartsWith("NP14_")), zona);
            }
        }

        [Test]
        public void VariantesDe_ZonaQuitada_DaNull()
        {
            Assert.IsNull(PeatonesBarriosT1482.VariantesDe("Malecon"));
            Assert.IsNull(PeatonesBarriosT1482.VariantesDe("Cerro"));
        }
    }
}
