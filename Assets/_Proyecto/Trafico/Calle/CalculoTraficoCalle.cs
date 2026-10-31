using System;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// Cómo maneja un carro del tráfico de la calle (T-451), sin Unity: va a su velocidad de crucero, frena en cuanto hay algo adelante (más
    /// fuerte cuanto más cerca), se detiene si está muy cerca, pita cuando lleva rato bloqueado y también se asusta con un disparo (se detiene
    /// un rato y pita una vez). El tiempo y las distancias entran como parámetros; no usa azar.
    /// </summary>
    public sealed class CalculoTraficoCalle
    {
        private readonly float crucero;
        private readonly float aceleracion;
        private readonly float frenado;
        private readonly float distanciaFrenado;
        private readonly float distanciaParada;
        private readonly float segundosParaPitar;
        private readonly float segundosEntrePitidos;

        private float bloqueado;
        private float desdeUltimoPitido;
        private float panicoRestante;
        private bool pitidoPendiente;

        /// <param name="velocidadCrucero">Velocidad normal (m/s).</param>
        /// <param name="aceleracion">Cuánto acelera al librarse (m/s²).</param>
        /// <param name="frenado">Cuánto frena (m/s²).</param>
        /// <param name="distanciaFrenado">A esta distancia de un obstáculo empieza a frenar (m).</param>
        /// <param name="distanciaParada">A esta distancia o menos se detiene (m); menor que la de frenado.</param>
        /// <param name="segundosParaPitar">Bloqueado este tiempo, pita (s).</param>
        /// <param name="segundosEntrePitidos">Mínimo entre dos pitidos (s).</param>
        public CalculoTraficoCalle(float velocidadCrucero, float aceleracion, float frenado, float distanciaFrenado, float distanciaParada,
            float segundosParaPitar, float segundosEntrePitidos)
        {
            if (velocidadCrucero <= 0f || aceleracion <= 0f || frenado <= 0f || distanciaFrenado <= distanciaParada || distanciaParada < 0f)
            {
                throw new ArgumentException("Velocidad, aceleración y frenado mayores que 0 y la distancia de frenado mayor que la de parada.");
            }
            crucero = velocidadCrucero;
            this.aceleracion = aceleracion;
            this.frenado = frenado;
            this.distanciaFrenado = distanciaFrenado;
            this.distanciaParada = distanciaParada;
            this.segundosParaPitar = Math.Max(0f, segundosParaPitar);
            this.segundosEntrePitidos = Math.Max(0f, segundosEntrePitidos);
            desdeUltimoPitido = this.segundosEntrePitidos;
        }

        public float Velocidad { get; private set; }

        /// <summary>True mientras se detuvo por un susto (un disparo).</summary>
        public bool EnPanico => panicoRestante > 0f;

        /// <summary>True si hay un obstáculo muy cerca y no se mueve.</summary>
        public bool Bloqueado => bloqueado > 0f;

        /// <summary>La velocidad a la que quiere ir según el obstáculo de enfrente (<see cref="float.MaxValue"/> si no hay).</summary>
        public float VelocidadObjetivo(float distanciaAlObstaculo)
        {
            if (panicoRestante > 0f || distanciaAlObstaculo <= distanciaParada)
            {
                return 0f;
            }
            if (distanciaAlObstaculo >= distanciaFrenado)
            {
                return crucero;
            }
            return crucero * (distanciaAlObstaculo - distanciaParada) / (distanciaFrenado - distanciaParada);
        }

        /// <summary>Un disparo cerca: se detiene <paramref name="segundos"/> y pita una vez.</summary>
        public void Asustar(float segundos)
        {
            panicoRestante = Math.Max(panicoRestante, segundos);
            pitidoPendiente = true;
        }

        /// <summary>Avanza el reloj y devuelve la velocidad nueva (m/s).</summary>
        public float Avanzar(float dt, float distanciaAlObstaculo)
        {
            if (panicoRestante > 0f)
            {
                panicoRestante = Math.Max(0f, panicoRestante - dt);
            }
            float objetivo = VelocidadObjetivo(distanciaAlObstaculo);
            Velocidad = objetivo > Velocidad
                ? Math.Min(objetivo, Velocidad + aceleracion * dt)
                : Math.Max(objetivo, Velocidad - frenado * dt);
            desdeUltimoPitido += dt;
            bool detenidoPorObstaculo = Velocidad < 0.3f && distanciaAlObstaculo < distanciaFrenado && panicoRestante <= 0f;
            bloqueado = detenidoPorObstaculo ? bloqueado + dt : 0f;
            if (bloqueado >= segundosParaPitar && bloqueado > 0f && desdeUltimoPitido >= segundosEntrePitidos)
            {
                pitidoPendiente = true;
            }
            return Velocidad;
        }

        /// <summary>True una sola vez cuando le toca pitar (por bloqueo o por susto); al consultarlo se apaga.</summary>
        public bool TomarPitido()
        {
            if (!pitidoPendiente)
            {
                return false;
            }
            pitidoPendiente = false;
            desdeUltimoPitido = 0f;
            return true;
        }
    }
}
