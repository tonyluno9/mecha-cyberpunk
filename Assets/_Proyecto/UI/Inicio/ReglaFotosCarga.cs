using UnityEngine;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// T-1316: la regla de las fotos de la pantalla de carga: cuál se ve, cuál entra con fundido, y su zoom y paneo lentos.
    /// La foto siempre cubre la pantalla: el paneo nunca pasa del margen que deja el zoom.
    /// </summary>
    public static class ReglaFotosCarga
    {
        public const float SegundosPorFoto = 6.5f;
        public const float SegundosFundido = 1.3f;
        /// <summary>T-1330: sin zoom ni paneo: la foto se ve entera, con su proporción, sin recortar (bordes negros si hace falta).</summary>
        public const float EscalaInicial = 1f;
        public const float EscalaFinal = 1f;
        public const float PaneoMaximo = 0f;
        private const float ProporcionVertical = 0.6f;
        private const float GiroPorFoto = 2.4f;

        public struct Estado
        {
            public int Actual;
            public float TiempoActual;
            public float AlfaActual;
            public int Anterior;
            public float TiempoAnterior;
        }

        /// <summary>Qué fotos se ven a los <paramref name="t"/> segundos, con <paramref name="cantidad"/> fotos y empezando en <paramref name="primera"/>.</summary>
        public static Estado Calcular(float t, int cantidad, int primera)
        {
            Estado e = new Estado { Actual = -1, Anterior = -1, AlfaActual = 1f };
            if (cantidad <= 0)
            {
                return e;
            }
            t = Mathf.Max(0f, t);
            int vuelta = Mathf.FloorToInt(t / SegundosPorFoto);
            float local = t - vuelta * SegundosPorFoto;
            e.Actual = Mod(primera + vuelta, cantidad);
            e.TiempoActual = local;
            if (vuelta > 0 && local < SegundosFundido && cantidad > 1)
            {
                e.Anterior = Mod(primera + vuelta - 1, cantidad);
                e.TiempoAnterior = local + SegundosPorFoto;
                e.AlfaActual = Mathf.SmoothStep(0f, 1f, local / SegundosFundido);
            }
            return e;
        }

        /// <summary>Zoom de una foto que lleva <paramref name="t"/> segundos en pantalla.</summary>
        /// <summary>T-1330: tamaño de una foto de <paramref name="ancho"/> x <paramref name="alto"/> px que cabe entera en la pantalla (FitInParent), sin estirar.</summary>
        public static Vector2 TamanoEncajado(float ancho, float alto, float anchoPantalla, float altoPantalla)
        {
            float aspecto = ancho / alto;
            return aspecto > anchoPantalla / altoPantalla
                ? new Vector2(anchoPantalla, anchoPantalla / aspecto)
                : new Vector2(altoPantalla * aspecto, altoPantalla);
        }

        /// <summary>
        /// Carga minimalista (Tony): tamaño de una foto de <paramref name="ancho"/> x <paramref name="alto"/> px que llena la pantalla
        /// conservando la proporción (Envelope: lo que sobra se recorta por el lado más corto, lo mínimo posible). Nunca estira.
        /// </summary>
        public static Vector2 TamanoCubierto(float ancho, float alto, float anchoPantalla, float altoPantalla)
        {
            float aspecto = ancho / alto;
            return aspecto > anchoPantalla / altoPantalla
                ? new Vector2(altoPantalla * aspecto, altoPantalla)
                : new Vector2(anchoPantalla, anchoPantalla / aspecto);
        }

        /// <summary>Lo más que se recorta de la foto (fracción del lado recortado) para llenar la pantalla; si haría falta más, bordes negros.</summary>
        public const float RecorteMaximo = 0.12f;

        /// <summary>
        /// Tamaño de la foto en la pantalla de carga: la llena recortando lo mínimo (<see cref="TamanoCubierto"/>) si así se recorta
        /// hasta <see cref="RecorteMaximo"/>; en pantallas más cuadradas (4:3, 5:4) se ve entera con bordes negros (<see cref="TamanoEncajado"/>).
        /// Nunca se deforma.
        /// </summary>
        public static Vector2 TamanoEnPantalla(float ancho, float alto, float anchoPantalla, float altoPantalla)
        {
            Vector2 cubierto = TamanoCubierto(ancho, alto, anchoPantalla, altoPantalla);
            float recorte = Mathf.Max(1f - anchoPantalla / cubierto.x, 1f - altoPantalla / cubierto.y);
            return recorte <= RecorteMaximo ? cubierto : TamanoEncajado(ancho, alto, anchoPantalla, altoPantalla);
        }

        public static float Escala(float t) => Mathf.Lerp(EscalaInicial, EscalaFinal, Progreso(t));

        /// <summary>Desplazamiento (px) de la foto <paramref name="indice"/> a los <paramref name="t"/> segundos: cruza de un lado al otro en una dirección propia.</summary>
        public static Vector2 Paneo(int indice, float t)
        {
            float angulo = indice * GiroPorFoto;
            Vector2 direccion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo) * ProporcionVertical);
            return direccion * Mathf.Lerp(-PaneoMaximo, PaneoMaximo, Progreso(t));
        }

        private static float Progreso(float t) => Mathf.Clamp01(t / (SegundosPorFoto + SegundosFundido));

        private static int Mod(int a, int n) => ((a % n) + n) % n;
    }
}
