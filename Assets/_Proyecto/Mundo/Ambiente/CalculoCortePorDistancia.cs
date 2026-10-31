using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-425: cuenta de la altura relativa en pantalla a la que un objeto chico deja de dibujarse (LODGroup con un solo nivel).
    /// Un objeto de <c>tamanio</c> metros ocupa tamanio / (2 · d · tan(fov/2)) de la altura de la pantalla a d metros.
    /// </summary>
    public static class CalculoCortePorDistancia
    {
        /// <summary>Altura relativa (0..1) bajo la cual se corta el objeto para que desaparezca a <paramref name="distancia"/> metros.</summary>
        public static float AlturaDeCorte(float tamanio, float distancia, float campoVisionVertical)
        {
            if (tamanio <= 0f || distancia <= 0f)
            {
                return 0f;
            }
            float mitad = Mathf.Tan(Mathf.Clamp(campoVisionVertical, 1f, 170f) * 0.5f * Mathf.Deg2Rad);
            return Mathf.Clamp01(tamanio / (2f * distancia * mitad));
        }

        /// <summary>True si el objeto es lo bastante chico para cortarse por distancia.</summary>
        public static bool EsChico(float tamanioMayor, float tamanioMaximo)
        {
            return tamanioMayor > 0f && tamanioMayor <= tamanioMaximo;
        }
    }
}
