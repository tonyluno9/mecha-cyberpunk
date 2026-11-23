using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ArsenalPrestadoPruebas
    {
        private GameObject objeto;
        private ArsenalJugador arsenal;

        [SetUp]
        public void SetUp()
        {
            objeto = new GameObject("ArsenalPrueba");
            arsenal = objeto.AddComponent<ArsenalJugador>();
            CargadorPistola.MunicionInfinita = false;
        }

        [TearDown]
        public void TearDown()
        {
            CargadorPistola.MunicionInfinita = false;
            Object.DestroyImmediate(objeto);
        }

        [Test]
        public void PrestarTodas_TodasLasArmasSeTienen_YMunicionInfinita()
        {
            Assert.IsFalse(arsenal.Tiene(ArmaAPie.Escopeta));
            arsenal.PrestarTodas();
            foreach (ArmaAPie arma in System.Enum.GetValues(typeof(ArmaAPie)))
            {
                Assert.IsTrue(arsenal.Tiene(arma), arma.ToString());
            }
            Assert.IsTrue(arsenal.Prestado);
            Assert.IsTrue(CargadorPistola.MunicionInfinita);
        }

        [Test]
        public void DevolverPrestadas_DejaElArsenalComoEstaba()
        {
            int escopetaAntes = arsenal.Nivel(ArmaAPie.Escopeta);
            int pistolaAntes = arsenal.Nivel(ArmaAPie.Pistola);
            arsenal.PrestarTodas();
            arsenal.DevolverPrestadas();
            Assert.AreEqual(escopetaAntes, arsenal.Nivel(ArmaAPie.Escopeta));
            Assert.AreEqual(pistolaAntes, arsenal.Nivel(ArmaAPie.Pistola));
            Assert.IsFalse(arsenal.Prestado);
            Assert.IsFalse(CargadorPistola.MunicionInfinita);
        }

        [Test]
        public void Cargador_ConMunicionInfinita_NoGastaBalas()
        {
            CargadorPistola cargador = new CargadorPistola(8, 0f, 1f);
            CargadorPistola.MunicionInfinita = true;
            Assert.IsTrue(cargador.IntentarDisparar());
            Assert.AreEqual(8, cargador.Balas);
            CargadorPistola.MunicionInfinita = false;
            Assert.IsTrue(cargador.IntentarDisparar());
            Assert.AreEqual(7, cargador.Balas);
        }
    }
}
