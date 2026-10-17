using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Regla pura de la apariencia del jugador: los índices negativos pasan a 0.</summary>
    public static class ReglaApariencia
    {
        /// <summary>Devuelve la apariencia con cada índice negativo cambiado por 0.</summary>
        public static AparienciaJugador Normalizar(AparienciaJugador apariencia)
        {
            return new AparienciaJugador(
                apariencia.Cara < 0 ? 0 : apariencia.Cara,
                apariencia.Ropa < 0 ? 0 : apariencia.Ropa,
                apariencia.Implante < 0 ? 0 : apariencia.Implante);
        }
    }
}
