using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>Lógica pura de la música del menú: fundidos (el bucle es loop normal: Tony hizo el tema para empalmar sin clic).</summary>
    public static class ReglaMusicaMenu
    {
        /// <summary>Avanza un nivel (0 a 1) hacia <paramref name="objetivo"/> a ritmo constante: de 0 a 1 en <paramref name="segundos"/>.</summary>
        public static float Fundir(float actual, float objetivo, float dt, float segundos)
        {
            if (segundos <= 0f)
            {
                return objetivo;
            }
            return Mathf.MoveTowards(actual, objetivo, dt / segundos);
        }
    }
}
