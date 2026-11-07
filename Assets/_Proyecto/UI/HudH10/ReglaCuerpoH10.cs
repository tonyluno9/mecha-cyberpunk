using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Partes del muñequito del HUD (T-700).</summary>
    public enum ZonaCuerpoH10
    {
        Cabeza,
        Torso,
        Brazos,
        Piernas
    }

    /// <summary>Lógica pura del muñequito: en qué parte pegó un golpe y cuánto sigue roja.</summary>
    public static class ReglaCuerpoH10
    {
        public const float AlturaJugador = 1.8f;
        public const float SegundosRojo = 4f;
        private const float DesdeCabeza = 0.82f;
        private const float HastaPiernas = 0.5f;
        private const float LateralBrazos = 0.28f;

        /// <summary>
        /// La parte golpeada según la altura del impacto sobre los pies y su distancia al eje del cuerpo (m): cabeza arriba,
        /// piernas abajo, brazos si pegó a los lados y torso en el resto.
        /// </summary>
        public static ZonaCuerpoH10 Zona(float alturaSobrePies, float lateral)
        {
            float relativa = alturaSobrePies / AlturaJugador;
            if (relativa >= DesdeCabeza)
            {
                return ZonaCuerpoH10.Cabeza;
            }
            if (relativa < HastaPiernas)
            {
                return ZonaCuerpoH10.Piernas;
            }
            return Mathf.Abs(lateral) > LateralBrazos ? ZonaCuerpoH10.Brazos : ZonaCuerpoH10.Torso;
        }

        /// <summary>Qué tan roja está la parte golpeada: 1 al golpe, baja a 0 en <see cref="SegundosRojo"/>.</summary>
        public static float Intensidad(float segundosDesdeGolpe)
        {
            return Mathf.Clamp01(1f - segundosDesdeGolpe / SegundosRojo);
        }
    }
}
