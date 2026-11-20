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
        public float Maxima { get; private set; }
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

        /// <summary>Cura hasta la máxima y devuelve lo que curó. No revive ni reinicia la espera de la regeneración.</summary>
        public float Curar(float cantidad)
        {
            if (EstaKO || cantidad <= 0f || Actual >= Maxima)
            {
                return 0f;
            }
            float curado = Math.Min(cantidad, Maxima - Actual);
            Actual += curado;
            Cambio?.Invoke();
            return curado;
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

        /// <summary>
        /// T-1529: cambia la vida máxima (los implantes la suben). Al subir, la vida de ahora sube lo mismo (se gana esa vida); al bajar, no queda por
        /// encima del nuevo máximo. Con KO no cambia la vida de ahora. Avisa solo si algo cambió.
        /// </summary>
        public void PonerMaxima(float maxima)
        {
            if (maxima <= 0f || Math.Abs(maxima - Maxima) < 1e-4f)
            {
                return;
            }
            float delta = maxima - Maxima;
            Maxima = maxima;
            if (!EstaKO)
            {
                Actual = Math.Min(Maxima, Math.Max(0f, Actual + Math.Max(0f, delta)));
            }
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
