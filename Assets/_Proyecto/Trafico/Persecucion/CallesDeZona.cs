using System.Collections.Generic;
using MechaCyberpunk.Core.Mundo;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1350: agrega al <see cref="GrafoCalles"/> las calles de las zonas nuevas (Malecón, Cerro, Autopista, Puerto). Un nodo por punto de la línea del
    /// centro (con su y real), tramos entre puntos seguidos y cada extremo unido a la red si queda cerca. Es lógica pura: no lee la escena
    /// (la sobrecarga sin parámetros lee <see cref="CalleZona.Activas"/>).
    /// </summary>
    public static class CallesDeZona
    {
        /// <summary>Un extremo solo se une a un nodo que esté a esta diferencia de altura (m) o menos: la autopista no se pega a las calles de abajo.</summary>
        public const float DiferenciaMaximaDeAltura = 4f;

        /// <summary>Hasta dónde (m) llega un extremo para unirse a una calle sin salida de las afueras que apunta hacia él.</summary>
        public const float AlcanceDeAfueras = 140f;

        private const float DistanciaMismoPunto = 0.05f;
        private const float DiferenciaMismoPunto = 0.5f;

        /// <summary>Una calle de zona: línea del centro en mundo.</summary>
        public readonly struct Definicion
        {
            public readonly IReadOnlyList<Vector3> Puntos;
            public readonly bool Autopista;

            public Definicion(IReadOnlyList<Vector3> puntos, bool autopista)
            {
                Puntos = puntos;
                Autopista = autopista;
            }
        }

        /// <summary>Agrega las <see cref="CalleZona.Activas"/> al grafo. Devuelve cuántos extremos quedaron unidos a algo.</summary>
        public static int Agregar(GrafoCalles grafo)
        {
            if (grafo == null || CalleZona.Activas.Count == 0)
            {
                return 0;
            }
            List<Definicion> calles = new List<Definicion>();
            foreach (CalleZona calle in CalleZona.Activas)
            {
                if (calle != null && calle.Cantidad >= 2)
                {
                    calles.Add(new Definicion(calle.Puntos(), calle.Autopista));
                }
            }
            return Agregar(grafo, calles, CalleZona.DistanciaUnion);
        }

        /// <summary>Agrega las calles al grafo. Devuelve cuántos extremos quedaron unidos a algo.</summary>
        public static int Agregar(GrafoCalles grafo, IReadOnlyList<Definicion> calles, float distanciaUnion)
        {
            Dictionary<int, int> propietario = new Dictionary<int, int>(); // nodo de zona -> calle
            List<int> extremos = new List<int>();                          // pares (primero, último) por calle
            List<bool> reutilizado = new List<bool>();
            for (int c = 0; c < calles.Count; c++)
            {
                IReadOnlyList<Vector3> puntos = calles[c].Puntos;
                int anterior = -1;
                int primero = -1;
                bool primeroReusado = false;
                bool ultimoReusado = false;
                for (int i = 0; i < puntos.Count; i++)
                {
                    int existente = BuscarMismoPunto(grafo, puntos[i]);
                    bool reusado = existente >= 0;
                    int nodo = reusado ? existente : grafo.AgregarNodo(puntos[i], true);
                    if (!reusado)
                    {
                        propietario[nodo] = c;
                    }
                    if (anterior >= 0)
                    {
                        grafo.Enlazar(anterior, nodo, calles[c].Autopista);
                    }
                    else
                    {
                        primero = nodo;
                        primeroReusado = reusado;
                    }
                    ultimoReusado = reusado;
                    anterior = nodo;
                }
                extremos.Add(primero);
                extremos.Add(anterior);
                reutilizado.Add(primeroReusado);
                reutilizado.Add(ultimoReusado);
            }

            int unidos = 0;
            List<int> sueltos = new List<int>();
            for (int k = 0; k < extremos.Count; k++)
            {
                int extremo = extremos[k];
                if (extremo < 0)
                {
                    continue;
                }
                if (reutilizado[k])
                {
                    unidos++;
                    continue;
                }
                int cercano = BuscarCercano(grafo, extremo, k / 2, propietario, distanciaUnion);
                if (cercano >= 0)
                {
                    grafo.Enlazar(extremo, cercano);
                    unidos++;
                }
                else
                {
                    sueltos.Add(extremo);
                }
            }
            foreach (int extremo in sueltos)
            {
                int callejon = BuscarCalleSinSalida(grafo, extremo, distanciaUnion);
                if (callejon >= 0)
                {
                    grafo.Enlazar(callejon, extremo);
                    unidos++;
                }
            }
            return unidos;
        }

        private static int BuscarMismoPunto(GrafoCalles grafo, Vector3 punto)
        {
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                Vector3 n = grafo.Nodo(i);
                if (Plano(n, punto) <= DistanciaMismoPunto && Mathf.Abs(n.y - punto.y) <= DiferenciaMismoPunto)
                {
                    return i;
                }
            }
            return -1;
        }

        // El nodo más cercano en planta (menos de la distancia de unión y a altura parecida) que no sea de la misma calle.
        private static int BuscarCercano(GrafoCalles grafo, int extremo, int calle, Dictionary<int, int> propietario, float distanciaUnion)
        {
            Vector3 punto = grafo.Nodo(extremo);
            int mejor = -1;
            float menor = distanciaUnion;
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                if (i == extremo || (propietario.TryGetValue(i, out int dueno) && dueno == calle))
                {
                    continue;
                }
                Vector3 n = grafo.Nodo(i);
                float d = Plano(n, punto);
                if (d <= menor && Mathf.Abs(n.y - punto.y) <= DiferenciaMaximaDeAltura)
                {
                    menor = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        // Una calle sin salida (de la cuadrícula o de sus afueras) cuyo rumbo pasa a menos de la distancia de unión del extremo: la calle sigue hasta él.
        private static int BuscarCalleSinSalida(GrafoCalles grafo, int extremo, float distanciaUnion)
        {
            Vector3 punto = grafo.Nodo(extremo);
            int mejor = -1;
            float menor = AlcanceDeAfueras;
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                if (grafo.EsDeZona(i) || grafo.Vecinos(i).Count != 1)
                {
                    continue;
                }
                Vector3 n = grafo.Nodo(i);
                Vector3 rumbo = n - grafo.Nodo(grafo.Vecinos(i)[0]);
                rumbo.y = 0f;
                if (rumbo.sqrMagnitude < 0.01f || Mathf.Abs(n.y - punto.y) > DiferenciaMaximaDeAltura)
                {
                    continue;
                }
                rumbo.Normalize();
                Vector3 hacia = punto - n;
                hacia.y = 0f;
                float delante = Vector3.Dot(hacia, rumbo);
                float lateral = Mathf.Abs(hacia.x * rumbo.z - hacia.z * rumbo.x);
                if (delante > 0f && delante <= menor && lateral <= distanciaUnion)
                {
                    menor = delante;
                    mejor = i;
                }
            }
            return mejor;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        }
    }
}
