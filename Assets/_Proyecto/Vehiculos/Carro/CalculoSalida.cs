namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>Por dónde puede bajar el conductor.</summary>
    public enum LadoSalida
    {
        /// <summary>Todas las salidas están ocupadas: no se puede bajar.</summary>
        Ninguno,
        /// <summary>Junto a la puerta del conductor (izquierda).</summary>
        Conductor,
        /// <summary>Del lado del copiloto (derecha), si la del conductor está bloqueada.</summary>
        Copiloto,
        /// <summary>T-657: por detrás del carro, si las dos puertas están bloqueadas.</summary>
        Atras,
        /// <summary>T-657: desde el techo, si tampoco se puede por atrás (último recurso).</summary>
        Arriba
    }

    /// <summary>Lógica pura de la salida del carro (sin física), para probarla en EditMode.</summary>
    public static class CalculoSalida
    {
        /// <summary>Elige la salida: primero la del conductor, luego la del copiloto, y si no hay ninguna libre, ninguna.</summary>
        public static LadoSalida Elegir(bool conductorLibre, bool copilotoLibre)
        {
            if (conductorLibre)
            {
                return LadoSalida.Conductor;
            }
            return copilotoLibre ? LadoSalida.Copiloto : LadoSalida.Ninguno;
        }

        /// <summary>T-657: igual que la otra, y si las dos puertas están bloqueadas prueba por atrás y, al final, desde arriba.</summary>
        public static LadoSalida Elegir(bool conductorLibre, bool copilotoLibre, bool atrasLibre, bool arribaLibre)
        {
            LadoSalida lado = Elegir(conductorLibre, copilotoLibre);
            if (lado != LadoSalida.Ninguno)
            {
                return lado;
            }
            if (atrasLibre)
            {
                return LadoSalida.Atras;
            }
            return arribaLibre ? LadoSalida.Arriba : LadoSalida.Ninguno;
        }

        /// <summary>True si el carro va lo bastante lento para bajar (estrictamente menor que el máximo).</summary>
        public static bool VaLentoParaBajar(float rapidez, float rapidezMaxima)
        {
            return rapidez < rapidezMaxima;
        }
    }
}
