using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// Trigger que cubre el interior de un local. Al entrar el jugador marca <c>JugadorDentro</c> del lugar y publica
    /// <c>EventosLugares.PublicarEntro</c>; al salir, lo contrario. Una vez por entrada y salida aunque el jugador tenga
    /// varios colliders. Las luces del interior se apagan mientras no hay nadie dentro ni la puerta está abierta.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ZonaInterior : MonoBehaviour
    {
        [SerializeField] private LugarMundo lugar;
        [Tooltip("Puerta del local: con la hoja abierta las luces siguen prendidas para verse desde el umbral.")]
        [SerializeField] private PuertaLocal puerta;
        [SerializeField] private Light[] luces;

        private readonly HashSet<Collider> dentro = new HashSet<Collider>();
        private readonly List<Collider> sobrantes = new List<Collider>();
        private BoxCollider cajaZona;
        private bool jugadorDentro;
        private bool lucesPrendidas = true;

        public bool JugadorDentro => jugadorDentro;
        public bool LucesPrendidas => lucesPrendidas;
        public Light[] Luces => luces;

        private void Awake()
        {
            if (lugar == null)
            {
                Debug.LogError("[Mundo] ZonaInterior en '" + name + "' necesita su LugarMundo.", this);
                enabled = false;
                return;
            }
            cajaZona = GetComponent<BoxCollider>();
            cajaZona.isTrigger = true;
        }

        private void OnEnable()
        {
            if (puerta != null)
            {
                puerta.Cambio += AlCambiarPuerta;
            }
            Refrescar();
        }

        private void OnDisable()
        {
            if (puerta != null)
            {
                puerta.Cambio -= AlCambiarPuerta;
            }
            dentro.Clear();
            Actualizar();
        }

        private void OnTriggerEnter(Collider otro)
        {
            if (otro.GetComponentInParent<IJugador>() == null)
            {
                return;
            }
            dentro.Add(otro);
            Actualizar();
        }

        private void OnTriggerExit(Collider otro)
        {
            if (dentro.Remove(otro))
            {
                Actualizar();
            }
        }

        // Un collider destruido o apagado dentro de la zona no avisa su salida: se limpia aquí.
        private void Update()
        {
            if (dentro.Count == 0)
            {
                return;
            }
            sobrantes.Clear();
            foreach (Collider c in dentro)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || !cajaZona.bounds.Intersects(c.bounds))
                {
                    sobrantes.Add(c);
                }
            }
            if (sobrantes.Count == 0)
            {
                return;
            }
            foreach (Collider c in sobrantes)
            {
                dentro.Remove(c);
            }
            Actualizar();
        }


        private void AlCambiarPuerta(bool abierta)
        {
            Refrescar();
        }

        private void Actualizar()
        {
            bool hayJugador = dentro.Count > 0;
            if (hayJugador != jugadorDentro)
            {
                jugadorDentro = hayJugador;
                lugar.MarcarJugadorDentro(hayJugador);
                if (hayJugador)
                {
                    Debug.Log("[Mundo] Entró al " + lugar.Id);
                    EventosLugares.PublicarEntro(lugar.Id);
                }
                else
                {
                    Debug.Log("[Mundo] Salió del " + lugar.Id);
                    EventosLugares.PublicarSalio(lugar.Id);
                }
            }
            Refrescar();
        }

        private void Refrescar()
        {
            bool prender = jugadorDentro || (puerta != null && puerta.EstaAbierta);
            if (prender == lucesPrendidas && luces != null && EstadoCoincide(prender))
            {
                return;
            }
            lucesPrendidas = prender;
            if (luces == null)
            {
                return;
            }
            foreach (Light luz in luces)
            {
                if (luz != null)
                {
                    luz.enabled = prender;
                }
            }
        }

        private bool EstadoCoincide(bool prender)
        {
            foreach (Light luz in luces)
            {
                if (luz != null && luz.enabled != prender)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
