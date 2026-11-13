using System;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>
    /// T-977: cuándo rechinan las llantas del carro que maneja el jugador (VEH_FRENO_DERRAPE): al frenar fuerte yendo rápido, con el freno de mano
    /// o al dar vuelta tan rápido que el carro se va de lado. Tiene histéresis (arranca con más que lo que necesita para seguir) para que no
    /// prenda y apague a cada cuadro, y un descanso entre derrapes para que no se repita en exceso. Pura: el tiempo entra como parámetro.
    /// </summary>
    public static class ReglaDerrapeSonido
    {
        /// <summary>Ruta de lado (m/s) a la que empieza a rechinar al dar vuelta.</summary>
        public const float LateralParaEmpezar = 3.5f;

        /// <summary>Por debajo de esta ruta de lado (m/s) deja de rechinar.</summary>
        public const float LateralParaSeguir = 2f;

        /// <summary>Rapidez hacia adelante (m/s) a partir de la que frenar fuerte rechina.</summary>
        public const float RapidezFrenazo = 10f;

        /// <summary>Rapidez (m/s) mínima para cualquier derrape; más lento nunca rechina.</summary>
        public const float RapidezMinima = 5f;

        /// <summary>Freno (0 a 1) que cuenta como frenazo.</summary>
        public const float FrenoFuerte = 0.7f;

        /// <summary>Descanso (s) entre el fin de un derrape y el siguiente.</summary>
        public const float SegundosDescanso = 1.2f;

        /// <summary>Lo menos que dura un derrape una vez que arrancó (s), para que no sea un chasquido.</summary>
        public const float SegundosMinimos = 0.35f;

        /// <summary>
        /// Si las llantas rechinan ahora. <paramref name="sonando"/>: si ya rechinaban; <paramref name="desdeCambio"/>: segundos desde que empezaron
        /// (si suenan) o desde que pararon (si no).
        /// </summary>
        public static bool Rechina(float velocidadAdelante, float velocidadLateral, float frenar, float frenoMano, bool sonando, float desdeCambio)
        {
            float lateral = Math.Abs(velocidadLateral);
            float rapidez = (float)Math.Sqrt(velocidadAdelante * velocidadAdelante + velocidadLateral * velocidadLateral);
            if (sonando)
            {
                if (desdeCambio < SegundosMinimos) return true;
                if (rapidez < RapidezMinima * 0.6f) return false;
                return lateral >= LateralParaSeguir || (frenar >= FrenoFuerte * 0.7f && velocidadAdelante > RapidezMinima)
                    || (frenoMano > 0.3f && rapidez > RapidezMinima);
            }
            if (desdeCambio < SegundosDescanso || rapidez < RapidezMinima) return false;
            return lateral >= LateralParaEmpezar || (frenar >= FrenoFuerte && velocidadAdelante >= RapidezFrenazo) || frenoMano > 0.5f;
        }
    }
}
