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
        /// <summary>Cuánto (m) puede pasarse un carro del área antes de contar como fuera (un carro dando vuelta pegado al muro).</summary>
        public const float ToleranciaFuera = 3f;
        /// <summary>Lado mínimo (m) del área para darla por buena.</summary>
        public const float LadoMinimo = 100f;
        /// <summary>Cuánto (m) se mete el área hacia adentro del trazo del muro. 0: los finales de calle de la cuadrícula (a ~1 m del muro) siguen dentro; el carro da vuelta antes por ReglaCallejon.</summary>
        public const float MargenPorOmision = 0f;

        private static Rect? cache;
        private static bool buscada;
        private static Object fuente; // un objeto del muro del que salió el área; si se destruye, el área ya no vale

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            cache = null;
            buscada = false;
            // T-1526: el área es de la escena: al cargar o descargar una escena se vuelve a buscar (si no, una prueba o una escena nueva heredaban el muro de la anterior).
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AlCambiarEscena;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= AlDescargarEscena;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += AlCambiarEscena;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += AlDescargarEscena;
        }

        private static void AlCambiarEscena(UnityEngine.SceneManagement.Scene escena, UnityEngine.SceneManagement.LoadSceneMode modo)
        {
            cache = null;
            buscada = false;
        }

        private static void AlDescargarEscena(UnityEngine.SceneManagement.Scene escena)
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
            fuente = null;
            buscada = true;
        }

        /// <summary>El área del interior del muro (x, y = x, z), o null si no hay muro. La busca una vez por escena.</summary>
        public static Rect? Actual(float margen = MargenPorOmision)
        {
            if (buscada && (object)fuente != null && fuente == null)
            {
                buscada = false; // el muro de donde salió el área ya no existe (otra escena, o se borró)
            }
            if (!buscada)
            {
                buscada = true;
                cache = Buscar(margen);
            }
            return cache;
        }

        /// <summary>true si el punto está fuera del muro. Sin área conocida siempre false (no se recicla nada por error).</summary>
        public static bool Fuera(Vector3 punto, float tolerancia = ToleranciaFuera)
        {
            Rect? area = Actual();
            return area.HasValue && (punto.x < area.Value.xMin - tolerancia || punto.x > area.Value.xMax + tolerancia || punto.z < area.Value.yMin - tolerancia || punto.z > area.Value.yMax + tolerancia);
        }

        /// <summary>Recorta el grafo al interior del muro. Devuelve cuántos nodos quitó (0 si no hay muro).</summary>
        public static int Recortar(Persecucion.GrafoCalles grafo, float margen = MargenPorOmision)
        {
            Rect? area = Actual(margen);
            if (grafo == null)
            {
                return 0;
            }
            if (!area.HasValue)
            {
                Debug.LogWarning("[Trafico] T-1526: no encontré el área del muro (piezas MU01/MU02/MU03, T1513_MuroCiudad o muro viejo); el grafo de " + grafo.Cantidad + " nodos no se recorta.");
                return 0;
            }
            int quitados = grafo.Recortar(area.Value);
            Debug.Log("[Trafico] T-1526: área del muro " + area.Value + "; grafo de " + grafo.Cantidad + " nodos: " + quitados + " desconectados, " + grafo.NodosAjustados + " movidos al borde"
                + (quitados == 0 && grafo.NodosAjustados == 0 ? " (nada fuera del área, o el área dejaba muy pocos nodos)" : "") + ".");
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
            fuente = null;
            GameObject raiz = GameObject.Find(NombreRaiz);
            fuente = raiz;
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
                    fuente = t;
                }
            }
            area = Calcular(puntos, margen);
            if (area.HasValue)
            {
                return area;
            }
            puntos.Clear();
            GameObject orillas = GameObject.Find(RutaOrillas);
            fuente = orillas;
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
