using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// T-62: ventana de aviso al centro de la pantalla, con el estilo de la UI nueva (la de Opciones y la pausa): el texto y un botón ENTENDIDO
    /// (clic o Enter). Mientras está abierta el juego se pausa (<c>Time.timeScale</c> 0 y <see cref="Pausa"/>), el cursor se libera y el control a pie
    /// se congela; al cerrarla todo vuelve como estaba. Si llega otro aviso con una abierta, espera su turno. Se abre con <see cref="Mostrar"/>.
    /// </summary>
    public class VentanaAviso : MonoBehaviour
    {
        public const int OrdenCanvas = 90;
        public const string TextoBoton = "ENTENDIDO";
        private const string TextoTitulo = "AVISO";
        private const string SonidoAceptar = "UI_ACEPTAR";
        private const string SonidoAbrir = "UI_MENU_ABRIR";
        private const int TamanoTexto = 34;
        private const int TamanoTitulo = 44;
        private static readonly Vector2 TamanoPanel = new Vector2(1000f, 460f);
        private static readonly Vector2 TamanoBoton = new Vector2(440f, 64f);

        private static VentanaAviso actual;

        private readonly Queue<KeyValuePair<string, Action>> cola = new Queue<KeyValuePair<string, Action>>();
        private InputAction aceptar;
        private GameObject raiz;
        private Text texto;
        private Action alEntender;
        private bool abierta;
        private int cuadroAbierta;
        private float escalaPrevia = 1f;
        private CursorLockMode bloqueoPrevio;
        private bool cursorVisiblePrevio;
        private ControladorAPie controlCongelado;

        /// <summary>True mientras hay una ventana abierta.</summary>
        public static bool Abierta => actual != null && actual.abierta;

        /// <summary>El texto de la ventana abierta (vacío si no hay).</summary>
        public static string TextoActual => Abierta ? actual.texto.text : string.Empty;

        /// <summary>El botón ENTENDIDO de la ventana abierta (para pruebas), o null.</summary>
        public static BotonCyberfix BotonActual => Abierta ? actual.raiz.GetComponentInChildren<BotonCyberfix>(true) : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => actual = null;

        /// <summary>Abre la ventana con el texto (o la pone en cola si ya hay una). <paramref name="alEntender"/> se llama al cerrarla.</summary>
        public static void Mostrar(string aviso, Action alEntender = null)
        {
            if (actual == null)
            {
                actual = new GameObject("VentanaAviso").AddComponent<VentanaAviso>();
            }
            actual.Encolar(aviso, alEntender);
        }

        /// <summary>Cierra la ventana abierta como si se pulsara ENTENDIDO (pruebas y clic del botón).</summary>
        public static void Entender()
        {
            if (Abierta)
            {
                actual.Cerrar();
            }
        }

        private void Awake()
        {
            if (actual == null)
            {
                actual = this;
            }
            aceptar = new InputAction("AvisoEnter", InputActionType.Button);
            aceptar.AddBinding("<Keyboard>/enter");
            aceptar.AddBinding("<Keyboard>/numpadEnter");
            Construir();
            raiz.SetActive(false);
        }

        private void OnDestroy()
        {
            if (abierta)
            {
                Restaurar();
            }
            aceptar?.Dispose();
            if (actual == this)
            {
                actual = null;
            }
        }

        private void Construir()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasVentanaAviso", OrdenCanvas);
            raiz = canvas.gameObject;
            Image velo = KitUI.CrearImagen(canvas.transform, "Velo", TrazoH10.ConAlfa(TrazoH10.FondoPantalla, 0.55f));
            KitUI.Estirar(velo.rectTransform);
            velo.raycastTarget = true; // el clic no pasa al juego
            Image panel = TrazoH10.Panel(canvas.transform, "Panel", TrazoH10.Turquesa, TrazoH10.FondoPantalla);
            TrazoH10.Colocar(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, TamanoPanel);
            Text titulo = TrazoH10.Etiqueta(panel.transform, "Titulo", TextoTitulo, TamanoTitulo, TrazoH10.Amarillo, TextAnchor.MiddleCenter);
            TrazoH10.Colocar(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-80f, 64f));
            texto = TrazoH10.Etiqueta(panel.transform, "Texto", string.Empty, TamanoTexto, TrazoH10.Texto, TextAnchor.MiddleCenter);
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            TrazoH10.Colocar(texto.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), Vector2.zero);
            texto.rectTransform.offsetMin = new Vector2(60f, 130f);
            texto.rectTransform.offsetMax = new Vector2(-60f, -100f);
            BotonCyberfix boton = KitUI.CrearBoton(panel.transform, TextoBoton, Entender);
            VestidoH10.Boton(boton);
            KitUI.Colocar((RectTransform)boton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), TamanoBoton);
            VestidoH10.Fuente(panel.transform);
        }

        private void Encolar(string aviso, Action callback)
        {
            cola.Enqueue(new KeyValuePair<string, Action>(aviso, callback));
            if (!abierta)
            {
                Abrir();
            }
        }

        private void Abrir()
        {
            KeyValuePair<string, Action> siguiente = cola.Dequeue();
            texto.text = siguiente.Key;
            alEntender = siguiente.Value;
            if (!abierta)
            {
                abierta = true;
                escalaPrevia = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                Pausa.Fijar(true);
                bloqueoPrevio = Cursor.lockState;
                cursorVisiblePrevio = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                IJugador jugador = Registro<IJugador>.Actual;
                controlCongelado = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<ControladorAPie>() : null;
                if (controlCongelado != null && controlCongelado.ControlActivo)
                {
                    controlCongelado.ActivarControl(false);
                }
                else
                {
                    controlCongelado = null;
                }
                PilaPantallas.Abrir(this);
                KitUI.AsegurarEventSystem();
                aceptar.Enable();
                Sonidos.Reproducir(SonidoAbrir);
            }
            cuadroAbierta = Time.frameCount;
            raiz.SetActive(true);
        }

        private void Update()
        {
            if (abierta && Time.frameCount != cuadroAbierta && aceptar.WasPressedThisFrame())
            {
                Cerrar();
            }
        }

        private void Cerrar()
        {
            Sonidos.Reproducir(SonidoAceptar);
            Action callback = alEntender;
            alEntender = null;
            if (cola.Count > 0)
            {
                callback?.Invoke();
                Abrir();
                return;
            }
            Restaurar();
            callback?.Invoke();
        }

        private void Restaurar()
        {
            abierta = false;
            if (raiz != null)
            {
                raiz.SetActive(false);
            }
            aceptar.Disable();
            PilaPantallas.Cerrar(this);
            Time.timeScale = escalaPrevia;
            Pausa.Fijar(false);
            Cursor.lockState = bloqueoPrevio;
            Cursor.visible = cursorVisiblePrevio;
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }
    }
}
