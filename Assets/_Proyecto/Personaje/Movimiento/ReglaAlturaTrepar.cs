namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Por qué se permitió o se negó un intento de trepar o colgarse.</summary>
    public enum MotivoTrepar
    {
        Permitido,
        DemasiadoAlto
    }

    /// <summary>
    /// T-1663: regla pura para que el jugador a pie no suba a los techos. Un agarre, mantle, colgado o salto de barda solo se permite
    /// si la altura final de los pies sobre el piso de la calle no pasa de un máximo (por defecto ~2.5 m); un techo de edificio queda mucho más arriba.
    /// </summary>
    public static class ReglaAlturaTrepar
    {
        /// <summary>Evalúa la altura final (m sobre el piso de la calle) contra el máximo (m).</summary>
        public static MotivoTrepar Evaluar(float alturaFinalSobreCalle, float alturaMaximaSobreCalle)
        {
            return alturaFinalSobreCalle <= alturaMaximaSobreCalle ? MotivoTrepar.Permitido : MotivoTrepar.DemasiadoAlto;
        }
    }
}
