namespace MechaCyberpunk.IA.APie
{
    /// <summary>T-53: arma de fuego del enemigo humano. Define el cargador y el sonido (el mismo ID que el arma del jugador).</summary>
    public enum TipoArmaEnemigo
    {
        Pistola,
        Escopeta,
        Subfusil
    }

    /// <summary>T-53: en qué punto del ciclo del arma está el enemigo.</summary>
    public enum FaseArmaEnemigo
    {
        Guardada,
        Sacando,
        Lista,
        Recargando
    }

    /// <summary>
    /// T-53: reglas de combate del enemigo humano iguales a las del jugador: para disparar primero SACA el arma y espera a que esté en pose,
    /// el cargador tiene la capacidad del arma y al vaciarse RECARGA (clip y tiempo) antes de seguir, y al dejar de pelear la GUARDA.
    /// Lógica pura: recibe el tiempo y lo que ve el animador; devuelve qué animación pedir.
    /// </summary>
    public sealed class ReglaArmaEnemigo
    {
        /// <summary>Lo que el cerebro debe pedirle al visual en este paso.</summary>
        public struct Salida
        {
            /// <summary>Empezó a sacar el arma en este paso.</summary>
            public bool EmpezoSacar;
            /// <summary>Se vació el cargador: empieza la recarga en este paso.</summary>
            public bool EmpezoRecargar;
            /// <summary>Pasó el tiempo sin pelear: se guarda el arma.</summary>
            public bool Guardar;
        }

        private readonly float segundosRecarga;
        private readonly float segundosMaxSacar;
        private readonly float segundosParaGuardar;
        private float temporizador;
        private float sinPelea;

        public ReglaArmaEnemigo(TipoArmaEnemigo tipo, float segundosRecarga, float segundosMaxSacar, float segundosParaGuardar)
        {
            Tipo = tipo;
            this.segundosRecarga = segundosRecarga;
            this.segundosMaxSacar = segundosMaxSacar;
            this.segundosParaGuardar = segundosParaGuardar;
            Municion = Capacidad(tipo);
        }

        public TipoArmaEnemigo Tipo { get; }
        public FaseArmaEnemigo Fase { get; private set; } = FaseArmaEnemigo.Guardada;
        public int Municion { get; private set; }

        /// <summary>Balas que caben en el cargador de cada arma (las mismas que da el catálogo del jugador: pistola 12, escopeta 6, subfusil 30).</summary>
        public static int Capacidad(TipoArmaEnemigo tipo)
        {
            switch (tipo)
            {
                case TipoArmaEnemigo.Escopeta: return 6;
                case TipoArmaEnemigo.Subfusil: return 30;
                default: return 12;
            }
        }

        /// <summary>True si el arma está fuera de la funda (sacando, lista o recargando).</summary>
        public bool ArmaFuera => Fase != FaseArmaEnemigo.Guardada;

        /// <summary>True si puede disparar ya: arma en pose y con balas.</summary>
        public bool PuedeDisparar => Fase == FaseArmaEnemigo.Lista && Municion > 0;

        /// <summary>Gasta una bala si puede disparar. Devuelve true si se gastó (había arma en pose y balas).</summary>
        public bool RegistrarDisparo()
        {
            if (!PuedeDisparar)
            {
                return false;
            }
            Municion--;
            return true;
        }

        /// <summary>
        /// <paramref name="enPelea"/>: el enemigo tiene a quién atacar. <paramref name="armaEnPose"/>: el animador ya terminó de sacar el arma.
        /// </summary>
        public Salida Avanzar(float dt, bool enPelea, bool armaEnPose)
        {
            Salida salida = default;
            switch (Fase)
            {
                case FaseArmaEnemigo.Guardada:
                    if (enPelea)
                    {
                        Fase = FaseArmaEnemigo.Sacando;
                        temporizador = 0f;
                        sinPelea = 0f;
                        salida.EmpezoSacar = true;
                    }
                    break;
                case FaseArmaEnemigo.Sacando:
                    temporizador += dt;
                    if (armaEnPose || temporizador >= segundosMaxSacar)
                    {
                        Fase = FaseArmaEnemigo.Lista;
                    }
                    break;
                case FaseArmaEnemigo.Lista:
                    sinPelea = enPelea ? 0f : sinPelea + dt;
                    if (Municion <= 0)
                    {
                        Fase = FaseArmaEnemigo.Recargando;
                        temporizador = 0f;
                        salida.EmpezoRecargar = true;
                    }
                    else if (sinPelea >= segundosParaGuardar)
                    {
                        Fase = FaseArmaEnemigo.Guardada;
                        salida.Guardar = true;
                    }
                    break;
                case FaseArmaEnemigo.Recargando:
                    temporizador += dt;
                    if (temporizador >= segundosRecarga)
                    {
                        Municion = Capacidad(Tipo);
                        Fase = FaseArmaEnemigo.Lista;
                    }
                    break;
            }
            return salida;
        }
    }
}
