using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>Zoom del mapa grande centrado en el cursor (T-409), sin Unity vivo.</summary>
    public static class CalculoMapa
    {
        /// <summary>
        /// Nuevo desplazamiento del dibujo para que el punto bajo <paramref name="cursor"/> (px desde el centro de la vista) siga bajo el cursor
        /// al pasar de <paramref name="zoomAntes"/> a <paramref name="zoomDespues"/>. Queda dentro de <see cref="LimitarDesplazamiento"/>.
        /// </summary>
        public static Vector2 DesplazamientoAlZoom(Vector2 desplazamiento, Vector2 cursor, float zoomAntes, float zoomDespues, Vector2 mitadVista)
        {
            Vector2 debajo = (cursor - desplazamiento) / zoomAntes;
            return LimitarDesplazamiento(cursor - debajo * zoomDespues, zoomDespues, mitadVista);
        }

        /// <summary>Con zoom 1 el dibujo queda centrado; con más zoom se mueve hasta que su orilla llega a la de la vista.</summary>
        public static Vector2 LimitarDesplazamiento(Vector2 desplazamiento, float zoom, Vector2 mitadVista)
        {
            Vector2 limite = mitadVista * Mathf.Max(0f, zoom - 1f);
            return new Vector2(Mathf.Clamp(desplazamiento.x, -limite.x, limite.x), Mathf.Clamp(desplazamiento.y, -limite.y, limite.y));
        }

        /// <summary>De un punto local del dibujo (px a zoom 1) a la vista (px desde su centro).</summary>
        public static Vector2 AVista(Vector2 local, Vector2 desplazamiento, float zoom) => local * zoom + desplazamiento;
    }
}
