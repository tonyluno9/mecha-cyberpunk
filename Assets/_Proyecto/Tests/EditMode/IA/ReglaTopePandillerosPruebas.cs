using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1495: tope de pandilleros vivos por punto y por zona.</summary>
    public class ReglaTopePandillerosPruebas
    {
        [Test]
        public void Cupo_ZonaVacia_EsElTopePorPunto()
        {
            Assert.AreEqual(ReglaTopePandilleros.PorPunto, ReglaTopePandilleros.Cupo(0, 0));
        }

        [Test]
        public void Cupo_GrupoYaLleno_EsCero()
        {
            Assert.AreEqual(0, ReglaTopePandilleros.Cupo(ReglaTopePandilleros.PorPunto, ReglaTopePandilleros.PorPunto));
        }

        [Test]
        public void Cupo_LaZonaYaTieneElTope_NoDejaAparecerMas()
        {
            Assert.AreEqual(0, ReglaTopePandilleros.Cupo(0, ReglaTopePandilleros.EnZona));
        }

        [Test]
        public void Cupo_ZonaCasiLlena_DejaSoloLoQueFalta()
        {
            Assert.AreEqual(2, ReglaTopePandilleros.Cupo(3, ReglaTopePandilleros.EnZona - 2));
        }

        [Test]
        public void Cupo_NuncaEsNegativo()
        {
            Assert.AreEqual(0, ReglaTopePandilleros.Cupo(20, 50));
        }

        [Test]
        public void Topes_SonCoherentes()
        {
            Assert.LessOrEqual(ReglaTopePandilleros.PorPunto, ReglaTopePandilleros.EnZona);
            Assert.AreEqual(40f, ReglaTopePandilleros.RadioZona);
        }
    }
}
