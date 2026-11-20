namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1490: daño de los puños contra enemigos. Lógica pura; con otro blanco (muñeco, civil) el daño no cambia.</summary>
    public static class ReglaDanioPunos
    {
        public static float Danio(float danio, float multiplicadorContraEnemigos, bool objetivoEsEnemigo)
        {
            if (!objetivoEsEnemigo)
            {
                return danio;
            }
            return danio * (multiplicadorContraEnemigos < 0f ? 0f : multiplicadorContraEnemigos);
        }

        /// <summary>Golpes necesarios para dejar en KO a un enemigo con esa vida.</summary>
        public static int GolpesParaDerribar(float vida, float danioPorGolpe)
        {
            return danioPorGolpe <= 0f ? int.MaxValue : (int)System.Math.Ceiling(vida / danioPorGolpe - 1e-4f);
        }
    }
}
