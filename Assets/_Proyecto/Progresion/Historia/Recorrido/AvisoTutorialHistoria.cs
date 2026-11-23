using System.Collections.Generic;
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

        // T-78: una cola (antes era uno solo): "YA ERES LIBRE" y el aviso de la máquina pueden pedirse en el mismo cuadro.
        private readonly List<KeyValuePair<string, string>> pendientes = new List<KeyValuePair<string, string>>();

        /// <summary>True si hay un aviso esperando o ya abierto.</summary>
        public bool Visible => pendientes.Count > 0 || VentanaAviso.Abierta;
        public string TextoActual => VentanaAviso.Abierta ? VentanaAviso.TextoActual : pendientes.Count > 0 ? pendientes[0].Value : string.Empty;

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
            foreach (KeyValuePair<string, string> p in pendientes)
            {
                if (p.Key == id)
                {
                    return;
                }
            }
            pendientes.Add(new KeyValuePair<string, string>(id, aviso));
        }

        /// <summary>Cancela el aviso pendiente (uno ya abierto lo cierra el jugador con ENTENDIDO).</summary>
        public void Quitar()
        {
            pendientes.Clear();
        }

        private void Update()
        {
            if (pendientes.Count == 0)
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
            List<KeyValuePair<string, string>> lista = new List<KeyValuePair<string, string>>(pendientes);
            Quitar();
            foreach (KeyValuePair<string, string> p in lista)
            {
                if (AvisosUnicos.Marcar(p.Key))
                {
                    VentanaAviso.Mostrar(p.Value); // la ventana pone en cola las que lleguen con una abierta
                }
            }
        }
    }
}
