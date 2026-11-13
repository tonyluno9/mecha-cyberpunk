using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Lógica pura del agarre de bordes: decide si se puede, cuánto dura y por dónde pasa el jugador.</summary>
    public static class CalculoAgarre
    {
        /// <summary>Parte del tiempo que se usa en subir hasta la altura del borde; el resto avanza hacia la posición final.</summary>
        public const float FraccionSubida = 0.6f;

        public static float Segundos(float alturaBorde, float segundosBase, float segundosPorMetro)
        {
            return segundosBase + segundosPorMetro * Mathf.Max(0f, alturaBorde);
        }

        /// <summary>El borde se agarra si está entre la altura mínima y la máxima, la superficie es de pararse, y hay espacio y apoyo arriba.</summary>
        public static ResultadoAgarre Evaluar(float alturaBorde, bool hayEspacio, bool hayApoyo, float normalY,
            float alturaMinima, float alturaMaxima, float normalMinima, float segundosBase, float segundosPorMetro)
        {
            bool valido = alturaBorde >= alturaMinima && alturaBorde <= alturaMaxima
                && normalY >= normalMinima && hayEspacio && hayApoyo;
            return new ResultadoAgarre(valido, valido ? Segundos(alturaBorde, segundosBase, segundosPorMetro) : 0f);
        }

        /// <summary>Posición en el trayecto de dos fases: primero sube en vertical hasta la altura final y luego avanza al destino.</summary>
        public static Vector3 Posicion(Vector3 inicio, Vector3 final, float progreso)
        {
            float t = Mathf.Clamp01(progreso);
            Vector3 intermedio = new Vector3(inicio.x, final.y, inicio.z);
            if (t < FraccionSubida)
            {
                return Vector3.Lerp(inicio, intermedio, Mathf.SmoothStep(0f, 1f, t / FraccionSubida));
            }
            return Vector3.Lerp(intermedio, final, Mathf.SmoothStep(0f, 1f, (t - FraccionSubida) / (1f - FraccionSubida)));
        }
    }
}
