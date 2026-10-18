using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Taller
{
    /// <summary>
    /// Pantalla "PIEZAS DEL MECHA" del banco del taller (T-169). Vive dentro del mecha y se abre cuando su <see cref="BancoTaller"/>
    /// avisa <see cref="BancoTaller.PiezasPedidas"/>. Dos niveles: las 5 ranuras con la pieza puesta (y la vida, el daño y la velocidad
    /// del mecha armado) y, al entrar a una ranura, sus opciones de <see cref="TallerPiezas"/> con la diferencia contra lo puesto.
    /// Mientras está abierta congela al jugador a pie; el control vuelve un cuadro después de cerrar (D-tony-90).
    /// </summary>
    public class PantallaPiezasTaller : MonoBehaviour
    {
        private const string FormatoNumero = "0.##";
        private const string FormatoNumeroVelocidad = "0.0#";
        private const string FormatoDiferencia = "+0.##;−0.##;0";
        private const string FormatoColor = "<color=#{0}>{1}</color>";
        private const string Separador = " · ";
        private const float MinimoDiferencia = 0.005f;

        [SerializeField] private ConfigPantallaPiezas config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoSubtitulo;
        [SerializeField] private Text textoDinero;
        [SerializeField] private RectTransform listaFilas;
        [Tooltip("Texto que se copia por cada fila (debe estar apagado).")]
        [SerializeField] private Text plantillaFila;
        [SerializeField] private Text textoEstadisticas;
        [SerializeField] private Text textoDiferencia;
        [SerializeField] private Text textoAviso;
        [SerializeField] private Text textoAyuda;
        [Tooltip("Mueve la elección hacia arriba (↑).")]
        [SerializeField] private InputAction arriba = new InputAction("PiezasArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [Tooltip("Mueve la elección hacia abajo (↓).")]
        [SerializeField] private InputAction abajo = new InputAction("PiezasAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [Tooltip("Entra a la ranura (→).")]
        [SerializeField] private InputAction derecha = new InputAction("PiezasDerecha", InputActionType.Button, "<Keyboard>/rightArrow");
        [Tooltip("Regresa a las ranuras (←).")]
        [SerializeField] private InputAction izquierda = new InputAction("PiezasIzquierda", InputActionType.Button, "<Keyboard>/leftArrow");
        [Tooltip("Entra a la ranura o elige la pieza (Enter).")]
        [SerializeField] private InputAction confirmar = new InputAction("PiezasEnter", InputActionType.Button, "<Keyboard>/enter");
        [Tooltip("Regresa o cierra (Esc).")]
        [SerializeField] private InputAction salir = new InputAction("PiezasEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly List<Text> filas = new List<Text>();
        private readonly List<string> textosBase = new List<string>();
        private List<OpcionPieza> opciones = new List<OpcionPieza>();
        private BancoTaller banco;
        private TallerPiezas taller;
        private EquipoMecha equipo;
        private ControladorAPie controlCongelado;
        private NivelPantallaPiezas nivel;
        private int indiceRanura;
        private int indicePieza;
        private int dineroPintado;
        private int frameApertura;
        private string aviso = string.Empty;
        private bool avisoEsError;

        public bool Visible => nivel != NivelPantallaPiezas.Cerrada;
        public NivelPantallaPiezas Nivel => nivel;

        /// <summary>Fila elegida en el nivel actual (ranura o pieza).</summary>
        public int IndiceElegido => nivel == NivelPantallaPiezas.Piezas ? indicePieza : indiceRanura;
        public int CantidadFilas => textosBase.Count;
        public string Titulo => Leer(textoTitulo);
        public string Subtitulo => Leer(textoSubtitulo);
        public string Dinero => Leer(textoDinero);
        public string Estadisticas => Leer(textoEstadisticas);
        public string Diferencia => Leer(textoDiferencia);
        public string Aviso => Leer(textoAviso);
        public string Ayuda => Leer(textoAyuda);
        public Canvas Canvas => GetComponent<Canvas>();

        /// <summary>Texto de una fila tal como se ve (la elegida lleva corchetes).</summary>
        public string TextoFila(int posicion) => filas[posicion].text;

        /// <summary>Texto de una fila sin los corchetes de elegida.</summary>
        public string TextoBaseFila(int posicion) => textosBase[posicion];

        public Color ColorFila(int posicion) => filas[posicion].color;

        private static string Leer(Text texto) => texto != null ? texto.text : string.Empty;

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || textoSubtitulo == null
                || textoDinero == null || listaFilas == null || plantillaFila == null || textoEstadisticas == null
                || textoDiferencia == null || textoAviso == null || textoAyuda == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaPiezasTaller de " + name, this);
                enabled = false;
                return;
            }
            plantillaFila.gameObject.SetActive(false);
            contenido.SetActive(false);
        }

        private void OnEnable()
        {
            banco = GetComponentInParent<BancoTaller>();
            if (banco == null)
            {
                Debug.LogWarning("[UI] PantallaPiezasTaller de " + name + " no encontró un BancoTaller en su mecha.", this);
                return;
            }
            taller = banco.GetComponent<TallerPiezas>();
            equipo = banco.GetComponent<EquipoMecha>();
            banco.PiezasPedidas += Mostrar;
        }

        private void OnDisable()
        {
            if (banco != null)
            {
                banco.PiezasPedidas -= Mostrar;
                banco = null;
            }
            taller = null;
            equipo = null;
            Cerrar(true);
        }

        private void Mostrar()
        {
            if (Visible || taller == null || equipo == null || !taller.Disponible)
            {
                return;
            }
            nivel = NivelPantallaPiezas.Ranuras;
            indiceRanura = 0;
            indicePieza = 0;
            aviso = string.Empty;
            frameApertura = Time.frameCount;

            fondo.color = config.ColorFondo;
            Preparar(textoTitulo, config.TextoTitulo, config.ColorTitulo, config.TamanoTitulo);
            Preparar(textoSubtitulo, string.Empty, config.ColorTexto, config.TamanoSubtitulo);
            Preparar(textoDinero, string.Empty, config.ColorDinero, config.TamanoDinero);
            Preparar(textoEstadisticas, string.Empty, config.ColorTexto, config.TamanoEstadisticas);
            Preparar(textoDiferencia, string.Empty, config.ColorTexto, config.TamanoDiferencia);
            Preparar(textoAviso, string.Empty, config.ColorAviso, config.TamanoAviso);
            Preparar(textoAyuda, string.Empty, config.ColorTexto, config.TamanoAyuda);
            Pintar();

            contenido.SetActive(true);
            arriba.Enable();
            abajo.Enable();
            derecha.Enable();
            izquierda.Enable();
            confirmar.Enable();
            salir.Enable();
            MantenerCongelado();
        }

        // Si la pantalla de reparar acaba de cerrarse, su coroutine devuelve el control un cuadro después: aquí se vuelve a congelar.
        private void MantenerCongelado()
        {
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
            if (!Visible)
            {
                return;
            }
            if (taller == null || !taller.Disponible)
            {
                Cerrar(false);
                return;
            }
            MantenerCongelado();
            ICartera cartera = Registro<ICartera>.Actual;
            if ((cartera != null ? cartera.Dinero : 0) != dineroPintado)
            {
                RefrescarOpciones();
                Pintar();
            }
            if (Time.frameCount == frameApertura)
            {
                return;
            }
            if (nivel == NivelPantallaPiezas.Ranuras)
            {
                LeerRanuras();
            }
            else
            {
                LeerPiezas();
            }
        }

        private void LeerRanuras()
        {
            if (salir.WasPressedThisFrame())
            {
                Cerrar(false);
                return;
            }
            if (confirmar.WasPressedThisFrame() || derecha.WasPressedThisFrame())
            {
                EntrarARanura();
                return;
            }
            int anterior = indiceRanura;
            if (arriba.WasPressedThisFrame())
            {
                indiceRanura = Mathf.Max(0, indiceRanura - 1);
            }
            if (abajo.WasPressedThisFrame())
            {
                indiceRanura = Mathf.Min(equipo.Puestas.Count - 1, indiceRanura + 1);
            }
            if (indiceRanura != anterior)
            {
                PonerAviso(string.Empty, false);
                Pintar();
            }
        }

        private void LeerPiezas()
        {
            if (salir.WasPressedThisFrame() || izquierda.WasPressedThisFrame())
            {
                VolverARanuras();
                return;
            }
            if (confirmar.WasPressedThisFrame())
            {
                ElegirPieza();
                return;
            }
            int anterior = indicePieza;
            if (arriba.WasPressedThisFrame())
            {
                indicePieza = Mathf.Max(0, indicePieza - 1);
            }
            if (abajo.WasPressedThisFrame())
            {
                indicePieza = Mathf.Min(opciones.Count - 1, indicePieza + 1);
            }
            if (indicePieza != anterior)
            {
                PonerAviso(string.Empty, false);
                Pintar();
            }
        }

        private void EntrarARanura()
        {
            List<OpcionPieza> nuevas = taller.Opciones((RanuraPieza)indiceRanura);
            if (nuevas.Count == 0)
            {
                PonerAviso(config.TextoSinPiezas, true);
                Pintar();
                return;
            }
            opciones = nuevas;
            nivel = NivelPantallaPiezas.Piezas;
            indicePieza = 0;
            PonerAviso(string.Empty, false);
            Pintar();
        }

        private void VolverARanuras()
        {
            nivel = NivelPantallaPiezas.Ranuras;
            PonerAviso(string.Empty, false);
            Pintar();
        }

        // Recalcula la lista de piezas (el dinero o la liga pudieron cambiar). Si ya no queda ninguna, regresa a las ranuras.
        private void RefrescarOpciones()
        {
            if (nivel != NivelPantallaPiezas.Piezas)
            {
                return;
            }
            opciones = taller.Opciones((RanuraPieza)indiceRanura);
            if (opciones.Count == 0)
            {
                nivel = NivelPantallaPiezas.Ranuras;
                return;
            }
            indicePieza = Mathf.Clamp(indicePieza, 0, opciones.Count - 1);
        }

        private void ElegirPieza()
        {
            RefrescarOpciones();
            if (nivel != NivelPantallaPiezas.Piezas)
            {
                Pintar();
                return;
            }
            OpcionPieza elegida = opciones[indicePieza];
            if (elegida.Tipo == TipoOpcionPieza.Bloqueada)
            {
                Pintar();
                return;
            }
            if (!elegida.PuedeElegir)
            {
                PonerAviso(TextoMotivo(elegida.Motivo), true);
                Pintar();
                return;
            }
            ResultadoTallerPiezas resultado = taller.Elegir(elegida);
            if (!resultado.Hecho)
            {
                PonerAviso(config.TextoNoSePudo, true);
                Pintar();
                return;
            }
            string texto = string.Format(config.FormatoPuesta, elegida.Pieza.Nombre);
            if (resultado.SeVendio && resultado.Quitada != null)
            {
                texto += Separador + string.Format(config.FormatoVendida, FormatoDinero.Texto(resultado.Vendida), resultado.Quitada.Nombre);
            }
            nivel = NivelPantallaPiezas.Ranuras;
            PonerAviso(texto, false);
            Pintar();
        }

        private string TextoMotivo(MotivoNoElegible motivo)
        {
            switch (motivo)
            {
                case MotivoNoElegible.NoAlcanza: return config.TextoNoAlcanza;
                case MotivoNoElegible.TallerBloqueado: return config.TextoTallerBloqueado;
                default: return config.TextoNoSePudo;
            }
        }

        private void PonerAviso(string texto, bool esError)
        {
            aviso = texto;
            avisoEsError = esError;
        }

        private void Cerrar(bool inmediato)
        {
            bool estabaVisible = Visible;
            nivel = NivelPantallaPiezas.Cerrada;
            arriba.Disable();
            abajo.Disable();
            derecha.Disable();
            izquierda.Disable();
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

        private void Pintar()
        {
            if (nivel == NivelPantallaPiezas.Ranuras)
            {
                PintarRanuras();
            }
            else if (nivel == NivelPantallaPiezas.Piezas)
            {
                PintarPiezas();
            }
            PintarComun();
        }

        private void PintarRanuras()
        {
            int cantidad = equipo.Puestas.Count;
            AsegurarFilas(cantidad);
            for (int i = 0; i < cantidad; i++)
            {
                DatosPieza pieza = equipo.Puestas[i];
                PintarFila(i, pieza != null ? pieza.Nombre : string.Empty,
                    pieza != null ? config.ColorDeRama(pieza.Rama) : config.ColorBloqueada, i == indiceRanura);
            }
            textoSubtitulo.text = config.SubtituloRanuras;
            textoDiferencia.text = string.Empty;
            textoAyuda.text = config.AyudaRanuras;
        }

        private void PintarPiezas()
        {
            AsegurarFilas(opciones.Count);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            for (int i = 0; i < opciones.Count; i++)
            {
                OpcionPieza opcion = opciones[i];
                Color color = opcion.Tipo == TipoOpcionPieza.Bloqueada ? config.ColorBloqueada : config.ColorDeRama(opcion.Pieza.Rama);
                PintarFila(i, TextoOpcion(opcion, inventario), color, i == indicePieza);
            }
            DatosPieza puesta = equipo.Puesta((RanuraPieza)indiceRanura);
            textoSubtitulo.text = string.Format(config.FormatoSubtituloPiezas, puesta != null ? puesta.Nombre : string.Empty);
            textoDiferencia.text = TextoDiferencia(opciones[Mathf.Clamp(indicePieza, 0, opciones.Count - 1)]);
            textoAyuda.text = config.AyudaPiezas;
        }

        private void PintarComun()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            dineroPintado = cartera != null ? cartera.Dinero : 0;
            textoDinero.text = string.Format(config.FormatoDinero, FormatoDinero.Texto(dineroPintado));
            textoDinero.color = dineroPintado < 0 ? config.ColorDeuda : config.ColorDinero;

            ValoresMecha valores = equipo.Valores;
            CultureInfo cultura = CultureInfo.InvariantCulture;
            textoEstadisticas.text = string.Format(config.FormatoEstadisticas, valores.vidaMaxima.ToString(FormatoNumero, cultura),
                valores.danioGolpe.ToString(FormatoNumero, cultura), valores.velocidadMaxima.ToString(FormatoNumeroVelocidad, cultura));

            textoAviso.text = aviso;
            textoAviso.color = avisoEsError ? config.ColorError : config.ColorAviso;
        }

        private string TextoOpcion(OpcionPieza opcion, ServicioInventario inventario)
        {
            switch (opcion.Tipo)
            {
                case TipoOpcionPieza.Poner:
                    return string.Format(config.FormatoPoner, opcion.Pieza.Nombre, inventario != null ? inventario.Cantidad(opcion.Pieza) : 0);
                case TipoOpcionPieza.Comprar:
                    return string.Format(config.FormatoComprar, opcion.Pieza.Nombre, FormatoDinero.Texto(opcion.Precio));
                default:
                    return string.Format(config.FormatoBloqueada, opcion.Pieza.Nombre, opcion.Pieza.Nivel);
            }
        }

        // "VIDA +28 · DAÑO −1 · VEL +0.25": verde lo que sube y magenta lo que baja; lo que no cambia no sale.
        private string TextoDiferencia(OpcionPieza opcion)
        {
            var partes = new List<string>();
            AgregarDiferencia(partes, config.EtiquetaVida, opcion.DiferenciaVida);
            AgregarDiferencia(partes, config.EtiquetaDanio, opcion.DiferenciaDanio);
            AgregarDiferencia(partes, config.EtiquetaVelocidad, opcion.DiferenciaVelocidad);
            return partes.Count == 0 ? config.TextoSinCambios : string.Join(Separador, partes);
        }

        private void AgregarDiferencia(List<string> partes, string etiqueta, float diferencia)
        {
            if (Mathf.Abs(diferencia) < MinimoDiferencia)
            {
                return;
            }
            Color color = diferencia > 0f ? config.ColorSube : config.ColorBaja;
            string numero = diferencia.ToString(FormatoDiferencia, CultureInfo.InvariantCulture);
            partes.Add(string.Format(FormatoColor, ColorUtility.ToHtmlStringRGB(color), etiqueta + " " + numero));
        }

        private void PintarFila(int posicion, string texto, Color color, bool elegida)
        {
            textosBase[posicion] = texto;
            filas[posicion].text = elegida ? string.Format(config.FormatoElegida, texto) : texto;
            filas[posicion].color = color;
        }

        private void AsegurarFilas(int cantidad)
        {
            while (filas.Count < cantidad)
            {
                Text texto = Instantiate(plantillaFila, listaFilas);
                texto.fontSize = config.TamanoFilas;
                filas.Add(texto);
            }
            while (textosBase.Count < cantidad)
            {
                textosBase.Add(string.Empty);
            }
            if (textosBase.Count > cantidad)
            {
                textosBase.RemoveRange(cantidad, textosBase.Count - cantidad);
            }
            for (int i = 0; i < filas.Count; i++)
            {
                filas[i].gameObject.SetActive(i < cantidad);
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
