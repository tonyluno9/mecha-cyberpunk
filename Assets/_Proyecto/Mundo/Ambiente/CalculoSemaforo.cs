using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>Luz que está encendida en un semáforo.</summary>
    public enum FaseSemaforo
    {
        Verde,
        Ambar,
        Rojo
    }

    /// <summary>
    /// T-910: la lógica del ciclo de un semáforo, sin MonoBehaviour: verde, ámbar y rojo en ese orden, repetido. Recibe los segundos que pasan
    /// (<see cref="Avanzar"/>) y dice qué luz toca. El desfase mueve el arranque dentro del ciclo (medio ciclo = contrafase con la calle cruzada).
    /// </summary>
    public sealed class CalculoSemaforo
    {
        private readonly float verde;
        private readonly float ambar;
        private readonly float rojo;
        private float tiempo;

        public CalculoSemaforo(float segundosVerde, float segundosAmbar, float segundosRojo, float desfase = 0f)
        {
            verde = Mathf.Max(0.01f, segundosVerde);
            ambar = Mathf.Max(0.01f, segundosAmbar);
            rojo = Mathf.Max(0.01f, segundosRojo);
            tiempo = Mod(desfase, Ciclo);
        }

        /// <summary>Duración total del ciclo (s).</summary>
        public float Ciclo => verde + ambar + rojo;

        /// <summary>Segundos dentro del ciclo, de 0 a <see cref="Ciclo"/>.</summary>
        public float Tiempo => tiempo;

        /// <summary>La luz encendida ahora.</summary>
        public FaseSemaforo Fase => FaseEn(tiempo);

        public void Avanzar(float dt)
        {
            tiempo = Mod(tiempo + Mathf.Max(0f, dt), Ciclo);
        }

        public FaseSemaforo FaseEn(float t)
        {
            t = Mod(t, Ciclo);
            if (t < verde)
            {
                return FaseSemaforo.Verde;
            }
            return t < verde + ambar ? FaseSemaforo.Ambar : FaseSemaforo.Rojo;
        }

        private static float Mod(float x, float m)
        {
            float r = x % m;
            return r < 0f ? r + m : r;
        }
    }
}
