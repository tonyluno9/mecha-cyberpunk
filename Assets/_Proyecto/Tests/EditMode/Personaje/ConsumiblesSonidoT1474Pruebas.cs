using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1474: cada consumible pide su sonido (lata antes de beber, papitas, venda, curación) por la fachada de sonidos.</summary>
    public class ConsumiblesSonidoT1474Pruebas
    {
        private sealed class SonidosFalsos : ISonidos
        {
            public readonly List<string> Ids = new List<string>();
            public void Reproducir(string id) { Ids.Add(id); }
            public void ReproducirEn(string id, Vector3 posicion) { Ids.Add(id); }
            public int ReproducirBucle(string id, Transform sigue) { Ids.Add("bucle:" + id); return 0; }
            public void DetenerBucle(int bucle) { }
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) { return 1f; }
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private SonidosFalsos sonidos;

        [SetUp]
        public void SetUp()
        {
            sonidos = new SonidosFalsos();
            Registro<ISonidos>.Agregar(sonidos);
        }

        [TearDown]
        public void TearDown()
        {
            Registro<ISonidos>.Quitar(sonidos);
        }

        [Test]
        public void Sonar_BebidaEnergetica_AbreLaLataAntesDeBeber()
        {
            ReglaSonidoConsumible.Sonar("bebida_energetica");
            CollectionAssert.AreEqual(new[] { "CON_LATA_ABRIR", "CON_BEBER" }, sonidos.Ids);
        }

        [TestCase("cafe", "CON_BEBER")]
        [TestCase("papitas", "CON_PAPITAS_COMER")]
        [TestCase("venda", "CON_VENDA")]
        [TestCase("botiquin_grande", "CON_VENDA")]
        [TestCase("bateria_escudo", "CON_CURACION")]
        public void Sonar_CadaConsumible_PideSuSonido(string id, string esperado)
        {
            ReglaSonidoConsumible.Sonar(id);
            CollectionAssert.AreEqual(new[] { esperado }, sonidos.Ids);
        }

        [Test]
        public void Sonar_IdVacio_NoPideNada()
        {
            ReglaSonidoConsumible.Sonar(null);
            ReglaSonidoConsumible.Sonar("");
            Assert.AreEqual(0, sonidos.Ids.Count);
        }
    }
}
