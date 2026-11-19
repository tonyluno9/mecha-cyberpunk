using MechaCyberpunk.Vehiculos.Efectos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1472: el tope de sonidos de explosión de los vehículos (cadena de carros y barriles) y los IDs que pide.</summary>
    public class LimiteSonidoExplosionT1472Pruebas
    {
        [Test]
        public void Permite_DiezEnElMismoFrame_SuenaUna()
        {
            LimiteSonidoExplosion limite = new LimiteSonidoExplosion();
            int sonaron = 0;
            for (int i = 0; i < 10; i++)
            {
                if (limite.Permite(5f))
                {
                    sonaron++;
                }
            }
            Assert.AreEqual(1, sonaron);
        }

        [Test]
        public void Permite_EscalonadasCada01s_MaximoTresEnLaVentana()
        {
            LimiteSonidoExplosion limite = new LimiteSonidoExplosion();
            int sonaron = 0;
            for (int i = 0; i < 5; i++)
            {
                if (limite.Permite(i * 0.1f))
                {
                    sonaron++;
                }
            }
            Assert.AreEqual(LimiteSonidoExplosion.MaxEnVentana, sonaron);
        }

        [Test]
        public void Permite_PasadaLaVentana_VuelveASonar()
        {
            LimiteSonidoExplosion limite = new LimiteSonidoExplosion();
            limite.Permite(0f);
            limite.Permite(0.2f);
            limite.Permite(0.4f);
            Assert.IsFalse(limite.Permite(0.5f));
            Assert.IsTrue(limite.Permite(1.2f));
        }

        [Test]
        public void Ids_ExplosionYFuego()
        {
            Assert.AreEqual("EXP_ONDA", LimiteSonidoExplosion.IdOnda);
            Assert.AreEqual("EXP_ONDA", BarrilExplosivo.IdExplosion);
            Assert.AreEqual("FUE_LOOP_CHICO", LimiteSonidoExplosion.IdFuegoChico);
        }
    }
}
