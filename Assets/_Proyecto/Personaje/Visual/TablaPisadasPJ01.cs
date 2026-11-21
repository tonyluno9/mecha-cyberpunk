using System;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1534: tabla de pisadas de los clips PJ01 de la BITACORA de Blender (pedido 19-nov noche): fotograma del archivo (1 a <c>Cuadros</c>, 30 fps) en que toca el
    /// pie izquierdo y el derecho. El izquierdo cae en el fotograma 1 (= el último: bucle perfecto). Pura: sin Unity.
    /// Los seis clips de locomoción ya traen estos eventos en su importación (ImportadorClipsPJ01.ClipsConPisadas); los demás (caminar apuntando y escaleras) no, y
    /// <see cref="ReceptorPasosPJ01"/> dispara sus pasos con el tiempo normalizado del estado del Animator, sin modificar ningún clip ni FBX.
    /// </summary>
    public static class TablaPisadasPJ01
    {
        public readonly struct Pisadas
        {
            public readonly string Clip;
            public readonly int Cuadros;
            public readonly int Izquierdo;
            public readonly int Derecho;
            /// <summary>True si la importación ya agrega los eventos de pisada al clip (no se duplican por tiempo).</summary>
            public readonly bool ConEventos;

            public Pisadas(string clip, int cuadros, int izquierdo, int derecho, bool conEventos)
            {
                Clip = clip;
                Cuadros = cuadros;
                Izquierdo = izquierdo;
                Derecho = derecho;
                ConEventos = conEventos;
            }

            /// <summary>Fase (0 a 1 dentro de un ciclo) del pie izquierdo; 0 = cuadro 1.</summary>
            public float FaseIzquierdo => Fase(Izquierdo);

            /// <summary>Fase (0 a 1 dentro de un ciclo) del pie derecho.</summary>
            public float FaseDerecho => Fase(Derecho);

            private float Fase(int cuadro) => Cuadros <= 1 ? 0f : Math.Max(0f, (cuadro - 1f) / (Cuadros - 1f));
        }

        public const string Prefijo = "PJ01_";

        public static readonly Pisadas[] Todas =
        {
            new Pisadas("Caminar", 31, 1, 16, true),
            new Pisadas("Trote", 21, 1, 11, true),
            new Pisadas("Sprint", 19, 1, 10, true),
            new Pisadas("Trote_Izquierda", 21, 1, 11, true),
            new Pisadas("Trote_Derecha", 21, 1, 11, true),
            new Pisadas("Trote_Atras", 21, 1, 11, true),
            new Pisadas("CaminarApuntando_Adelante", 27, 1, 14, false),
            new Pisadas("CaminarApuntando_Atras", 27, 1, 14, false),
            new Pisadas("CaminarApuntando_Izquierda", 23, 1, 12, false),
            new Pisadas("CaminarApuntando_Derecha", 23, 1, 12, false),
            new Pisadas("Escaleras_Subir", 37, 1, 19, false),
            new Pisadas("Escaleras_Bajar", 35, 1, 18, false),
        };

        /// <summary>Las pisadas del clip por su nombre ("PJ01_Caminar" o "Caminar"); false si el clip no lleva.</summary>
        public static bool Buscar(string nombreClip, out Pisadas pisadas)
        {
            pisadas = default;
            if (string.IsNullOrEmpty(nombreClip))
            {
                return false;
            }
            string baseNombre = nombreClip.StartsWith(Prefijo, StringComparison.Ordinal) ? nombreClip.Substring(Prefijo.Length) : nombreClip;
            foreach (Pisadas p in Todas)
            {
                if (p.Clip == baseNombre)
                {
                    pisadas = p;
                    return true;
                }
            }
            return false;
        }
    }
}
