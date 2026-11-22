namespace MechaCyberpunk.IA.Datos
{
    /// <summary>T-1651 (T27): cuándo una facción pasa de advertir a pelear de verdad (golpes, armas).</summary>
    public enum CuandoPelea
    {
        /// <summary>SKULLS y SNAKES: advierten, empujan y tiran, y solo pelean si el jugador ataca (golpe, arma o disparo cerca).</summary>
        SoloSiElJugadorAtaca,
        /// <summary>Guardias de lugares: advierten si sacas el arma dentro; solo atacan si disparas (o pegas).</summary>
        SoloSiDisparas,
        /// <summary>Policía: solo por delitos del jugador (búsqueda), avisa con altavoz, persigue y dispara. Lo lleva Trafico (T8, T17, T25).</summary>
        SoloPorDelitos,
        /// <summary>Nova: sin advertencia, atacan directo. Solo con 5 estrellas o en la historia (T7, T17).</summary>
        SinAdvertencia,
        /// <summary>Civiles: nunca pelean, huyen (T11).</summary>
        Nunca
    }
}
