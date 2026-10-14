using MechaCyberpunk.Mecha.Llamado;

namespace MechaCyberpunk.Mecha.Calor
{
    /// <summary>Lógica pura de cuánto calor policial da el mecha (T-067).</summary>
    public static class ReglaCalorMecha
    {
        /// <summary>Calor que suma el resultado de un llamado: solo si el mecha llega.</summary>
        public static float CalorAlLlamar(ResultadoLlamado resultado, float calorAlLlamar)
        {
            return resultado == ResultadoLlamado.Llega ? calorAlLlamar : 0f;
        }

        /// <summary>
        /// Calor de un frame de <paramref name="dt"/> s. Solo suma si el mecha nació Estacionado (los de arena, en arranque
        /// Directo, nunca dan calor), el jugador lo maneja y está en la calle.
        /// </summary>
        public static float CalorPorFrame(bool arranqueEstacionado, bool jugadorAlMando, EstadoLlamado estado, float calorPorSegundo, float dt)
        {
            if (!arranqueEstacionado || !jugadorAlMando || estado != EstadoLlamado.EnCalle || dt <= 0f)
            {
                return 0f;
            }
            return calorPorSegundo * dt;
        }
    }
}
