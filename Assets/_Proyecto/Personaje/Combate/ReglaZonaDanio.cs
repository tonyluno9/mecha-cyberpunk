using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Daño por zona de las balas del jugador (T-677). La cabeza hace <see cref="MultiplicadorCabeza"/> veces el daño del cuerpo.
    /// Cuenta como cabeza un impacto a menos de <see cref="RadioCabeza"/> del hueso de la cabeza (si el personaje tiene rig humanoide) o, si no,
    /// en el <see cref="FraccionCabeza"/> de arriba de su collider. Vale para enemigos, aliados, civiles y cualquier otro IRecibeDanio que no sea un objeto.
    /// </summary>
    public static class ReglaZonaDanio
    {
        /// <summary>Daño de la cabeza contra el del cuerpo (x).</summary>
        public const float MultiplicadorCabeza = 3f;
        /// <summary>Fracción de arriba del collider que cuenta como cabeza (0.15 = el 15 % superior).</summary>
        public const float FraccionCabeza = 0.15f;
        /// <summary>Distancia al hueso de la cabeza (m) que cuenta como cabeza.</summary>
        public const float RadioCabeza = 0.2f;

        /// <summary>True si un impacto a la altura <paramref name="y"/> cae en el 15 % superior de un cuerpo que va de <paramref name="yMin"/> a <paramref name="yMax"/>.</summary>
        public static bool EsCabezaPorAltura(float y, float yMin, float yMax)
        {
            float alto = yMax - yMin;
            if (alto <= 0.01f)
            {
                return false;
            }
            return y >= yMax - alto * FraccionCabeza;
        }

        /// <summary>True si el impacto está dentro del radio de la cabeza.</summary>
        public static bool EsCabezaPorHueso(Vector3 impacto, Vector3 cabeza, float escala = 1f)
        {
            float radio = RadioCabeza * Mathf.Max(0.1f, escala);
            return (impacto - cabeza).sqrMagnitude <= radio * radio;
        }

        /// <summary>Multiplicador de daño: <see cref="MultiplicadorCabeza"/> si es cabeza, 1 si es cuerpo.</summary>
        public static float Multiplicador(bool cabeza) => cabeza ? MultiplicadorCabeza : 1f;

        /// <summary>Decide si el impacto sobre este collider es a la cabeza: cerca del hueso de la cabeza (Animator humanoide) o en el 15 % de arriba del collider.</summary>
        public static bool EsCabeza(Collider collider, Vector3 impacto)
        {
            Bounds b = collider.bounds;
            if (EsCabezaPorAltura(impacto.y, b.min.y, b.max.y))
            {
                return true;
            }
            Animator animador = collider.GetComponentInParent<Animator>();
            if (animador == null)
            {
                animador = collider.GetComponentInChildren<Animator>();
            }
            if (animador != null && animador.isHuman)
            {
                Transform cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
                if (cabeza != null)
                {
                    // El hueso de la cabeza queda en el cuello; el centro de la cabeza está unos 10 cm más arriba.
                    Vector3 centro = cabeza.position + Vector3.up * 0.1f * Mathf.Abs(animador.transform.lossyScale.y);
                    return EsCabezaPorHueso(impacto, centro, Mathf.Abs(animador.transform.lossyScale.y));
                }
            }
            return false;
        }
    }
}
