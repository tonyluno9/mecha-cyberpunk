namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1660 (T42): la puerta de NOVA, lógica pura. Nova no llega hasta que mataste a <c>minimoMuertos</c> policías Y ya llegaron las camionetas.
    /// Orden de escalada: patrullas, camionetas, (puerta), humanoides de Nova sin armadura (E15), los de armadura (E18), perros (E17), robots grandes (E16) y el jefe (E14).
    /// Un mínimo en 0 apaga esa condición.
    /// </summary>
    public static class ReglaPuertaNova
    {
        /// <summary>Camionetas que hay que esperar: las configuradas si el juego tiene camioneta, 0 si no existe el modelo (no se puede esperar a lo que nunca sale).</summary>
        public static int CamionetasRequeridas(bool hayCamioneta, int configuradas) => hayCamioneta ? System.Math.Max(0, configuradas) : 0;

        /// <summary>True si ya se cumplen las dos condiciones: policías muertos y camionetas llegadas.</summary>
        public static bool Abierta(int policiasMuertos, int minimoMuertos, int camionetasLlegadas, int camionetasRequeridas)
            => policiasMuertos >= minimoMuertos && camionetasLlegadas >= camionetasRequeridas;

        /// <summary>Nombre de la fase de Nova para depurar, según el nivel desbloqueado (1 a 8) y si la puerta está abierta.</summary>
        public static string NombreFase(int nivelDesbloqueado, bool puertaAbierta)
        {
            if (!puertaAbierta)
            {
                return "cerrada (solo policia)";
            }
            switch (nivelDesbloqueado)
            {
                case 4: return "humanoides sin armadura (E15)";
                case 5: return "humanoides con armadura (E18)";
                case 6: return "perros y drones de Nova";
                case 7: return "robots grandes (E16)";
                case 8: return "jefe (E14)";
                default: return "abierta (espera su nivel)";
            }
        }
    }
}
