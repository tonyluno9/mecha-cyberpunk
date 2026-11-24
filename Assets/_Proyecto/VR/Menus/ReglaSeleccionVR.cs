using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR44, lógica pura: a qué botón de un panel va el láser. Lo que toca de verdad gana; si no toca un botón, el más cercano dentro de
    /// <see cref="RadioImanPx"/> (magnetismo: la zona de clic de cada botón es más grande que el botón). Para que no tiemble entre dos botones
    /// juntos, el que ya estaba elegido se queda mientras no haya otro claramente más cerca (<see cref="HisteresisPx"/>).
    /// Las distancias van en px de la pantalla de referencia (1920 x 1080).
    /// </summary>
    public static class ReglaSeleccionVR
    {
        /// <summary>Hasta qué distancia del borde de un botón el láser lo elige (px de referencia; ~4 cm en el panel a 1.5 m).</summary>
        public const float RadioImanPx = 48f;
        /// <summary>Cuánto más cerca tiene que estar otro botón para quitarle la elección al de ahora (px de referencia).</summary>
        public const float HisteresisPx = 14f;
        /// <summary>Un "botón" más grande que esta fracción de la pantalla es un fondo (cerrar al tocar fuera, listas): no atrae al láser.</summary>
        public const float FraccionAreaMaxima = 0.3f;

        /// <summary>Un botón candidato: su distancia al punto del láser (px, 0 dentro) y su orden de dibujo (más alto = encima).</summary>
        public struct Candidato
        {
            public float DistanciaPx;
            public int Orden;
            public int Profundidad;
            public float AreaPx;

            public Candidato(float distanciaPx, int orden, int profundidad, float areaPx)
            {
                DistanciaPx = distanciaPx;
                Orden = orden;
                Profundidad = profundidad;
                AreaPx = areaPx;
            }
        }

        /// <summary>Distancia (px) de un punto a un rectángulo; 0 si está dentro.</summary>
        public static float DistanciaARect(Vector2 punto, Rect rect)
        {
            float dx = Mathf.Max(rect.xMin - punto.x, 0f, punto.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - punto.y, 0f, punto.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>True si un botón de este tamaño (px²) es tan grande que cuenta como fondo.</summary>
        public static bool EsFondo(float areaPx)
        {
            return areaPx > FraccionAreaMaxima * ReglaLienzosVR.AnchoReferencia * ReglaLienzosVR.AltoReferencia;
        }

        /// <summary>
        /// El índice del botón elegido, o -1. <paramref name="directo"/>: el que toca el láser de verdad (-1 si ninguno);
        /// <paramref name="actual"/>: el que estaba elegido el cuadro anterior (-1 si ninguno o ya no está).
        /// </summary>
        public static int Elegir(IReadOnlyList<Candidato> candidatos, int directo, int actual)
        {
            if (directo >= 0 && directo < candidatos.Count)
            {
                return directo;
            }
            int mejor = -1;
            for (int i = 0; i < candidatos.Count; i++)
            {
                Candidato c = candidatos[i];
                if (c.DistanciaPx > RadioImanPx || EsFondo(c.AreaPx))
                {
                    continue;
                }
                if (mejor < 0 || Mejor(c, candidatos[mejor]))
                {
                    mejor = i;
                }
            }
            if (actual >= 0 && actual < candidatos.Count && actual != mejor)
            {
                Candidato a = candidatos[actual];
                bool sigueCerca = a.DistanciaPx <= RadioImanPx && !EsFondo(a.AreaPx);
                if (sigueCerca && (mejor < 0 || a.DistanciaPx <= candidatos[mejor].DistanciaPx + HisteresisPx))
                {
                    return actual;
                }
            }
            return mejor;
        }

        // Más cerca gana; a la misma distancia (los dos contienen el punto), el que va encima y luego el más chico.
        private static bool Mejor(Candidato a, Candidato b)
        {
            if (!Mathf.Approximately(a.DistanciaPx, b.DistanciaPx))
            {
                return a.DistanciaPx < b.DistanciaPx;
            }
            if (a.Orden != b.Orden)
            {
                return a.Orden > b.Orden;
            }
            if (a.Profundidad != b.Profundidad)
            {
                return a.Profundidad > b.Profundidad;
            }
            return a.AreaPx < b.AreaPx;
        }
    }
}
