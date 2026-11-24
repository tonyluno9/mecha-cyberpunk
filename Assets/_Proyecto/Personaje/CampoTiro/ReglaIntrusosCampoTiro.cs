namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// VR50: quién no puede estar dentro de la sala del campo de tiro. Ningún peatón, pandillero, policía, dron ni carro entra o cruza la sala:
    /// todo lo que camina (NavMeshAgent o CharacterController), un cuerpo pesado (carro) o un personaje que recibe daño y no es del jugador,
    /// de la sala ni un objetivo del campo, se saca. Regla pura para las pruebas; la aplica <see cref="GuardiaSalaCampoTiro"/>.
    /// </summary>
    public static class ReglaIntrusosCampoTiro
    {
        /// <summary>Masa (kg) desde la que un Rigidbody cuenta como vehículo.</summary>
        public const float MasaVehiculo = 300f;

        /// <summary>Metros extra alrededor de la sala que también se vigilan (lo que viene atravesando una pared).</summary>
        public const float MargenVigilancia = 3f;

        /// <summary>Segundos entre dos barridos de la sala.</summary>
        public const float SegundosEntreBarridos = 0.2f;

        public static bool EsIntruso(bool esDelJugador, bool esDeLaSala, bool esObjetivo, bool tieneAgente, bool tieneControlador, float masaCuerpo, bool esPersonajeConDanio)
        {
            if (esDelJugador || esDeLaSala || esObjetivo)
            {
                return false;
            }
            return tieneAgente || tieneControlador || masaCuerpo >= MasaVehiculo || esPersonajeConDanio;
        }
    }
}
