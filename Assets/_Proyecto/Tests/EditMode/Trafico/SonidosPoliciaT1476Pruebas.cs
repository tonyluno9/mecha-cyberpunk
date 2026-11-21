using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1476: sonidos de la policía (radio y sirenas) con un ISonidos falso.</summary>
    public class SonidosPoliciaT1476Pruebas
    {
        private class Falso : ISonidos
        {
            public readonly List<string> Pedidos = new List<string>();
            public readonly List<int> Detenidos = new List<int>();
            public void Reproducir(string id) => Pedidos.Add(id);
            public void ReproducirEn(string id, Vector3 posicion) => Pedidos.Add(id);
            public int ReproducirBucle(string id, Transform sigue) { Pedidos.Add("bucle:" + id); return 7; }
            public void DetenerBucle(int bucle) => Detenidos.Add(bucle);
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) => 1f;
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private Falso falso;
        private ConfigPersecucion config;

        [SetUp]
        public void Preparar()
        {
            falso = new Falso();
            Registro<ISonidos>.Agregar(falso);
            config = ScriptableObject.CreateInstance<ConfigPersecucion>();
            RadioPolicia.ReiniciarEnfriamientos();
        }

        [TearDown]
        public void Limpiar()
        {
            Registro<ISonidos>.Quitar(falso);
            Object.DestroyImmediate(config);
            RadioPolicia.ReiniciarEnfriamientos();
        }

        [Test]
        public void Radio_PideAvisoUnaVezSinSpam()
        {
            Assert.IsTrue(RadioPolicia.Sonar(Vector3.zero, config));
            Assert.IsFalse(RadioPolicia.Sonar(Vector3.zero, config), "el segundo aviso seguido no suena");
            CollectionAssert.AreEqual(new[] { "POL_RADIO_AVISO" }, falso.Pedidos);
        }

        [Test]
        public void Radio_DespachoPideSuIDYTieneSuEnfriamiento()
        {
            Assert.IsTrue(RadioPolicia.SonarDespacho(Vector3.zero, config));
            Assert.IsFalse(RadioPolicia.SonarDespacho(Vector3.zero, config));
            CollectionAssert.AreEqual(new[] { "POL_RADIO_AVISO" }, falso.Pedidos);
        }

        [Test]
        public void Sirena_ArrancaBloopYBucle_YSeDetiene()
        {
            GameObject patrulla = new GameObject("Patrulla_T1476");
            int bucle = SonidosPolicia.IniciarSirena(patrulla.transform);
            Assert.AreEqual(7, bucle);
            CollectionAssert.AreEqual(new[] { "POL_SIRENA_CORTA", "bucle:POL_SIRENA_LOOP" }, falso.Pedidos);
            SonidosPolicia.DetenerSirena(bucle);
            CollectionAssert.AreEqual(new[] { 7 }, falso.Detenidos);
            SonidosPolicia.DetenerSirena(Sonidos.SinBucle);
            Assert.AreEqual(1, falso.Detenidos.Count, "sin bucle no detiene nada");
            Object.DestroyImmediate(patrulla);
        }
    }
}
