using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>T-1310: lógica pura del parkour y la caída del jugador (sin escena).</summary>
    public static class ReglaParkour
    {
        /// <summary>Muro bajo (entre la mínima y la máxima de barda) se salta; un borde más alto (hasta la máxima de agarre) se trepa.</summary>
        public static AccionParkour Clasificar(float altura, float alturaMinimaBarda, float alturaMaximaBarda, float alturaMaximaBorde)
        {
            if (altura >= alturaMinimaBarda && altura <= alturaMaximaBarda)
            {
                return AccionParkour.SaltarBarda;
            }
            if (altura > alturaMaximaBarda && altura <= alturaMaximaBorde)
            {
                return AccionParkour.TreparBorde;
            }
            return AccionParkour.Ninguna;
        }

        /// <summary>True si la caída (m) es de las que se amortiguan rodando.</summary>
        public static bool DebeRodar(float caida, float alturaRodar)
        {
            return caida >= alturaRodar;
        }

        /// <summary>
        /// Daño de una caída: 0 hasta el umbral y después <paramref name="danioPorMetro"/> por metro, con tope en la fracción de la vida máxima y sin dejar al jugador en 0
        /// (una caída nunca mata: deja al menos 1 punto de vida).
        /// </summary>
        public static float DanioCaida(float caida, float umbral, float danioPorMetro, float fraccionMaxima, float vidaActual, float vidaMaxima)
        {
            if (caida <= umbral)
            {
                return 0f;
            }
            float danio = (caida - umbral) * danioPorMetro;
            danio = Mathf.Min(danio, vidaMaxima * fraccionMaxima);
            return Mathf.Clamp(danio, 0f, Mathf.Max(0f, vidaActual - 1f));
        }
    }
}
