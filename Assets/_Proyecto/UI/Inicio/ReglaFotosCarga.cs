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
        public const float EscalaInicial = 1.06f;
        public const float EscalaFinal = 1.16f;
        /// <summary>Paneo máximo (px de 1920x1080) hacia cada lado; cabe en el margen del zoom inicial (3 % de 1080 = 32 px en vertical).</summary>
        public const float PaneoMaximo = 44f;
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
