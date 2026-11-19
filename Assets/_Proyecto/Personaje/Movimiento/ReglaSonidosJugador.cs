namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Reglas puras de los sonidos del jugador a pie (T-1473): IDs, caída mínima del aterrizaje y tiempo mínimo entre pasos.</summary>
    public static class ReglaSonidosJugador
    {
        public const string Aterrizaje = "JUG_ATERRIZAJE";
        public const string Rodada = "JUG_RODADA";
        public const string Trepar = "JUG_TREPAR";
        public const string GolpeCarne = "MEL_GOLPE_CARNE";
        public const string GolpeAire = "MEL_GOLPE_AIRE";

        /// <summary>Caída mínima (m) para que suene el aterrizaje.</summary>
        public const float CaidaMinimaAterrizaje = 1.2f;

        /// <summary>Segundos mínimos entre dos pasos.</summary>
        public const float SegundosMinimosEntrePasos = 0.25f;

        public static bool DebeSonarAterrizaje(float metrosCaida) => metrosCaida > CaidaMinimaAterrizaje;

        public static string IdGolpe(bool conecto) => conecto ? GolpeCarne : GolpeAire;
    }

    /// <summary>No deja pedir un paso antes de pasado el tiempo mínimo desde el último que sí sonó.</summary>
    public class LimitadorPasos
    {
        private readonly float minimo;
        private float ultimo = float.NegativeInfinity;

        public LimitadorPasos(float segundosMinimos = ReglaSonidosJugador.SegundosMinimosEntrePasos)
        {
            minimo = segundosMinimos;
        }

        /// <summary>Devuelve true si ya puede sonar un paso en el tiempo dado, y lo anota.</summary>
        public bool Permitir(float tiempo)
        {
            if (tiempo - ultimo < minimo) return false;
            ultimo = tiempo;
            return true;
        }
    }
}
