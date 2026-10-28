using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Progresion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>
    /// Pantalla de la clínica (T-193): un Canvas Overlay con los 4 espacios (lo instalado y la opción siguiente con su precio, en gris
    /// si la liga no la abre y en naranja suave si no alcanza), la descripción de la elegida y la vista previa del cuerpo con esa
    /// opción puesta de prueba. ↑ ↓ eligen; Enter pide confirmar y el segundo Enter avisa <c>alInstalar</c>; Esc sale.
    /// Quien la abre cobra y la cierra con <see cref="Ocultar"/>. La tecla que la abre en el mismo cuadro no cuenta.
    /// </summary>
    public class PantallaClinica : MonoBehaviour
    {
        private const int Espacios = 4;

        [SerializeField] private ConfigPantallaClinica config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoDinero;
        [Tooltip("Una por espacio, en el orden de EspacioImplante: lo instalado.")]
        [SerializeField] private Text[] textosEspacio;
        [Tooltip("Una por espacio, en el orden de EspacioImplante: la opción siguiente.")]
        [SerializeField] private Text[] textosOpcion;
        [SerializeField] private Text textoDescripcion;
        [SerializeField] private Text textoAviso;
        [SerializeField] private Text textoAyuda;
        [SerializeField] private RawImage imagenVista;
        [SerializeField] private VistaPreviaApariencia vista;
        [Tooltip("Elige la fila de arriba (↑).")]
        [SerializeField] private InputAction arriba = new InputAction("ClinicaArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [Tooltip("Elige la fila de abajo (↓).")]
        [SerializeField] private InputAction abajo = new InputAction("ClinicaAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [Tooltip("Pide confirmar y confirma (Enter).")]
        [SerializeField] private InputAction confirmar = new InputAction("ClinicaEnter", InputActionType.Button, "<Keyboard>/enter");
        [Tooltip("Cancela la confirmación o sale (Esc).")]
        [SerializeField] private InputAction salir = new InputAction("ClinicaEsc", InputActionType.Button, "<Keyboard>/escape");

        private ServicioImplantes servicio;
        private Action<DatosImplante> alInstalar;
        private Action alSalir;
        private bool visible;
        private bool confirmando;
        private int indice;
        private int cuadroAbierta;

        public bool Visible => visible;
        public bool Confirmando => confirmando;
        public int Indice => indice;
        public int CantidadFilas => Espacios;
        public ConfigPantallaClinica Config => config;
        public VistaPreviaApariencia Vista => vista;
        public string Titulo => textoTitulo != null ? textoTitulo.text : string.Empty;
        public string Dinero => textoDinero != null ? textoDinero.text : string.Empty;
        public string Descripcion => textoDescripcion != null ? textoDescripcion.text : string.Empty;
        public string Aviso => textoAviso != null && textoAviso.gameObject.activeSelf ? textoAviso.text : string.Empty;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;
        public Canvas Canvas => GetComponent<Canvas>();

        /// <summary>Lo instalado en la fila: "OJOS · OJOS DE LECHUZA" o "OJOS · —".</summary>
        public string TextoEspacio(int fila) => textosEspacio[fila].text;

        /// <summary>La opción siguiente de la fila (con la marca si es la elegida).</summary>
        public string TextoOpcion(int fila) => textosOpcion[fila].text;

        public Color ColorOpcion(int fila) => textosOpcion[fila].color;

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || textoDinero == null || textoDescripcion == null
                || textoAviso == null || textoAyuda == null || imagenVista == null || vista == null
                || textosEspacio == null || textosEspacio.Length != Espacios || textosOpcion == null || textosOpcion.Length != Espacios)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaClinica de " + name, this);
                enabled = false;
                return;
            }
            contenido.SetActive(false);
        }

        private void OnDisable()
        {
            Ocultar();
        }

        /// <summary>Abre la pantalla con el primer espacio elegido. <paramref name="alInstalar"/> recibe el implante confirmado.</summary>
        public void Mostrar(ServicioImplantes servicio, Action<DatosImplante> alInstalar, Action alSalir)
        {
            this.servicio = servicio;
            this.alInstalar = alInstalar;
            this.alSalir = alSalir;
            indice = 0;
            confirmando = false;
            fondo.color = config.ColorFondo;
            Preparar(textoTitulo, config.TextoTitulo, config.ColorElegido, config.TamanoTitulo);
            Preparar(textoAyuda, config.TextoAyuda, config.ColorNormal, config.TamanoAyuda);
            textoAviso.gameObject.SetActive(false);
            vista.Mostrar(true);
            imagenVista.texture = vista.Textura;
            visible = true;
            cuadroAbierta = Time.frameCount;
            contenido.SetActive(true);
            Pintar();
            arriba.Enable();
            abajo.Enable();
            confirmar.Enable();
            salir.Enable();
        }

        public void Ocultar()
        {
            visible = false;
            confirmando = false;
            if (arriba != null)
            {
                arriba.Disable();
                abajo.Disable();
                confirmar.Disable();
                salir.Disable();
            }
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
            if (vista != null && vista.Cuerpo != null && vista.EstaEncendida)
            {
                vista.Cuerpo.QuitarPrueba();
                vista.Mostrar(false);
            }
        }

        /// <summary>Muestra un aviso en la línea de abajo, en naranja suave.</summary>
        public void MostrarAviso(string texto)
        {
            confirmando = false;
            Preparar(textoAviso, texto, config.ColorAviso, config.TamanoOpcion);
            textoAviso.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!visible || Time.frameCount == cuadroAbierta)
            {
                return;
            }
            if (salir.WasPressedThisFrame())
            {
                if (confirmando)
                {
                    confirmando = false;
                    textoAviso.gameObject.SetActive(false);
                }
                else
                {
                    alSalir?.Invoke();
                }
                return;
            }
            if (confirmar.WasPressedThisFrame())
            {
                AlConfirmar();
                return;
            }
            int paso = (abajo.WasPressedThisFrame() ? 1 : 0) - (arriba.WasPressedThisFrame() ? 1 : 0);
            if (paso != 0)
            {
                int nuevo = Mathf.Clamp(indice + paso, 0, Espacios - 1);
                confirmando = false;
                textoAviso.gameObject.SetActive(false);
                if (nuevo != indice)
                {
                    indice = nuevo;
                    Pintar();
                }
            }
        }

        private void AlConfirmar()
        {
            OfertaImplante oferta = servicio.Opcion((EspacioImplante)indice);
            if (confirmando)
            {
                confirmando = false;
                textoAviso.gameObject.SetActive(false);
                if (oferta.Estado == EstadoOfertaImplante.Instalar)
                {
                    alInstalar?.Invoke(oferta.Implante);
                }
                return;
            }
            switch (oferta.Estado)
            {
                case EstadoOfertaImplante.Instalar:
                    confirmando = true;
                    Preparar(textoAviso, string.Format(config.FormatoConfirmar, FormatoDinero.Texto(oferta.Costo)), config.ColorPrecio, config.TamanoOpcion);
                    textoAviso.gameObject.SetActive(true);
                    break;
                case EstadoOfertaImplante.SinDinero:
                    MostrarAviso(config.TextoAvisoSinDinero);
                    break;
            }
        }

        private void Pintar()
        {
            Preparar(textoDinero, string.Format(config.FormatoDinero, FormatoDinero.Texto(Registro<ICartera>.Actual?.Dinero ?? 0)),
                config.ColorNormal, config.TamanoDinero);
            for (int i = 0; i < Espacios; i++)
            {
                EspacioImplante espacio = (EspacioImplante)i;
                string instalado = servicio.Nivel(espacio) > 0 ? servicio.Nombre(espacio) : config.TextoVacio;
                OfertaImplante oferta = servicio.Opcion(espacio);
                bool elegida = i == indice;
                Preparar(textosEspacio[i], string.Format(config.FormatoEspacio, config.NombreEspacio(i), instalado),
                    elegida ? config.ColorElegido : config.ColorNormal, config.TamanoEspacio);
                string texto = TextoDeOferta(oferta, servicio.Nivel(espacio));
                Color color = oferta.Estado == EstadoOfertaImplante.LigaBaja ? config.ColorApagado
                    : oferta.Estado == EstadoOfertaImplante.SinDinero ? config.ColorAviso
                    : elegida ? config.ColorElegido : config.ColorNormal;
                Preparar(textosOpcion[i], elegida ? string.Format(config.FormatoElegido, texto) : texto, color, config.TamanoOpcion);
            }

            EspacioImplante elegido = (EspacioImplante)indice;
            OfertaImplante ofertaElegida = servicio.Opcion(elegido);
            DatosImplante mostrado = ofertaElegida.Implante != null ? ofertaElegida.Implante : servicio.ImplanteInstalado(elegido);
            Preparar(textoDescripcion, mostrado != null ? mostrado.Descripcion : string.Empty, config.ColorNormal, config.TamanoDescripcion);

            // La vista previa lleva puesta la opción elegida de prueba (se quita al mover la elección o al salir).
            vista.Cuerpo.QuitarPrueba();
            if (ofertaElegida.Implante != null)
            {
                vista.Cuerpo.MostrarPrueba(elegido, ofertaElegida.Implante.Nivel);
            }
        }

        private string TextoDeOferta(OfertaImplante oferta, int nivelInstalado)
        {
            switch (oferta.Estado)
            {
                case EstadoOfertaImplante.YaLoMejor:
                    return config.TextoLoMejor;
                case EstadoOfertaImplante.LigaBaja:
                    return string.Format(config.FormatoLiga, oferta.Implante.Nombre, oferta.LigaQueAbre);
                case EstadoOfertaImplante.SinDinero:
                    return string.Format(config.FormatoSinDinero, oferta.Implante.Nombre);
                default:
                    string precio = string.Format(config.FormatoPrecio, oferta.Implante.Nombre, FormatoDinero.Texto(oferta.Costo));
                    return nivelInstalado > 0 ? precio + " · " + config.TextoDiferencia : precio;
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
