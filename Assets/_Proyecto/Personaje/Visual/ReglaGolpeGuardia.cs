namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-715: el golpe del guardia de Nova en tres tiempos: AVISO (se ve y se oye que va a pegar), IMPACTO (aquí llega el daño si el jugador sigue a su alcance) y
    /// RECARGA. Si el jugador se va o se acaba la alerta durante el aviso, el golpe se cancela. Cada tercer golpe es la barrida (más fuerte). Pura, sin Unity.
    /// </summary>
    public sealed class ReglaGolpeGuardia
    {
        public enum Fase
        {
            Libre,
            Aviso,
            Recarga
        }

        public struct Salida
        {
            /// <summary>True en el paso en que empieza el aviso (se prende la luz y arranca la animación).</summary>
            public bool InicioAviso;
            /// <summary>True en el paso en que el golpe conecta (si el jugador sigue a su alcance).</summary>
            public bool Impacto;
            /// <summary>True si el golpe de ese impacto es la barrida.</summary>
            public bool Fuerte;
        }

        private readonly float segundosAviso;
        private readonly float segundosRecarga;
        private float temporizador;

        public ReglaGolpeGuardia(float segundosAviso, float segundosRecarga)
        {
            this.segundosAviso = segundosAviso < 0.05f ? 0.05f : segundosAviso;
            this.segundosRecarga = segundosRecarga < 0f ? 0f : segundosRecarga;
        }

        public Fase Actual { get; private set; } = Fase.Libre;

        /// <summary>Golpes que ha empezado (el tercero, el sexto... es la barrida).</summary>
        public int Golpes { get; private set; }

        /// <summary>El golpe en curso es la barrida.</summary>
        public bool Fuerte { get; private set; }

        /// <summary>Avance del aviso de 0 a 1 (0 fuera del aviso).</summary>
        public float ProgresoAviso => Actual == Fase.Aviso ? (temporizador >= segundosAviso ? 1f : temporizador / segundosAviso) : 0f;

        public Salida Avanzar(float dt, bool alerta, bool jugadorAlAlcance)
        {
            Salida salida = new Salida();
            switch (Actual)
            {
                case Fase.Libre:
                    if (alerta && jugadorAlAlcance)
                    {
                        Golpes++;
                        Fuerte = Golpes % 3 == 0;
                        Actual = Fase.Aviso;
                        temporizador = 0f;
                        salida.InicioAviso = true;
                    }
                    break;
                case Fase.Aviso:
                    if (!alerta || !jugadorAlAlcance)
                    {
                        Actual = Fase.Libre;
                        temporizador = 0f;
                        break;
                    }
                    temporizador += dt;
                    if (temporizador >= segundosAviso)
                    {
                        salida.Impacto = true;
                        salida.Fuerte = Fuerte;
                        Actual = Fase.Recarga;
                        temporizador = 0f;
                    }
                    break;
                case Fase.Recarga:
                    temporizador += dt;
                    if (temporizador >= segundosRecarga)
                    {
                        Actual = Fase.Libre;
                        temporizador = 0f;
                    }
                    break;
            }
            return salida;
        }

        public void Reiniciar()
        {
            Actual = Fase.Libre;
            temporizador = 0f;
        }
    }
}
