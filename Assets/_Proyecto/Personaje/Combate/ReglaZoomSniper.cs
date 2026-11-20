using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1527: lógica pura del zoom ajustable del francotirador (la rueda del ratón mientras se apunta).</summary>
    public static class ReglaZoomSniper
    {
        /// <summary>True si la rueda ajusta el zoom y no cambia de arma: con el francotirador en la mano y apuntando.</summary>
        public static bool RuedaAjustaZoom(ArmaAPie arma, bool apuntando) => arma == ArmaAPie.Sniper && apuntando;

        /// <summary>
        /// El FOV después de mover la rueda <paramref name="rueda"/> (positivo acerca: FOV menor). Cada muesca multiplica o divide el FOV por
        /// <paramref name="factorPaso"/> y el resultado queda entre <paramref name="minimo"/> y <paramref name="maximo"/>.
        /// </summary>
        public static float AjustarFov(float fov, float rueda, float factorPaso, float minimo, float maximo)
        {
            if (rueda == 0f)
            {
                return Mathf.Clamp(fov, minimo, maximo);
            }
            float factor = Mathf.Max(1.01f, factorPaso);
            float nuevo = rueda > 0f ? fov / factor : fov * factor;
            return Mathf.Clamp(nuevo, minimo, maximo);
        }
    }
}
