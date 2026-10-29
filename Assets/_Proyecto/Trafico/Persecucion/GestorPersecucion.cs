using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Crea y retira a los perseguidores según el calor (T-341). Escucha <c>ICalor.NivelCambio</c> (el calor aparece cuando el jugador ya está en escena, por eso
    /// se engancha al registro) y deja drones y patrullas con las cantidades de <see cref="CalculoPersecucion"/>. Al entrar el jugador a la casa borra el calor
    /// y por lo tanto retira a todos. Las calles salen de las manzanas (hijos "Manzana_N") del objeto padre, normalmente Barrio.
    /// </summary>
    public class GestorPersecucion : MonoBehaviour
    {
        private const string PrefijoManzana = "Manzana_";
        private const float LadoManzana = 80f;          // medida del barrio (CreadorBarrio, m)
        private const float AlturaCalles = 0.2f;        // sobre la banqueta (m)
        private const float GradosPorVuelta = 360f;

        [SerializeField] private ConfigPersecucion config;
        [SerializeField] private DronMundo prefabDron;
        [SerializeField] private PatrullaMundo prefabPatrulla;

        private readonly List<DronMundo> drones = new List<DronMundo>();
        private readonly List<PatrullaMundo> patrullas = new List<PatrullaMundo>();
        private CalculoPersecucion calculo;
        private GrafoCalles calles;
        private ICalor calor;
        private System.Random azar;

        public int Drones => drones.Count;
        public int Patrullas => patrullas.Count;

        /// <summary>Lo usan el creador de editor y las pruebas, antes de que el gestor se active.</summary>
        public void Configurar(ConfigPersecucion numeros, DronMundo dron, PatrullaMundo patrulla)
        {
            config = numeros;
            prefabDron = dron;
            prefabPatrulla = patrulla;
        }

        private void Awake()
        {
            if (config == null || prefabDron == null || prefabPatrulla == null)
            {
                Debug.LogError("[Trafico] GestorPersecucion necesita config y los prefabs de dron y patrulla.", this);
                enabled = false;
                return;
            }
            calculo = config.Crear();
            azar = new System.Random(341);
        }

        private void OnEnable()
        {
            Registro<ICalor>.Agregado += Enlazar;
            Registro<ICalor>.Quitado += Desenlazar;
            EventosLugares.JugadorEntro += AlEntrarALugar;
            if (Registro<ICalor>.Actual != null)
            {
                Enlazar(Registro<ICalor>.Actual);
            }
        }

        private void OnDisable()
        {
            Registro<ICalor>.Agregado -= Enlazar;
            Registro<ICalor>.Quitado -= Desenlazar;
            EventosLugares.JugadorEntro -= AlEntrarALugar;
            Desenlazar(calor);
            Reconciliar(0);
        }

        private void Enlazar(ICalor nuevo)
        {
            Desenlazar(calor);
            calor = nuevo;
            calor.NivelCambio += Reconciliar;
            Reconciliar(calor.Nivel);
        }

        private void Desenlazar(ICalor viejo)
        {
            if (viejo != null && ReferenceEquals(viejo, calor))
            {
                calor.NivelCambio -= Reconciliar;
                calor = null;
                Reconciliar(0);
            }
        }

        private void AlEntrarALugar(IdLugar lugar)
        {
            if (lugar == IdLugar.Casa)
            {
                Registro<ICalor>.Actual?.Borrar();
            }
        }

        // Deja exactamente las cantidades que pide el nivel: crea las que faltan y retira las que sobran.
        private void Reconciliar(int nivel)
        {
            int dronesPedidos = calculo == null ? 0 : calculo.Drones(nivel);
            int patrullasPedidas = calculo == null ? 0 : calculo.Patrullas(nivel);
            while (drones.Count < dronesPedidos)
            {
                CrearDron();
            }
            Retirar(drones, dronesPedidos);
            while (patrullas.Count < patrullasPedidas)
            {
                CrearPatrulla();
            }
            Retirar(patrullas, patrullasPedidas);
        }

        private static void Retirar<T>(List<T> lista, int cantidad) where T : Component
        {
            while (lista.Count > cantidad)
            {
                int ultimo = lista.Count - 1;
                Destroy(lista[ultimo].gameObject);
                lista.RemoveAt(ultimo);
            }
        }

        private void CrearDron()
        {
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            float angulo = (float)azar.NextDouble() * GradosPorVuelta * Mathf.Deg2Rad;
            Vector3 lejos = centro + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * config.DistanciaAparicionDron + Vector3.up * config.AlturaDron;
            DronMundo dron = Instantiate(prefabDron, lejos, Quaternion.identity, transform);
            dron.Preparar(calculo, config, PosicionDelJugador);
            drones.Add(dron);
        }

        private void CrearPatrulla()
        {
            if (calles == null)
            {
                calles = ArmarCalles();
            }
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            PatrullaMundo patrulla = Instantiate(prefabPatrulla, transform);
            patrulla.Preparar(calculo, config, calles, calles.MasLejano(centro), PosicionDelJugador);
            patrullas.Add(patrulla);
        }

        private GrafoCalles ArmarCalles()
        {
            List<Vector2> centros = new List<Vector2>();
            foreach (Transform hijo in transform.parent != null ? transform.parent : transform)
            {
                if (hijo.name.StartsWith(PrefijoManzana))
                {
                    centros.Add(new Vector2(hijo.position.x, hijo.position.z));
                }
            }
            return GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}
