using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Creditos
{
    /// <summary>
    /// La pantalla de CRÉDITOS (T-471), hecha con el kit en un canvas de orden 80. Los créditos suben solos; las flechas arriba y abajo
    /// (o W S), AvPág RePág y la rueda los mueven a mano. Esc la cierra y llama al callback. Se abre desde cualquier pantalla con
    /// PantallaCreditos.Abrir(alCerrar) (la pantalla de título agrega un botón CRÉDITOS que llama a ese método). El texto sale de
    /// Resources/Creditos (docs/creditos.md). Se apila en <see cref="PilaPantallas"/>.
    /// </summary>
    public class PantallaCreditos : MonoBehaviour
    {
        public const int OrdenCanvas = 80;
        public const string TextoAyuda = "↑ ↓ O RUEDA MOVER · ESC VOLVER";
        public const string SonidoCancelar = "UI_CANCELAR";

        [Header("Medidas (px a 1920x1080)")]
        [Tooltip("Ancho del panel.")]
        [Min(100f)] [SerializeField] private float anchoPanel = 1100f;
        [Tooltip("Alto del panel.")]
        [Min(100f)] [SerializeField] private float altoPanel = 960f;
        [Tooltip("Separación entre líneas (px).")]
        [Min(0f)] [SerializeField] private float separacion = 8f;
        [Tooltip("Alto de una línea vacía (px).")]
        [Min(0f)] [SerializeField] private float altoEspacio = 28f;

        [Header("Movimiento")]
        [Tooltip("Velocidad del desplazamiento automático (px/s).")]
        [Min(0f)] [SerializeField] private float velocidadAuto = 70f;
        [Tooltip("Velocidad al mantener las flechas (px/s).")]
        [Min(0f)] [SerializeField] private float velocidadManual = 600f;
        [Tooltip("Píxeles por cada unidad de la rueda del ratón.")]
        [Min(0f)] [SerializeField] private float pasoRueda = 0.5f;
        [Tooltip("Segundos que el automático espera después de mover a mano.")]
        [Min(0f)] [SerializeField] private float pausaManual = 2f;

        private Action alCerrar;
        private RectTransform viewport;
        private RectTransform contenido;
        private Text[] textos = Array.Empty<Text>();
        private int cuadroAbierto = -1;
        private float desplazamiento;
        private float reanudaEn;
        private bool abierta;

        public bool Abierta => abierta;
        /// <summary>Cuánto se ha subido el texto (px); 0 deja el principio pegado arriba del marco.</summary>
        public float Desplazamiento => desplazamiento;
        public float Maximo => contenido == null || viewport == null ? 0f : Mathf.Max(0f, contenido.rect.height - viewport.rect.height);
        public float Minimo => viewport == null ? 0f : -viewport.rect.height;
        public IReadOnlyList<Text> Lineas => textos;
        public RectTransform Contenido => contenido;

        /// <summary>Crea y abre la pantalla. <paramref name="alCerrar"/> se llama una vez al cerrarse con Esc.</summary>
        public static PantallaCreditos Abrir(Action alCerrar = null)
        {
            GameObject objeto = new GameObject("PantallaCreditos");
            PantallaCreditos pantalla = objeto.AddComponent<PantallaCreditos>();
            pantalla.Mostrar(CreditosTexto.Cargar(), alCerrar);
            return pantalla;
        }

        /// <summary>Muestra el texto dado (Abrir lo lee de Resources).</summary>
        public void Mostrar(string texto, Action alCerrarPantalla)
        {
            alCerrar = alCerrarPantalla;
            Armar(CreditosTexto.Parsear(texto));
            abierta = true;
            cuadroAbierto = Time.frameCount;
            PilaPantallas.Abrir(this);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contenido);
            Fijar(Minimo);
        }

        private void Armar(List<LineaCredito> lineas)
        {
            EstiloCyberfix e = KitUI.Estilo;
            float relleno = e.Relleno;
            float altoTitulo = 0f; // el título CRÉDITOS es la primera línea del texto
            float altoAyuda = e.AltoFilaHud;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasCreditos", OrdenCanvas);
            KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", new Vector2(anchoPanel, altoPanel));
            Image fondo = panel.GetComponent<Image>();
            if (fondo != null)
            {
                Color c = fondo.color;
                c.a = 1f;
                fondo.color = c;
            }

            float altoViewport = altoPanel - 3f * relleno - altoTitulo - altoAyuda;
            GameObject marco = new GameObject("Marco", typeof(RectTransform), typeof(RectMask2D));
            marco.transform.SetParent(panel, false);
            viewport = (RectTransform)marco.transform;
            KitUI.Colocar(viewport, new Vector2(0.5f, 1f), new Vector2(0f, -(2f * relleno + altoTitulo)), new Vector2(anchoPanel - 4f * relleno, altoViewport));

            GameObject cuerpo = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cuerpo.transform.SetParent(viewport, false);
            contenido = (RectTransform)cuerpo.transform;
            contenido.anchorMin = new Vector2(0f, 1f);
            contenido.anchorMax = new Vector2(1f, 1f);
            contenido.pivot = new Vector2(0.5f, 1f);
            contenido.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup grupo = cuerpo.GetComponent<VerticalLayoutGroup>();
            grupo.spacing = separacion;
            grupo.childAlignment = TextAnchor.UpperCenter;
            grupo.childControlWidth = true;
            grupo.childControlHeight = true;
            grupo.childForceExpandWidth = true;
            grupo.childForceExpandHeight = false;
            cuerpo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            List<Text> creados = new List<Text>();
            foreach (LineaCredito linea in lineas)
            {
                creados.Add(CrearLinea(contenido, linea, e));
            }
            textos = creados.ToArray();

            Text ayuda = KitUI.CrearTexto(panel, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(anchoPanel, altoAyuda));
            ayuda.text = TextoAyuda;
            ayuda.color = e.Turquesa;
        }

        private Text CrearLinea(Transform padre, LineaCredito linea, EstiloCyberfix e)
        {
            RolTexto rol = RolTexto.Texto;
            Color color = e.Texto;
            switch (linea.Tipo)
            {
                case TipoLineaCredito.Titulo:
                    rol = RolTexto.Titulo;
                    color = e.Naranja;
                    break;
                case TipoLineaCredito.Seccion:
                    rol = RolTexto.Encabezado;
                    color = e.Naranja;
                    break;
                case TipoLineaCredito.Subseccion:
                    rol = RolTexto.Boton;
                    color = e.Turquesa;
                    break;
                case TipoLineaCredito.Lista:
                    rol = RolTexto.Boton;
                    break;
            }
            Text t = KitUI.CrearTexto(padre, "Linea", rol, TextAnchor.UpperCenter);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = color;
            t.text = linea.Texto;
            if (linea.Tipo == TipoLineaCredito.Espacio)
            {
                LayoutElement elemento = t.gameObject.AddComponent<LayoutElement>();
                elemento.minHeight = altoEspacio;
                elemento.preferredHeight = altoEspacio;
            }
            return t;
        }

        private void Update()
        {
            if (!abierta)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;
            if (teclado != null && Time.frameCount != cuadroAbierto && teclado.escapeKey.wasPressedThisFrame)
            {
                Cerrar();
                return;
            }
            float manual = 0f;
            if (teclado != null)
            {
                if (teclado.upArrowKey.isPressed || teclado.wKey.isPressed)
                {
                    manual -= velocidadManual * dt;
                }
                if (teclado.downArrowKey.isPressed || teclado.sKey.isPressed)
                {
                    manual += velocidadManual * dt;
                }
                float pagina = viewport != null ? viewport.rect.height * 0.9f : 0f;
                if (teclado.pageUpKey.wasPressedThisFrame)
                {
                    manual -= pagina;
                }
                if (teclado.pageDownKey.wasPressedThisFrame)
                {
                    manual += pagina;
                }
            }
            if (raton != null)
            {
                // La rueda arriba (positiva) lleva el texto hacia el principio, como en una página.
                manual -= raton.scroll.ReadValue().y * pasoRueda;
            }
            if (Mathf.Abs(manual) > 0.0001f)
            {
                reanudaEn = Time.unscaledTime + pausaManual;
                Fijar(desplazamiento + manual);
            }
            else if (Time.unscaledTime >= reanudaEn)
            {
                Fijar(desplazamiento + velocidadAuto * dt);
            }
        }

        private void Fijar(float valor)
        {
            desplazamiento = Mathf.Clamp(valor, Minimo, Maximo);
            if (contenido != null)
            {
                contenido.anchoredPosition = new Vector2(0f, desplazamiento);
            }
        }

        public void Cerrar()
        {
            if (!abierta)
            {
                return;
            }
            abierta = false;
            Sonidos.Reproducir(SonidoCancelar);
            PilaPantallas.Cerrar(this);
            Action callback = alCerrar;
            alCerrar = null;
            Destroy(gameObject);
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            if (abierta)
            {
                abierta = false;
                PilaPantallas.Cerrar(this);
            }
        }
    }
}
