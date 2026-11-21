using System;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1534: dice qué pies pisaron entre dos lecturas del tiempo normalizado de un estado en bucle (continuo: 0 a 1 en la primera vuelta, 1 a 2 en la segunda...).
    /// Un pie pisa al cruzar su fase dentro del ciclo. Pura: sin Unity. Un salto de más de una vuelta o hacia atrás no dispara nada (cambio de estado o de clip).
    /// </summary>
    public sealed class DetectorPisadasPorTiempo
    {
        private float anterior = float.NaN;

        /// <summary>Olvida la lectura anterior (al cambiar de clip o de estado).</summary>
        public void Reiniciar() => anterior = float.NaN;

        /// <summary>
        /// Lee el tiempo normalizado y devuelve los pies que pisaron desde la lectura anterior: bit 0 el izquierdo, bit 1 el derecho (0 si ninguno).
        /// La primera lectura solo fija el punto de partida.
        /// </summary>
        public int Avanzar(float tiempoNormalizado, float faseIzquierdo, float faseDerecho)
        {
            if (float.IsNaN(anterior))
            {
                anterior = tiempoNormalizado;
                return 0;
            }
            float ant = anterior;
            anterior = tiempoNormalizado;
            if (tiempoNormalizado <= ant || tiempoNormalizado - ant >= 1f)
            {
                return 0;
            }
            int pies = 0;
            if (Cruza(ant, tiempoNormalizado, faseIzquierdo)) pies |= 1;
            if (Cruza(ant, tiempoNormalizado, faseDerecho)) pies |= 2;
            return pies;
        }

        // Hay una fase (fase + k) en (desde, hasta].
        private static bool Cruza(float desde, float hasta, float fase)
        {
            return Math.Floor(hasta - fase) > Math.Floor(desde - fase);
        }
    }
}
