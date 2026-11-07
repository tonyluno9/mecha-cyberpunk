namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-714: cuándo los SKULLS llaman refuerzos. Cada SKULL vencido en su territorio cuenta; con <see cref="MuertesParaLlamar"/> vencidos sale un carro con refuerzos,
    /// y no sale otro hasta pasado el enfriamiento ni mientras otro va en camino. Pura, sin Unity.
    /// </summary>
    public sealed class ReglaRefuerzos
    {
        public const int MuertesParaLlamar = 3;
        public const float SegundosEnfriamiento = 60f;
        public const int MaximoEnCamino = 1;

        private readonly int muertesParaLlamar;
        private readonly float enfriamiento;
        private readonly int maximoEnCamino;
        private float proximaLlamada;

        public ReglaRefuerzos(int muertesParaLlamar = MuertesParaLlamar, float enfriamiento = SegundosEnfriamiento, int maximoEnCamino = MaximoEnCamino)
        {
            this.muertesParaLlamar = muertesParaLlamar < 1 ? 1 : muertesParaLlamar;
            this.enfriamiento = enfriamiento < 0f ? 0f : enfriamiento;
            this.maximoEnCamino = maximoEnCamino < 1 ? 1 : maximoEnCamino;
        }

        /// <summary>Vencidos desde la última llamada.</summary>
        public int Muertes { get; private set; }

        public void RegistrarMuertes(int cuantas)
        {
            if (cuantas > 0)
            {
                Muertes += cuantas;
            }
        }

        /// <summary>True si ya hay suficientes vencidos, pasó el enfriamiento y no van ya los carros permitidos en camino.</summary>
        public bool DebeLlamar(float ahora, int enCamino) => Muertes >= muertesParaLlamar && ahora >= proximaLlamada && enCamino < maximoEnCamino;

        /// <summary>Salió el carro: se vacía la cuenta y empieza el enfriamiento.</summary>
        public void Llamado(float ahora)
        {
            Muertes = 0;
            proximaLlamada = ahora + enfriamiento;
        }
    }
}
