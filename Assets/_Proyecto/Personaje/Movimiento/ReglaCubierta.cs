using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Lógica pura de la cubierta (T-421). <c>normal</c> apunta de la cubierta hacia el jugador, en el plano horizontal.</summary>
    public static class ReglaCubierta
    {
        /// <summary>True si un tirador en esa dirección (desde el jugador) está del otro lado de la cubierta.</summary>
        public static bool Protege(Vector3 haciaTirador, Vector3 normal, float cosenoProteccion)
        {
            haciaTirador.y = 0f;
            normal.y = 0f;
            if (haciaTirador.sqrMagnitude < 0.0001f || normal.sqrMagnitude < 0.0001f)
            {
                return false;
            }
            return Vector3.Dot(haciaTirador.normalized, normal.normalized) <= -cosenoProteccion;
        }

        /// <summary>True si el movimiento se aleja de la cubierta lo bastante para soltarla.</summary>
        public static bool DebeSalir(Vector3 movimiento, Vector3 normal, float cosenoSalida)
        {
            movimiento.y = 0f;
            normal.y = 0f;
            if (movimiento.sqrMagnitude < 0.01f || normal.sqrMagnitude < 0.0001f)
            {
                return false;
            }
            return Vector3.Dot(movimiento.normalized, normal.normalized) >= cosenoSalida;
        }
    }

    /// <summary>Estado puro de la rodada (T-421): duración, cuadros de invulnerabilidad y enfriamiento.</summary>
    public class RodadaAPie
    {
        private float transcurrido = float.MaxValue;
        private float duracion = 1f;
        private float invulnerabilidad;
        private float enfriamiento;

        public bool Activa => transcurrido < duracion;
        public bool Invulnerable => transcurrido < invulnerabilidad;
        public bool Lista => transcurrido >= enfriamiento;
        /// <summary>Avance de la rodada, 0 a 1.</summary>
        public float Progreso => Activa ? transcurrido / duracion : 0f;

        public bool Iniciar(float duracionRodada, float segundosInvulnerable, float segundosEnfriamiento)
        {
            if (!Lista)
            {
                return false;
            }
            duracion = duracionRodada;
            invulnerabilidad = segundosInvulnerable;
            enfriamiento = Mathf.Max(segundosEnfriamiento, duracionRodada);
            transcurrido = 0f;
            return true;
        }

        public void Avanzar(float dt)
        {
            if (transcurrido < float.MaxValue / 2f)
            {
                transcurrido += dt;
            }
        }
    }
}
