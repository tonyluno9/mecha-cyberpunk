using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// La ruta por las calles (T-632, movida aquí en T-669): del origen al destino por el <see cref="GrafoCalles"/>. Fuera de las calles, el primer y el
    /// último tramo son rectos hasta el punto de calle más cercano.
    /// </summary>
    public static class CalculoRutaCalles
    {
        public static List<Vector3> Ruta(GrafoCalles calles, Vector3 origen, Vector3 destino)
        {
            origen.y = 0f;
            destino.y = 0f;
            Calle desdeCalle = MasCercana(calles, origen);
            Calle hastaCalle = MasCercana(calles, destino);
            List<Vector3> ruta = new List<Vector3> { origen };
            if (desdeCalle.A == hastaCalle.A && desdeCalle.B == hastaCalle.B)
            {
                ruta.Add(desdeCalle.Punto);
                ruta.Add(hastaCalle.Punto);
                ruta.Add(destino);
                return ruta;
            }
            List<int> mejor = null;
            float menor = float.MaxValue;
            int[] salidas = { desdeCalle.A, desdeCalle.B };
            int[] llegadas = { hastaCalle.A, hastaCalle.B };
            foreach (int salida in salidas)
            {
                foreach (int llegada in llegadas)
                {
                    List<int> camino = calles.Camino(salida, llegada);
                    if (camino.Count == 0)
                    {
                        continue;
                    }
                    float largo = Vector3.Distance(desdeCalle.Punto, Plano(calles.Nodo(salida))) + Vector3.Distance(Plano(calles.Nodo(llegada)), hastaCalle.Punto);
                    for (int i = 1; i < camino.Count; i++)
                    {
                        largo += Vector3.Distance(Plano(calles.Nodo(camino[i - 1])), Plano(calles.Nodo(camino[i])));
                    }
                    if (largo < menor)
                    {
                        menor = largo;
                        mejor = camino;
                    }
                }
            }
            if (mejor == null)
            {
                return new List<Vector3>();
            }
            // Si el camino sale por el otro extremo de la calle, no regreses por el primero.
            if (mejor.Count > 1 && (mejor[0] == desdeCalle.A || mejor[0] == desdeCalle.B)
                && (mejor[1] == desdeCalle.A || mejor[1] == desdeCalle.B))
            {
                mejor.RemoveAt(0);
            }
            if (mejor.Count > 1 && (mejor[mejor.Count - 1] == hastaCalle.A || mejor[mejor.Count - 1] == hastaCalle.B)
                && (mejor[mejor.Count - 2] == hastaCalle.A || mejor[mejor.Count - 2] == hastaCalle.B))
            {
                mejor.RemoveAt(mejor.Count - 1);
            }
            ruta.Add(desdeCalle.Punto);
            foreach (int nodo in mejor)
            {
                ruta.Add(Plano(calles.Nodo(nodo)));
            }
            ruta.Add(hastaCalle.Punto);
            ruta.Add(destino);
            return ruta;
        }

        /// <summary>Largo (m) de una ruta.</summary>
        public static float Largo(IReadOnlyList<Vector3> ruta)
        {
            float total = 0f;
            for (int i = 1; i < ruta.Count; i++)
            {
                total += Vector3.Distance(ruta[i - 1], ruta[i]);
            }
            return total;
        }

        private struct Calle
        {
            public int A;
            public int B;
            public Vector3 Punto;
        }

        // El tramo de calle (arista) cuyo punto más cercano al punto dado está más cerca, con ese punto.
        private static Calle MasCercana(GrafoCalles calles, Vector3 punto)
        {
            Calle mejor = new Calle { A = 0, B = 0, Punto = Plano(calles.Nodo(0)) };
            float menor = float.MaxValue;
            for (int a = 0; a < calles.Cantidad; a++)
            {
                foreach (int b in calles.Vecinos(a))
                {
                    if (b < a)
                    {
                        continue;
                    }
                    Vector3 pa = Plano(calles.Nodo(a));
                    Vector3 pb = Plano(calles.Nodo(b));
                    Vector3 ab = pb - pa;
                    float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(punto - pa, ab) / ab.sqrMagnitude) : 0f;
                    Vector3 cerca = pa + ab * t;
                    float d = Vector3.Distance(punto, cerca);
                    if (d < menor)
                    {
                        menor = d;
                        mejor = new Calle { A = a, B = b, Punto = cerca };
                    }
                }
            }
            return mejor;
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
