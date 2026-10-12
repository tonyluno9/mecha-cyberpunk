using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Lógica pura del movimiento a pie: velocidad horizontal con inercia y velocidad vertical con salto.</summary>
    public class MovimientoAPie
    {
        /// <summary>Velocidad vertical que se mantiene en el piso para que el CharacterController siga detectándolo (m/s).</summary>
        public const float VelocidadVerticalEnPiso = -2f;

        private const float DuracionPasoMinima = 0.0001f;

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
        /// Calcula cuánto se mueve en vertical este frame (m). Salta solo si está en el piso y se pidió saltar.
        /// La integración es exacta para gravedad constante, así que el salto llega a la altura pedida.
        /// </summary>
        public float ActualizarVertical(bool enPiso, bool saltar, float dt, float alturaSalto, float gravedad)
        {
            if (enPiso && saltar)
            {
                VelocidadVertical = VelocidadInicialSalto(alturaSalto, gravedad);
            }
            else if (enPiso && VelocidadVertical <= 0f)
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
        }
    }
}
