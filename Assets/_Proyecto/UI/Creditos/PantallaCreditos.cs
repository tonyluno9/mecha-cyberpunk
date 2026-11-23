using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
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
        private const int TamanoTituloCreditos = 72;

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
        [Tooltip("T50: fracción de la velocidad automática con la que arranca el desplazamiento (0 a 1); luego acelera suave hasta 1.")]
        [Range(0f, 1f)] [SerializeField] private float arranqueSuave = 0.3f;
        [Tooltip("T50: segundos que tarda en llegar a la velocidad completa.")]
        [Min(0.01f)] [SerializeField] private float segundosArranque = 0.5f;

        private Action alCerrar;
        private RectTransform viewport;
        private RectTransform contenido;
        private Text[] textos = Array.Empty<Text>();
        private int cuadroAbierto = -1;
        private float desplazamiento;
        private float reanudaEn;
        private bool abierta;
        private float rampa;
        private bool musicaYaSonaba;

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
            // T50: la música del menú suena con los créditos (si ya sonaba, sigue sin reiniciarse; si no, al cerrar se apaga).
            musicaYaSonaba = MusicaMenu.Sonando;
            MusicaMenu.Sonar();
            rampa = arranqueSuave;
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
            Image velo = KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");
            velo.color = TrazoH10.ConAlfa(TrazoH10.FondoPantalla, 0.93f);
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", new Vector2(anchoPanel, altoPanel));
            Image fondo = panel.GetComponent<Image>();
            if (fondo != null)
            {
                // T50: el panel del menú nuevo (H10): cristal con su línea fina, casi opaco para que el juego de atrás no estorbe la lectura.
                VestidoH10.Panel(fondo);
                fondo.color = TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.96f);
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
            ayuda.color = TrazoH10.TextoTenue;
            VestidoH10.Fuente(panel);
        }

        private Text CrearLinea(Transform padre, LineaCredito linea, EstiloCyberfix e)
        {
            // T50: colores y tamaños del menú nuevo (TrazoH10): títulos y secciones en turquesa, la lista en blanco y el resto tenue.
            RolTexto rol = RolTexto.Texto;
            Color color = TrazoH10.TextoTenue;
            int tamano = TrazoH10.TamanoDato;
            switch (linea.Tipo)
            {
                case TipoLineaCredito.Titulo:
                    rol = RolTexto.Titulo;
                    color = TrazoH10.Turquesa;
                    tamano = TamanoTituloCreditos;
                    break;
                case TipoLineaCredito.Seccion:
                    rol = RolTexto.Encabezado;
                    color = TrazoH10.Turquesa;
                    tamano = TrazoH10.TamanoTitulo;
                    break;
                case TipoLineaCredito.Subseccion:
                    rol = RolTexto.Boton;
                    color = TrazoH10.Amarillo;
                    tamano = TrazoH10.TamanoSeccion;
                    break;
                case TipoLineaCredito.Lista:
                    rol = RolTexto.Boton;
                    color = TrazoH10.Texto;
                    tamano = TrazoH10.TamanoFila;
                    break;
            }
            Text t = KitUI.CrearTexto(padre, "Linea", rol, TextAnchor.UpperCenter);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = color;
            t.font = TrazoH10.Fuente;
            t.fontSize = tamano;
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
                rampa = arranqueSuave;
                Fijar(desplazamiento + manual);
            }
            else if (Time.unscaledTime >= reanudaEn)
            {
                // T50: el desplazamiento automático acelera suave (al abrir y al reanudar tras mover a mano) en vez de arrancar de golpe.
                rampa = Mathf.MoveTowards(rampa, 1f, dt / segundosArranque);
                Fijar(desplazamiento + velocidadAuto * rampa * dt);
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
            if (!musicaYaSonaba)
            {
                MusicaMenu.Apagar();
            }
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
