using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1498: reglas de la nave de Nova (altura de flote, cuántos soldados, descenso por la cuerda).</summary>
    public class NaveNovaT1498Pruebas
    {
        [Test]
        public void AlturaDeFlote_ConAireDeSobra_FlotaALaAlturaDeseada()
        {
            Assert.AreEqual(10f, ReglaNaveNova.AlturaDeFlote(60f, 10f, 6f, 2f), 0.0001f);
        }

        [Test]
        public void AlturaDeFlote_ConTechoCerca_BajaPeroNoDeLaMinima()
        {
            Assert.AreEqual(8f, ReglaNaveNova.AlturaDeFlote(10f, 10f, 6f, 2f), 0.0001f);
        }

        [Test]
        public void AlturaDeFlote_ConTechoMuyBajo_NoSirveElPunto()
        {
            Assert.Less(ReglaNaveNova.AlturaDeFlote(7f, 10f, 6f, 2f), 0f);
        }

        [Test]
        public void Soldados_DosConCuatro_CuatroConCinco_NadaBajoCuatro()
        {
            Assert.AreEqual(0, ReglaNaveNova.Soldados(3, 2, 4));
            Assert.AreEqual(2, ReglaNaveNova.Soldados(4, 2, 4));
            Assert.AreEqual(4, ReglaNaveNova.Soldados(5, 2, 4));
        }

        [Test]
        public void PosicionDescenso_EsLinealYTerminaEnElPiso()
        {
            Vector3 desde = new Vector3(0f, 10f, 0f);
            Vector3 hasta = new Vector3(0f, 0f, 0f);
            Assert.AreEqual(7f, ReglaNaveNova.PosicionDescenso(desde, hasta, 0.5f, 6f).y, 0.0001f);
            Assert.AreEqual(4f, ReglaNaveNova.PosicionDescenso(desde, hasta, 1f, 6f).y, 0.0001f);
            Assert.AreEqual(0f, ReglaNaveNova.PosicionDescenso(desde, hasta, 5f, 6f).y, 0.0001f);
            Assert.AreEqual(10f, ReglaNaveNova.PosicionDescenso(desde, hasta, 0f, 6f).y, 0.0001f);
        }

        [Test]
        public void SegundosDescenso_AlturaEntreVelocidad()
        {
            Assert.AreEqual(2f, ReglaNaveNova.SegundosDescenso(12f, 6f), 0.0001f);
            Assert.AreEqual(0f, ReglaNaveNova.SegundosDescenso(12f, 0f), 0.0001f);
        }

        [Test]
        public void PuntoLejano_EstaALaDistanciaYMasAlto()
        {
            Vector3 punto = ReglaNaveNova.PuntoLejano(new Vector3(5f, 10f, 5f), new Vector3(0f, 7f, 3f), 100f, 15f);
            Assert.AreEqual(25f, punto.y, 0.0001f);
            Vector3 plano = punto - new Vector3(5f, 25f, 5f);
            Assert.AreEqual(100f, plano.magnitude, 0.001f);
            Assert.AreEqual(5f, punto.x, 0.0001f);
        }
    }
}
