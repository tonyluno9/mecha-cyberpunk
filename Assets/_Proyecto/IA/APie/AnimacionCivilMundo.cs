using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1495: puente entre el peatón de la ciudad (Trafico, que no referencia a Personaje) y su <see cref="VisualPersonaje"/>. El peatón avisa con
    /// SendMessage (AlHerirCivil, AlMorirCivil, AlRevivirCivil) y aquí se pone la reacción de daño y la animación de muerte (MuerteAdelante, MuerteAtras,
    /// MuerteDesmayo del controlador base, o el estado Caer de los controladores propios). Va en la raíz del prefab Peaton.
    /// </summary>
    public class AnimacionCivilMundo : MonoBehaviour
    {
        private VisualPersonaje visual;

        public VisualPersonaje Visual => visual != null ? visual : (visual = GetComponentInChildren<VisualPersonaje>(false));

        /// <summary>Recibe un golpe sin morir: reacción de daño según la dirección (del atacante al personaje).</summary>
        public void AlHerirCivil(Vector3 direccion)
        {
            if (Visual != null)
            {
                Visual.Herir(Plano(direccion));
            }
        }

        /// <summary>Muere: elige la caída según la dirección del golpe y la reproduce.</summary>
        public void AlMorirCivil(Vector3 direccion)
        {
            if (Visual != null)
            {
                Visual.RegistrarGolpe(Plano(direccion));
                Visual.Caido = true;
            }
        }

        /// <summary>El peatón vuelve a la vida (el pool lo reutiliza).</summary>
        public void AlRevivirCivil()
        {
            if (Visual != null)
            {
                Visual.Caido = false;
            }
        }

        private Vector3 Plano(Vector3 direccion)
        {
            direccion.y = 0f;
            return direccion.sqrMagnitude < 1e-6f ? transform.forward : direccion.normalized;
        }
    }
}
