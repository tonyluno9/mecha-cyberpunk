using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El círculo de búsqueda de la policía (T-665) en el minimapa y en el mapa grande: un círculo rojo translúcido con borde fino, centrado donde te
    /// vieron por última vez. Fijo en rojo mientras te ven; si no te ven, parpadea entre rojo y azul como en GTA. Lee <c>Registro&lt;IZonaBusqueda&gt;</c>.
    /// </summary>
    public sealed class CirculoBusquedaMapa
    {
        private const float AlfaRelleno = 0.07f;
        private const float SegundosParpadeo = 0.5f;
        private static readonly Color Azul = new Color(0.25f, 0.5f, 1f, 1f);
        private readonly Image relleno;
        private readonly Image borde;

        /// <param name="padre">Un contenedor cuyas unidades son píxeles de mapa: (x, z) del mundo por metros por píxel.</param>
        public CirculoBusquedaMapa(Transform padre)
        {
            relleno = IconosMapa.Pieza(padre, "ZonaBusqueda", IconosMapa.Circulo, Color.red, 1f);
            relleno.raycastTarget = false;
            borde = IconosMapa.Pieza(relleno.transform, "Borde", IconosMapa.AnilloFino, Color.red, 1f);
            borde.raycastTarget = false;
            KitUI.Estirar(borde.rectTransform);
            relleno.gameObject.SetActive(false);
        }

        public bool Visible => relleno.gameObject.activeSelf;

        public RectTransform Rect => relleno.rectTransform;

        /// <summary>Color con el que se pinta el borde ahora.</summary>
        public Color ColorBorde => borde.color;

        /// <summary>Lo coloca y lo colorea según la zona activa. <paramref name="pixelesPorMetro"/> es la escala del contenedor.</summary>
        public void Actualizar(float pixelesPorMetro)
        {
            IZonaBusqueda zona = Registro<IZonaBusqueda>.Actual;
            bool hay = zona != null && zona.Activa;
            if (relleno.gameObject.activeSelf != hay)
            {
                relleno.gameObject.SetActive(hay);
            }
            if (!hay)
            {
                return;
            }
            relleno.rectTransform.anchoredPosition = GeometriaBarrio.AMapa(zona.Centro) * pixelesPorMetro;
            relleno.rectTransform.sizeDelta = Vector2.one * (2f * zona.Radio * pixelesPorMetro);
            bool azul = !zona.Visto && Mathf.FloorToInt(Time.unscaledTime / SegundosParpadeo) % 2 == 1;
            Color baseColor = azul ? Azul : KitUI.Estilo.Enemigo;
            relleno.color = new Color(baseColor.r, baseColor.g, baseColor.b, AlfaRelleno);
            borde.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        }
    }
}
