using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>Textos y colores compartidos por el HUD de zona y el mapa (T-404).</summary>
    public static class FormatoTerritorio
    {
        // SNAKES #73FF1A (fuera de la paleta de la UI, docs/diseno/ui-cyberfix.md); SKULLS van en el magenta del kit.
        private static readonly Color ColorViboras = new Color(0.451f, 1f, 0.102f);

        /// <summary>Máximo de caracteres de un aviso del HUD; si no cabe se corta (T-415).</summary>
        public const int LargoMaximoAviso = 40;

        private static IPerfilJugador Perfil => Registro<IPerfilJugador>.Actual;

        /// <summary>Corta un aviso a <see cref="LargoMaximoAviso"/> caracteres.</summary>
        public static string Cortar(string texto) => texto.Length <= LargoMaximoAviso ? texto : texto.Substring(0, LargoMaximoAviso);

        public static Color ColorDe(IdPandilla dueno)
        {
            switch (dueno)
            {
                case IdPandilla.Jugador: return ColoresPandilla.Color(Perfil != null ? Perfil.ColorPandilla : ColoresPandilla.Predeterminado);
                case IdPandilla.Calaveras: return KitUI.Estilo.Magenta;
                case IdPandilla.Viboras: return ColorViboras;
                default: return KitUI.Estilo.Desactivado;
            }
        }

        public static string NombreDe(IdPandilla dueno)
        {
            switch (dueno)
            {
                case IdPandilla.Jugador: return Perfil != null ? Perfil.NombrePandilla : ColoresPandilla.NombrePredeterminado;
                case IdPandilla.Calaveras: return "SKULLS";
                case IdPandilla.Viboras: return "SNAKES";
                default: return "SIN DUEÑO";
            }
        }

        /// <summary>"EL TIANGUIS · SKULLS · RESPETO 40".</summary>
        public static string Linea(ITerritorio territorio, string zona)
        {
            return territorio.Nombre(zona) + " · " + NombreDe(territorio.Dueno(zona)) + " · RESPETO " + territorio.Respeto(zona);
        }

        /// <summary>Un Text del kit (fuente, sombra y color de la guía), anclado en el padre; nunca menor que el mínimo de 20 px (T-408).</summary>
        public static Text CrearTexto(Transform padre, string nombre, int tamano, TextAnchor alineacion, Vector2 anclaMin, Vector2 anclaMax, Vector2 posicion, Vector2 tamanoCaja)
        {
            Text texto = KitUI.CrearTexto(padre, nombre, RolTexto.Texto, alineacion);
            texto.fontSize = Mathf.Max(KitUI.Estilo.Tamano(RolTexto.Minimo), tamano);
            RectTransform rect = texto.rectTransform;
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.pivot = new Vector2(anclaMin.x, anclaMin.y);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamanoCaja;
            return texto;
        }

        /// <summary>Canvas del kit (Overlay con el CanvasScaler de la guía).</summary>
        public static Canvas CrearCanvas(Transform padre, string nombre, int orden) => KitUI.CrearCanvas(padre, nombre, orden);
    }
}
