using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Lógica pura del marcador del objetivo (T-408, D-tony-240). Recibe el punto en coordenadas de vista de la cámara
    /// (<c>Camera.WorldToViewportPoint</c>: x e y de 0 a 1, z = profundidad en m) y el tamaño del lienzo (px de referencia):
    /// si el punto está enfrente y dentro de la pantalla, el rombo va donde se proyecta; si no (fuera o detrás), una flecha en el borde,
    /// a <c>margen</c> px, apunta hacia él. Un punto detrás nunca sale en el centro: se refleja para que la flecha quede del lado correcto,
    /// a media altura del borde izquierdo o derecho (justo detrás, abajo).
    /// </summary>
    public static class CalculoMarcador
    {
        private const float CasiCero = 0.0001f;

        public readonly struct Resultado
        {
            /// <summary>True si el rombo se ve (enfrente y dentro de la pantalla).</summary>
            public bool EnPantalla { get; }

            /// <summary>Posición (px del lienzo, desde abajo a la izquierda) del rombo o de la flecha.</summary>
            public Vector2 Posicion { get; }

            /// <summary>Hacia dónde apunta la flecha (°, 0 = derecha, 90 = arriba). Solo vale fuera de la pantalla.</summary>
            public float Angulo { get; }

            public Resultado(bool enPantalla, Vector2 posicion, float angulo)
            {
                EnPantalla = enPantalla;
                Posicion = posicion;
                Angulo = angulo;
            }
        }

        /// <summary>"3 m", "45 m" o "1.2 km".</summary>
        public static string TextoDistancia(float metros) => CalculoFlechaObjetivo.TextoDistancia(metros);

        public static Resultado Calcular(Vector3 puntoVista, Vector2 lienzo, float margen)
        {
            bool delante = puntoVista.z > CasiCero;
            if (delante && puntoVista.x >= 0f && puntoVista.x <= 1f && puntoVista.y >= 0f && puntoVista.y <= 1f)
            {
                return new Resultado(true, new Vector2(puntoVista.x * lienzo.x, puntoVista.y * lienzo.y), 0f);
            }

            Vector2 centro = lienzo * 0.5f;
            Vector2 direccion = new Vector2((puntoVista.x - 0.5f) * lienzo.x, (puntoVista.y - 0.5f) * lienzo.y);
            if (!delante)
            {
                // Detrás de la cámara la proyección sale reflejada; además la flecha va a un lado (o abajo), nunca arriba.
                direccion = new Vector2(-direccion.x, 0f);
            }
            if (direccion.sqrMagnitude < CasiCero)
            {
                // Justo detrás: la flecha va abajo.
                direccion = Vector2.down;
            }
            float mitadX = Mathf.Max(0f, centro.x - margen);
            float mitadY = Mathf.Max(0f, centro.y - margen);
            float escalaX = Mathf.Abs(direccion.x) > CasiCero ? mitadX / Mathf.Abs(direccion.x) : float.MaxValue;
            float escalaY = Mathf.Abs(direccion.y) > CasiCero ? mitadY / Mathf.Abs(direccion.y) : float.MaxValue;
            Vector2 borde = centro + direccion * Mathf.Min(escalaX, escalaY);
            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            return new Resultado(false, borde, angulo);
        }
    }
}
