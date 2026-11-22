using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>Lógica pura de la música del menú: fundidos y cuándo empieza el cruce del bucle.</summary>
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

        /// <summary>
        /// True cuando a la pista le quedan <paramref name="segundosCruce"/> o menos: ahí arranca la otra copia desde el inicio (el archivo
        /// no empalma limpio: termina más bajo de lo que empieza, así que el bucle se hace con un cruce suave, no con loop).
        /// </summary>
        public static bool TocaCruce(float tiempo, float largo, float segundosCruce)
        {
            return largo > segundosCruce * 2f && largo - tiempo <= segundosCruce;
        }
    }
}
