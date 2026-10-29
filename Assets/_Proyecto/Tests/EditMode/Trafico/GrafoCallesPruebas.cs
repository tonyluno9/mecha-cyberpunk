using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>Las calles como grafo (T-341): se arman con las seis manzanas del barrio, el camino más corto sigue las calles y nunca las abandona.</summary>
    public class GrafoCallesPruebas
    {
        private const float Lado = 80f;
        private const float Altura = 0.2f;
        private const float Paso = 94f;                  // 80 de manzana + 14 de calle
        private const float Tolerancia = 0.01f;
        private const float MetrosEntreMuestras = 1f;
        private const int NodosEsperados = 8;            // 2 calles verticales x 3 niveles + calle horizontal: 2 extremos extra

        private static readonly List<Vector2> Manzanas = new List<Vector2>
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        [Test]
        public void DeManzanas_ArmaLosCrucesYLosExtremos()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Manzanas, Lado, Altura);
            Assert.AreEqual(NodosEsperados, grafo.Cantidad, "Dos calles verticales (3 niveles cada una) y la horizontal con 2 extremos; los cruces se comparten");
        }

        [Test]
        public void Camino_UneDosExtremos_SoloPorCalles()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Manzanas, Lado, Altura);
            int desde = grafo.MasCercano(new Vector3(-Paso / 2f, 0f, -200f));
            int hasta = grafo.MasCercano(new Vector3(Paso / 2f, 0f, 200f));
            List<int> camino = grafo.Camino(desde, hasta);
            Assert.GreaterOrEqual(camino.Count, 2);
            Assert.AreEqual(desde, camino[0]);
            Assert.AreEqual(hasta, camino[camino.Count - 1]);
            for (int i = 0; i < camino.Count - 1; i++)
            {
                Vector3 a = grafo.Nodo(camino[i]);
                Vector3 b = grafo.Nodo(camino[i + 1]);
                float largo = Vector3.Distance(a, b);
                for (float d = 0f; d <= largo; d += MetrosEntreMuestras)
                {
                    Assert.AreEqual(0f, grafo.DistanciaALaCalle(Vector3.Lerp(a, b, d / largo)), Tolerancia);
                }
            }
        }

        [Test]
        public void Camino_AlMismoNodo_EsSoloEseNodo()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Manzanas, Lado, Altura);
            List<int> camino = grafo.Camino(0, 0);
            Assert.AreEqual(1, camino.Count);
        }

        [Test]
        public void DistanciaALaCalle_DentroDeUnaManzana_EsMayorQueCero()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Manzanas, Lado, Altura);
            Assert.Greater(grafo.DistanciaALaCalle(new Vector3(-Paso, 0f, Paso / 2f)), 1f);
        }

        [Test]
        public void MasLejano_EstaMasLejosQueElMasCercano()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Manzanas, Lado, Altura);
            Vector3 punto = new Vector3(-Paso / 2f, 0f, 0f);
            Vector3 cerca = grafo.Nodo(grafo.MasCercano(punto));
            Vector3 lejos = grafo.Nodo(grafo.MasLejano(punto));
            Assert.Greater(Vector3.Distance(lejos, punto), Vector3.Distance(cerca, punto));
        }

        [Test]
        public void DeManzanas_SinManzanas_Falla()
        {
            Assert.Throws<System.ArgumentException>(() => GrafoCalles.DeManzanas(new List<Vector2>(), Lado, Altura));
        }
    }
}
