using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1324: lógica pura del daño del rayo de plasma: mata a la gente de un toque, destruye objetos y atraviesa varios colliders.</summary>
    public static class ReglaDanioRayo
    {
        /// <summary>True si el rayo sigue después de haber atravesado <paramref name="atravesadas"/> paredes u objetos, con un máximo de <paramref name="maximo"/>.</summary>
        public static bool AtraviesaOtra(int atravesadas, int maximo)
        {
            return atravesadas < maximo;
        }

        /// <summary>
        /// Daño de un tic del rayo. Una persona recibe al menos su vida actual por <paramref name="factorMataGente"/> (un toque la mata);
        /// un objeto o carro recibe <paramref name="multiplicadorObjetos"/> veces el daño.
        /// </summary>
        public static float Calcular(float danioDelTic, bool esObjeto, float vidaActual, float multiplicadorObjetos, float factorMataGente)
        {
            if (esObjeto)
            {
                return danioDelTic * Mathf.Max(1f, multiplicadorObjetos);
            }
            return Mathf.Max(danioDelTic, Mathf.Max(0f, vidaActual) * Mathf.Max(1f, factorMataGente));
        }
    }
}
