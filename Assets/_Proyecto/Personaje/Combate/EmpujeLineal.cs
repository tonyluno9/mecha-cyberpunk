using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1487: empuje puro que frena en línea recta hasta cero. El desplazamiento total es velocidad × segundos / 2.</summary>
    public sealed class EmpujeLineal
    {
        private Vector3 velocidadInicial;
        private float duracion;
        private float transcurrido;

        public bool Activo => duracion > 0f && transcurrido < duracion;

        /// <summary>Empieza un empuje nuevo; si hay uno más fuerte en curso, se queda ese.</summary>
        public void Iniciar(Vector3 velocidad, float segundos)
        {
            if (segundos <= 0f || velocidad.sqrMagnitude < 1e-6f)
            {
                return;
            }
            if (Activo && VelocidadActual().sqrMagnitude >= velocidad.sqrMagnitude)
            {
                return;
            }
            velocidadInicial = velocidad;
            duracion = segundos;
            transcurrido = 0f;
        }

        /// <summary>Desplazamiento (m) de este paso.</summary>
        public Vector3 Avanzar(float dt)
        {
            if (!Activo || dt <= 0f)
            {
                return Vector3.zero;
            }
            float fin = Mathf.Min(duracion, transcurrido + dt);
            float fraccionInicio = 1f - transcurrido / duracion;
            float fraccionFin = 1f - fin / duracion;
            float paso = fin - transcurrido;
            transcurrido = fin;
            return velocidadInicial * ((fraccionInicio + fraccionFin) * 0.5f * paso);
        }

        public Vector3 VelocidadActual() => Activo ? velocidadInicial * (1f - transcurrido / duracion) : Vector3.zero;
    }
}
