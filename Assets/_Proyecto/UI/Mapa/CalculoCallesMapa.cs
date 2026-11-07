using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Cuentas de las calles del minimapa (T-701), sin Unity vivo. Las calles se sacan de las manzanas: lo que no es edificio entre edificios es calle,
    /// y sus bordes son los contornos de las manzanas. Así no dependen de cómo se llamen los objetos de la ciudad.
    /// </summary>
    public static class CalculoCallesMapa
    {
        /// <summary>Un tramo de línea del mapa (m: x este, y norte).</summary>
        public readonly struct Tramo
        {
            public readonly Vector2 A;
            public readonly Vector2 B;

            public Tramo(Vector2 a, Vector2 b)
            {
                A = a;
                B = b;
            }

            public float Largo => Vector2.Distance(A, B);
        }

        /// <summary>Un rectángulo (4 esquinas alrededor, X y Z del mundo): la huella de un edificio.</summary>
        public readonly struct Huella
        {
            public readonly Vector2[] Esquinas;

            public Huella(Vector2 e0, Vector2 e1, Vector2 e2, Vector2 e3)
            {
                Esquinas = new[] { e0, e1, e2, e3 };
            }

            /// <summary>Largo de los lados 0-1 y 1-2.</summary>
            public Vector2 Medidas => new Vector2(Vector2.Distance(Esquinas[0], Esquinas[1]), Vector2.Distance(Esquinas[1], Esquinas[2]));

            /// <summary>Si el punto cae dentro del rectángulo.</summary>
            public bool Contiene(Vector2 punto)
            {
                int signo = 0;
                for (int k = 0; k < 4; k++)
                {
                    Vector2 p = Esquinas[k];
                    Vector2 q = Esquinas[(k + 1) % 4];
                    float cruz = (q.x - p.x) * (punto.y - p.y) - (q.y - p.y) * (punto.x - p.x);
                    int s = cruz > 0f ? 1 : (cruz < 0f ? -1 : 0);
                    if (s != 0)
                    {
                        if (signo != 0 && s != signo)
                        {
                            return false;
                        }
                        signo = s;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Los contornos de las manzanas: se marca en una cuadrícula de <paramref name="celda"/> m lo que cubren las huellas y se sacan las líneas donde
        /// lo ocupado toca lo libre. Lo que queda entre manzanas son las calles, así que las líneas son los bordes de las calles. Huellas que se tocan
        /// se funden en una sola manzana (sin línea entre ellas). Los tramos más cortos que <paramref name="largoMinimo"/> se quitan (migajas). Si la cuadrícula saliera de más de <paramref name="maximoCeldas"/> por lado, la celda crece.
        /// </summary>
        public static List<Tramo> ContornosDeManzanas(IReadOnlyList<Huella> huellas, float celda = 1.5f, int maximoCeldas = 900, float largoMinimo = 4f)
        {
            List<Tramo> resultado = new List<Tramo>();
            if (huellas.Count == 0)
            {
                return resultado;
            }
            Vector2 minimo = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maximo = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < huellas.Count; i++)
            {
                foreach (Vector2 e in huellas[i].Esquinas)
                {
                    minimo = Vector2.Min(minimo, e);
                    maximo = Vector2.Max(maximo, e);
                }
            }
            minimo -= Vector2.one * celda;
            maximo += Vector2.one * celda;
            Vector2 tamano = maximo - minimo;
            celda = Mathf.Max(celda, Mathf.Max(tamano.x, tamano.y) / maximoCeldas);
            int columnas = Mathf.CeilToInt(tamano.x / celda) + 1;
            int filas = Mathf.CeilToInt(tamano.y / celda) + 1;
            bool[] ocupada = new bool[columnas * filas];
            for (int i = 0; i < huellas.Count; i++)
            {
                Vector2 bMin = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 bMax = new Vector2(float.MinValue, float.MinValue);
                foreach (Vector2 e in huellas[i].Esquinas)
                {
                    bMin = Vector2.Min(bMin, e);
                    bMax = Vector2.Max(bMax, e);
                }
                int x0 = Mathf.Max(0, Mathf.FloorToInt((bMin.x - minimo.x) / celda));
                int x1 = Mathf.Min(columnas - 1, Mathf.FloorToInt((bMax.x - minimo.x) / celda));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((bMin.y - minimo.y) / celda));
                int y1 = Mathf.Min(filas - 1, Mathf.FloorToInt((bMax.y - minimo.y) / celda));
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        if (huellas[i].Contiene(minimo + new Vector2(x + 0.5f, y + 0.5f) * celda))
                        {
                            ocupada[y * columnas + x] = true;
                        }
                    }
                }
            }
            // Líneas horizontales: entre la fila y-1 y la y; se unen las celdas seguidas.
            for (int y = 0; y <= filas; y++)
            {
                int inicio = -1;
                for (int x = 0; x <= columnas; x++)
                {
                    bool borde = x < columnas && Ocupada(ocupada, columnas, filas, x, y - 1) != Ocupada(ocupada, columnas, filas, x, y);
                    if (borde && inicio < 0)
                    {
                        inicio = x;
                    }
                    else if (!borde && inicio >= 0)
                    {
                        resultado.Add(new Tramo(minimo + new Vector2(inicio, y) * celda, minimo + new Vector2(x, y) * celda));
                        inicio = -1;
                    }
                }
            }
            // Líneas verticales: entre la columna x-1 y la x.
            for (int x = 0; x <= columnas; x++)
            {
                int inicio = -1;
                for (int y = 0; y <= filas; y++)
                {
                    bool borde = y < filas && Ocupada(ocupada, columnas, filas, x - 1, y) != Ocupada(ocupada, columnas, filas, x, y);
                    if (borde && inicio < 0)
                    {
                        inicio = y;
                    }
                    else if (!borde && inicio >= 0)
                    {
                        resultado.Add(new Tramo(minimo + new Vector2(x, inicio) * celda, minimo + new Vector2(x, y) * celda));
                        inicio = -1;
                    }
                }
            }
            resultado.RemoveAll(t => t.Largo < largoMinimo);
            return resultado;
        }

        private static bool Ocupada(bool[] ocupada, int columnas, int filas, int x, int y) =>
            x >= 0 && y >= 0 && x < columnas && y < filas && ocupada[y * columnas + x];

        /// <summary>Distancia (m) del punto al tramo.</summary>
        public static float DistanciaATramo(Vector2 punto, Tramo tramo)
        {
            Vector2 d = tramo.B - tramo.A;
            float largo2 = d.sqrMagnitude;
            float t = largo2 > 0f ? Mathf.Clamp01(Vector2.Dot(punto - tramo.A, d) / largo2) : 0f;
            return Vector2.Distance(punto, tramo.A + d * t);
        }

        /// <summary>
        /// Separa los íconos que se encimarían: se queda con los primeros y descarta los que quedan a menos de <paramref name="distanciaMinima"/> px de
        /// otro ya puesto o a menos de <paramref name="radioCentro"/> px del centro (el jugador). Devuelve cuáles se quedan.
        /// </summary>
        public static bool[] SinEncimar(IReadOnlyList<Vector2> posiciones, IReadOnlyList<bool> visibles, float distanciaMinima, float radioCentro)
        {
            bool[] queda = new bool[posiciones.Count];
            List<Vector2> puestos = new List<Vector2>();
            for (int i = 0; i < posiciones.Count; i++)
            {
                if (!visibles[i])
                {
                    continue;
                }
                bool libre = posiciones[i].magnitude >= radioCentro;
                for (int j = 0; j < puestos.Count && libre; j++)
                {
                    libre = Vector2.Distance(posiciones[i], puestos[j]) >= distanciaMinima;
                }
                if (libre)
                {
                    puestos.Add(posiciones[i]);
                    queda[i] = true;
                }
            }
            return queda;
        }
    }
}
