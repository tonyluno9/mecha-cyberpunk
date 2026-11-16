using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T-1006: limita qué tan rápido puede cambiar el volante de una patrulla, para que no tiemble ni salte de un lado al otro entre cuadros.</summary>
    public static class ReglaSuavizadoVolante
    {
        /// <summary>El volante (-1 a 1) después de <paramref name="dt"/> s intentando llegar a <paramref name="deseado"/>, sin cambiar más de <paramref name="velocidadMaxima"/> por segundo.</summary>
        public static float Seguir(float actual, float deseado, float velocidadMaxima, float dt)
        {
            float meta = Mathf.Clamp(deseado, -1f, 1f);
            return Mathf.MoveTowards(actual, meta, Mathf.Max(0f, velocidadMaxima) * Mathf.Max(0f, dt));
        }
    }
}
