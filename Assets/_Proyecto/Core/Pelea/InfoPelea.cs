namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Qué pelea está por empezar, para que el mundo la acompañe (carteles, luces de la jaula). Lo entrega
    /// <see cref="EventosPelea.PeleaPresentada"/>.
    /// </summary>
    public readonly struct InfoPelea
    {
        /// <summary>Alias del rival, tal como se muestra en pantalla.</summary>
        public readonly string Rival;

        /// <summary>Liga de la pelea, de 1 a 4. 0 = pelea fuera de liga (casera, prólogo, emboscada).</summary>
        public readonly int Liga;

        /// <summary>Pelea de jefe: el campeón de una liga o el campeón del prólogo.</summary>
        public readonly bool EsJefe;

        public InfoPelea(string rival, int liga, bool esJefe)
        {
            Rival = rival;
            Liga = liga;
            EsJefe = esJefe;
        }
    }
}
