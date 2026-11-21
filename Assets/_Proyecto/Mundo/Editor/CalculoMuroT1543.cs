using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1543: cuentas puras (sin escena) de "la ciudad dentro del muro, sin nada detrás y sin huecos".
    /// Todo en el plano XZ; un Rect usa x = X y y = Z. La "línea" es el rectángulo del trazo del muro (el centro de sus piezas).
    /// </summary>
    public static class CalculoMuroT1543
    {
        /// <summary>Lado de la rejilla con que se buscan huecos (m).</summary>
        public const float Celda = 4f;

        /// <summary>Radio libre alrededor del punto de suelo para llamarlo hueco (m).</summary>
        public const float RadioHueco = 3f;

        /// <summary>Altura libre sobre el suelo para llamarlo hueco (m).</summary>
        public const float AltoHueco = 3f;

        /// <summary>Cuánto se recorta el piso más allá de la línea del muro (m).</summary>
        public const float HolguraPiso = 3f;

        /// <summary>Paso con que un lote se retrae o se estira hasta tocar a sus vecinos (m).</summary>
        public const float PasoAjuste = 0.5f;

        /// <summary>Lo máximo que un lote se estira o se retrae por lado (m).</summary>
        public const float AjusteMaximoLado = 4f;

        /// <summary>Estiramiento máximo entre ejes de un edificio de relleno.</summary>
        public const float EstiramientoMaximo = 3f;

        /// <summary>Largo de fachada que se busca para cada edificio de un lote (m).</summary>
        public const float AnchoDeseado = 18f;

        public enum Lado
        {
            Este,
            Oeste,
            Norte,
            Sur
        }

        /// <summary>Rectángulo del muro a partir de sus segmentos (sus extremos).</summary>
        public static Rect RectDeSegmentos(IList<CalculoMuroT1513.Segmento> segmentos)
        {
            if (segmentos == null || segmentos.Count == 0)
            {
                return new Rect(0f, 0f, 0f, 0f);
            }
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (CalculoMuroT1513.Segmento s in segmentos)
            {
                x0 = Mathf.Min(x0, Mathf.Min(s.A.x, s.B.x));
                x1 = Mathf.Max(x1, Mathf.Max(s.A.x, s.B.x));
                z0 = Mathf.Min(z0, Mathf.Min(s.A.z, s.B.z));
                z1 = Mathf.Max(z1, Mathf.Max(s.A.z, s.B.z));
            }
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }

        public static Rect Expandir(Rect r, float m)
        {
            return Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);
        }

        /// <summary>El centro (x, z) queda fuera de la línea del muro más el margen.</summary>
        public static bool CentroFuera(Rect linea, float margen, Vector3 centro)
        {
            return !Expandir(linea, margen).Contains(new Vector2(centro.x, centro.z));
        }

        /// <summary>La caja no toca para nada la línea del muro más el margen: está toda afuera.</summary>
        public static bool CajaFuera(Rect linea, float margen, Bounds b)
        {
            Rect e = Expandir(linea, margen);
            return b.max.x < e.xMin || b.min.x > e.xMax || b.max.z < e.yMin || b.min.z > e.yMax;
        }

        /// <summary>La caja se sale del rectángulo (línea más holgura) por algún lado.</summary>
        public static bool Excede(Rect linea, float holgura, Bounds b)
        {
            Rect e = Expandir(linea, holgura);
            return b.min.x < e.xMin - 0.01f || b.max.x > e.xMax + 0.01f || b.min.z < e.yMin - 0.01f || b.max.z > e.yMax + 0.01f;
        }

        /// <summary>
        /// Recorte de una caja a (línea + holgura) en XZ. Devuelve falso si no queda nada. min y max son (x, z) de la caja recortada.
        /// </summary>
        public static bool Recortar(Rect linea, float holgura, Bounds b, out Vector2 min, out Vector2 max)
        {
            Rect e = Expandir(linea, holgura);
            min = new Vector2(Mathf.Max(b.min.x, e.xMin), Mathf.Max(b.min.z, e.yMin));
            max = new Vector2(Mathf.Min(b.max.x, e.xMax), Mathf.Min(b.max.z, e.yMax));
            return max.x - min.x > 0.01f && max.y - min.y > 0.01f;
        }

        /// <summary>Distancia en el plano XZ del punto a la caja (0 si está encima).</summary>
        public static float DistanciaXZ(Bounds b, Vector2 p)
        {
            float dx = Mathf.Max(Mathf.Max(b.min.x - p.x, 0f), p.x - b.max.x);
            float dz = Mathf.Max(Mathf.Max(b.min.z - p.y, 0f), p.y - b.max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Barrio por cuadrante (mapa final M13): Nova arriba-izquierda, Neón Alto arriba-derecha, Suburbios abajo-izquierda, Mercado abajo-derecha.</summary>
        public static string Barrio(Vector2 p)
        {
            if (p.y >= 0f)
            {
                return p.x < 0f ? "Nova" : "NeonAlto";
            }
            return p.x < 0f ? "Suburbios" : "Mercado";
        }

        /// <summary>La clave de paleta de edificios (CalculoEdificiosT1512) que usa un barrio. Nova comparte la de Neón Alto (ED08, ED09, NV01...).</summary>
        public static string ClavePaleta(string barrio)
        {
            return barrio == "Nova" ? "NeonAlto" : barrio;
        }

        /// <summary>Hacia dónde mira una luz puesta en una torre del muro para alumbrar la parte de afuera: el lado más cercano (en las esquinas, la diagonal).</summary>
        public static Vector3 DireccionAfuera(Vector3 posicion, Rect linea)
        {
            float dE = Mathf.Abs(linea.xMax - posicion.x);
            float dO = Mathf.Abs(posicion.x - linea.xMin);
            float dN = Mathf.Abs(linea.yMax - posicion.z);
            float dS = Mathf.Abs(posicion.z - linea.yMin);
            float dx = Mathf.Min(dE, dO);
            float dz = Mathf.Min(dN, dS);
            const float cerca = 12f;
            float x = dE <= dO ? 1f : -1f;
            float z = dN <= dS ? 1f : -1f;
            Vector3 d;
            if (dx < cerca && dz < cerca)
            {
                d = new Vector3(x, 0f, z);
            }
            else if (dx <= dz)
            {
                d = new Vector3(x, 0f, 0f);
            }
            else
            {
                d = new Vector3(0f, 0f, z);
            }
            return d.normalized;
        }

        // ------------------------------------------------------------------ huecos

        /// <summary>Componentes conexas (4 vecinos) de un conjunto de celdas, de mayor a menor.</summary>
        public static List<List<Vector2Int>> Componentes(ICollection<Vector2Int> celdas)
        {
            HashSet<Vector2Int> pendientes = new HashSet<Vector2Int>(celdas);
            List<List<Vector2Int>> salida = new List<List<Vector2Int>>();
            Vector2Int[] vecinos = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (pendientes.Count > 0)
            {
                Vector2Int semilla = default;
                foreach (Vector2Int c in pendientes)
                {
                    semilla = c;
                    break;
                }
                List<Vector2Int> comp = new List<Vector2Int>();
                Queue<Vector2Int> cola = new Queue<Vector2Int>();
                cola.Enqueue(semilla);
                pendientes.Remove(semilla);
                while (cola.Count > 0)
                {
                    Vector2Int c = cola.Dequeue();
                    comp.Add(c);
                    foreach (Vector2Int v in vecinos)
                    {
                        Vector2Int n = c + v;
                        if (pendientes.Remove(n))
                        {
                            cola.Enqueue(n);
                        }
                    }
                }
                salida.Add(comp);
            }
            salida.Sort((a, b) => b.Count.CompareTo(a.Count));
            return salida;
        }

        /// <summary>
        /// Parte un conjunto de celdas en rectángulos: siempre el de mayor área que cabe entero, hasta que no quede ninguna celda.
        /// Cada RectInt está en celdas (position = esquina mínima, size = ancho y alto en celdas).
        /// </summary>
        public static List<RectInt> Rectangulos(ICollection<Vector2Int> celdas)
        {
            HashSet<Vector2Int> resto = new HashSet<Vector2Int>(celdas);
            List<RectInt> salida = new List<RectInt>();
            while (resto.Count > 0)
            {
                RectInt mejor = MayorRectangulo(resto);
                if (mejor.width <= 0 || mejor.height <= 0)
                {
                    break;
                }
                salida.Add(mejor);
                for (int x = mejor.xMin; x < mejor.xMax; x++)
                {
                    for (int y = mejor.yMin; y < mejor.yMax; y++)
                    {
                        resto.Remove(new Vector2Int(x, y));
                    }
                }
            }
            return salida;
        }

        private static RectInt MayorRectangulo(HashSet<Vector2Int> celdas)
        {
            int x0 = int.MaxValue, x1 = int.MinValue, y0 = int.MaxValue, y1 = int.MinValue;
            foreach (Vector2Int c in celdas)
            {
                x0 = Mathf.Min(x0, c.x);
                x1 = Mathf.Max(x1, c.x);
                y0 = Mathf.Min(y0, c.y);
                y1 = Mathf.Max(y1, c.y);
            }
            int ancho = x1 - x0 + 1;
            int alto = y1 - y0 + 1;
            int[] altura = new int[ancho];
            RectInt mejor = new RectInt(0, 0, 0, 0);
            int mejorArea = 0;
            for (int y = y0; y <= y1; y++)
            {
                for (int i = 0; i < ancho; i++)
                {
                    altura[i] = celdas.Contains(new Vector2Int(x0 + i, y)) ? altura[i] + 1 : 0;
                }
                // Mayor rectángulo bajo el histograma de esta fila (el borde superior es la fila y).
                Stack<int> pila = new Stack<int>();
                for (int i = 0; i <= ancho; i++)
                {
                    int h = i == ancho ? 0 : altura[i];
                    while (pila.Count > 0 && altura[pila.Peek()] >= h)
                    {
                        int alturaBarra = altura[pila.Pop()];
                        int izq = pila.Count == 0 ? 0 : pila.Peek() + 1;
                        int w = i - izq;
                        int area = w * alturaBarra;
                        if (area > mejorArea)
                        {
                            mejorArea = area;
                            mejor = new RectInt(x0 + izq, y - alturaBarra + 1, w, alturaBarra);
                        }
                    }
                    pila.Push(i);
                }
            }
            if (alto < 0)
            {
                return new RectInt(0, 0, 0, 0);
            }
            return mejor;
        }

        /// <summary>Esquinas del rectángulo de celdas en metros, con la rejilla anclada en el origen (la celda (0,0) cubre 0..Celda).</summary>
        public static Rect EnMetros(RectInt celdas)
        {
            return new Rect(celdas.xMin * Celda, celdas.yMin * Celda, celdas.width * Celda, celdas.height * Celda);
        }

        public static Vector2Int CeldaDe(Vector2 p)
        {
            return new Vector2Int(Mathf.FloorToInt(p.x / Celda), Mathf.FloorToInt(p.y / Celda));
        }

        public static Vector2 CentroDeCelda(Vector2Int c)
        {
            return new Vector2((c.x + 0.5f) * Celda, (c.y + 0.5f) * Celda);
        }

        // ------------------------------------------------------------------ edificios de relleno

        public struct Modelo
        {
            public Vector3 Tam;
        }

        public struct Ajuste
        {
            public int Indice;
            public Vector3 Escala;
            public float Razon;
        }

        /// <summary>
        /// Cuántos edificios caben a lo largo del frente y cuál modelo y escala usa cada uno. ancho es el largo de la fachada y fondo la profundidad.
        /// Cada edificio queda con escala (sx, sy, sz) donde x va a lo ancho y z al fondo; la altura sale de la raíz de sx*sz (sin pasar de 0.8..2.0).
        /// Devuelve una lista vacía si ningún modelo cabe con el estiramiento permitido.
        /// </summary>
        public static List<Ajuste> RepartirFrente(IList<Modelo> paleta, float ancho, float fondo, int semilla)
        {
            List<Ajuste> mejorLista = new List<Ajuste>();
            if (paleta == null || paleta.Count == 0 || ancho < 1f || fondo < 1f)
            {
                return mejorLista;
            }
            int n0 = Mathf.Max(1, Mathf.RoundToInt(ancho / AnchoDeseado));
            float mejorPeor = float.MaxValue;
            for (int n = Mathf.Max(1, n0 - 1); n <= n0 + 1; n++)
            {
                float w = ancho / n;
                List<Ajuste> lista = new List<Ajuste>();
                float peor = 0f;
                for (int k = 0; k < n; k++)
                {
                    List<Ajuste> candidatos = new List<Ajuste>();
                    for (int i = 0; i < paleta.Count; i++)
                    {
                        Vector3 t = paleta[i].Tam;
                        if (t.x < 0.01f || t.y < 0.01f || t.z < 0.01f)
                        {
                            continue;
                        }
                        float sx = w / t.x;
                        float sz = fondo / t.z;
                        float sy = Mathf.Clamp(Mathf.Sqrt(sx * sz), 0.8f, 2f);
                        float menor = Mathf.Min(sx, Mathf.Min(sy, sz));
                        float mayor = Mathf.Max(sx, Mathf.Max(sy, sz));
                        candidatos.Add(new Ajuste { Indice = i, Escala = new Vector3(sx, sy, sz), Razon = mayor / menor });
                    }
                    if (candidatos.Count == 0)
                    {
                        return new List<Ajuste>();
                    }
                    candidatos.Sort((a, b) => a.Razon.CompareTo(b.Razon));
                    int buenos = 1;
                    while (buenos < candidatos.Count && buenos < 3 && candidatos[buenos].Razon <= candidatos[0].Razon + 0.3f)
                    {
                        buenos++;
                    }
                    Ajuste elegido = candidatos[Mathf.Abs(semilla * 31 + k * 7) % buenos];
                    lista.Add(elegido);
                    peor = Mathf.Max(peor, elegido.Razon);
                }
                if (peor < mejorPeor)
                {
                    mejorPeor = peor;
                    mejorLista = lista;
                }
            }
            if (mejorPeor > EstiramientoMaximo)
            {
                return new List<Ajuste>();
            }
            return mejorLista;
        }
    }
}
