using UnityEngine;

namespace MechaCyberpunk.UI.Voces
{
    /// <summary>T-1624 (T15): reglas puras de los subtítulos del mundo: cuánto duran y cuándo se ven.</summary>
    public static class ReglaSubtitulos
    {
        /// <summary>Distancia máxima (m) del que habla al jugador para verlo subtitulado.</summary>
        public const float DistanciaMaxima = 15f;
        public const float SegundosBase = 1.2f;
        public const float SegundosPorLetra = 0.055f;
        public const float SegundosMinimos = 1.8f;
        public const float SegundosMaximos = 7f;
        /// <summary>Margen fuera de la pantalla (fracción del viewport) que aún cuenta como a la vista.</summary>
        public const float MargenVista = 0.05f;

        /// <summary>Segundos en pantalla: la duración pedida si es mayor que 0; si no, según el largo del texto.</summary>
        public static float Duracion(float pedida, string texto)
        {
            if (pedida > 0f)
            {
                return pedida;
            }
            int letras = string.IsNullOrEmpty(texto) ? 0 : texto.Length;
            return Mathf.Clamp(SegundosBase + letras * SegundosPorLetra, SegundosMinimos, SegundosMaximos);
        }

        /// <summary>True si se debe mostrar: opción encendida, a menos de <see cref="DistanciaMaxima"/> y dentro de la pantalla (delante de la cámara).</summary>
        public static bool DebeVerse(bool activado, float distancia, Vector3 viewport)
        {
            if (!activado || distancia > DistanciaMaxima || viewport.z <= 0f)
            {
                return false;
            }
            return viewport.x >= -MargenVista && viewport.x <= 1f + MargenVista && viewport.y >= -MargenVista && viewport.y <= 1f + MargenVista;
        }
    }
}
