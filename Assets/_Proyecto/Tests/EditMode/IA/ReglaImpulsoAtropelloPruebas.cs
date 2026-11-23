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

        // T-1708 (T75): a 30 km/h (8,33 m/s) y a 20 km/h el carro arrolla y el NPC sale volando en su dirección y cae.
        [TestCase(30f)]
        [TestCase(20f)]
        public void Carro_ArribaDe20KmH_Arrolla_YElNpcSaleVolandoHaciaDelante(float kmh)
        {
            Vector3 carro = new Vector3(0f, 0f, kmh / 3.6f);
            Assert.IsTrue(ReglaImpulsoAtropello.Arrolla(carro, new Vector3(0f, 0f, 2f)));
            Vector3 v = ReglaImpulsoAtropello.VelocidadDeVuelo(carro);
            Assert.Greater(v.z, 4f, "sale hacia donde va el carro");
            Assert.GreaterOrEqual(v.y, 3f, "sale hacia arriba");
            float tiempoVuelo = 2f * v.y / 9.81f;
            Assert.Greater(v.z * tiempoVuelo, 2.5f, "recorre metros antes de caer");
            Assert.LessOrEqual(ReglaImpulsoAtropello.RapidezMinima * 3.6f, 20f, "el umbral no deja sin arrollar a 20 km/h");
        }
    }
}
