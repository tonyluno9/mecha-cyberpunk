using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1526: el interior del muro de la ciudad en el plano (x, z). El tráfico, las patrullas y las persecuciones solo ciclan dentro de él:
    /// los nodos de calle fuera del área se desconectan del grafo (<see cref="Persecucion.GrafoCalles.Recortar"/>) y un carro que acabe fuera se recicla adentro.
    /// El área sale de las piezas del muro (MU01 muro, MU02 torres, MU03 puertas, o el muro provisional T530_Afueras/Orillas/Muro) o de un dato manual
    /// (<see cref="Definir"/>, que <see cref="TraficoCalle"/> llena desde su campo serializado). Sin muro en la escena no hay área y no se recorta nada.
    /// </summary>
    public static class AreaMuro
    {
        public const string NombreRaiz = "T1513_MuroCiudad";
        public const string RutaOrillas = "T530_Afueras/Orillas";
        public const string NombreMuroViejo = "Muro";
        /// <summary>Prefijos de las piezas del muro por si no cuelgan de la raíz esperada (nada de "Puerta_" suelto: hay puertas de edificios).</summary>
        public static readonly string[] PrefijosPieza = { "MU01", "MU02", "MU03" };
        /// <summary>Con menos puntos que esto no se puede decir que hay un muro que cierra la ciudad.</summary>
        public const int MinimoDePuntos = 4;
        /// <summary>Lado mínimo (m) del área para darla por buena.</summary>
        public const float LadoMinimo = 100f;
        /// <summary>Cuánto (m) se mete el área hacia adentro del trazo del muro (grosor, collider y el largo de un carro).</summary>
        public const float MargenPorOmision = 4f;

        private static Rect? cache;
        private static bool buscada;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            cache = null;
            buscada = false;
        }

        /// <summary>Fija el área a mano (dato serializado o pruebas). Un área vacía (ancho o alto 0) la borra y vuelve a buscar en la escena.</summary>
        public static void Definir(Rect area)
        {
            if (area.width <= 0f || area.height <= 0f)
            {
                cache = null;
                buscada = false;
                return;
            }
            cache = area;
            buscada = true;
        }

        /// <summary>El área del interior del muro (x, y = x, z), o null si no hay muro. La busca una vez por escena.</summary>
        public static Rect? Actual(float margen = MargenPorOmision)
        {
            if (!buscada)
            {
                buscada = true;
                cache = Buscar(margen);
            }
            return cache;
        }

        /// <summary>true si el punto está fuera del muro. Sin área conocida siempre false (no se recicla nada por error).</summary>
        public static bool Fuera(Vector3 punto)
        {
            Rect? area = Actual();
            return area.HasValue && !area.Value.Contains(new Vector2(punto.x, punto.z));
        }

        /// <summary>Recorta el grafo al interior del muro. Devuelve cuántos nodos quitó (0 si no hay muro).</summary>
        public static int Recortar(Persecucion.GrafoCalles grafo, float margen = MargenPorOmision)
        {
            Rect? area = Actual(margen);
            if (grafo == null || !area.HasValue)
            {
                return 0;
            }
            int quitados = grafo.Recortar(area.Value);
            if (quitados > 0)
            {
                Debug.Log("[Trafico] T-1526: " + quitados + " nodos de calle fuera del muro desconectados (" + grafo.Cantidad + " en total). Área " + area.Value);
            }
            return quitados;
        }

        /// <summary>
        /// Lógica pura: la caja que envuelve a los puntos, metida <paramref name="margen"/> m hacia adentro por cada lado. Null si hay pocos puntos o la caja
        /// queda más chica que <see cref="LadoMinimo"/> (no es un muro que cierre la ciudad).
        /// </summary>
        public static Rect? Calcular(IReadOnlyList<Vector2> puntos, float margen)
        {
            if (puntos == null || puntos.Count < MinimoDePuntos)
            {
                return null;
            }
            float minX = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxZ = float.MinValue;
            foreach (Vector2 p in puntos)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.y);
                maxZ = Mathf.Max(maxZ, p.y);
            }
            if (maxX - minX < LadoMinimo || maxZ - minZ < LadoMinimo)
            {
                return null;
            }
            float m = Mathf.Max(0f, margen);
            Rect area = Rect.MinMaxRect(minX + m, minZ + m, maxX - m, maxZ - m);
            return area.width > 0f && area.height > 0f ? area : (Rect?)null;
        }

        private static Rect? Buscar(float margen)
        {
            List<Vector2> puntos = new List<Vector2>();
            GameObject raiz = GameObject.Find(NombreRaiz);
            if (raiz != null)
            {
                foreach (Transform hijo in raiz.transform)
                {
                    puntos.Add(new Vector2(hijo.position.x, hijo.position.z));
                }
            }
            Rect? area = Calcular(puntos, margen);
            if (area.HasValue)
            {
                return area;
            }
            puntos.Clear();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (EsPiezaDelMuro(t.name))
                {
                    puntos.Add(new Vector2(t.position.x, t.position.z));
                }
            }
            area = Calcular(puntos, margen);
            if (area.HasValue)
            {
                return area;
            }
            puntos.Clear();
            GameObject orillas = GameObject.Find(RutaOrillas);
            if (orillas != null)
            {
                foreach (Transform hijo in orillas.transform)
                {
                    if (hijo.name != NombreMuroViejo)
                    {
                        continue;
                    }
                    // El muro provisional mide lossyScale.z de largo a lo largo de su forward (T-1513).
                    Vector3 f = hijo.forward;
                    f.y = 0f;
                    f.Normalize();
                    Vector2 mitad = new Vector2(f.x, f.z) * (hijo.lossyScale.z * 0.5f);
                    Vector2 centro = new Vector2(hijo.position.x, hijo.position.z);
                    puntos.Add(centro + mitad);
                    puntos.Add(centro - mitad);
                }
            }
            return Calcular(puntos, margen);
        }

        /// <summary>true si el nombre es de una pieza del muro (MU01, MU02 o MU03).</summary>
        public static bool EsPiezaDelMuro(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return false;
            }
            foreach (string prefijo in PrefijosPieza)
            {
                if (nombre.StartsWith(prefijo, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
