using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Core.Mundo;
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
            // T-1516: algunos peatones se quedan platicando en corros: los de un corro comparten ruta y punto de partida y se reparten alrededor del centro.
            var corros = ReglaPlatica.Corros(config.Maximo, config.UnoDeCadaPlatica);
            int rutaCorro = 0;
            float previosCorro = 0f;
            for (int i = 0; i < config.Maximo; i++)
            {
                int miembro = -1;
                int tamano = 0;
                foreach ((int primero, int tam) in corros)
                {
                    if (i >= primero && i < primero + tam)
                    {
                        miembro = i - primero;
                        tamano = tam;
                    }
                }
                int ruta = i % rutas.Count;
                int semilla = azar.Next();
                float previos = (float)azar.NextDouble() * SegundosPreviosMaximos;
                if (miembro == 0)
                {
                    rutaCorro = ruta;
                    previosCorro = previos;
                }
                else if (miembro > 0)
                {
                    ruta = rutaCorro;
                    previos = previosCorro;
                }
                PeatonMundo peaton = Instantiate(prefabPeaton, transform);
                peaton.name = "Peaton_" + (i + 1);
                peaton.Preparar(rutas[ruta].puntos, config, new System.Random(semilla), PosicionDelJugador, previos);
                if (miembro >= 0)
                {
                    peaton.Platicar(miembro, tamano);
                }
                peaton.gameObject.SetActive(false);
                peatones.Add(peaton);
            }
            // T-1622: cada corro sabe quiénes son los suyos (si uno ve una muerte, huyen todos).
            foreach ((int primero, int tam) in corros)
            {
                var miembros = new PeatonMundo[tam];
                for (int k = 0; k < tam; k++)
                {
                    miembros[k] = peatones[primero + k];
                }
                foreach (PeatonMundo miembro in miembros)
                {
                    miembro.AsignarCorro(miembros);
                }
            }
            CrearPeatonesDeZonas();
        }

        // T-1350: un peatón por ruta de banqueta de las calles de zona (sin autopistas), hasta MaximoPeatonesDeZonas, a la misma proporción día/noche.
        private const int MaximoPeatonesDeZonas = 16;
        private const float AlturaBanquetaZona = 0.2f;     // m
        private const float MitadBanquetaZona = 1.5f;      // m, del borde de la calle al centro de la banqueta
        private readonly List<PeatonMundo> peatonesDeZonas = new List<PeatonMundo>();

        /// <summary>T-1350: peatones de las banquetas de zonas nuevas (aparte de <see cref="Peatones"/>).</summary>
        public IReadOnlyList<PeatonMundo> PeatonesDeZonas => peatonesDeZonas;

        private void CrearPeatonesDeZonas()
        {
            List<Vector3[]> rutasZona = new List<Vector3[]>();
            foreach (CalleZona calle in CalleZona.Activas)
            {
                if (calle == null || !calle.Banquetas || calle.Autopista || calle.Cantidad < 2)
                {
                    continue;
                }
                foreach (Vector3[] ruta in RutasBanqueta.DeCalleZona(calle.Puntos(), calle.Ancho, MitadBanquetaZona, AlturaBanquetaZona))
                {
                    for (int i = 0; i < ruta.Length; i++)
                    {
                        ruta[i] = transform.InverseTransformPoint(ruta[i]); // los peatones andan en espacio local de este grupo
                    }
                    rutasZona.Add(ruta);
                }
            }
            int cuantos = Mathf.Min(rutasZona.Count, MaximoPeatonesDeZonas);
            for (int i = 0; i < cuantos; i++)
            {
                PeatonMundo peaton = Instantiate(prefabPeaton, transform);
                peaton.name = "PeatonZona_" + (i + 1);
                peaton.Preparar(rutasZona[i * rutasZona.Count / cuantos], config, new System.Random(azar.Next()), PosicionDelJugador, (float)azar.NextDouble() * SegundosPreviosMaximos);
                peaton.gameObject.SetActive(false);
                peatonesDeZonas.Add(peaton);
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
            int objetivoZonas = Mathf.CeilToInt(peatonesDeZonas.Count * (float)objetivo / Mathf.Max(1, config.Maximo));
            for (int i = 0; i < peatonesDeZonas.Count; i++)
            {
                bool debeEstar = i < objetivoZonas;
                if (peatonesDeZonas[i].gameObject.activeSelf != debeEstar)
                {
                    peatonesDeZonas[i].gameObject.SetActive(debeEstar);
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
