using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>T-1485: cuentas puras de la cámara a pie (FOV al correr, intensidad del balanceo). Sin MonoBehaviour: se prueban en EditMode.</summary>
    public static class LogicaCamaraAPie
    {
        /// <summary>De 0 a 1: qué tan rápido va el jugador entre <paramref name="inicio"/> y <paramref name="fin"/> (m/s). 0 si fin &lt;= inicio y no llega.</summary>
        public static float FactorVelocidad(float velocidad, float inicio, float fin)
        {
            if (fin <= inicio)
            {
                return velocidad >= fin ? 1f : 0f;
            }
            return Mathf.Clamp01((velocidad - inicio) / (fin - inicio));
        }

        /// <summary>FOV objetivo (°): al apuntar manda el FOV de apuntar (sin extra); si no, el normal más el extra de correr por el factor (0-1).</summary>
        public static float FovObjetivo(float fovNormal, float fovApuntando, float extraCorrer, float factorCorrer, bool apuntando)
        {
            return apuntando ? fovApuntando : fovNormal + extraCorrer * Mathf.Clamp01(factorCorrer);
        }

        /// <summary>Suavizado exponencial independiente de los cuadros por segundo.</summary>
        public static float Suavizar(float actual, float objetivo, float rapidez, float dt)
        {
            return Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-Mathf.Max(0f, rapidez) * Mathf.Max(0f, dt)));
        }
    }
}
