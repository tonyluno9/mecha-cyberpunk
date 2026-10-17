using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Taller
{
    /// <summary>
    /// Pantalla del banco del taller (T-149): vive dentro del mecha, escucha a su <see cref="BancoTaller"/> y, cuando este pide
    /// opciones, muestra la vida, el dinero (rojo si es negativo) y las opciones con ↑ ↓; Enter elige y Esc sale sin cobrar.
    /// Mientras está abierta congela al jugador a pie; el control vuelve un cuadro después de cerrar (D-tony-67).
    /// </summary>
    public class PantallaBancoTaller : MonoBehaviour
    {
        [SerializeField] private ConfigPantallaBancoTaller config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoVida;
        [SerializeField] private Text textoDinero;
        [SerializeField] private RectTransform filaOpciones;
        [Tooltip("Texto que se copia por cada opción (debe estar apagado).")]
        [SerializeField] private Text plantillaOpcion;
        [SerializeField] private Text textoAyuda;
        [Tooltip("Mueve la elección hacia arriba (↑).")]
        [SerializeField] private InputAction arriba = new InputAction("BancoArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [Tooltip("Mueve la elección hacia abajo (↓).")]
        [SerializeField] private InputAction abajo = new InputAction("BancoAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [Tooltip("Elige la opción (Enter).")]
        [SerializeField] private InputAction confirmar = new InputAction("BancoEnter", InputActionType.Button, "<Keyboard>/enter");
        [Tooltip("Sale sin cobrar (Esc).")]
        [SerializeField] private InputAction salir = new InputAction("BancoEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly List<Text> textosOpciones = new List<Text>();
        private readonly List<OpcionReparacion> opciones = new List<OpcionReparacion>();
        private BancoTaller banco;
        private ControladorAPie controlCongelado;
        private int indice;
        private bool visible;

        public bool Visible => visible;
        public int IndiceElegido => indice;
        public int CantidadOpciones => textosOpciones.Count;
        public string Titulo => textoTitulo != null ? textoTitulo.text : string.Empty;
        public string Vida => textoVida != null ? textoVida.text : string.Empty;
        public string Dinero => textoDinero != null ? textoDinero.text : string.Empty;
        public Color ColorDinero => textoDinero != null ? textoDinero.color : Color.clear;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;
        public Canvas Canvas => GetComponent<Canvas>();

        /// <summary>Texto de una opción tal como se ve (la elegida lleva corchetes).</summary>
        public string TextoOpcion(int posicion) => textosOpciones[posicion].text;

        /// <summary>Texto de una opción sin los corchetes de elegida.</summary>
        public string TextoBaseOpcion(int posicion) => opciones[posicion].Texto;

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || textoVida == null || textoDinero == null
                || filaOpciones == null || plantillaOpcion == null || textoAyuda == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaBancoTaller de " + name, this);
                enabled = false;
                return;
            }
            plantillaOpcion.gameObject.SetActive(false);
            contenido.SetActive(false);
        }

        private void OnEnable()
        {
            banco = GetComponentInParent<BancoTaller>();
            if (banco == null)
            {
                Debug.LogWarning("[UI] PantallaBancoTaller de " + name + " no encontró un BancoTaller en su mecha.", this);
                return;
            }
            banco.OpcionesPedidas += Mostrar;
        }

        private void OnDisable()
        {
            if (banco != null)
            {
                banco.OpcionesPedidas -= Mostrar;
                banco = null;
            }
            Cerrar(true);
        }

        private void Mostrar(IReadOnlyList<OpcionReparacion> nuevas)
        {
            if (visible || nuevas == null || nuevas.Count == 0)
            {
                return;
            }
            opciones.Clear();
            opciones.AddRange(nuevas);
            indice = 0;

            fondo.color = config.ColorFondo;
            Preparar(textoTitulo, config.TextoTitulo, config.ColorElegido, config.TamanoTitulo);
            Preparar(textoVida, string.Empty, config.ColorNormal, config.TamanoTexto);
            Preparar(textoDinero, string.Empty, config.ColorDinero, config.TamanoTexto);
            Preparar(textoAyuda, config.TextoAyuda, config.ColorNormal, config.TamanoAyuda);
            ConstruirFilas();
            Pintar();

            visible = true;
            contenido.SetActive(true);
            arriba.Enable();
            abajo.Enable();
            confirmar.Enable();
            salir.Enable();

            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
        }

        private void Update()
        {
            if (!visible)
            {
                return;
            }
            PintarVidaYDinero();
            if (salir.WasPressedThisFrame())
            {
                Cerrar(false);
                return;
            }
            if (confirmar.WasPressedThisFrame())
            {
                OpcionReparacion elegida = opciones[indice];
                banco.Elegir(elegida);
                Cerrar(false);
                return;
            }
            int anterior = indice;
            if (arriba.WasPressedThisFrame())
            {
                indice = Mathf.Max(0, indice - 1);
            }
            if (abajo.WasPressedThisFrame())
            {
                indice = Mathf.Min(opciones.Count - 1, indice + 1);
            }
            if (indice != anterior)
            {
                Pintar();
            }
        }

        private void Cerrar(bool inmediato)
        {
            bool estabaVisible = visible;
            visible = false;
            arriba.Disable();
            abajo.Disable();
            confirmar.Disable();
            salir.Disable();
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
            if (!estabaVisible && controlCongelado == null)
            {
                return;
            }
            if (!inmediato && isActiveAndEnabled)
            {
                StartCoroutine(DevolverControlDespues());
            }
            else
            {
                DevolverControl();
            }
        }

        private IEnumerator DevolverControlDespues()
        {
            yield return null;
            DevolverControl();
        }

        private void DevolverControl()
        {
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }

        private void ConstruirFilas()
        {
            foreach (Text viejo in textosOpciones)
            {
                if (viejo != null)
                {
                    Destroy(viejo.gameObject);
                }
            }
            textosOpciones.Clear();
            for (int i = 0; i < opciones.Count; i++)
            {
                Text texto = Instantiate(plantillaOpcion, filaOpciones);
                texto.gameObject.SetActive(true);
                texto.fontSize = config.TamanoOpciones;
                textosOpciones.Add(texto);
            }
        }

        private void Pintar()
        {
            for (int i = 0; i < textosOpciones.Count; i++)
            {
                bool elegida = i == indice;
                textosOpciones[i].text = elegida ? string.Format(config.FormatoElegido, opciones[i].Texto) : opciones[i].Texto;
                textosOpciones[i].color = elegida ? config.ColorElegido : config.ColorNormal;
            }
            PintarVidaYDinero();
        }

        private void PintarVidaYDinero()
        {
            if (banco != null)
            {
                textoVida.text = string.Format(config.FormatoVida, Mathf.RoundToInt(banco.VidaActual), Mathf.RoundToInt(banco.VidaMaxima));
            }
            ICartera cartera = Registro<ICartera>.Actual;
            int dinero = cartera != null ? cartera.Dinero : 0;
            textoDinero.text = string.Format(config.FormatoDinero, FormatoDinero.Texto(dinero));
            textoDinero.color = dinero < 0 ? config.ColorDeuda : config.ColorDinero;
        }

        private static void Preparar(Text texto, string contenidoTexto, Color color, int tamano)
        {
            texto.text = contenidoTexto;
            texto.color = color;
            texto.fontSize = tamano;
        }
    }
}
