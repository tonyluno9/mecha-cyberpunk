using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// Lista compartida de puntos de vista de la medición de FPS (T-1366): la usan el medidor del build y el diagnóstico del editor.
    /// Ajusta las coordenadas aquí. Mapa: x este, z norte; Malecón al sur (z -205..-262), Cerro al oeste (x -295..-480), Autopista z 250 y 10 m,
    /// Puerto al sureste (x 115..330, z -45..-250). Las posiciones son aproximadas.
    /// </summary>
    public static class VistasFpsT1366
    {
        public static readonly IReadOnlyList<VistaFpsT1366> Todas = new[]
        {
            new VistaFpsT1366("Malecon", "paseo", -120f, -235f, 60f, -235f),
            new VistaFpsT1366("Malecon", "avenida", -47f, -205f, -47f, -262f),
            new VistaFpsT1366("Malecon", "muelle", 100f, -250f, 20f, -250f),
            new VistaFpsT1366("Malecon", "aerea", -60f, -230f, -60f, -200f, 70f, 0f),

            new VistaFpsT1366("Cerro", "calle_base", -305f, -100f, -305f, 0f),
            new VistaFpsT1366("Cerro", "zigzag_medio", -360f, 100f, -360f, 175f),
            new VistaFpsT1366("Cerro", "plaza_arriba", -400f, 190f, -350f, 190f),
            new VistaFpsT1366("Cerro", "aerea", -380f, 0f, -380f, 60f, 80f, 0f),

            new VistaFpsT1366("Autopista", "tablero", 0f, 250f, 120f, 250f),
            new VistaFpsT1366("Autopista", "tunel", -335f, 235f, -360f, 185f),
            new VistaFpsT1366("Autopista", "calle_baja", 0f, 250f, 80f, 250f, 1.7f, 1.7f, 5f),

            new VistaFpsT1366("Puerto", "pasillo_contenedores", 230f, -90f, 230f, -200f),
            new VistaFpsT1366("Puerto", "muelle", 300f, -248f, 200f, -248f),
            new VistaFpsT1366("Puerto", "aerea", 230f, -90f, 230f, -170f, 70f, 0f),

            new VistaFpsT1366("Barrio", "Suburbios", -47f, 0f, -100f, 0f),
            new VistaFpsT1366("Barrio", "Mercado", 0f, 0f, 47f, 0f),
            new VistaFpsT1366("Barrio", "NeonAlto", 47f, 40f, 47f, 88f),
        };

        /// <summary>
        /// Posición de la cámara y punto al que mira, con la altura sacada del suelo bajo cada punto
        /// (<paramref name="alturaSuelo"/> recibe x, z y la y de inicio del rayo y devuelve la y del suelo, o NaN si no hay).
        /// </summary>
        public static void Resolver(in VistaFpsT1366 vista, Func<float, float, float, float> alturaSuelo, out Vector3 posicion, out Vector3 mirar)
        {
            float sueloCamara = SueloONulo(alturaSuelo(vista.X, vista.Z, vista.RayoDesdeY));
            float sueloMirar = SueloONulo(alturaSuelo(vista.MirarX, vista.MirarZ, vista.RayoDesdeY));
            posicion = new Vector3(vista.X, sueloCamara + vista.SobreSuelo, vista.Z);
            mirar = new Vector3(vista.MirarX, sueloMirar + vista.MirarSobreSuelo, vista.MirarZ);
        }

        private static float SueloONulo(float y) => float.IsNaN(y) ? 0f : y;

        /// <summary>Rayo hacia abajo con la física; NaN si no pega en nada.</summary>
        public static float AlturaConFisica(float x, float z, float desdeY)
        {
            return Physics.Raycast(new Vector3(x, desdeY, z), Vector3.down, out RaycastHit golpe, desdeY + 20f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? golpe.point.y
                : float.NaN;
        }
    }
}
