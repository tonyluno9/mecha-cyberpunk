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

        [SerializeField] private ConfigTraficoCalle config;
        [SerializeField] private CarroTrafico prefabCarro;
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
                    cuenta += c != null && c.gameObject.activeSelf ? 1 : 0;
                }
                return cuenta;
            }
        }

        /// <summary>Lo usan el creador de editor y las pruebas; debe llamarse antes de que arranque el tráfico.</summary>
        public void Configurar(ConfigTraficoCalle numeros, CarroTrafico prefab, Transform padreDeManzanas = null)
        {
            config = numeros;
            prefabCarro = prefab;
            barrio = padreDeManzanas;
        }

        private void Start()
        {
            if (config == null || prefabCarro == null)
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
            System.Random azar = new System.Random(Semilla);
            for (int i = 0; i < config.Maximo; i++)
            {
                CarroTrafico carro = Instantiate(prefabCarro, transform);
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
            for (int i = 0; i < carros.Count; i++)
            {
                bool debeEstar = i < objetivo;
                if (carros[i].gameObject.activeSelf != debeEstar)
                {
                    carros[i].gameObject.SetActive(debeEstar);
                }
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
