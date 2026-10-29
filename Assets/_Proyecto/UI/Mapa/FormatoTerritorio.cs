using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>Textos y colores compartidos por el HUD de zona y el mapa (T-404).</summary>
    public static class FormatoTerritorio
    {
        private static readonly Color ColorJugador = new Color(1f, 0.5f, 0.1f);
        private static readonly Color ColorCalaveras = new Color(1f, 0.1f, 0.8f);
        private static readonly Color ColorViboras = new Color(0.45f, 1f, 0.1f);
        private static readonly Color ColorNadie = new Color(0.6f, 0.6f, 0.6f);

        public static Color ColorDe(IdPandilla dueno)
        {
            switch (dueno)
            {
                case IdPandilla.Jugador: return ColorJugador;
                case IdPandilla.Calaveras: return ColorCalaveras;
                case IdPandilla.Viboras: return ColorViboras;
                default: return ColorNadie;
            }
        }

        public static string NombreDe(IdPandilla dueno)
        {
            switch (dueno)
            {
                case IdPandilla.Jugador: return "TUYA";
                case IdPandilla.Calaveras: return "LOS CALAVERAS";
                case IdPandilla.Viboras: return "LAS VÍBORAS";
                default: return "SIN DUEÑO";
            }
        }

        /// <summary>"EL TIANGUIS · LOS CALAVERAS · RESPETO 40".</summary>
        public static string Linea(ITerritorio territorio, string zona)
        {
            return territorio.Nombre(zona) + " · " + NombreDe(territorio.Dueno(zona)) + " · RESPETO " + territorio.Respeto(zona);
        }

        /// <summary>Un Text con sombra, anclado en el padre. La fuente es la integrada de Unity.</summary>
        public static Text CrearTexto(Transform padre, string nombre, int tamano, TextAnchor alineacion, Vector2 anclaMin, Vector2 anclaMax, Vector2 posicion, Vector2 tamanoCaja)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.pivot = new Vector2(anclaMin.x, anclaMin.y);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamanoCaja;
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = tamano;
            texto.alignment = alineacion;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            Shadow sombra = objeto.AddComponent<Shadow>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sombra.effectDistance = new Vector2(1.5f, -1.5f);
            return texto;
        }

        public static Canvas CrearCanvas(Transform padre, string nombre, int orden)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = orden;
            CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;
            return canvas;
        }
    }
}
