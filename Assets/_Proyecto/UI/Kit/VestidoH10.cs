using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// T-902: lo común para llevar a la guía H10 las pantallas que vienen con piezas ya armadas del kit viejo (pausa, tienda) o que se arman
    /// con la misma cabecera y barra de teclas (mapa, inventario). Todo se hace al abrir, por código: no se toca ningún prefab.
    /// </summary>
    public static class VestidoH10
    {
        public const float AltoCabecera = 96f;
        public const float AltoPie = 72f;
        public const string NombreLinea = "LineaH10";
        private const string NombreMarco = "MarcoH10";
        private const string NombreBorde = "Borde";
        private const float AlfaFondoBoton = 0f;
        private const float AlfaElegido = 0.12f;
        private const float AlfaPulsado = 0.3f;
        private const float AltoTexto = 60f;
        private const float AltoSubtitulo = 28f;

        /// <summary>Barra de título: nombre de la pantalla a la izquierda, dinero a la derecha. Devuelve el texto del dinero (el llamador lo llena).</summary>
        public static Text Cabecera(Transform padre, string titulo, string subtitulo, string etiquetaDinero, out Text textoTitulo)
        {
            float margen = TrazoH10.MargenPantalla;
            textoTitulo = TrazoH10.Etiqueta(padre, "Titulo", titulo, TrazoH10.TamanoTitulo, TrazoH10.Texto);
            TrazoH10.Punto(textoTitulo.rectTransform, new Vector2(0f, 1f), new Vector2(margen, -14f), new Vector2(900f, AltoTexto));
            if (!string.IsNullOrEmpty(subtitulo))
            {
                Text sub = TrazoH10.Etiqueta(padre, "Subtitulo", subtitulo, TrazoH10.TamanoDato, TrazoH10.Turquesa);
                TrazoH10.Punto(sub.rectTransform, new Vector2(0f, 1f), new Vector2(margen + 2f, -72f), new Vector2(1000f, AltoSubtitulo));
            }
            TrazoH10.Recta(padre, "TiraCabecera", TrazoH10.TurquesaTenue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -AltoCabecera), new Vector2(-2f * margen, TrazoH10.GrosorLinea));
            RectTransform regla = TrazoH10.Regla(padre, "Regla", 41, 12f, 10f, TrazoH10.TurquesaTenue);
            TrazoH10.Punto(regla, new Vector2(1f, 1f), new Vector2(-margen, -AltoCabecera - 2f), regla.sizeDelta);
            Text etiqueta = TrazoH10.Etiqueta(padre, "EtiquetaDinero", etiquetaDinero, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.MiddleRight);
            TrazoH10.Punto(etiqueta.rectTransform, new Vector2(1f, 1f), new Vector2(-margen, -72f), new Vector2(400f, AltoSubtitulo));
            Text dinero = TrazoH10.Etiqueta(padre, "Dinero", string.Empty, TrazoH10.TamanoCifra, TrazoH10.Turquesa, TextAnchor.MiddleRight);
            TrazoH10.Punto(dinero.rectTransform, new Vector2(1f, 1f), new Vector2(-margen, -14f), new Vector2(500f, AltoTexto));
            return dinero;
        }

        /// <summary>Barra de teclas de abajo.</summary>
        public static Text Pie(Transform padre, string ayuda)
        {
            Text texto = TrazoH10.Etiqueta(padre, "Ayuda", ayuda, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.MiddleCenter);
            TrazoH10.Punto(texto.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1600f, 32f));
            TrazoH10.Recta(padre, "TiraPie", TrazoH10.TurquesaTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, AltoPie), new Vector2(-2f * TrazoH10.MargenPantalla, TrazoH10.GrosorLinea));
            return texto;
        }

        /// <summary>Un panel del kit viejo pasa a panel H10: relleno con el corte, casi transparente y su línea fina. Se puede repetir.</summary>
        public static void Panel(Image panel)
        {
            panel.sprite = TrazoH10.Relleno;
            panel.type = Image.Type.Sliced;
            panel.color = TrazoH10.FondoPanel;
            Transform marco = panel.transform.Find(NombreMarco);
            if (marco != null)
            {
                marco.gameObject.SetActive(false);
            }
            if (panel.transform.Find(NombreLinea) == null)
            {
                Image linea = TrazoH10.Imagen(panel.transform, NombreLinea, TrazoH10.TurquesaTenue, TrazoH10.Linea);
                TrazoH10.Estirar(linea.rectTransform);
            }
        }

        /// <summary>Un botón del kit viejo pasa a fila H10: sin fondo, línea fina que se enciende, letra Rajdhani. Se puede repetir.</summary>
        public static void Boton(BotonCyberfix boton, int tamano = TrazoH10.TamanoFila)
        {
            boton.UsarAcento(TrazoH10.Turquesa);
            boton.UsarFondoReposo(new Color(0f, 0f, 0f, AlfaFondoBoton), AlfaElegido, AlfaPulsado);
            Transform borde = boton.transform.Find(NombreBorde);
            if (borde != null)
            {
                borde.gameObject.SetActive(false);
            }
            Image linea;
            Transform existente = boton.transform.Find(NombreLinea);
            if (existente == null)
            {
                linea = TrazoH10.Imagen(boton.transform, NombreLinea, TrazoH10.TurquesaTenue, TrazoH10.Linea);
                TrazoH10.Estirar(linea.rectTransform);
                HoverH10.Agregar(boton.gameObject, linea, TrazoH10.TurquesaTenue, TrazoH10.Turquesa);
            }
            foreach (Text texto in boton.GetComponentsInChildren<Text>(true))
            {
                texto.font = TrazoH10.Fuente;
                texto.fontSize = tamano;
                texto.fontStyle = FontStyle.Normal;
            }
        }

        /// <summary>Rajdhani en todos los textos de la pantalla (los que no se arman con TrazoH10).</summary>
        public static void Fuente(Transform raiz)
        {
            foreach (Text texto in raiz.GetComponentsInChildren<Text>(true))
            {
                texto.font = TrazoH10.Fuente;
                texto.fontStyle = FontStyle.Normal;
            }
        }
    }
}
