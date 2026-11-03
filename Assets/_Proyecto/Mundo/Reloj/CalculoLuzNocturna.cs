namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// T-535: cuánto brilla una luz nocturna según la hora (0 apagada, 1 prendida). De noche, 1. Al amanecer no se apaga de golpe: sigue
    /// prendida y se apaga poco a poco en <see cref="HorasApagando"/> h; al anochecer empieza a prender <see cref="HorasPrendiendo"/> h antes.
    /// Lógica pura: recibe la hora y las horas de amanecer y anochecer.
    /// </summary>
    public static class CalculoLuzNocturna
    {
        public const float HorasApagando = 1f;
        public const float HorasPrendiendo = 0.5f;

        public static float Factor(float hora, float amanecer, float anochecer)
        {
            bool deNoche = hora < amanecer || hora >= anochecer;
            if (deNoche)
            {
                return 1f;
            }
            float desdeAmanecer = hora - amanecer;
            if (desdeAmanecer < HorasApagando)
            {
                return 1f - desdeAmanecer / HorasApagando;
            }
            float antesDeAnochecer = anochecer - hora;
            if (antesDeAnochecer < HorasPrendiendo)
            {
                return 1f - antesDeAnochecer / HorasPrendiendo;
            }
            return 0f;
        }
    }
}
