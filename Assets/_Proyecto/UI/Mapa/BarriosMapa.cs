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

        public static readonly Barrio[] Todos =
        {
            new Barrio("SUBURBIOS", Rect.MinMaxRect(MapaImagen.MinX, MapaImagen.MinZ, -47f, MapaImagen.MaxZ), new Color(1f, 0.55f, 0.2f), new Vector2(-125f, 120f)),
            new Barrio("EL MERCADO", Rect.MinMaxRect(-47f, MapaImagen.MinZ, 47f, MapaImagen.MaxZ), new Color(1f, 0.78f, 0.25f), new Vector2(0f, 120f)),
            new Barrio("NEÓN ALTO", Rect.MinMaxRect(47f, 0f, MapaImagen.MaxX, MapaImagen.MaxZ), new Color(1f, 0.2f, 0.75f), new Vector2(122f, 120f)),
            new Barrio("NOVA", Rect.MinMaxRect(47f, MapaImagen.MinZ, MapaImagen.MaxX, 0f), new Color(0.25f, 0.55f, 1f), new Vector2(122f, -120f)),
        };

        /// <summary>El barrio al que pertenece el punto (x, z): el del rectángulo que lo contiene, o el más cercano.</summary>
        public static int De(Vector2 mundo)
        {
            int mejor = 0;
            float menor = float.MaxValue;
            for (int i = 0; i < Todos.Length; i++)
            {
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
