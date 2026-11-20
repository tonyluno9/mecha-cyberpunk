using MechaCyberpunk.Combate.Golpe;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1487: lógica pura del combo del golpe a pie. Recuerda un clic dado un poco antes de tiempo (buffer), deja encadenar el
    /// siguiente golpe cuando la recuperación pasó de la ventana (sin esperar a volver a Listo) y reinicia el combo si el jugador se detiene.
    /// El golpe final del combo no encadena: hay que esperar a que termine.
    /// </summary>
    public sealed class ReglaComboGolpe
    {
        private readonly int golpes;
        private readonly float segundosBuffer;
        private readonly float ventana;
        private readonly float segundosReinicio;
        private float buffer;
        private float tiempoLibre;

        public ReglaComboGolpe(int golpes, float segundosBuffer, float ventana, float segundosReinicio)
        {
            this.golpes = golpes < 1 ? 1 : golpes;
            this.segundosBuffer = segundosBuffer < 0f ? 0f : segundosBuffer;
            this.ventana = ventana < 0f ? 0f : (ventana > 1f ? 1f : ventana);
            this.segundosReinicio = segundosReinicio < 0.01f ? 0.01f : segundosReinicio;
        }

        /// <summary>Índice (desde 0) del golpe en curso o del último dado; -1 si no hay combo.</summary>
        public int Paso { get; private set; } = -1;

        /// <summary>El golpe en curso es el último del combo.</summary>
        public bool EsGolpeFinal => Paso == golpes - 1;

        public bool HayBuffer => buffer > 0f;

        public int Golpes => golpes;

        /// <summary>El jugador pidió golpear: se recuerda <c>segundosBuffer</c> segundos.</summary>
        public void RegistrarEntrada() => buffer = segundosBuffer;

        /// <summary>True si el siguiente golpe del combo ya puede arrancar con esta fase del ciclo.</summary>
        public bool PuedeArrancar(FaseGolpe fase, float progreso)
        {
            if (fase == FaseGolpe.Listo)
            {
                return true;
            }
            return fase == FaseGolpe.Recuperando && Paso >= 0 && !EsGolpeFinal && progreso >= ventana;
        }

        /// <summary>
        /// Avanza los temporizadores (con <paramref name="permitir"/> en false nunca arranca: el llamador espera, p. ej. a que acabe la animación). Devuelve true cuando hay que arrancar el siguiente golpe ahora (consume el buffer y avanza <see cref="Paso"/>).
        /// </summary>
        public bool Avanzar(float dt, FaseGolpe fase, float progreso, bool permitir = true)
        {
            if (buffer > 0f)
            {
                buffer -= dt;
            }
            if (fase == FaseGolpe.Listo)
            {
                tiempoLibre += dt;
                if (tiempoLibre >= segundosReinicio)
                {
                    Paso = -1;
                }
            }
            else
            {
                tiempoLibre = 0f;
            }
            if (!permitir || buffer <= 0f || !PuedeArrancar(fase, progreso))
            {
                return false;
            }
            buffer = 0f;
            tiempoLibre = 0f;
            Paso = (Paso + 1) % golpes;
            return true;
        }

        /// <summary>Cancela el combo y el buffer (KO, derribo, cambio de arma).</summary>
        public void Reiniciar()
        {
            Paso = -1;
            buffer = 0f;
            tiempoLibre = 0f;
        }
    }
}
