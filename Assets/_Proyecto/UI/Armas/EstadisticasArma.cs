using MechaCyberpunk.Personaje;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>Los números de un arma en un nivel, como los muestra el banco (T-814). Sin Unity: se arma a mano en las pruebas.</summary>
    public readonly struct EstadisticasArma
    {
        /// <summary>Daño de un disparo completo (puntos; en la escopeta, todos los perdigones).</summary>
        public readonly float Danio;
        /// <summary>Balas en el cargador.</summary>
        public readonly int Cargador;
        /// <summary>Tiempo de recarga (s).</summary>
        public readonly float Recarga;
        /// <summary>Disparos por segundo.</summary>
        public readonly float Cadencia;
        /// <summary>Dispersión sin apuntar (°).</summary>
        public readonly float Dispersion;

        public EstadisticasArma(float danio, int cargador, float recarga, float cadencia, float dispersion)
        {
            Danio = danio;
            Cargador = cargador;
            Recarga = recarga;
            Cadencia = cadencia;
            Dispersion = dispersion;
        }

        public static EstadisticasArma De(DatosArma datos)
        {
            if (datos == null)
            {
                return default;
            }
            float cadencia = datos.TiempoEntreDisparos > 0f ? 1f / datos.TiempoEntreDisparos : 0f;
            return new EstadisticasArma(datos.Danio * datos.Perdigones, datos.Cargador, datos.Recarga, cadencia, datos.Dispersion);
        }

        public float Valor(EstadisticaArma estadistica)
        {
            switch (estadistica)
            {
                case EstadisticaArma.Danio: return Danio;
                case EstadisticaArma.Cargador: return Cargador;
                case EstadisticaArma.Recarga: return Recarga;
                case EstadisticaArma.Cadencia: return Cadencia;
                default: return Dispersion;
            }
        }
    }
}
