using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>Colores, fuentes y constructores pequeños de la pantalla de inicio y de carga (todo se arma por código).</summary>
    public static class UIInicio
    {
        public const string NombreMaterialAditivo = "UIAditivo";

        public static readonly Color Cian = new Color(0.10f, 0.92f, 0.98f, 1f);
        public static readonly Color Magenta = new Color(1f, 0.10f, 0.72f, 1f);
        public static readonly Color Naranja = new Color(1f, 0.56f, 0.12f, 1f);
        public static readonly Color Blanco = new Color(0.96f, 0.98f, 1f, 1f);
        public static readonly Color TextoSuave = new Color(0.66f, 0.71f, 0.82f, 1f);
        public static readonly Color TextoApagado = new Color(0.40f, 0.43f, 0.52f, 1f);
        public static readonly Color Noche = new Color(0.015f, 0.018f, 0.045f, 1f);
        public static readonly Color PanelFondo = new Color(0.025f, 0.03f, 0.065f, 0.9f);

        private static Material aditivo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => aditivo = null;

        public static Font FuenteTitulos => KitUI.Estilo.FuenteDe(RolTexto.Titulo);

        public static Font FuenteTexto => KitUI.Estilo.FuenteDe(RolTexto.Texto);

        /// <summary>Material de mezcla aditiva (UI/Resources/UIAditivo.mat). Null si falta: los brillos salen con mezcla normal.</summary>
        public static Material Aditivo
        {
            get
            {
                if (aditivo == null)
                {
                    aditivo = Resources.Load<Material>(NombreMaterialAditivo);
                }
                return aditivo;
            }
        }

        public static RectTransform Nodo(Transform padre, string nombre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.layer = padre.gameObject.layer;
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            return rect;
        }

        public static Image Imagen(Transform padre, string nombre, Color color, Sprite sprite = null, bool aditiva = false)
        {
            RectTransform rect = Nodo(padre, nombre);
            Image imagen = rect.gameObject.AddComponent<Image>();
            imagen.sprite = sprite != null ? sprite : SpritesInicio.Blanco;
            imagen.color = color;
            imagen.raycastTarget = false;
            if (aditiva && Aditivo != null)
            {
                imagen.material = Aditivo;
            }
            return imagen;
        }

        public static Text Texto(Transform padre, string nombre, string contenido, Font fuente, int tamano, Color color, TextAnchor ancla, bool aditivo = false)
        {
            RectTransform rect = Nodo(padre, nombre);
            Text texto = rect.gameObject.AddComponent<Text>();
            texto.font = fuente;
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Normal;
            texto.alignment = ancla;
            texto.color = color;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.supportRichText = false;
            texto.raycastTarget = false;
            texto.text = contenido;
            if (aditivo && Aditivo != null)
            {
                texto.material = Aditivo;
            }
            return texto;
        }

        public static void Anclar(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 posicion, Vector2 tamano)
        {
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.pivot = pivote;
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
        }

        /// <summary>Ancla en un punto (0..1) con el pivote en ese mismo punto.</summary>
        public static void Punto(RectTransform rect, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            Anclar(rect, ancla, ancla, ancla, posicion, tamano);
        }

        public static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Color ConAlfa(Color color, float alfa)
        {
            color.a = alfa;
            return color;
        }

        /// <summary>Halo de neón de un texto: copias aditivas con contorno ancho y casi transparente.</summary>
        public static Text Halo(Text origen, Color color, float alfa, float distancia)
        {
            Text halo = Texto(origen.transform.parent, origen.name + "_Halo", origen.text, origen.font, origen.fontSize, ConAlfa(color, alfa), origen.alignment, true);
            halo.transform.SetSiblingIndex(origen.transform.GetSiblingIndex());
            RectTransform destino = halo.rectTransform;
            RectTransform fuente = origen.rectTransform;
            destino.anchorMin = fuente.anchorMin;
            destino.anchorMax = fuente.anchorMax;
            destino.pivot = fuente.pivot;
            destino.anchoredPosition = fuente.anchoredPosition;
            destino.sizeDelta = fuente.sizeDelta;
            Outline contorno = halo.gameObject.AddComponent<Outline>();
            contorno.effectColor = ConAlfa(color, alfa * 0.5f);
            contorno.effectDistance = new Vector2(distancia, distancia);
            contorno.useGraphicAlpha = false;
            return halo;
        }
    }
}
