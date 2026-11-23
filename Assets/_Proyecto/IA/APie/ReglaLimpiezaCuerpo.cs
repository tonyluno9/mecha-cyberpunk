using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1703 (T71): cuándo se limpia el cuerpo de un pandillero, policía a pie o civil. Pasado el mínimo (45 s en los pandilleros) se quita solo si está fuera de la vista de la
    /// cámara principal; si sigue a la vista espera a que salga, con un máximo de <see cref="SegundosMaximos"/>. Pura, salvo el ayudante de cámara.
    /// </summary>
    public static class ReglaLimpiezaCuerpo
    {
        /// <summary>Mínimo por omisión (s) antes de limpiar un cuerpo.</summary>
        public const float SegundosMinimos = 45f;
        /// <summary>Máximo (s): pasado esto se quita aunque esté a la vista.</summary>
        public const float SegundosMaximos = 120f;

        /// <summary>True si el cuerpo (caído hace <paramref name="segundos"/>) debe limpiarse ahora.</summary>
        public static bool Debe(float segundos, float minimo, bool fueraDeVista)
        {
            float min = Mathf.Max(0f, minimo);
            float max = Mathf.Max(SegundosMaximos, min);
            return segundos >= max || (segundos >= min && fueraDeVista);
        }

        /// <summary>True si <paramref name="punto"/> no se ve en la cámara principal (detrás, fuera del encuadre con margen) o si no hay cámara.</summary>
        public static bool FueraDeVista(Vector3 punto)
        {
            Camera camara = Camera.main;
            if (camara == null)
            {
                return true;
            }
            Vector3 v = camara.WorldToViewportPoint(punto);
            return v.z < 0f || v.x < -0.1f || v.x > 1.1f || v.y < -0.1f || v.y > 1.1f;
        }
    }
}
