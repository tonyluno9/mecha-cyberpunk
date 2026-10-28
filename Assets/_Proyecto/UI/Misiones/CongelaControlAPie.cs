using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;

namespace MechaCyberpunk.UI.Misiones
{
    /// <summary>Congela el control del jugador a pie mientras una pantalla está abierta y lo devuelve al cerrarla (T-186).</summary>
    public sealed class CongelaControlAPie
    {
        private ControladorAPie congelado;

        /// <summary>El controlador del jugador a pie, o null si no hay.</summary>
        public static ControladorAPie Controlador()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            return jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
        }

        /// <summary>Congela si el control estaba activo (si otra pantalla ya lo congeló, no hace nada).</summary>
        public void Congelar()
        {
            ControladorAPie controlador = Controlador();
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                congelado = controlador;
            }
        }

        public void Liberar()
        {
            if (congelado != null)
            {
                congelado.ActivarControl(true);
                congelado = null;
            }
        }
    }
}
