using System;
using System.Collections.Generic;
using System.Globalization;
using MechaCyberpunk.Economia;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Peleas
{
    /// <summary>
    /// Pantalla de apuesta del mostrador de peleas (T-145): Canvas Overlay a pantalla completa con el rival, el premio, el momio,
    /// la fila de montos (← → eligen, saltando los que no se pueden pagar), la pieza en juego y la ayuda. Enter avisa
    /// <c>alPelear(monto)</c> y Esc avisa <c>alSalir</c>; quien la abre decide qué hacer y la cierra con <see cref="Ocultar"/>.
    /// </summary>
    public class PantallaApuesta : MonoBehaviour
    {
        [SerializeField] private ConfigPantallaApuesta config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoRival;
        [SerializeField] private Text textoPremio;
        [SerializeField] private Text textoMomio;
        [SerializeField] private RectTransform filaMontos;
        [Tooltip("Texto que se copia por cada opción de la fila de montos (debe estar apagado).")]
        [SerializeField] private Text plantillaMonto;
        [SerializeField] private Text textoPieza;
        [SerializeField] private Text textoAviso;
        [SerializeField] private Text textoAyuda;
        [Tooltip("Mueve la elección a la izquierda (←).")]
        [SerializeField] private InputAction izquierda = new InputAction("ApuestaIzquierda", InputActionType.Button, "<Keyboard>/leftArrow");
        [Tooltip("Mueve la elección a la derecha (→).")]
        [SerializeField] private InputAction derecha = new InputAction("ApuestaDerecha", InputActionType.Button, "<Keyboard>/rightArrow");
        [Tooltip("Confirma y pelea (Enter).")]
        [SerializeField] private InputAction confirmar = new InputAction("ApuestaEnter", InputActionType.Button, "<Keyboard>/enter");
        [Tooltip("Sale sin pelear (Esc).")]
        [SerializeField] private InputAction salir = new InputAction("ApuestaEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly List<Text> textosMontos = new List<Text>();
        private SeleccionApuesta seleccion;
        private Action<int> alPelear;
        private Action alSalir;
        private bool visible;
        private int cuadroAbierta;

        public bool Visible => visible;
        /// <summary>Monto elegido ($); 0 es sin apuesta.</summary>
        public int Monto => seleccion != null ? seleccion.Monto : 0;
        public int IndiceElegido => seleccion != null ? seleccion.Indice : 0;
        public string Titulo => textoTitulo != null ? textoTitulo.text : string.Empty;
        public string Rival => textoRival != null ? textoRival.text : string.Empty;
        public string Premio => textoPremio != null ? textoPremio.text : string.Empty;
        public string Momio => textoMomio != null ? textoMomio.text : string.Empty;
        public string Pieza => textoPieza != null && textoPieza.gameObject.activeSelf ? textoPieza.text : string.Empty;
        public string Aviso => textoAviso != null && textoAviso.gameObject.activeSelf ? textoAviso.text : string.Empty;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;
        public int CantidadOpciones => textosMontos.Count;
        public Canvas Canvas => GetComponent<Canvas>();

        /// <summary>Texto del aviso "no alcanza" de la configuración.</summary>
        public string TextoNoAlcanza => config != null ? config.TextoNoAlcanza : string.Empty;

        /// <summary>Texto de una opción de la fila de montos.</summary>
        public string TextoOpcion(int indice) => textosMontos[indice].text;

        /// <summary>Color de una opción de la fila de montos (elegido, normal o apagado).</summary>
        public Color ColorOpcion(int indice) => textosMontos[indice].color;

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || textoRival == null || textoPremio == null
                || textoMomio == null || filaMontos == null || plantillaMonto == null || textoPieza == null || textoAviso == null
                || textoAyuda == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaApuesta de " + name, this);
                enabled = false;
                return;
            }
            plantillaMonto.gameObject.SetActive(false);
            contenido.SetActive(false);
        }

        private void OnDisable()
        {
            DesactivarEntrada();
            visible = false;
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        /// <summary>
        /// Abre la pantalla. <paramref name="puedeApostar"/> dice si un monto se puede pagar (los que no, se ven apagados).
        /// <paramref name="pieza"/> puede ser null (sin pieza en juego). Empieza en "SIN APUESTA".
        /// </summary>
        public void Mostrar(string rival, int premio, float momio, int[] montos, DatosObjeto pieza, int apuestaMinimaPorPieza,
            Func<int, bool> puedeApostar, Action<int> alPelear, Action alSalir)
        {
            this.alPelear = alPelear;
            this.alSalir = alSalir;
            seleccion = new SeleccionApuesta(montos, puedeApostar);

            fondo.color = config.ColorFondo;
            Preparar(textoTitulo, config.TextoTitulo, config.ColorElegido, config.TamanoTitulo);
            Preparar(textoRival, string.Format(config.FormatoRival, rival), config.ColorNormal, config.TamanoTexto);
            Preparar(textoPremio, string.Format(config.FormatoPremio, FormatoDinero.Texto(premio)), config.ColorPremio, config.TamanoTexto);
            Preparar(textoMomio, string.Format(config.FormatoMomio, momio.ToString("0.##", CultureInfo.InvariantCulture)),
                config.ColorNormal, config.TamanoTexto);
            Preparar(textoAyuda, config.TextoAyuda, config.ColorNormal, config.TamanoAyuda);
            Preparar(textoAviso, string.Empty, config.ColorAviso, config.TamanoTexto);
            textoAviso.gameObject.SetActive(false);
            if (pieza != null)
            {
                Preparar(textoPieza, string.Format(config.FormatoPieza, pieza.Nombre.ToUpperInvariant(), FormatoDinero.Texto(apuestaMinimaPorPieza)),
                    config.ColorPremio, config.TamanoAyuda + 8);
                textoPieza.gameObject.SetActive(true);
            }
            else
            {
                textoPieza.gameObject.SetActive(false);
            }

            ConstruirFila();
            Pintar();
            cuadroAbierta = Time.frameCount;
            visible = true;
            contenido.SetActive(true);
            izquierda.Enable();
            derecha.Enable();
            confirmar.Enable();
            salir.Enable();
        }

        /// <summary>Muestra un aviso en rojo bajo la fila de montos (por ejemplo, que no alcanzó).</summary>
        public void MostrarAviso(string texto)
        {
            textoAviso.text = texto;
            textoAviso.gameObject.SetActive(true);
        }

        public void Ocultar()
        {
            visible = false;
            DesactivarEntrada();
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        private void DesactivarEntrada()
        {
            izquierda.Disable();
            derecha.Disable();
            confirmar.Disable();
            salir.Disable();
        }

        private void Update()
        {
            // La tecla que abrió la pantalla en este cuadro (por ejemplo, el Enter de la cartelera) no cuenta como una de la pantalla.
            if (!visible || Time.frameCount == cuadroAbierta)
            {
                return;
            }
            if (salir.WasPressedThisFrame())
            {
                alSalir?.Invoke();
                return;
            }
            if (confirmar.WasPressedThisFrame())
            {
                alPelear?.Invoke(seleccion.Monto);
                return;
            }
            bool movio = false;
            if (izquierda.WasPressedThisFrame())
            {
                movio |= seleccion.Mover(-1);
            }
            if (derecha.WasPressedThisFrame())
            {
                movio |= seleccion.Mover(1);
            }
            if (movio)
            {
                textoAviso.gameObject.SetActive(false);
                Pintar();
            }
        }

        private void ConstruirFila()
        {
            foreach (Text viejo in textosMontos)
            {
                if (viejo != null)
                {
                    Destroy(viejo.gameObject);
                }
            }
            textosMontos.Clear();
            for (int i = 0; i < seleccion.Cantidad; i++)
            {
                Text texto = Instantiate(plantillaMonto, filaMontos);
                texto.gameObject.SetActive(true);
                texto.fontSize = config.TamanoMontos;
                textosMontos.Add(texto);
            }
        }

        private void Pintar()
        {
            for (int i = 0; i < textosMontos.Count; i++)
            {
                string etiqueta = i == 0 ? config.TextoSinApuesta : string.Format(config.FormatoMonto, FormatoDinero.Texto(seleccion.MontoDe(i)));
                bool elegido = i == seleccion.Indice;
                textosMontos[i].text = elegido ? string.Format(config.FormatoElegido, etiqueta) : etiqueta;
                textosMontos[i].color = elegido ? config.ColorElegido : seleccion.Disponible(i) ? config.ColorNormal : config.ColorApagado;
            }
        }

        private static void Preparar(Text texto, string contenidoTexto, Color color, int tamano)
        {
            texto.text = contenidoTexto;
            texto.color = color;
            texto.fontSize = tamano;
        }
    }
}
