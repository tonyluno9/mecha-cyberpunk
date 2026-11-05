using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-623: gestos del jugador al interactuar, con los clips de Mixamo. Abrir una puerta (Inter_AbrirPuerta), recoger un arma del piso (Inter_Recoger),
    /// usar cualquier otro interactuable como mostradores o tableros (Inter_Boton) y sacar el teléfono (Inter_Telefono, lo llama la pantalla del teléfono).
    /// Lo agrega JugadorAPie solo; escucha a <see cref="InteraccionAPie"/>.
    /// </summary>
    public class GestosJugador : MonoBehaviour
    {
        private InteraccionAPie interaccion;
        private VisualPersonaje visual;

        /// <summary>El último gesto que se lanzó (para las pruebas y el HUD).</summary>
        public GestoVisual UltimoGesto { get; private set; }

        private void Awake()
        {
            interaccion = GetComponent<InteraccionAPie>();
        }

        private void OnEnable()
        {
            if (interaccion != null)
            {
                interaccion.PuertaIntentada += AlIntentarPuerta;
                interaccion.InteractuableUsado += AlUsar;
            }
        }

        private void OnDisable()
        {
            if (interaccion != null)
            {
                interaccion.PuertaIntentada -= AlIntentarPuerta;
                interaccion.InteractuableUsado -= AlUsar;
            }
        }

        private void AlIntentarPuerta(IPuerta puerta, ResultadoPuerta resultado)
        {
            if (resultado == ResultadoPuerta.Abierta)
            {
                Gestear(GestoVisual.AbrirPuerta);
            }
        }

        private void AlUsar(IInteractuable usado)
        {
            Gestear(usado is ArmaEnPiso ? GestoVisual.Recoger : GestoVisual.Boton);
        }

        /// <summary>Saca el teléfono (la pantalla del teléfono lo llama al abrirse).</summary>
        public void SacarTelefono() => Gestear(GestoVisual.Telefono);

        /// <summary>Lanza un gesto en el modelo del jugador, si va a pie y el modelo está activo.</summary>
        public void Gestear(GestoVisual gesto)
        {
            visual = GetComponentInChildren<VisualPersonaje>(false);
            if (visual == null)
            {
                return;
            }
            UltimoGesto = gesto;
            visual.Gestear(gesto);
        }
    }
}
