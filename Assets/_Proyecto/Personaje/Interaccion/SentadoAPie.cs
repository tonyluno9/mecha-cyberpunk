using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>
    /// T-1628: el jugador a pie sentado en un <see cref="AsientoInteractuable"/>. Se agrega solo al jugador la primera vez que se sienta. Mientras está sentado el movimiento
    /// queda suspendido (la cámara sigue), el CharacterController apagado y PJ01_Sentado en bucle (VisualPersonaje.Sentado). Se levanta con E o al mover WASD
    /// (después de un tiempo mínimo), y también si queda KO o pierde el control.
    /// </summary>
    public class SentadoAPie : MonoBehaviour
    {
        private const float SegundosMinimos = 0.8f;
        private const float UmbralMovimiento = 0.5f;

        private ControladorAPie controlador;
        private CharacterController cc;
        private VisualPersonaje visual;
        private InteraccionAPie interaccion;
        private VidaAPie vida;
        private float desde;

        public AsientoInteractuable Asiento { get; private set; }
        public bool EstaSentado => Asiento != null;

        /// <summary>Sienta al jugador en el asiento. False si no hay jugador a pie, ya está sentado o el visual no tiene el estado.</summary>
        public static bool Sentar(AsientoInteractuable asiento)
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || asiento == null || asiento.EstaOcupado || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            SentadoAPie s = jugador.GetComponent<SentadoAPie>();
            if (s == null)
            {
                s = jugador.gameObject.AddComponent<SentadoAPie>();
            }
            return s.Iniciar(asiento);
        }

        private bool Iniciar(AsientoInteractuable asiento)
        {
            if (EstaSentado)
            {
                return false;
            }
            controlador = GetComponent<ControladorAPie>();
            cc = GetComponent<CharacterController>();
            interaccion = GetComponent<InteraccionAPie>();
            vida = GetComponentInChildren<VidaAPie>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (controlador == null || cc == null || visual == null || !visual.PuedeSentarse || !controlador.ControlActivo || controlador.MovimientoSuspendido)
            {
                Debug.LogWarning("[Personaje] No puede sentarse ahora (sin visual con estado Sentado, o con el control ocupado).", this);
                return false;
            }
            Asiento = asiento;
            asiento.Ocupar(gameObject);
            controlador.MovimientoSuspendido = true;
            cc.enabled = false;
            visual.ForzarSuelo = true;
            transform.SetPositionAndRotation(asiento.PosicionSentado, Quaternion.LookRotation(asiento.Frente, Vector3.up));
            visual.Sentado = true;
            desde = Time.time;
            Debug.Log("[Personaje] Jugador se sienta en " + asiento.name, this);
            return true;
        }

        private void Update()
        {
            if (!EstaSentado)
            {
                return;
            }
            bool pierdeControl = !controlador.ControlActivo || (vida != null && vida.EstaKO);
            bool quiereLevantarse = Time.time - desde >= SegundosMinimos
                && ((interaccion != null && interaccion.PulsoInteractuar) || controlador.LeerMovimiento().sqrMagnitude > UmbralMovimiento * UmbralMovimiento);
            if (pierdeControl || quiereLevantarse)
            {
                Levantar();
            }
        }

        private void OnDisable()
        {
            if (EstaSentado)
            {
                Levantar();
            }
        }

        /// <summary>Se levanta y deja al jugador de pie delante del asiento.</summary>
        public void Levantar()
        {
            if (!EstaSentado)
            {
                return;
            }
            AsientoInteractuable asiento = Asiento;
            Asiento = null;
            visual.Sentado = false;
            visual.ForzarSuelo = false;
            transform.SetPositionAndRotation(asiento.PosicionSalida, Quaternion.LookRotation(asiento.Frente, Vector3.up));
            cc.enabled = true;
            controlador.ReanudarMovimiento(Vector3.zero);
            asiento.Liberar();
            Debug.Log("[Personaje] Jugador se levanta de " + asiento.name, this);
        }
    }
}
