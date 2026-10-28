using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// Los peatones del barrio (T-331). Se cuelga de Barrio: reparte los peatones del <see cref="ConfigPeatones"/> en las rutas de las banquetas
    /// (una por manzana) y enciende solo los que toca según la franja del día (más de noche que de día). Lee el reloj y el jugador del registro de Core.
    /// </summary>
    public class GrupoPeatones : MonoBehaviour
    {
        private const float SegundosPreviosMaximos = 90f;
        private const int Semilla = 331;

        [SerializeField] private ConfigPeatones config;
        [SerializeField] private PeatonMundo prefabPeaton;
        [Tooltip("Una ruta por manzana, en el espacio local del barrio.")]
        [SerializeField] private List<RutaSerializada> rutas = new List<RutaSerializada>();

        private readonly List<PeatonMundo> peatones = new List<PeatonMundo>();
        private System.Random azar;

        [Serializable]
        public class RutaSerializada
        {
            public Vector3[] puntos;
        }

        public IReadOnlyList<PeatonMundo> Peatones => peatones;

        /// <summary>Cuántos peatones andan ahora (activos).</summary>
        public int Vivos
        {
            get
            {
                int cuenta = 0;
                foreach (PeatonMundo p in peatones)
                {
                    cuenta += p.gameObject.activeSelf ? 1 : 0;
                }
                return cuenta;
            }
        }

        /// <summary>Lo usan el creador de editor y las pruebas; debe llamarse antes de que arranque el grupo.</summary>
        public void Configurar(ConfigPeatones numeros, PeatonMundo prefab, IReadOnlyList<Vector3[]> rutasLocales)
        {
            config = numeros;
            prefabPeaton = prefab;
            rutas.Clear();
            foreach (Vector3[] puntos in rutasLocales)
            {
                rutas.Add(new RutaSerializada { puntos = puntos });
            }
        }

        private void Start()
        {
            if (config == null || prefabPeaton == null || rutas.Count == 0)
            {
                Debug.LogError("[Trafico] GrupoPeatones necesita config, prefab y al menos una ruta.", this);
                enabled = false;
                return;
            }
            azar = new System.Random(Semilla);
            for (int i = 0; i < config.Maximo; i++)
            {
                PeatonMundo peaton = Instantiate(prefabPeaton, transform);
                peaton.name = "Peaton_" + (i + 1);
                peaton.Preparar(rutas[i % rutas.Count].puntos, config, new System.Random(azar.Next()), PosicionDelJugador, (float)azar.NextDouble() * SegundosPreviosMaximos);
                peaton.gameObject.SetActive(false);
                peatones.Add(peaton);
            }
        }

        private void Update()
        {
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            int objetivo = config.Cantidad(reloj != null && reloj.EsDeNoche);
            for (int i = 0; i < peatones.Count; i++)
            {
                bool debeEstar = i < objetivo;
                if (peatones[i].gameObject.activeSelf != debeEstar)
                {
                    peatones[i].gameObject.SetActive(debeEstar);
                }
            }
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}
