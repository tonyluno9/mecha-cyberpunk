using System;

namespace MechaCyberpunk.Personaje.Habilidades
{
    /// <summary>Las tres habilidades de prótesis (T-992), en el orden de sus teclas: [1] PIERNAS, [2] BRAZOS, [3] CABEZA.</summary>
    public enum HabilidadProtesis
    {
        Piernas = 0,
        Brazos = 1,
        Cabeza = 2
    }

    /// <summary>Lista: se puede usar. Activa: el medidor se vacía y el efecto corre. Recargando: el medidor se llena y no se puede usar.</summary>
    public enum EstadoHabilidad
    {
        Lista = 0,
        Activa = 1,
        Recargando = 2
    }

    /// <summary>
    /// Lógica pura de una habilidad de prótesis (T-992): Lista, Activa (dura según el nivel del implante, el II dura más) y Recargando.
    /// Sin implante (nivel 0) no se activa. El medidor va de 0 a 1: lleno si está lista, se vacía activa y se llena recargando.
    /// </summary>
    public sealed class ReglaHabilidadProtesis
    {
        private readonly float duracionNivel1;
        private readonly float duracionNivel2;
        private readonly float recarga;
        private float duracionActual;
        private float restante;

        public ReglaHabilidadProtesis(float duracionNivel1, float duracionNivel2, float recarga)
        {
            this.duracionNivel1 = Math.Max(0.01f, duracionNivel1);
            this.duracionNivel2 = Math.Max(this.duracionNivel1, duracionNivel2);
            this.recarga = Math.Max(0.01f, recarga);
        }

        public EstadoHabilidad Estado { get; private set; } = EstadoHabilidad.Lista;

        /// <summary>Segundos que faltan de la fase en curso (0 si está lista).</summary>
        public float Restante => Estado == EstadoHabilidad.Lista ? 0f : restante;

        /// <summary>La duración de la habilidad para ese nivel de implante (0 sin implante).</summary>
        public float Duracion(int nivel) => nivel <= 0 ? 0f : (nivel >= 2 ? duracionNivel2 : duracionNivel1);

        /// <summary>Lleno (1) si está lista, se vacía mientras está activa y se llena mientras recarga.</summary>
        public float Medidor
        {
            get
            {
                switch (Estado)
                {
                    case EstadoHabilidad.Activa: return Math.Clamp(restante / duracionActual, 0f, 1f);
                    case EstadoHabilidad.Recargando: return Math.Clamp(1f - restante / recarga, 0f, 1f);
                    default: return 1f;
                }
            }
        }

        /// <summary>La activa si está lista y hay implante (<paramref name="nivel"/> mayor que 0). False si no se pudo.</summary>
        public bool Activar(int nivel)
        {
            if (Estado != EstadoHabilidad.Lista || nivel <= 0)
            {
                return false;
            }
            duracionActual = Duracion(nivel);
            restante = duracionActual;
            Estado = EstadoHabilidad.Activa;
            return true;
        }

        /// <summary>Si estaba activa, la corta y pasa a recargar completa (muerte, quitar el implante). Si no, no hace nada.</summary>
        public void Cortar()
        {
            if (Estado == EstadoHabilidad.Activa)
            {
                Recargar();
            }
        }

        /// <summary>Avanza el reloj. Los tiempos siguen corriendo aunque el jugador no pueda usar la tecla.</summary>
        public void Avanzar(float dt)
        {
            if (Estado == EstadoHabilidad.Lista || dt <= 0f)
            {
                return;
            }
            restante -= dt;
            if (restante > 0f)
            {
                return;
            }
            if (Estado == EstadoHabilidad.Activa)
            {
                Recargar();
            }
            else
            {
                Estado = EstadoHabilidad.Lista;
                restante = 0f;
            }
        }

        private void Recargar()
        {
            Estado = EstadoHabilidad.Recargando;
            restante = recarga;
        }
    }
}
