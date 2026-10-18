using System;

namespace MechaCyberpunk.Combate.Vida
{
    /// <summary>Cuenta pura de la vida al cambiar los datos del mecha: se conservan los puntos de daño (D-tony-119).</summary>
    public static class ReglaCambioDatos
    {
        /// <summary>
        /// La vida actual tras pasar de <paramref name="maximaVieja"/> a <paramref name="maximaNueva"/>:
        /// <c>maximaNueva - (maximaVieja - actual)</c> limitada a [1, maximaNueva]. En KO devuelve 0 (sigue en KO).
        /// </summary>
        public static float NuevaVida(float actual, float maximaVieja, float maximaNueva, bool enKO)
        {
            if (enKO)
            {
                return 0f;
            }
            float danio = Math.Max(0f, maximaVieja - actual);
            float nueva = maximaNueva - danio;
            return Math.Min(Math.Max(nueva, 1f), maximaNueva);
        }
    }
}
