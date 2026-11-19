namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// Cómo se mezcla un ID (T-593): cuántas voces del mismo grupo pueden sonar a la vez (la más vieja se corta), volumen relativo, variación de
    /// tono al azar y el tiempo mínimo entre dos repeticiones. Se obtiene de <see cref="ReglaMezclaSonidos.Para"/>.
    /// </summary>
    public readonly struct PoliticaSonido
    {
        public PoliticaSonido(string grupo, int maxVoces, float volumen, float variacionTono, float segundosMinimos, float distanciaMaxima = 0f, float tonoBase = 1f, float variacionVolumen = 0f)
        {
            Grupo = grupo;
            MaxVoces = maxVoces;
            Volumen = volumen;
            VariacionTono = variacionTono;
            SegundosMinimos = segundosMinimos;
            DistanciaMaxima = distanciaMaxima;
            TonoBase = tonoBase;
            VariacionVolumen = variacionVolumen;
        }

        /// <summary>Nombre del grupo que comparte el límite de voces.</summary>
        public string Grupo { get; }

        /// <summary>Voces simultáneas del grupo; 0 = sin límite.</summary>
        public int MaxVoces { get; }

        /// <summary>Factor del volumen (0 a 1) sobre el de la sección.</summary>
        public float Volumen { get; }

        /// <summary>El tono varía entre 1 - x y 1 + x (por ejemplo 0.04 = +-4 %).</summary>
        public float VariacionTono { get; }

        /// <summary>Si el mismo ID se pide antes de este tiempo (s) se ignora; 0 = sin mínimo.</summary>
        public float SegundosMinimos { get; }

        /// <summary>Distancia (m) a la que el sonido 3D ya no se oye; 0 = la del gestor.</summary>
        public float DistanciaMaxima { get; }

        /// <summary>T-1471: tono de partida (el subfusil usa el disparo de la pistola más agudo); el tono final es TonoBase por la variación al azar.</summary>
        public float TonoBase { get; }

        /// <summary>T-1471: el volumen baja al azar hasta este porcentaje (0.05 = hasta -5 %), para que nada suene idéntico dos veces.</summary>
        public float VariacionVolumen { get; }
    }
}
