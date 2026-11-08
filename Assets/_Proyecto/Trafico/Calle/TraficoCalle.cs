using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// El tráfico de la calle del barrio (T-451): arma el grafo de calles con las manzanas (las mismas que usa la persecución), reparte los carros
    /// en nodos al azar y, según el reloj del mundo, deja prendidos más de día y menos de noche. Los carros andan solos (<see cref="CarroTrafico"/>).
    /// </summary>
    public class TraficoCalle : MonoBehaviour
    {
        private const int Semilla = 451;
        private const float DistanciaMinimaAlJugador = 25f;
        private const int IntentosDeNodo = 12;

        /// <summary>Largo (m) que siguen las calles fuera del barrio (las afueras de T-530 miden 100 m).</summary>
        public const float LargoAfueras = 92f;

        [SerializeField] private ConfigTraficoCalle config;
        [SerializeField] private CarroTrafico prefabCarro;
        [Tooltip("T-511: los carros con modelo real; si hay, cada carro del tráfico usa uno de estos (en orden y repitiendo) en vez de prefabCarro.")]
        [SerializeField] private List<CarroTrafico> prefabsCarro = new List<CarroTrafico>();
        [Tooltip("Opcional: el padre de las manzanas (hijos Manzana_). Sin él se usa el del GestorPersecucion del barrio.")]
        [SerializeField] private Transform barrio;

        private readonly List<CarroTrafico> carros = new List<CarroTrafico>();
        private GrafoCalles calles;
        private bool listo;

        public IReadOnlyList<CarroTrafico> Carros => carros;

        /// <summary>Cuántos carros andan ahora (prendidos).</summary>
        public int Vivos
        {
            get
            {
                int cuenta = 0;
                foreach (CarroTrafico c in carros)
                {
                    cuenta += c != null && !c.Robado && !c.EnRefuerzo && c.gameObject.activeSelf ? 1 : 0;
                }
                return cuenta;
            }
        }

        /// <summary>
        /// T-714: manda un carro del pool a <paramref name="destino"/> como refuerzo. Sale en el nodo de calle (fuera de la vista de la cámara con margen y a más de
        /// <paramref name="distanciaMinima"/> m del destino, sin otro carro encima) cuya ruta por las calles tarda lo más cerca de <paramref name="segundosObjetivo"/> a
        /// <paramref name="velocidad"/> (al menos <paramref name="segundosMinimos"/>), como las patrullas de T-708. Devuelve el carro, o null si no hay carro libre o
        /// no hay por dónde salir sin que se vea.
        /// </summary>
        public CarroTrafico EnviarRefuerzo(Vector3 destino, float velocidad, float segundosObjetivo, float segundosMinimos, float distanciaMinima)
        {
            if (!listo || calles == null)
            {
                return null;
            }
            CarroTrafico carro = CarroLibreParaRefuerzo(destino);
            if (carro == null)
            {
                return null;
            }
            Camera camara = Camera.main;
            int llegada = calles.MasCercano(destino);
            List<float> metros = new List<float>(calles.Cantidad);
            List<bool> permitido = new List<bool>(calles.Cantidad);
            for (int i = 0; i < calles.Cantidad; i++)
            {
                Vector3 nodo = calles.Nodo(i);
                Vector3 d = nodo - destino;
                d.y = 0f;
                bool vale = d.magnitude >= distanciaMinima && !CarroTrafico.PuntoEnVista(camara, nodo) && !HayCarroEn(nodo) && EspacioLibre.Libre(nodo, carro.transform);
                permitido.Add(vale);
                metros.Add(vale ? CalculoLlegadaPatrulla.MetrosDeRuta(calles, i, llegada) : float.MaxValue);
            }
            int salida = CalculoLlegadaPatrulla.Elegir(metros, permitido, velocidad, segundosObjetivo, segundosMinimos);
            if (salida < 0 || !carro.IniciarRefuerzo(salida, llegada, velocidad))
            {
                return null;
            }
            return carro;
        }

        private static bool HayCarroEn(Vector3 punto)
        {
            foreach (CarroTrafico c in CarroTrafico.Activos)
            {
                Vector3 d = c.transform.position - punto;
                d.y = 0f;
                if (d.magnitude < SeparacionAlSalir)
                {
                    return true;
                }
            }
            return false;
        }

        private const float SeparacionAlSalir = 14f;

        /// <summary>
        /// T-714: el carro del pool que se manda de refuerzo: de preferencia uno apagado (sin usar); si todos andan, uno que no se vea en la cámara y esté a más de
        /// <see cref="DistanciaMinimaDelRefuerzo"/> m de <paramref name="destino"/> (se recicla sin que nadie lo vea desaparecer, como T-704). Nunca uno robado ni ya de refuerzo; null si no hay.
        /// </summary>
        public CarroTrafico CarroLibreParaRefuerzo(Vector3? destino = null)
        {
            CarroTrafico oculto = null;
            Camera camara = Camera.main;
            foreach (CarroTrafico c in carros)
            {
                if (c == null || c.Robado || c.EnRefuerzo || c.EstaSiendoRobado)
                {
                    continue;
                }
                if (!c.gameObject.activeSelf)
                {
                    return c;
                }
                Vector3 d = destino.HasValue ? c.transform.position - destino.Value : Vector3.zero;
                d.y = 0f;
                if (oculto == null && !c.EnVista(camara) && (!destino.HasValue || d.magnitude >= DistanciaMinimaDelRefuerzo))
                {
                    oculto = c;
                }
            }
            return oculto;
        }

        /// <summary>T-714: a esta distancia (m) del destino debe estar un carro que anda para poder reciclarlo como refuerzo.</summary>
        public const float DistanciaMinimaDelRefuerzo = 45f;

        /// <summary>Lo usan el creador de editor y las pruebas; debe llamarse antes de que arranque el tráfico.</summary>
        public void Configurar(ConfigTraficoCalle numeros, CarroTrafico prefab, Transform padreDeManzanas = null)
        {
            config = numeros;
            prefabCarro = prefab;
            barrio = padreDeManzanas;
        }

        /// <summary>T-511: igual, pero con varios modelos de carro (cada carro de la calle usa uno).</summary>
        public void Configurar(ConfigTraficoCalle numeros, IEnumerable<CarroTrafico> prefabs, Transform padreDeManzanas = null)
        {
            config = numeros;
            prefabsCarro = new List<CarroTrafico>(prefabs);
            barrio = padreDeManzanas;
        }

        private void Start()
        {
            if (config == null || (prefabCarro == null && prefabsCarro.Count == 0))
            {
                Debug.LogError("[Trafico] TraficoCalle necesita la config y el prefab del carro.", this);
                enabled = false;
                return;
            }
            GestorPersecucion gestor = FindFirstObjectByType<GestorPersecucion>(FindObjectsInactive.Include);
            Transform padre = barrio != null ? barrio : (gestor != null ? gestor.transform.parent : null);
            calles = padre != null ? GestorPersecucion.ArmarCallesDe(padre) : null;
            if (calles == null || calles.Cantidad < 2)
            {
                Debug.LogWarning("[Trafico] TraficoCalle: no encontré las manzanas del barrio; no hay tráfico.");
                enabled = false;
                return;
            }
            AlargarHaciaLasAfueras(calles);
            System.Random azar = new System.Random(Semilla);
            for (int i = 0; i < config.Maximo; i++)
            {
                CarroTrafico molde = prefabsCarro.Count > 0 ? prefabsCarro[i % prefabsCarro.Count] : prefabCarro;
                CarroTrafico carro = Instantiate(molde, transform);
                carro.name = "CarroTrafico_" + (i + 1);
                carro.Preparar(config, calles, ElegirNodo(azar), new System.Random(azar.Next()), PosicionDelJugador);
                carro.gameObject.SetActive(false);
                carros.Add(carro);
            }
            listo = true;
        }

        private void Update()
        {
            if (!listo)
            {
                return;
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            int objetivo = config.Cantidad(reloj != null && reloj.EsDeNoche);
            int lugares = objetivo;
            for (int i = 0; i < carros.Count; i++)
            {
                if (carros[i] == null || carros[i].Robado || carros[i].EnRefuerzo)
                {
                    continue;
                }
                bool debeEstar = lugares > 0;
                lugares--;
                if (carros[i].gameObject.activeSelf != debeEstar)
                {
                    CambiarEstado(carros[i], debeEstar);
                }
            }
        }

        // T-704: ciclo normal de un pool. Un carro solo entra donde la cámara no lo ve y lejos del jugador (si no hay sitio así, espera al siguiente
        // cuadro) y solo sale si la cámara no lo ve (si se ve, sigue su camino y sale cuando se aleje o dé la vuelta a la esquina).
        private static void CambiarEstado(CarroTrafico carro, bool debeEstar)
        {
            if (debeEstar)
            {
                if (carro.Reaparecer(false, true))
                {
                    carro.gameObject.SetActive(true);
                }
                return;
            }
            if (!carro.EnVista(Camera.main))
            {
                carro.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// T-532: las calles que llegan a la orilla del barrio siguen <see cref="LargoAfueras"/> m (las calles de las afueras de T-530); los carros
        /// entran y salen de la ciudad por ahí y desaparecen al final, lejos de la vista.
        /// </summary>
        public static void AlargarHaciaLasAfueras(GrafoCalles grafo)
        {
            int cantidad = grafo.Cantidad;
            for (int i = 0; i < cantidad; i++)
            {
                if (grafo.Vecinos(i).Count != 1)
                {
                    continue;
                }
                Vector3 hacia = grafo.Nodo(i) - grafo.Nodo(grafo.Vecinos(i)[0]);
                hacia.y = 0f;
                if (hacia.sqrMagnitude < 0.01f)
                {
                    continue;
                }
                grafo.AgregarTramo(i, grafo.Nodo(i) + hacia.normalized * LargoAfueras);
            }
        }

        // Un nodo lejos del jugador (para que no salgan carros encima de él).
        private int ElegirNodo(System.Random azar)
        {
            Vector3? jugador = PosicionDelJugador();
            int nodo = azar.Next(calles.Cantidad);
            for (int i = 0; i < IntentosDeNodo && jugador.HasValue; i++)
            {
                Vector3 d = calles.Nodo(nodo) - jugador.Value;
                d.y = 0f;
                if (d.magnitude >= DistanciaMinimaAlJugador)
                {
                    break;
                }
                nodo = azar.Next(calles.Cantidad);
            }
            return nodo;
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}
