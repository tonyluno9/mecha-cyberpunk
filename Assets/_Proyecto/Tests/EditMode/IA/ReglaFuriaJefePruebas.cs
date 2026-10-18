using MechaCyberpunk.IA.Rival;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    public class ReglaFuriaJefePruebas
    {
        private const float VidaMaxima = 150f;
        private ReglaFuriaJefe regla;

        [SetUp]
        public void SetUp()
        {
            regla = new ReglaFuriaJefe(0.5f);
        }

        [Test]
        public void Actualizar_Vida76De150_NoEntraEnFuria()
        {
            Assert.IsFalse(regla.Actualizar(76f, VidaMaxima));
            Assert.IsFalse(regla.EnFuria);
        }

        [Test]
        public void Actualizar_Vida75De150_EntraEnFuria()
        {
            Assert.IsTrue(regla.Actualizar(75f, VidaMaxima));
            Assert.IsTrue(regla.EnFuria);
        }

        [Test]
        public void Actualizar_YaEnFuria_NoVuelveAAvisar()
        {
            regla.Actualizar(75f, VidaMaxima);
            Assert.IsFalse(regla.Actualizar(40f, VidaMaxima));
            Assert.IsTrue(regla.EnFuria);
        }

        [Test]
        public void Actualizar_EnKO_NoEntraEnFuria()
        {
            Assert.IsFalse(regla.Actualizar(0f, VidaMaxima));
            Assert.IsFalse(regla.EnFuria);
        }

        [Test]
        public void Actualizar_VidaMaximaCero_NoEntraEnFuria()
        {
            Assert.IsFalse(regla.Actualizar(10f, 0f));
        }

        [Test]
        public void Reiniciar_DespuesDeLaFuria_PuedeEntrarOtraVez()
        {
            regla.Actualizar(75f, VidaMaxima);
            regla.Reiniciar();
            Assert.IsFalse(regla.EnFuria);
            Assert.IsTrue(regla.Actualizar(70f, VidaMaxima));
        }
    }
}
