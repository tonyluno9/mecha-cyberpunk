using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1492: dónde se dibuja el marcador del dron que te dispara. Lógica pura: recibe el punto del dron en coordenadas de vista de la cámara
    /// (0..1, z = distancia delante de la cámara) y devuelve la posición en pantalla (píxeles, origen arriba a la izquierda, como IMGUI).
    /// Si el dron está en pantalla, el marcador va sobre él; si está fuera o detrás, va en el borde de la pantalla apuntando hacia él.
    /// </summary>
    public static class CalculoMarcadorDron
    {
        /// <summary>Distancia (px) al borde de pantalla a la que se queda el marcador cuando el dron está fuera.</summary>
        public const float MargenBorde = 48f;

        /// <summary>
        /// Calcula la posición del marcador. <paramref name="fuera"/> es true si hay que pegarlo al borde; <paramref name="grados"/> es el ángulo de la
        /// flecha (0 arriba, 90 derecha) y solo sirve cuando <paramref name="fuera"/> es true.
        /// </summary>
        public static Vector2 Colocar(Vector3 vista, float ancho, float alto, out bool fuera, out float grados)
        {
            grados = 0f;
            bool delante = vista.z > 0f;
            bool dentro = delante && vista.x >= 0f && vista.x <= 1f && vista.y >= 0f && vista.y <= 1f;
            if (dentro)
            {
                fuera = false;
                return new Vector2(vista.x * ancho, (1f - vista.y) * alto);
            }
            fuera = true;
            // Dirección desde el centro hacia el dron, en píxeles con la Y hacia abajo. Detrás de la cámara los ejes se invierten.
            float dx = (vista.x - 0.5f) * ancho;
            float dy = (0.5f - vista.y) * alto;
            if (!delante)
            {
                dx = -dx;
                dy = -dy;
            }
            if (Mathf.Abs(dx) < 1e-3f && Mathf.Abs(dy) < 1e-3f)
            {
                dy = alto; // justo detrás: se pone abajo
            }
            float mediaX = Mathf.Max(1f, ancho * 0.5f - MargenBorde);
            float mediaY = Mathf.Max(1f, alto * 0.5f - MargenBorde);
            float escala = Mathf.Min(mediaX / Mathf.Max(Mathf.Abs(dx), 1e-3f), mediaY / Mathf.Max(Mathf.Abs(dy), 1e-3f));
            Vector2 desdeCentro = new Vector2(dx, dy) * escala;
            grados = Mathf.Atan2(desdeCentro.x, -desdeCentro.y) * Mathf.Rad2Deg;
            return new Vector2(ancho * 0.5f + desdeCentro.x, alto * 0.5f + desdeCentro.y);
        }

        /// <summary>True mientras el dron avisa, o hace menos de <paramref name="segundosTrasTiro"/> s que disparó.</summary>
        public static bool Amenazando(bool avisando, float ahora, float ultimoTiro, float segundosTrasTiro)
        {
            return avisando || (ultimoTiro >= 0f && ahora - ultimoTiro < segundosTrasTiro);
        }
    }
}
