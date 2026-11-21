using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Efectos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1532: qué ID de explosión, escombros, cristales y fuego pide cada tipo (lógica pura).</summary>
    public class SonidosExplosion20novPruebas
    {
        [TestCase(TipoExplosionSonido.Carro, "EXP_CARRO")]
        [TestCase(TipoExplosionSonido.Barril, "EXP_BARRIL")]
        [TestCase(TipoExplosionSonido.DronRobot, "EXP_DRON_ROBOT")]
        [TestCase(TipoExplosionSonido.Nave, "EXP_NAVE")]
        [TestCase(TipoExplosionSonido.Onda, "EXP_ONDA")]
        public void IdPara_CadaTipo(TipoExplosionSonido tipo, string esperado)
        {
            Assert.AreEqual(esperado, LimiteSonidoExplosion.IdPara(tipo));
        }

        [Test]
        public void Detalle_CarroYNave_EscombrosYCristales()
        {
            CollectionAssert.AreEqual(new[] { "EXP_ESCOMBROS", "EXP_CRISTALES" }, LimiteSonidoExplosion.IdsDetalle(TipoExplosionSonido.Carro));
            CollectionAssert.AreEqual(new[] { "EXP_ESCOMBROS", "EXP_CRISTALES" }, LimiteSonidoExplosion.IdsDetalle(TipoExplosionSonido.Nave));
        }

        [Test]
        public void Detalle_BarrilSoloEscombros_DronNada()
        {
            CollectionAssert.AreEqual(new[] { "EXP_ESCOMBROS" }, LimiteSonidoExplosion.IdsDetalle(TipoExplosionSonido.Barril));
            Assert.IsEmpty(LimiteSonidoExplosion.IdsDetalle(TipoExplosionSonido.DronRobot));
            Assert.IsEmpty(LimiteSonidoExplosion.IdsDetalle(TipoExplosionSonido.Onda));
        }

        [Test]
        public void Ids_ConstantesDeCadaEvento()
        {
            Assert.AreEqual("EXP_CARRO", LimiteSonidoExplosion.IdCarro);
            Assert.AreEqual("EXP_BARRIL", BarrilExplosivo.IdExplosion);
            Assert.AreEqual("EXP_DRON_ROBOT", DronMundo.IdExplosion);
            Assert.AreEqual("FUE_ENCENDER", LimiteSonidoExplosion.IdFuegoEncender);
            Assert.AreEqual("FUE_LOOP_GRANDE", LimiteSonidoExplosion.IdFuegoGrande);
            Assert.AreEqual("FUE_LOOP_CHICO", LimiteSonidoExplosion.IdFuegoChico);
            Assert.Greater(LimiteSonidoExplosion.RetrasoDetalle, 0f);
        }
    }
}
