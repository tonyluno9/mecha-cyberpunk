namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1494: ciclo del perro robot de Nova (E17). Patrulla; si el jugador entra a su radio de detección (o está en alerta) lo persigue; al llegar al alcance
    /// avisa un instante, muerde con un impulso corto hacia adelante y se recupera antes de volver a morder. Si el jugador se aleja mucho y no está en
    /// alerta, deja de perseguir. Lógica pura sin Unity.
    /// </summary>
    public class ReglaPerroNova
    {
        public enum Fase
        {
            Patrulla,
            Persigue,
            Aviso,
            Impulso,
            Recupera
        }

        public readonly struct Entrada
        {
            public Entrada(float distancia, bool alerta, bool atacable)
            {
                Distancia = distancia;
                Alerta = alerta;
                Atacable = atacable;
            }

            public float Distancia { get; }
            public bool Alerta { get; }
            public bool Atacable { get; }
        }

        public readonly struct Salida
        {
            public Salida(bool mordida) => Mordida = mordida;

            /// <summary>True en el paso en que la mordida conecta (si el jugador sigue en alcance).</summary>
            public bool Mordida { get; }
        }

        /// <summary>Suelta la persecución cuando el jugador está más lejos que el radio por este factor.</summary>
        public const float FactorPerdida = 1.6f;

        private readonly float radioDeteccion;
        private readonly float alcance;
        private readonly float segundosAviso;
        private readonly float segundosImpulso;
        private readonly float segundosRecarga;
        private float temporizador;

        public ReglaPerroNova(float radioDeteccion, float alcance, float segundosAviso = 0.3f, float segundosImpulso = 0.2f, float segundosRecarga = 1.1f)
        {
            this.radioDeteccion = radioDeteccion;
            this.alcance = alcance;
            this.segundosAviso = segundosAviso;
            this.segundosImpulso = segundosImpulso;
            this.segundosRecarga = segundosRecarga;
        }

        public Fase Actual { get; private set; } = Fase.Patrulla;

        public void Reiniciar()
        {
            Actual = Fase.Patrulla;
            temporizador = 0f;
        }

        public Salida Avanzar(float dt, Entrada e)
        {
            bool ve = e.Atacable && (e.Alerta || e.Distancia <= radioDeteccion);
            switch (Actual)
            {
                case Fase.Patrulla:
                    if (ve)
                    {
                        Actual = Fase.Persigue;
                    }
                    break;
                case Fase.Persigue:
                    if (!e.Atacable || (!e.Alerta && e.Distancia > radioDeteccion * FactorPerdida))
                    {
                        Actual = Fase.Patrulla;
                    }
                    else if (e.Distancia <= alcance)
                    {
                        Actual = Fase.Aviso;
                        temporizador = 0f;
                    }
                    break;
                case Fase.Aviso:
                    temporizador += dt;
                    if (temporizador >= segundosAviso)
                    {
                        Actual = Fase.Impulso;
                        temporizador = 0f;
                        return new Salida(true);
                    }
                    break;
                case Fase.Impulso:
                    temporizador += dt;
                    if (temporizador >= segundosImpulso)
                    {
                        Actual = Fase.Recupera;
                        temporizador = 0f;
                    }
                    break;
                case Fase.Recupera:
                    temporizador += dt;
                    if (temporizador >= segundosRecarga)
                    {
                        Actual = ve ? Fase.Persigue : Fase.Patrulla;
                        temporizador = 0f;
                    }
                    break;
            }
            return new Salida(false);
        }
    }
}
