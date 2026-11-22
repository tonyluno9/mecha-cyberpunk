using System;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625: pantalla negra a pantalla completa con un título, una línea y una tecla. La usan el "CONTINUARÁ" del final de la demo (ENTER vuelve al
    /// título) y el fin del juego (negro, "CYBER CITY", "¿YA SALISTE?", y a los créditos solos o con ENTER).
    /// </summary>
    public class PantallaNegraRecorrido : MonoBehaviour
    {
        public const int OrdenCanvas = 30001;
        private const float SegundosFundido = 1f;
        private const float SegundosSinTecla = 1f;
        private const float AltoPorLetra = 1.4f;

        private Action alAceptar;
        private float segundosAuto;
        private float inicio;
        private Canvas canvas;
        private Image fondo;
        private Text titulo;
        private Text linea;
        private Text teclas;
        private bool aceptada;

        public bool Aceptada => aceptada;

        /// <summary>Muestra la pantalla. <paramref name="segundosAutomatico"/> mayor que 0: pasa sola a ese tiempo; ENTER siempre pasa (tras un segundo).</summary>
        public static PantallaNegraRecorrido Mostrar(string textoTitulo, string textoLinea, string textoTeclas, float segundosAutomatico, Action cuandoAcepta)
        {
            PantallaNegraRecorrido pantalla = new GameObject("PantallaNegraRecorrido").AddComponent<PantallaNegraRecorrido>();
            pantalla.Armar(textoTitulo, textoLinea, textoTeclas, segundosAutomatico, cuandoAcepta);
            return pantalla;
        }

        private void Armar(string textoTitulo, string textoLinea, string textoTeclas, float segundosAutomatico, Action cuandoAcepta)
        {
            alAceptar = cuandoAcepta;
            segundosAuto = segundosAutomatico;
            inicio = Time.unscaledTime;
            canvas = KitUI.CrearCanvas(transform, "CanvasPantallaNegra", OrdenCanvas);
            canvas.overrideSorting = true;
            fondo = KitUI.CrearImagen(canvas.transform, "Fondo", new Color(0f, 0f, 0f, 0f));
            KitUI.Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;
            EstiloCyberfix e = KitUI.Estilo;
            titulo = Centrado("Titulo", RolTexto.Titulo, e.Magenta, 0.58f);
            titulo.text = textoTitulo;
            linea = Centrado("Linea", RolTexto.Encabezado, e.Turquesa, 0.44f);
            linea.text = textoLinea;
            teclas = Centrado("Teclas", RolTexto.Ayuda, e.Desactivado, 0.3f);
            teclas.text = textoTeclas;
            PilaPantallas.Abrir(this);
            Refrescar(0f);
        }

        private Text Centrado(string nombre, RolTexto rol, Color color, float altura)
        {
            Text texto = KitUI.CrearTexto(canvas.transform, nombre, rol, TextAnchor.MiddleCenter);
            RectTransform rect = texto.rectTransform;
            rect.anchorMin = new Vector2(0f, altura);
            rect.anchorMax = new Vector2(1f, altura);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, KitUI.Estilo.Tamano(rol) * AltoPorLetra);
            texto.color = color;
            return texto;
        }

        private void OnDestroy() => PilaPantallas.Cerrar(this);

        private void Update()
        {
            if (aceptada)
            {
                return;
            }
            float t = Time.unscaledTime - inicio;
            Refrescar(Mathf.Clamp01(t / SegundosFundido));
            Keyboard teclado = Keyboard.current;
            bool enter = teclado != null && t >= SegundosSinTecla && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame);
            if (enter || (segundosAuto > 0f && t >= segundosAuto))
            {
                Aceptar();
            }
        }

        /// <summary>Pasa a lo siguiente (lo mismo que ENTER). Para pruebas.</summary>
        public void Aceptar()
        {
            if (aceptada)
            {
                return;
            }
            aceptada = true;
            PilaPantallas.Cerrar(this);
            // Se baja bajo los créditos (orden 80): el negro queda de fondo y los textos se apagan.
            canvas.sortingOrder = 79;
            titulo.enabled = false;
            linea.enabled = false;
            teclas.enabled = false;
            Action accion = alAceptar;
            alAceptar = null;
            try
            {
                accion?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            // Los créditos se abren encima; el negro se queda debajo hasta que se cierran y la escena cambia.
        }

        private void Refrescar(float alfa)
        {
            Color c = fondo.color;
            c.a = alfa;
            fondo.color = c;
            Tinte(titulo, alfa);
            Tinte(linea, alfa);
            Tinte(teclas, alfa);
        }

        private static void Tinte(Graphic g, float alfa)
        {
            Color c = g.color;
            c.a = alfa;
            g.color = c;
        }
    }
}
