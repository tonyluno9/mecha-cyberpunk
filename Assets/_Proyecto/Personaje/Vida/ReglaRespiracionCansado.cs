using System;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// T-voces-personajes-23nov: cuándo suena la respiración de cansado (JUG_RESPIRACION_CANSADO, bucle con fundido): el cansancio sube 1 por segundo
    /// corriendo y baja más rápido sin correr. Empieza a sonar al llegar a <see cref="Inicio"/> y deja de sonar al bajar de <see cref="Fin"/>
    /// (sigue jadeando un rato después de parar). Lógica pura: el tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaRespiracionCansado
    {
        /// <summary>Segundos corriendo seguidos para que empiece a jadear.</summary>
        public const float Inicio = 6f;

        /// <summary>Por debajo de este cansancio deja de jadear.</summary>
        public const float Fin = 2f;

        /// <summary>Tope del cansancio (s).</summary>
        public const float Maximo = 10f;

        /// <summary>Cuánto baja el cansancio por segundo sin correr.</summary>
        public const float RecuperacionPorSegundo = 1.5f;

        public float Cansancio { get; private set; }

        public bool Sonando { get; private set; }

        /// <summary>Avanza <paramref name="dt"/> segundos; devuelve si la respiración debe sonar.</summary>
        public bool Avanzar(float dt, bool corriendo)
        {
            Cansancio = corriendo ? Math.Min(Maximo, Cansancio + dt) : Math.Max(0f, Cansancio - dt * RecuperacionPorSegundo);
            if (!Sonando && Cansancio >= Inicio) Sonando = true;
            else if (Sonando && Cansancio < Fin) Sonando = false;
            return Sonando;
        }

        public void Reiniciar()
        {
            Cansancio = 0f;
            Sonando = false;
        }
    }
}
