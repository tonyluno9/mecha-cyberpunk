using MechaCyberpunk.IA.Rival;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    public class ReglaPiedadPruebas
    {
        private const float Piso = 0.15f;
        private const float Umbral = 0.35f;
        private const float VidaMaxima = 100f;
        private const float Margen = 1e-4f;

        private ReglaPiedad regla;

        [SetUp]
        public void SetUp()
        {
            regla = new ReglaPiedad(Piso, Umbral);
        }

        [Test]
        public void DanioAPedir_ConVidaDeSobra_PideElDanioNormal()
        {
            Assert.AreEqual(12f, regla.DanioAPedir(12f, 100f, VidaMaxima), Margen);
        }

        [Test]
        public void DanioAPedir_ElGolpeCruzariaElPiso_PideSoloHastaElPiso()
        {
            float pedido = regla.DanioAPedir(12f, 20f, VidaMaxima);

            Assert.AreEqual(5f, pedido, Margen);
            Assert.AreEqual(VidaMaxima * Piso, 20f - pedido, Margen);
        }

        [Test]
        public void DanioAPedir_EnElPisoOMenos_PideCero()
        {
            Assert.AreEqual(0f, regla.DanioAPedir(12f, 15f, VidaMaxima), Margen);
            Assert.AreEqual(0f, regla.DanioAPedir(12f, 10f, VidaMaxima), Margen);
            Assert.AreEqual(0f, regla.DanioAPedir(12f, 0f, VidaMaxima), Margen);
        }

        [Test]
        public void DanioAPedir_BarridoDeVidasYDanios_NuncaBajaLaVidaDelPiso()
        {
            for (float vida = VidaMaxima; vida >= 0f; vida -= 0.7f)
            {
                for (float danio = 0f; danio <= 150f; danio += 3.1f)
                {
                    float pedido = regla.DanioAPedir(danio, vida, VidaMaxima);
                    Assert.GreaterOrEqual(pedido, 0f);
                    Assert.LessOrEqual(pedido, danio + Margen);
                    Assert.GreaterOrEqual(vida - pedido, System.Math.Min(vida, VidaMaxima * Piso) - Margen,
                        "vida " + vida + " daño " + danio);
                }
            }
        }

        [Test]
        public void DanioAPedir_DanioNegativo_PideCero()
        {
            Assert.AreEqual(0f, regla.DanioAPedir(-5f, 100f, VidaMaxima), Margen);
        }

        [Test]
        public void DanioAPedir_VidaMaximaCeroONegativa_NoExplotaYDevuelveElDanioNormal()
        {
            Assert.AreEqual(12f, regla.DanioAPedir(12f, 0f, 0f), Margen);
            Assert.AreEqual(12f, regla.DanioAPedir(12f, 50f, -10f), Margen);
        }

        [Test]
        public void Actualizar_VidaMaximaCero_NoExplotaYNoSeDebilita()
        {
            Assert.IsFalse(regla.Actualizar(0f, 0f));
            Assert.IsFalse(regla.EstaDebil);
        }

        [Test]
        public void Actualizar_VidaEncimaDelUmbral_NoSeDebilita()
        {
            Assert.IsFalse(regla.Actualizar(36f, VidaMaxima));
            Assert.IsFalse(regla.EstaDebil);
        }

        [Test]
        public void Actualizar_VidaEnElUmbral_SeDebilita()
        {
            Assert.IsTrue(regla.Actualizar(35f, VidaMaxima));
            Assert.IsTrue(regla.EstaDebil);
        }

        [Test]
        public void Actualizar_YaDebilYLaVidaSube_SigueDebil()
        {
            regla.Actualizar(20f, VidaMaxima);

            Assert.IsTrue(regla.Actualizar(100f, VidaMaxima));
            Assert.IsTrue(regla.EstaDebil);
        }

        [Test]
        public void Reiniciar_ConDebilidad_LaQuita()
        {
            regla.Actualizar(20f, VidaMaxima);

            regla.Reiniciar();

            Assert.IsFalse(regla.EstaDebil);
            Assert.IsFalse(regla.Actualizar(100f, VidaMaxima));
        }
    }
}
