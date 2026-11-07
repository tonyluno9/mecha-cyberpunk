using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-714: los SKULLS de refuerzo llegan en carro. Vigila el respeto de las zonas de los SKULLS (<see cref="ITerritorio"/>): cada SKULL vencido suma 10; con los vencidos
    /// que pide <see cref="ReglaRefuerzos"/> manda un carro del pool del tráfico (<see cref="TraficoCalle.EnviarRefuerzo"/>) que sale fuera de la vista, llega manejando por las
    /// calles con un tiempo real (como las patrullas de T-708) y se estaciona cerca del jugador; ahí se bajan sus pasajeros (el prefab SkullRefuerzo, de Resources), por las dos
    /// puertas, y pelean. Solo a un jugador que va a pie y está en una zona que sigue siendo de los SKULLS. Lo agrega <see cref="GestorPersecucion"/> en su Awake.
    /// </summary>
    public class GestorRefuerzos : MonoBehaviour
    {
        public const string NombrePasajero = "SkullRefuerzo";
        private const int RespetoPorVencido = 10;
        private const float SegundosEntreRevisiones = 0.25f;
        private const float SeparacionPuerta = 1.9f;
        private const float SeparacionEntrePasajeros = 1.0f;
        private const float AlturaSobreSuelo = 0.05f;
        private const float DistanciaMinimaSalida = 60f;
        private const float VelocidadPorOmision = 14f;
        private const float SegundosObjetivoPorOmision = 12f;
        private const float SegundosMinimosPorOmision = 7f;

        private ConfigPersecucion config;
        private GameObject prefabPasajero;
        private ReglaRefuerzos regla = new ReglaRefuerzos();
        private readonly Dictionary<string, int> respetoAntes = new Dictionary<string, int>();
        private readonly List<CarroTrafico> enCamino = new List<CarroTrafico>();
        private readonly List<GameObject> pasajeros = new List<GameObject>();
        private float proximaRevision;

        /// <summary>Pasajeros por carro (los que se bajan).</summary>
        public int PasajerosPorCarro { get; set; } = 2;

        /// <summary>Carros de refuerzo mandados desde que empezó.</summary>
        public int CarrosEnviados { get; private set; }

        /// <summary>Los SKULLS que se bajaron de los carros.</summary>
        public IReadOnlyList<GameObject> Pasajeros => pasajeros;

        /// <summary>Los carros que todavía van en camino (no se han estacionado).</summary>
        public int EnCamino
        {
            get
            {
                int cuenta = 0;
                foreach (CarroTrafico c in enCamino)
                {
                    cuenta += c != null && !c.RefuerzoLlego ? 1 : 0;
                }
                return cuenta;
            }
        }

        /// <summary>Vencidos que lleva la cuenta hacia el próximo refuerzo.</summary>
        public int Muertes => regla.Muertes;

        /// <summary>Para las pruebas: la regla (por ejemplo con enfriamiento corto) y el prefab de los pasajeros.</summary>
        public void Configurar(ConfigPersecucion numeros, ReglaRefuerzos reglas = null, GameObject pasajero = null)
        {
            config = numeros;
            if (reglas != null)
            {
                regla = reglas;
            }
            if (pasajero != null)
            {
                prefabPasajero = pasajero;
            }
        }

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + SegundosEntreRevisiones;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            if (territorio == null || jugador == null || jugador.Raiz == null)
            {
                return;
            }
            ContarVencidos(territorio);
            if (jugador.Modo != ModoJugador.APie || !regla.DebeLlamar(Time.time, EnCamino))
            {
                return;
            }
            string zona = territorio.ZonaEn(jugador.Raiz.position);
            if (string.IsNullOrEmpty(zona) || territorio.Dueno(zona) != IdPandilla.Calaveras)
            {
                return;
            }
            Llamar(jugador.Raiz.position);
        }

        // El respeto de una zona de los SKULLS que sube es un SKULL vencido en ella.
        private void ContarVencidos(ITerritorio territorio)
        {
            foreach (string zona in territorio.Zonas)
            {
                int respeto = territorio.Respeto(zona);
                if (respetoAntes.TryGetValue(zona, out int antes) && territorio.Dueno(zona) == IdPandilla.Calaveras && respeto > antes)
                {
                    regla.RegistrarMuertes((respeto - antes) / RespetoPorVencido);
                }
                respetoAntes[zona] = respeto;
            }
        }

        /// <summary>Manda el carro ya (sin esperar a los vencidos). Devuelve true si salió uno.</summary>
        public bool Llamar(Vector3 destino)
        {
            if (prefabPasajero == null)
            {
                prefabPasajero = Resources.Load<GameObject>(NombrePasajero);
            }
            TraficoCalle trafico = FindFirstObjectByType<TraficoCalle>();
            if (prefabPasajero == null || trafico == null)
            {
                return false;
            }
            float velocidad = config != null ? config.VelocidadPatrulla : VelocidadPorOmision;
            float objetivo = config != null ? config.SegundosLlegadaPatrulla : SegundosObjetivoPorOmision;
            float minimos = config != null ? config.SegundosMinimosLlegadaPatrulla : SegundosMinimosPorOmision;
            float distancia = config != null ? config.DistanciaMinimaSalida : DistanciaMinimaSalida;
            CarroTrafico carro = trafico.EnviarRefuerzo(destino, velocidad, objetivo, minimos, distancia);
            if (carro == null)
            {
                return false;
            }
            regla.Llamado(Time.time);
            CarrosEnviados++;
            enCamino.Add(carro);
            carro.AlLlegarRefuerzo += AlLlegar;
            return true;
        }

        private void AlLlegar(CarroTrafico carro)
        {
            carro.AlLlegarRefuerzo -= AlLlegar;
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3? haciaJugador = jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
            for (int i = 0; i < PasajerosPorCarro; i++)
            {
                float lado = i % 2 == 0 ? 1f : -1f;
                Vector3 punto = carro.transform.position + carro.transform.right * (lado * SeparacionPuerta) - carro.transform.forward * (SeparacionEntrePasajeros * (i / 2));
                punto.y = Suelo(punto, carro.transform.position.y) + AlturaSobreSuelo;
                Vector3 mirada = (haciaJugador ?? carro.transform.position + carro.transform.forward) - punto;
                mirada.y = 0f;
                Quaternion giro = mirada.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mirada) : carro.transform.rotation;
                GameObject skull = Instantiate(prefabPasajero, punto, giro);
                skull.name = NombrePasajero + "_" + (pasajeros.Count + 1);
                pasajeros.Add(skull);
            }
        }

        private static float Suelo(Vector3 punto, float respaldo)
        {
            if (Physics.Raycast(punto + Vector3.up * 2f, Vector3.down, out RaycastHit golpe, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point.y;
            }
            return respaldo - 0.3f;
        }

        private void OnDestroy()
        {
            foreach (CarroTrafico c in enCamino)
            {
                if (c != null)
                {
                    c.AlLlegarRefuerzo -= AlLlegar;
                }
            }
        }
    }
}
