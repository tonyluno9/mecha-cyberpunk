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
        // T-986: tramos que un carro de verdad no puede recorrer (un cierre, una barda): los caminos los rodean o no existen.
        private readonly HashSet<long> tramosBloqueados = new HashSet<long>();

        // T-1350: nodos y tramos de las calles de zona (CalleZona): los de zona no se alargan hacia las afueras; los tramos rápidos son de autopista.
        private readonly HashSet<int> nodosDeZona = new HashSet<int>();
        private readonly HashSet<long> tramosRapidos = new HashSet<long>();
        // T-1526: nodos fuera del muro de la ciudad: sin vecinos y nunca se eligen para salir, llegar o retirarse.
        private readonly HashSet<int> nodosFuera = new HashSet<int>();
        private float alturaMinima = float.MaxValue;
        private float alturaMaxima = float.MinValue;

        /// <summary>T-1350: a partir de esta diferencia de altura (m) entre nodos, "el más cercano" cuenta también la y (autopista sobre calles).</summary>
        public const float DiferenciaDeAlturaTridimensional = 1.5f;

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

        /// <summary>
        /// T-532: alarga el grafo con un tramo nuevo de <paramref name="desde"/> a <paramref name="punto"/> (las calles de las afueras). Devuelve el índice
        /// del nodo nuevo.
        /// </summary>
        public int AgregarTramo(int desde, Vector3 punto)
        {
            int nuevo = AgregarNodo(punto);
            Enlazar(desde, nuevo);
            return nuevo;
        }

        /// <summary>T-1350: agrega un nodo suelto (sin vecinos) y devuelve su índice. <paramref name="deZona"/>: pertenece a una calle de zona.</summary>
        public int AgregarNodo(Vector3 punto, bool deZona = false)
        {
            nodos.Add(punto);
            vecinos.Add(new List<int>());
            alturaMinima = Mathf.Min(alturaMinima, punto.y);
            alturaMaxima = Mathf.Max(alturaMaxima, punto.y);
            int nuevo = nodos.Count - 1;
            if (deZona)
            {
                nodosDeZona.Add(nuevo);
            }
            return nuevo;
        }

        /// <summary>T-1350: une dos nodos con un tramo (en los dos sentidos). No repite un tramo ni une un nodo consigo mismo. <paramref name="rapido"/>: tramo de autopista.</summary>
        public void Enlazar(int a, int b, bool rapido = false)
        {
            if (a == b)
            {
                return;
            }
            if (!vecinos[a].Contains(b))
            {
                vecinos[a].Add(b);
                vecinos[b].Add(a);
            }
            if (rapido)
            {
                tramosRapidos.Add(LlaveDeTramo(a, b));
            }
        }

        /// <summary>T-1526: false si el nodo quedó fuera del muro (<see cref="Recortar"/>): no sirve para aparecer ni para ir.</summary>
        public bool Dentro(int indice) => nodosFuera.Count == 0 || !nodosFuera.Contains(indice);

        /// <summary>T-1526: cuántos nodos quedaron fuera del muro.</summary>
        public int NodosFuera => nodosFuera.Count;

        /// <summary>T-1526: cuántos nodos se movieron al borde del área porque quedaban apenas fuera (ver <see cref="Recortar"/>).</summary>
        public int NodosAjustados { get; private set; }

        /// <summary>
        /// T-1526: deja el grafo dentro de <paramref name="area"/> (el interior del muro, plano x, z). Un nodo que queda fuera por menos de <paramref name="holgura"/> m (el final de una
        /// calle de la cuadrícula, pegado al muro) se mueve al borde del área; los más lejanos (las calles de las afueras, que salen por las puertas del muro) se desconectan.
        /// Así una calle que llegaba a una salida termina dentro y los carros dan vuelta ahí. Devuelve cuántos nodos desconectó. Si el área dejaría menos de
        /// <paramref name="minimoDentro"/> nodos no hace nada (el área estaría mal detectada).
        /// </summary>
        public int Recortar(Rect area, float holgura = 24f, int minimoDentro = 4)
        {
            List<int> fuera = new List<int>();
            List<int> ajustar = new List<int>();
            for (int i = 0; i < nodos.Count; i++)
            {
                float dx = Mathf.Max(area.xMin - nodos[i].x, 0f, nodos[i].x - area.xMax);
                float dz = Mathf.Max(area.yMin - nodos[i].z, 0f, nodos[i].z - area.yMax);
                float d = Mathf.Max(dx, dz);
                if (d <= 0f)
                {
                    continue;
                }
                (d <= holgura ? ajustar : fuera).Add(i);
            }
            if ((fuera.Count == 0 && ajustar.Count == 0) || nodos.Count - fuera.Count < minimoDentro)
            {
                return 0;
            }
            foreach (int i in ajustar)
            {
                Vector3 p = nodos[i];
                // Rect.Contains no incluye el borde de arriba (xMax, yMax): se deja 1 cm adentro para que el nodo cuente como dentro del área.
                nodos[i] = new Vector3(Mathf.Clamp(p.x, area.xMin, area.xMax - 0.01f), p.y, Mathf.Clamp(p.z, area.yMin, area.yMax - 0.01f));
                NodosAjustados++;
            }
            foreach (int i in fuera)
            {
                foreach (int v in vecinos[i])
                {
                    vecinos[v].Remove(i);
                }
                vecinos[i].Clear();
                nodosFuera.Add(i);
            }
            // Un nodo de adentro que solo llegaba a lo de afuera queda suelto: tampoco sirve para nada.
            for (int i = 0; i < nodos.Count; i++)
            {
                if (vecinos[i].Count == 0 && nodosFuera.Add(i))
                {
                    fuera.Add(i);
                }
            }
            return fuera.Count;
        }

        /// <summary>T-1350: true si el nodo es de una calle de zona.</summary>
        public bool EsDeZona(int indice) => nodosDeZona.Contains(indice);

        /// <summary>T-1350: true si el tramo entre dos nodos es de autopista (los carros van más rápido).</summary>
        public bool EsRapido(int a, int b) => tramosRapidos.Count > 0 && tramosRapidos.Contains(LlaveDeTramo(a, b));

        /// <summary>T-1350: true si hay nodos a distinta altura (rampas, autopista elevada).</summary>
        public bool TieneAlturas => nodos.Count > 0 && alturaMaxima - alturaMinima > DiferenciaDeAlturaTridimensional;

        private static long LlaveDeTramo(int a, int b) => a < b ? (long)a * 1000003L + b : (long)b * 1000003L + a;

        /// <summary>T-986: marca el tramo entre dos nodos vecinos como cerrado: <see cref="Camino"/> ya no pasa por él.</summary>
        public void BloquearTramo(int a, int b)
        {
            tramosBloqueados.Add(LlaveDeTramo(a, b));
        }

        /// <summary>T-986: false si el tramo entre dos nodos vecinos está cerrado.</summary>
        public bool TramoLibre(int a, int b) => !tramosBloqueados.Contains(LlaveDeTramo(a, b));

        /// <summary>T-986: cuántos tramos están cerrados.</summary>
        public int TramosBloqueados => tramosBloqueados.Count;

        /// <summary>Índice del nodo más cercano a un punto (en el plano, ignorando la altura; con nodos a distinta altura, T-1350, en 3D).</summary>
        public int MasCercano(Vector3 punto)
        {
            int mejor = 0;
            float menor = float.MaxValue;
            bool tridimensional = TieneAlturas;
            for (int i = 0; i < nodos.Count; i++)
            {
                if (!Dentro(i))
                {
                    continue;
                }
                float d = tridimensional ?Vector3.Distance(nodos[i], punto) : Plano(nodos[i], punto);
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
                if (!Dentro(i))
                {
                    continue;
                }
                float d = Plano(nodos[i], punto);
                if (d > mayor)
                {
                    mayor = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>T-986: el nodo más lejano a un punto entre los que se puede llegar por las calles desde el nodo <paramref name="desde"/> (sin cruzar tramos cerrados).</summary>
        public int MasLejano(Vector3 punto, int desde)
        {
            int mejor = desde;
            float mayor = -1f;
            HashSet<int> vistos = new HashSet<int> { desde };
            Queue<int> cola = new Queue<int>();
            cola.Enqueue(desde);
            while (cola.Count > 0)
            {
                int actual = cola.Dequeue();
                float d = Plano(nodos[actual], punto);
                if (d > mayor)
                {
                    mayor = d;
                    mejor = actual;
                }
                foreach (int vecino in vecinos[actual])
                {
                    if (TramoLibre(actual, vecino) && vistos.Add(vecino))
                    {
                        cola.Enqueue(vecino);
                    }
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
                    if (anterior[vecino] < 0 && TramoLibre(actual, vecino))
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
            if (ab.sqrMagnitude <= 0f)
            {
                return pa.magnitude;
            }
            float t = Mathf.Clamp01(Vector2.Dot(pa, ab) / ab.sqrMagnitude);
            return (pa - ab * t).magnitude;
        }
    }
}
