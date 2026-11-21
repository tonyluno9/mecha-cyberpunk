using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Lógica pura del movimiento a pie: velocidad horizontal con inercia y velocidad vertical con salto.</summary>
    public class MovimientoAPie
    {
        /// <summary>Velocidad vertical que se mantiene en el piso para que el CharacterController siga detectándolo (m/s).</summary>
        public const float VelocidadVerticalEnPiso = -2f;

        /// <summary>
        /// T-1550 (decisión de Tony del 20-nov): segundos entre pedir el salto y el impulso: 3 cuadros a 30 fps (0.1 s), la agachada de PJ01_Salto_Despegue y PJ01_Salto_Correr_Despegue.
        /// El clip despega en el cuadro 4, así que el cuerpo sale del piso justo ahí.
        /// </summary>
        public const float SegundosRetardoSalto = 0.1f;

        private const float DuracionPasoMinima = 0.0001f;
        private float segundosParaSaltar;
        private bool saltoPendiente;

        /// <summary>T-1550: segundos que faltan para el impulso (la agachada del salto). Con 0 el salto sale en el mismo cuadro en que se pide.</summary>
        public float RetardoSalto { get; set; } = SegundosRetardoSalto;

        /// <summary>T-1550: true entre pedir el salto y el impulso (el personaje se agacha en el piso).</summary>
        public bool PreparandoSalto => saltoPendiente;

        /// <summary>T-1550: true solo en el cuadro en que sale el impulso del salto (el sonido y la animación del despegue cuelgan de él).</summary>
        public bool DespegoEsteFrame { get; private set; }

        /// <summary>Velocidad horizontal en el espacio del jugador (x lateral, y adelante, m/s).</summary>
        public Vector2 VelocidadHorizontal { get; private set; }

        /// <summary>Velocidad vertical (m/s, positivo sube).</summary>
        public float VelocidadVertical { get; private set; }

        /// <summary>Calcula la velocidad horizontal del frame. <paramref name="entrada"/> es WASD (-1 a 1 por eje).</summary>
        public Vector2 ActualizarHorizontal(Vector2 entrada, bool correr, bool enPiso, float dt,
            float velocidadCaminar, float velocidadCorrer, float aceleracion, float frenado, float controlAire)
        {
            Vector2 direccion = Vector2.ClampMagnitude(entrada, 1f);
            Vector2 objetivo = direccion * (correr ? velocidadCorrer : velocidadCaminar);
            float tasa = direccion.sqrMagnitude > 0f ? aceleracion : frenado;
            if (!enPiso)
            {
                tasa *= controlAire;
            }
            VelocidadHorizontal = Vector2.MoveTowards(VelocidadHorizontal, objetivo, tasa * Mathf.Max(dt, DuracionPasoMinima));
            return VelocidadHorizontal;
        }

        /// <summary>
        /// Calcula cuánto se mueve en vertical este frame (m). Pide saltar solo si está en el piso; el impulso sale <see cref="RetardoSalto"/> segundos después (T-1550: 0.1 s, para
        /// que despegue en el cuadro 4 del clip) y se cancela si deja el piso antes. La integración es exacta para gravedad constante, así que el salto llega a la altura pedida.
        /// </summary>
        public float ActualizarVertical(bool enPiso, bool saltar, float dt, float alturaSalto, float gravedad)
        {
            DespegoEsteFrame = false;
            if (!enPiso)
            {
                saltoPendiente = false;
            }
            else if (saltar && !saltoPendiente && VelocidadVertical <= 0f)
            {
                saltoPendiente = true;
                segundosParaSaltar = RetardoSalto;
            }
            if (enPiso && saltoPendiente)
            {
                segundosParaSaltar -= dt;
                if (segundosParaSaltar <= 0f)
                {
                    saltoPendiente = false;
                    DespegoEsteFrame = true;
                    VelocidadVertical = VelocidadInicialSalto(alturaSalto, gravedad);
                }
            }
            if (!DespegoEsteFrame && enPiso && VelocidadVertical <= 0f)
            {
                VelocidadVertical = VelocidadVerticalEnPiso;
                return VelocidadVertical * dt;
            }

            float desplazamiento = VelocidadVertical * dt - 0.5f * gravedad * dt * dt;
            VelocidadVertical -= gravedad * dt;
            return desplazamiento;
        }

        /// <summary>Velocidad vertical inicial (m/s) para llegar a la altura pedida con esa gravedad.</summary>
        public static float VelocidadInicialSalto(float alturaSalto, float gravedad)
        {
            return Mathf.Sqrt(2f * gravedad * alturaSalto);
        }

        /// <summary>Congela el movimiento (para KO, carro o teletransporte).</summary>
        public void Reiniciar()
        {
            VelocidadHorizontal = Vector2.zero;
            VelocidadVertical = 0f;
            saltoPendiente = false;
            DespegoEsteFrame = false;
        }
    }
}
