using UnityEngine;

namespace MechaCyberpunk.Mecha.Cabina
{
    /// <summary>Cuentas de la sacudida de la cabina, sin MonoBehaviour para poder probarlas.</summary>
    public static class CalculoSacudida
    {
        /// <summary>Intensidad inicial según el daño aplicado: crece de la mínima a la máxima hasta el daño de referencia.</summary>
        public static float IntensidadInicial(float danio, float danioReferencia, float intensidadMinima, float intensidadMaxima)
        {
            float proporcion = danioReferencia > 0f ? Mathf.Clamp01(Mathf.Max(danio, 0f) / danioReferencia) : 1f;
            return Mathf.Lerp(intensidadMinima, intensidadMaxima, proporcion);
        }

        /// <summary>
        /// Intensidad después de <paramref name="dt"/> segundos. Baja lineal y la máxima tarda <paramref name="duracion"/>
        /// en apagarse; nunca queda negativa.
        /// </summary>
        public static float Decaer(float intensidad, float dt, float duracion, float intensidadMaxima)
        {
            if (dt <= 0f || duracion <= 0f)
            {
                return Mathf.Max(intensidad, 0f);
            }
            return Mathf.Max(intensidad - intensidadMaxima / duracion * dt, 0f);
        }
    }
}
