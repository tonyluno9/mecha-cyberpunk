using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-708: dónde sale una patrulla según el tiempo que tarda en llegar por las calles.</summary>
    public class CalculoLlegadaPatrullaT708Pruebas
    {
        private static readonly bool[] Todos = { true, true, true, true };

        [Test]
        public void Elige_ElMasCercanoAlTiempoObjetivo_ConElMinimoCumplido()
        {
            // A 15 m/s tardan 2 s, 6 s, 11 s y 20 s; mínimo 7 s y objetivo 12 s: el de 11 s.
            int i = CalculoLlegadaPatrulla.Elegir(new[] { 30f, 90f, 165f, 300f }, Todos, 15f, 12f, 7f);
            Assert.AreEqual(2, i);
        }

        [Test]
        public void NoElige_UnPuntoQueLlegaAntesDelMinimo()
        {
            int i = CalculoLlegadaPatrulla.Elegir(new[] { 30f, 90f }, new[] { true, true }, 15f, 3f, 7f);
            Assert.AreEqual(1, i, "Ninguno llega al mínimo: sale el que más tarda");
        }

        [Test]
        public void Ignora_LosPuntosNoPermitidos_YLosSinRuta()
        {
            int i = CalculoLlegadaPatrulla.Elegir(new[] { 180f, float.MaxValue, 150f }, new[] { false, true, true }, 15f, 12f, 7f);
            Assert.AreEqual(2, i);
            Assert.AreEqual(-1, CalculoLlegadaPatrulla.Elegir(new[] { 180f }, new[] { false }, 15f, 12f, 7f), "Sin puntos permitidos no sale (espera)");
        }

        [Test]
        public void MetrosDeRuta_SumaLosTramosDeLasCalles()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(new List<Vector2> { new Vector2(0f, 0f), new Vector2(94f, 0f), new Vector2(0f, 94f), new Vector2(94f, 94f) }, 80f, 0.2f);
            int a = 0;
            int vecino = grafo.Vecinos(a)[0];
            Assert.AreEqual(Vector3.Distance(grafo.Nodo(a), grafo.Nodo(vecino)), CalculoLlegadaPatrulla.MetrosDeRuta(grafo, a, vecino), 0.001f, "Un tramo");
            int lejos = grafo.MasLejano(grafo.Nodo(a));
            float metros = CalculoLlegadaPatrulla.MetrosDeRuta(grafo, a, lejos);
            Assert.GreaterOrEqual(metros, Vector3.Distance(grafo.Nodo(a), grafo.Nodo(lejos)) - 0.01f, "Por las calles nunca es menos que en línea recta");
            Assert.AreEqual(0f, CalculoLlegadaPatrulla.MetrosDeRuta(grafo, a, a), 0.001f);
        }
    }
}
