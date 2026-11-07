using MechaCyberpunk.UI.Mapa;
using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Lógica pura del nombre de zona de arriba al centro (T-701): cuánto se ve y cómo se desvanece.</summary>
    public static class ReglaTituloZonaH10
    {
        public const string NombreCiudad = PantallaMapa.NombreCiudad;
        public const float SegundosVisible = 4f;
        public const float SegundosDesvanecer = 1.2f;

        /// <summary>"MEGALOPOLIS · SUBURBIOS" a partir del nombre de la zona.</summary>
        public static string Texto(string nombreZona) => NombreCiudad + " · " + nombreZona.ToUpperInvariant();

        /// <summary>True si el aviso es el nombre de una zona (empieza con la ciudad y un punto medio).</summary>
        public static bool EsTituloDeZona(string texto) => !string.IsNullOrEmpty(texto) && texto.StartsWith(NombreCiudad + " ·", System.StringComparison.Ordinal);

        /// <summary>Opacidad a los <paramref name="segundos"/> de haber salido: llena, y en el último tramo se desvanece.</summary>
        public static float Alfa(float segundos, float duracion)
        {
            if (segundos < 0f || segundos >= duracion)
            {
                return 0f;
            }
            float restante = duracion - segundos;
            return Mathf.Clamp01(restante / SegundosDesvanecer);
        }
    }
}
