using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Las calles del barrio como grafo de puntos (T-341): se arma con los centros de las manzanas (una cuadrícula) y sirve para que la patrulla
    /// siga las calles sin salirse. No usa física: el camino más corto sale de una búsqueda en anchura sobre los cruces.
    /// </summary>
    public sealed class GrafoCalles
    {
        private const float Precision = 100f;                 // las llaves de los nodos se redondean a centímetros

        private readonly List<Vector3> nodos = new List<Vector3>();
        private readonly List<List<int>> vecinos = new List<List<int>>();

        public int Cantidad => nodos.Count;

        public Vector3 Nodo(int indice) => nodos[indice];

        /// <summary>Los nodos vecinos de uno (a un tramo de calle de distancia).</summary>
        public IReadOnlyList<int> Vecinos(int indice) => vecinos[indice];

        /// <summary>
        /// Calles entre manzanas de <paramref name="lado"/> metros de lado, con centros en <paramref name="centros"/> (x, z) y calles al medio de cada par de
        /// columnas y filas; llegan hasta el borde de las manzanas de los extremos. <paramref name="altura"/> es la y de los puntos (m).
        /// </summary>
        public static GrafoCalles DeManzanas(IReadOnlyList<Vector2> centros, float lado, float altura)
        {
            if (centros == null || centros.Count == 0 || lado <= 0f)
            {
                throw new ArgumentException("Al menos una manzana y un lado mayor que 0.");
            }
            List<float> columnas = Distintos(centros, true);
            List<float> filas = Distintos(centros, false);
            float mitad = lado / 2f;
            List<float> callesX = Medios(columnas);
            List<float> callesZ = Medios(filas);
            List<float> nivelesX = new List<float>(callesX) { columnas[0] - mitad, columnas[columnas.Count - 1] + mitad };
            List<float> nivelesZ = new List<float>(callesZ) { filas[0] - mitad, filas[filas.Count - 1] + mitad };
            nivelesX.Sort();
            nivelesZ.Sort();

            GrafoCalles grafo = new GrafoCalles();
            Dictionary<long, int> indices = new Dictionary<long, int>();
            foreach (float x in callesX)
            {
                grafo.EnlazarSerie(indices, nivelesZ, z => new Vector3(x, altura, z));
            }
            foreach (float z in callesZ)
            {
                grafo.EnlazarSerie(indices, nivelesX, x => new Vector3(x, altura, z));
            }
            return grafo;
        }

        /// <summary>Índice del nodo más cercano a un punto (en el plano, ignorando la altura).</summary>
        public int MasCercano(Vector3 punto)
        {
            int mejor = 0;
            float menor = float.MaxValue;
            for (int i = 0; i < nodos.Count; i++)
            {
                float d = Plano(nodos[i], punto);
                if (d < menor)
                {
                    menor = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>Índice del nodo más lejano a un punto (en el plano).</summary>
        public int MasLejano(Vector3 punto)
        {
            int mejor = 0;
            float mayor = -1f;
            for (int i = 0; i < nodos.Count; i++)
            {
                float d = Plano(nodos[i], punto);
                if (d > mayor)
                {
                    mayor = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>Camino de nodos más corto en saltos de <paramref name="desde"/> a <paramref name="hasta"/>, ambos incluidos; vacío si no hay.</summary>
        public List<int> Camino(int desde, int hasta)
        {
            List<int> camino = new List<int>();
            int[] anterior = new int[nodos.Count];
            for (int i = 0; i < anterior.Length; i++)
            {
                anterior[i] = -1;
            }
            Queue<int> cola = new Queue<int>();
            cola.Enqueue(desde);
            anterior[desde] = desde;
            while (cola.Count > 0)
            {
                int actual = cola.Dequeue();
                if (actual == hasta)
                {
                    break;
                }
                foreach (int vecino in vecinos[actual])
                {
                    if (anterior[vecino] < 0)
                    {
                        anterior[vecino] = actual;
                        cola.Enqueue(vecino);
                    }
                }
            }
            if (anterior[hasta] < 0)
            {
                return camino;
            }
            for (int n = hasta; n != desde; n = anterior[n])
            {
                camino.Add(n);
            }
            camino.Add(desde);
            camino.Reverse();
            return camino;
        }

        /// <summary>Distancia (m, en el plano) de un punto a la calle más cercana: 0 si está sobre una calle.</summary>
        public float DistanciaALaCalle(Vector3 punto)
        {
            float menor = float.MaxValue;
            for (int i = 0; i < nodos.Count; i++)
            {
                foreach (int j in vecinos[i])
                {
                    if (j > i)
                    {
                        menor = Mathf.Min(menor, DistanciaASegmento(punto, nodos[i], nodos[j]));
                    }
                }
            }
            return menor;
        }

        private void EnlazarSerie(Dictionary<long, int> indices, List<float> niveles, Func<float, Vector3> punto)
        {
            int anterior = -1;
            foreach (float nivel in niveles)
            {
                int actual = Agregar(indices, punto(nivel));
                if (anterior >= 0)
                {
                    vecinos[anterior].Add(actual);
                    vecinos[actual].Add(anterior);
                }
                anterior = actual;
            }
        }

        private int Agregar(Dictionary<long, int> indices, Vector3 punto)
        {
            long llave = (long)Mathf.Round(punto.x * Precision) * 4000000L + (long)Mathf.Round(punto.z * Precision);
            if (!indices.TryGetValue(llave, out int indice))
            {
                indice = nodos.Count;
                indices[llave] = indice;
                nodos.Add(punto);
                vecinos.Add(new List<int>());
            }
            return indice;
        }

        private static List<float> Distintos(IReadOnlyList<Vector2> centros, bool enX)
        {
            List<float> valores = new List<float>();
            foreach (Vector2 c in centros)
            {
                float v = enX ? c.x : c.y;
                if (!valores.Exists(o => Mathf.Abs(o - v) < 1f / Precision))
                {
                    valores.Add(v);
                }
            }
            valores.Sort();
            return valores;
        }

        private static List<float> Medios(List<float> ordenados)
        {
            List<float> medios = new List<float>();
            for (int i = 0; i + 1 < ordenados.Count; i++)
            {
                medios.Add((ordenados[i] + ordenados[i + 1]) / 2f);
            }
            return medios;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        }

        private static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pa = new Vector2(p.x - a.x, p.z - a.z);
            Vector2 ab = new Vector2(b.x - a.x, b.z - a.z);
            float t = Mathf.Clamp01(Vector2.Dot(pa, ab) / ab.sqrMagnitude);
            return (pa - ab * t).magnitude;
        }
    }
}
