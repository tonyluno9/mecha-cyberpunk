using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Kit;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1662 (T44) y T-62: un aviso de tutorial de la historia. Sale en una ventana al centro con ENTENDIDO (<see cref="VentanaAviso"/>), que pausa el juego,
    /// y solo una vez por partida (<see cref="AvisosUnicos"/>, se guarda). Se pide con <see cref="Mostrar"/>; la ventana espera a que el jugador esté en juego
    /// (sin título, pantallas, cinemática ni pausa) y <see cref="Quitar"/> cancela uno que aún no salió. Lo crea el <see cref="DirectorRecorrido"/>.
    /// </summary>
    public class AvisoTutorialHistoria : MonoBehaviour
    {
        public static AvisoTutorialHistoria Actual { get; private set; }

        private string idPendiente;
        private string textoPendiente;

        /// <summary>True si hay un aviso esperando o ya abierto.</summary>
        public bool Visible => !string.IsNullOrEmpty(idPendiente) || VentanaAviso.Abierta;
        public string TextoActual => VentanaAviso.Abierta ? VentanaAviso.TextoActual : textoPendiente ?? string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Actual = null;

        private void Awake() => Actual = this;

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }

        /// <summary>Pide el aviso <paramref name="id"/>: sale en cuanto se pueda, una sola vez. Si ya salió en esta partida no hace nada.</summary>
        public void Mostrar(string id, string aviso)
        {
            if (AvisosUnicos.YaVisto(id))
            {
                return;
            }
            idPendiente = id;
            textoPendiente = aviso;
        }

        /// <summary>Cancela el aviso pendiente (uno ya abierto lo cierra el jugador con ENTENDIDO).</summary>
        public void Quitar()
        {
            idPendiente = null;
            textoPendiente = null;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(idPendiente))
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            bool enJuego = jugador != null && jugador.Raiz != null && !FlujoPartida.EnTitulo && !Pausa.Activa
                && PilaPantallas.Vacia && !CinematicaEnCurso.Activa;
            if (!enJuego)
            {
                return;
            }
            string id = idPendiente;
            string aviso = textoPendiente;
            Quitar();
            if (AvisosUnicos.Marcar(id))
            {
                VentanaAviso.Mostrar(aviso);
            }
        }
    }
}
