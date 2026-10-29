using MechaCyberpunk.IA.Adaptacion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-202: la regla pura de la RETIRADA del rival (azar inyectado).</summary>
    public class ReglaRetiradaPruebas
    {
        private static readonly float[] Probabilidad = { 0.45f, 0.7f };
        private static readonly float[] Factor = { 0.8f, 0.9f };
        private const float Demora = 0.15f;
        private const float Margen = 0.75f;
        private const float Alcance = 3f;
        private const float Recarga = 0.8f;
        private const float Tolerancia = 1e-5f;

        private float azar;

        private ReglaRetirada Nueva() => new ReglaRetirada(Probabilidad, Factor, Demora, Margen, () => azar);

        [Test]
        public void NivelCero_NuncaSeRetira()
        {
            azar = 0f;
            Assert.IsFalse(Nueva().Decidir(0, Alcance, Recarga, out _));
        }

        [Test]
        public void NivelUno_AzarBajo_PideDistanciaTiempoYFactor()
        {
            azar = 0f;
            Assert.IsTrue(Nueva().Decidir(1, Alcance, Recarga, out PedidoRetirada pedido));
            Assert.AreEqual(Demora, pedido.Demora, Tolerancia);
            Assert.AreEqual(Alcance + Margen, pedido.Distancia, Tolerancia, "Alcance del jugador más el margen");
            Assert.AreEqual(Recarga, pedido.SegundosMaximos, Tolerancia, "A lo más su recarga");
            Assert.AreEqual(0.8f, pedido.FactorVelocidad, Tolerancia);
        }

        [Test]
        public void NivelDos_UsaLaProbabilidadYElFactorDelNivelDos()
        {
            azar = 0.6f;
            Assert.IsFalse(Nueva().Decidir(1, Alcance, Recarga, out _), "0.6 no es menor que 0.45");
            Assert.IsTrue(Nueva().Decidir(2, Alcance, Recarga, out PedidoRetirada pedido), "0.6 es menor que 0.7");
            Assert.AreEqual(0.9f, pedido.FactorVelocidad, Tolerancia);
        }

        [Test]
        public void AzarJustoEnLaProbabilidad_NoSeRetira()
        {
            azar = 0.45f;
            Assert.IsFalse(Nueva().Decidir(1, Alcance, Recarga, out _));
        }

        [Test]
        public void LaDistanciaSigueAlAlcanceDelJugador()
        {
            azar = 0f;
            Nueva().Decidir(1, 4.5f, Recarga, out PedidoRetirada pedido);
            Assert.AreEqual(5.25f, pedido.Distancia, Tolerancia);
        }

        [Test]
        public void ElFactor_NuncaPasaDeUno()
        {
            azar = 0f;
            ReglaRetirada regla = new ReglaRetirada(Probabilidad, new[] { 1.5f, 2f }, Demora, Margen, () => azar);
            regla.Decidir(1, Alcance, Recarga, out PedidoRetirada pedido);
            Assert.AreEqual(1f, pedido.FactorVelocidad, Tolerancia);
        }

        [Test]
        public void ElTiempo_NuncaEsNegativo()
        {
            azar = 0f;
            Nueva().Decidir(1, Alcance, -1f, out PedidoRetirada pedido);
            Assert.AreEqual(0f, pedido.SegundosMaximos, Tolerancia);
        }
    }
}
