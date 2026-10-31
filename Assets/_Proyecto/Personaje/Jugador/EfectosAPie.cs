using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>Cálculo puro de lo que cambian los implantes a pie (T-192): correr, salto y golpe. Caminar y la pistola no cambian.</summary>
    public static class EfectosAPie
    {
        public static float VelocidadCorrer(DatosPersonaje datos, EfectosImplantes efectos) => datos.VelocidadCorrer * efectos.multiplicadorCorrer;

        public static float AlturaSalto(DatosPersonaje datos, EfectosImplantes efectos) => datos.AlturaSalto * efectos.multiplicadorSalto;

        /// <summary>Multiplica el radio y el alcance del apuntado asistido (OJOS, T-430): 1 sin implante.</summary>
        public static float FactorAlcanceApuntado(EfectosImplantes efectos) => 1f + Math.Max(0f, efectos.bonusAlcanceApuntado);

        /// <summary>Multiplica la patada de cámara al disparar (BRAZOS, T-430): 1 sin implante.</summary>
        public static float FactorRetroceso(EfectosImplantes efectos) => 1f - Math.Clamp(efectos.reduccionRetroceso, 0f, 0.9f);

        /// <summary>Segundos de enfriamiento de la rodada (PIERNAS, T-430).</summary>
        public static float EnfriamientoRodada(float basico, EfectosImplantes efectos) => basico * (1f - Math.Clamp(efectos.reduccionEnfriamientoRodada, 0f, 0.9f));

        /// <summary>Multiplica el tiempo de recarga (SINAPSIS, T-430): 1 sin implante.</summary>
        public static float FactorRecarga(EfectosImplantes efectos) => 1f - Math.Clamp(efectos.reduccionRecarga, 0f, 0.9f);

        public static float DanioGolpe(DatosPersonaje datos, EfectosImplantes efectos) => datos.DanioGolpe * efectos.multiplicadorGolpeAPie;
    }
}
