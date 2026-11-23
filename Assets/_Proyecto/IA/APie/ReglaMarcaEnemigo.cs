namespace MechaCyberpunk.IA.APie
{
    /// <summary>T-1706 (T67): cuándo un enemigo sale en el minimapa y en el mapa grande (lógica pura). Los de misión siempre; los demás si están en pelea y a menos de <see cref="Radio"/> m.</summary>
    public static class ReglaMarcaEnemigo
    {
        /// <summary>Radio (m) alrededor del jugador dentro del cual salen los enemigos que no son de misión.</summary>
        public const float Radio = 60f;

        public static bool Sale(bool deMision, bool enPelea, float distanciaAlJugador) => deMision || (enPelea && distanciaAlJugador <= Radio);
    }
}
