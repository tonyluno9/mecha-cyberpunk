using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>
    /// Vida en C# puro del jugador a pie: como <see cref="ModeloVida"/> pero además cura. Se regenera sola cuando pasan
    /// <c>segundosSinDanio</c> sin recibir daño (T-402). Lógica pura: recibe el tiempo.
    /// </summary>
    public class ModeloVidaRegenerable : IVida
    {
        private readonly float porSegundo;
        private readonly float espera;
        private float sinDanio;

        public float Actual { get; private set; }
        public float Maxima { get; }
        public bool EstaKO { get; private set; }

        public event Action Cambio;
        public event Action KO;

        public ModeloVidaRegenerable(float maxima, float regeneracionPorSegundo, float segundosSinDanio)
        {
            if (maxima <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxima), "La vida máxima debe ser mayor que 0.");
            }
            Maxima = maxima;
            Actual = maxima;
            porSegundo = Math.Max(0f, regeneracionPorSegundo);
            espera = Math.Max(0f, segundosSinDanio);
        }

        /// <summary>Resta vida sin bajar de 0 y devuelve lo que de verdad restó. Reinicia la espera de la regeneración.</summary>
        public float Aplicar(float cantidad)
        {
            if (EstaKO || cantidad <= 0f)
            {
                return 0f;
            }
            float restado = Math.Min(cantidad, Actual);
            Actual -= restado;
            sinDanio = 0f;
            if (Actual <= 0f)
            {
                Actual = 0f;
                EstaKO = true;
            }
            Cambio?.Invoke();
            if (EstaKO)
            {
                KO?.Invoke();
            }
            return restado;
        }

        /// <summary>Pasa el tiempo: si ya esperó lo suficiente, cura hasta la máxima.</summary>
        public void Avanzar(float dt)
        {
            if (EstaKO || Actual >= Maxima)
            {
                return;
            }
            sinDanio += dt;
            if (sinDanio < espera || porSegundo <= 0f)
            {
                return;
            }
            Actual = Math.Min(Maxima, Actual + porSegundo * dt);
            Cambio?.Invoke();
        }

        public void Restaurar()
        {
            Actual = Maxima;
            EstaKO = false;
            sinDanio = 0f;
            Cambio?.Invoke();
        }
    }
}
