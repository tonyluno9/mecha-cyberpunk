using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-734: recibe los Animation Events de Pistola_Sacar (a los 0.30 s) y Pistola_Guardar (a los 0.53 s) y se los pasa al <see cref="VisualPersonaje"/>,
    /// que decide si el arma está en la mano. Va en el mismo objeto que el Animator (los eventos llegan ahí); VisualPersonaje lo agrega solo si falta,
    /// así aliados, enemigos y NPC que comparten el ControladorBase no dejan avisos de "evento sin receptor".
    /// </summary>
    public class ReceptorEventosArma : MonoBehaviour
    {
        public const string FuncionSacar = "AlSacarPistola";
        public const string FuncionGuardar = "AlGuardarPistola";

        private VisualPersonaje visual;

        private VisualPersonaje Visual()
        {
            if (visual == null)
            {
                visual = GetComponentInParent<VisualPersonaje>();
            }
            return visual;
        }

        /// <summary>Evento de Pistola_Sacar (0.30 s): la mano está en la cadera y la pistola aparece. Si ya se soltó el apuntado, no aparece.</summary>
        public void AlSacarPistola()
        {
            VisualPersonaje v = Visual();
            if (v != null && v.ArmaLevantada)
            {
                v.PonerArmaEnMano(true);
            }
        }

        /// <summary>Evento de Pistola_Guardar (0.53 s): la mano llega a la cadera y la pistola desaparece. Si se volvió a apuntar, se queda.</summary>
        public void AlGuardarPistola()
        {
            VisualPersonaje v = Visual();
            if (v != null && !v.ArmaLevantada)
            {
                v.PonerArmaEnMano(false);
            }
        }
    }
}
