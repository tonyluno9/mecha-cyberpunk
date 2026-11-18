using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Los cuatro barrios del mapa (T-747), con el color de cada uno como en M10 (Suburbios naranja, El Mercado ámbar, Neón Alto magenta, Nova azul).
    /// Siguen las zonas del territorio: Suburbios al poniente, El Mercado en la franja central, Neón Alto al nororiente y Nova al suroriente.
    /// </summary>
    public static class BarriosMapa
    {
        public readonly struct Barrio
        {
            public readonly string Nombre;
            public readonly Rect Rectangulo;
            public readonly Color Color;
            /// <summary>Dónde va su nombre grande (x, z): lejos de los iconos de los lugares.</summary>
            public readonly Vector2 PuntoNombre;

            public Barrio(string nombre, Rect rectangulo, Color color, Vector2 puntoNombre)
            {
                Nombre = nombre;
                Rectangulo = rectangulo;
                Color = color;
                PuntoNombre = puntoNombre;
            }
        }

        /// <summary>Lo que se difumina el color entre un barrio y otro (m).</summary>
        public const float Difuminado = 14f;

        // T-1352: los cuatro barrios viejos conservan su cuadro de siempre (el de la imagen ahora es más grande).
        private const float ViejoMinX = -200f;
        private const float ViejoMaxX = 200f;
        private const float ViejoMinZ = -140f;
        private const float ViejoMaxZ = 140f;

        /// <summary>Índice del primer barrio de las zonas nuevas (Malecón, Colonia del Cerro, Puerto, Autopista) en <see cref="Todos"/>.</summary>
        public const int PrimeraZonaNueva = 4;

        public static readonly Barrio[] Todos =
        {
            new Barrio("SUBURBIOS", Rect.MinMaxRect(ViejoMinX, ViejoMinZ, -47f, ViejoMaxZ), new Color(1f, 0.55f, 0.2f), new Vector2(-125f, 120f)),
            new Barrio("EL MERCADO", Rect.MinMaxRect(-47f, ViejoMinZ, 47f, ViejoMaxZ), new Color(1f, 0.78f, 0.25f), new Vector2(0f, 120f)),
            new Barrio("NEÓN ALTO", Rect.MinMaxRect(47f, 0f, ViejoMaxX, ViejoMaxZ), new Color(1f, 0.2f, 0.75f), new Vector2(122f, 120f)),
            new Barrio("NOVA", Rect.MinMaxRect(47f, ViejoMinZ, ViejoMaxX, 0f), new Color(0.25f, 0.55f, 1f), new Vector2(122f, -120f)),
            // Zonas nuevas (docs/plan-zonas-18nov.md). Orden = prioridad si dos rectángulos se traslapan.
            new Barrio("MALECÓN", Rect.MinMaxRect(-330f, -262f, 175f, -205f), new Color(0.15f, 0.5f, 0.9f), new Vector2(-100f, -233f)),
            new Barrio("COLONIA DEL CERRO", Rect.MinMaxRect(-480f, -170f, -295f, 215f), new Color(0.85f, 0.45f, 0.2f), new Vector2(-388f, 100f)),
            new Barrio("PUERTO", Rect.MinMaxRect(115f, -250f, 330f, -45f), new Color(0.1f, 0.7f, 0.6f), new Vector2(222f, -110f)),
            new Barrio("AUTOPISTA", Rect.MinMaxRect(-330f, 235f, 240f, 265f), new Color(0.6f, 0.62f, 0.68f), new Vector2(-45f, 250f)),
        };

        /// <summary>El nombre de la zona nueva que contiene el punto (x, z), o null si no está en ninguna (el HUD lo usa en lugar del barrio del territorio).</summary>
        public static string ZonaNuevaEn(Vector2 mundo)
        {
            for (int i = PrimeraZonaNueva; i < Todos.Length; i++)
            {
                if (Todos[i].Rectangulo.Contains(mundo))
                {
                    return Todos[i].Nombre;
                }
            }
            return null;
        }

        /// <summary>El barrio al que pertenece el punto (x, z): el del rectángulo que lo contiene (las zonas nuevas mandan si se traslapan), o el más cercano.</summary>
        public static int De(Vector2 mundo)
        {
            int mejor = 0;
            float menor = float.MaxValue;
            for (int paso = 0; paso < Todos.Length; paso++)
            {
                int i = (paso + PrimeraZonaNueva) % Todos.Length;
                Rect r = Todos[i].Rectangulo;
                float dx = Mathf.Max(r.xMin - mundo.x, 0f, mundo.x - r.xMax);
                float dy = Mathf.Max(r.yMin - mundo.y, 0f, mundo.y - r.yMax);
                float d = dx * dx + dy * dy;
                if (d < menor)
                {
                    menor = d;
                    mejor = i;
                }
            }
            return mejor;
        }

        /// <summary>El color del punto: el del barrio, mezclado con el vecino cerca del borde.</summary>
        public static Color ColorEn(Vector2 mundo)
        {
            Color suma = new Color(0f, 0f, 0f, 0f);
            float pesos = 0f;
            for (int i = 0; i < Todos.Length; i++)
            {
                Rect r = Todos[i].Rectangulo;
                float dentro = Mathf.Min(Mathf.Min(mundo.x - r.xMin, r.xMax - mundo.x), Mathf.Min(mundo.y - r.yMin, r.yMax - mundo.y));
                float peso = Mathf.Clamp01(dentro / Difuminado + 0.5f);
                if (peso > 0f)
                {
                    suma += Todos[i].Color * peso;
                    pesos += peso;
                }
            }
            return pesos > 0f ? suma / pesos : Todos[De(mundo)].Color;
        }
    }
}
