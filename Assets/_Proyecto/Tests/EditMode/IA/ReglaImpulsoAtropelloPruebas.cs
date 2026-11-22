using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    public class ReglaImpulsoAtropelloPruebas
    {
        [Test]
        public void Arrolla_CarroRapidoHaciaElNpc_True()
        {
            Assert.IsTrue(ReglaImpulsoAtropello.Arrolla(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 2f)));
        }

        [Test]
        public void Arrolla_CarroLento_False()
        {
            Assert.IsFalse(ReglaImpulsoAtropello.Arrolla(new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 2f)));
        }

        [Test]
        public void Arrolla_NpcDetrasDelCarro_False()
        {
            Assert.IsFalse(ReglaImpulsoAtropello.Arrolla(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -2f)));
        }

        [Test]
        public void VelocidadDeVuelo_SaleHaciaDondeVaElCarroYSube()
        {
            Vector3 v = ReglaImpulsoAtropello.VelocidadDeVuelo(new Vector3(0f, 0f, 12f));
            Assert.Greater(v.z, 5f);
            Assert.AreEqual(0f, v.x, 0.001f);
            Assert.GreaterOrEqual(v.y, ReglaImpulsoAtropello.VerticalMinimo);
            Assert.LessOrEqual(v.y, ReglaImpulsoAtropello.VerticalMaximo);
        }

        [Test]
        public void VelocidadDeVuelo_RapidezEnormeSeRecortaAlTope()
        {
            Vector3 v = ReglaImpulsoAtropello.VelocidadDeVuelo(new Vector3(0f, 0f, 80f));
            Assert.AreEqual(ReglaImpulsoAtropello.RapidezHorizontalMaxima, new Vector2(v.x, v.z).magnitude, 0.001f);
        }

        [Test]
        public void Danio_CreceConLaRapidezYTieneTope()
        {
            Assert.Less(ReglaImpulsoAtropello.Danio(new Vector3(0f, 0f, 5f)), ReglaImpulsoAtropello.Danio(new Vector3(0f, 0f, 15f)));
            Assert.AreEqual(ReglaImpulsoAtropello.DanioMaximo, ReglaImpulsoAtropello.Danio(new Vector3(0f, 0f, 100f)), 0.001f);
        }
    }
}
