using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// La imagen cenital de la ciudad real (T-747) que usan el mapa grande y el minimapa, y las cuentas para pasar del mundo (m: x este, z norte) a
    /// la imagen y de vuelta. La imagen la genera CapturadorMapaCiudad (editor) con una cámara ortográfica sobre Ciudad_Prologo: cubre
    /// exactamente el rectángulo <see cref="Minimo"/>..<see cref="Maximo"/> del mundo, con el norte arriba. Sin Unity vivo.
    /// </summary>
    public static class MapaImagen
    {
        public const string RutaRecurso = "Mapa/MapaCiudad";
        public const float MinX = -200f;
        public const float MaxX = 200f;
        public const float MinZ = -140f;
        public const float MaxZ = 140f;
        /// <summary>Píxeles de la imagen por metro del mundo.</summary>
        public const int PixelesPorMetro = 6;
        /// <summary>Píxeles de pantalla (1920x1080) por metro con el zoom del mapa grande en 1: la imagen entera llena la vista de 800x560.</summary>
        public const float PixelesVistaPorMetro = 2f;

        public static Vector2 Minimo => new Vector2(MinX, MinZ);
        public static Vector2 Maximo => new Vector2(MaxX, MaxZ);
        /// <summary>Medidas del cuadro (m).</summary>
        public static Vector2 Tamano => new Vector2(MaxX - MinX, MaxZ - MinZ);
        /// <summary>Centro del cuadro (m).</summary>
        public static Vector2 Centro => (Minimo + Maximo) * 0.5f;
        public static int AnchoPx => Mathf.RoundToInt((MaxX - MinX) * PixelesPorMetro);
        public static int AltoPx => Mathf.RoundToInt((MaxZ - MinZ) * PixelesPorMetro);

        /// <summary>De un punto del mundo (x, z) a píxel de la imagen (origen abajo a la izquierda, y hacia el norte).</summary>
        public static Vector2 MundoAPixel(Vector2 mundo) => (mundo - Minimo) * PixelesPorMetro;

        /// <summary>De un píxel de la imagen al punto del mundo (x, z).</summary>
        public static Vector2 PixelAMundo(Vector2 pixel) => pixel / PixelesPorMetro + Minimo;

        /// <summary>De un punto del mundo a coordenada de textura 0..1 (u hacia el este, v hacia el norte).</summary>
        public static Vector2 MundoAUv(Vector2 mundo) => new Vector2((mundo.x - MinX) / (MaxX - MinX), (mundo.y - MinZ) / (MaxZ - MinZ));

        /// <summary>Si el punto cae dentro del cuadro de la imagen.</summary>
        public static bool Contiene(Vector2 mundo) => mundo.x >= MinX && mundo.x <= MaxX && mundo.y >= MinZ && mundo.y <= MaxZ;

        /// <summary>
        /// Dónde queda el punto del mundo en un dibujo del mapa cuyo centro es el del cuadro y que mide <paramref name="pixelesPorMetro"/> px por metro
        /// (px desde el centro del dibujo). Es la misma cuenta que usan los iconos y el jugador, así que caen sobre la imagen.
        /// </summary>
        public static Vector2 MundoADibujo(Vector2 mundo, float pixelesPorMetro) => (mundo - Centro) * pixelesPorMetro;

        /// <summary>La imagen del recurso, o null si aún no se generó.</summary>
        public static Texture2D Cargar() => Resources.Load<Texture2D>(RutaRecurso);
    }
}
