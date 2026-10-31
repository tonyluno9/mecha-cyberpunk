using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Hud;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Misiones
{
    /// <summary>Pestañas del teléfono.</summary>
    public enum PestanaTelefono
    {
        Contratos,
        Mision,
        Reputacion,
        Favores,
        Mensajes
    }

    /// <summary>Qué muestra la pestaña CONTRATOS.</summary>
    public enum EstadoContratos
    {
        Lista,
        NadieContesta,
        YaNoTeLlaman
    }

    /// <summary>
    /// El teléfono del jugador a pie (T-186): la tecla T lo abre y lo cierra. Tres pestañas (← →): CONTRATOS (las corporativas, solo de día,
    /// con la <see cref="PantallaMisiones"/> embebida), MISIÓN (la que está en curso, con abandonar y confirmación) y REPUTACIÓN (dos barras).
    /// No abre con otra pantalla abierta (el control a pie ya congelado), en carro, en mecha ni en KO. Congela el control mientras está abierto.
    /// </summary>
    public class PantallaTelefono : MonoBehaviour
    {
        private const int Pestanas = 5;
        private const int BarrasReputacion = 2;

        [SerializeField] private ConfigPantallaMisiones config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoPestanaContratos;
        [SerializeField] private Text textoPestanaMision;
        [SerializeField] private Text textoPestanaReputacion;
        [SerializeField] private Text textoPestanaFavores;
        [Tooltip("T-449: la quinta pestaña. Opcional: sin este texto la pestaña existe pero no se rotula.")]
        [SerializeField] private Text textoPestanaMensajes;
        [Tooltip("Texto de las pestañas CONTRATOS (sin lista) y MISIÓN.")]
        [SerializeField] private Text textoContenido;
        [SerializeField] private Text textoAyuda;
        [Tooltip("Agrupa las dos barras de reputación (barrio y corporaciones).")]
        [SerializeField] private GameObject grupoReputacion;
        [SerializeField] private Text[] textosBarras = new Text[BarrasReputacion];
        [SerializeField] private Image[] huecosBarras = new Image[BarrasReputacion];
        [SerializeField] private Image[] rellenosBarras = new Image[BarrasReputacion];
        [Tooltip("Acción Telefono del mapa APie (tecla T).")]
        [SerializeField] private InputActionReference telefono;
        [SerializeField] private InputAction izquierda = new InputAction("TelefonoIzquierda", InputActionType.Button, "<Keyboard>/leftArrow");
        [SerializeField] private InputAction derecha = new InputAction("TelefonoDerecha", InputActionType.Button, "<Keyboard>/rightArrow");
        [SerializeField] private InputAction confirmar = new InputAction("TelefonoEnter", InputActionType.Button, "<Keyboard>/enter");
        [SerializeField] private InputAction salir = new InputAction("TelefonoEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly CongelaControlAPie congela = new CongelaControlAPie();
        private PestanaTelefono pestana;
        private EstadoContratos estadoContratos;
        private bool abierto;
        private bool confirmando;
        private int segundosMostrados = -1;
        private int favoresMostrados = -1;
        private int mensajesMostrados = -1;
        private int barrioMostrado = int.MinValue;
        private int corporacionesMostrado = int.MinValue;

        public bool Abierto => abierto;
        public PestanaTelefono Pestana => pestana;
        public EstadoContratos EstadoContratos => estadoContratos;
        public bool Confirmando => confirmando;
        public string Titulo => textoTitulo != null ? textoTitulo.text : string.Empty;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;

        /// <summary>El texto de la pestaña actual cuando no es una lista ni las barras; vacío si no se ve.</summary>
        public string Contenido => textoContenido != null && textoContenido.gameObject.activeSelf ? textoContenido.text : string.Empty;

        /// <summary>El texto de una barra de reputación (0 barrio, 1 corporaciones); vacío si no se ve.</summary>
        public string TextoBarra(int barra) =>
            grupoReputacion != null && grupoReputacion.activeSelf ? textosBarras[barra].text : string.Empty;

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || textoPestanaContratos == null
                || textoPestanaMision == null || textoPestanaReputacion == null || textoPestanaFavores == null || textoContenido == null || textoAyuda == null
                || grupoReputacion == null || telefono == null || textosBarras == null || textosBarras.Length != BarrasReputacion
                || huecosBarras == null || huecosBarras.Length != BarrasReputacion
                || rellenosBarras == null || rellenosBarras.Length != BarrasReputacion)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaTelefono de " + name, this);
                enabled = false;
                return;
            }
            contenido.SetActive(false);
        }

        private void OnEnable()
        {
            if (telefono != null)
            {
                telefono.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (telefono != null)
            {
                telefono.action.Disable();
            }
            DesactivarEntrada();
            abierto = false;
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
            congela.Liberar();
        }

        private void DesactivarEntrada()
        {
            izquierda.Disable();
            derecha.Disable();
            confirmar.Disable();
            salir.Disable();
        }

        /// <summary>True si el jugador está a pie, con el control libre (ninguna otra pantalla) y sin KO.</summary>
        public bool PuedeAbrir()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            ControladorAPie controlador = jugador.GetComponent<ControladorAPie>();
            if (controlador == null || !controlador.ControlActivo)
            {
                return false;
            }
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            return vida == null || !vida.EstaKO;
        }

        public void Abrir()
        {
            if (abierto)
            {
                return;
            }
            congela.Congelar();
            abierto = true;
            confirmando = false;
            pestana = PestanaTelefono.Contratos;
            fondo.color = config.ColorFondo;
            izquierda.Enable();
            derecha.Enable();
            confirmar.Enable();
            salir.Enable();
            contenido.SetActive(true);
            Pintar();
        }

        public void Cerrar()
        {
            if (!abierto)
            {
                return;
            }
            abierto = false;
            confirmando = false;
            DesactivarEntrada();
            PantallaMisiones lista = Registro<PantallaMisiones>.Actual;
            if (lista != null && lista.Embebida)
            {
                lista.Ocultar();
            }
            contenido.SetActive(false);
            // El control vuelve un cuadro después, para que la tecla que cierra no llegue al jugador.
            if (isActiveAndEnabled)
            {
                StartCoroutine(LiberarDespues());
            }
            else
            {
                congela.Liberar();
            }
        }

        private IEnumerator LiberarDespues()
        {
            yield return null;
            congela.Liberar();
        }

        private void Update()
        {
            bool pulsoT = telefono.action.WasPressedThisFrame();
            if (!abierto)
            {
                if (pulsoT && PuedeAbrir())
                {
                    Abrir();
                }
                return;
            }
            if (salir.WasPressedThisFrame() && confirmando)
            {
                confirmando = false;
                Pintar();
                return;
            }
            if (pulsoT || salir.WasPressedThisFrame())
            {
                Cerrar();
                return;
            }
            if (!confirmando)
            {
                if (izquierda.WasPressedThisFrame() && pestana != PestanaTelefono.Contratos)
                {
                    CambiarPestana((PestanaTelefono)((int)pestana - 1));
                    return;
                }
                if (derecha.WasPressedThisFrame() && pestana != PestanaTelefono.Mensajes)
                {
                    CambiarPestana((PestanaTelefono)((int)pestana + 1));
                    return;
                }
            }
            if (pestana == PestanaTelefono.Mision && confirmar.WasPressedThisFrame())
            {
                PulsarAbandonar();
                return;
            }
            Refrescar();
        }

        private void CambiarPestana(PestanaTelefono nueva)
        {
            pestana = nueva;
            confirmando = false;
            Pintar();
        }

        private void PulsarAbandonar()
        {
            ServicioMisiones misiones = Registro<ServicioMisiones>.Actual;
            if (misiones == null || !misiones.HayMision)
            {
                return;
            }
            if (!confirmando)
            {
                confirmando = true;
                Pintar();
                return;
            }
            misiones.Abandonar();
            Cerrar();
        }

        // Lo que cambia solo mientras el teléfono está abierto: la hora (CONTRATOS), el tiempo de la misión y la reputación.
        private void Refrescar()
        {
            switch (pestana)
            {
                case PestanaTelefono.Contratos:
                    if (CalcularEstadoContratos() != estadoContratos)
                    {
                        Pintar();
                    }
                    break;
                case PestanaTelefono.Mision:
                    ServicioMisiones misiones = Registro<ServicioMisiones>.Actual;
                    int segundos = misiones != null && misiones.HayMision ? Mathf.CeilToInt(misiones.SegundosRestantes) : -1;
                    if (segundos != segundosMostrados)
                    {
                        Pintar();
                    }
                    break;
                case PestanaTelefono.Reputacion:
                    IReputacion reputacion = Registro<IReputacion>.Actual;
                    if (reputacion != null && (reputacion.Valor(TipoReputacion.Barrio) != barrioMostrado
                        || reputacion.Valor(TipoReputacion.Corporaciones) != corporacionesMostrado))
                    {
                        Pintar();
                    }
                    break;
                case PestanaTelefono.Favores:
                    ServicioFavores favores = Registro<ServicioFavores>.Actual;
                    if (favores != null && FirmaFavores(favores) != favoresMostrados)
                    {
                        Pintar();
                    }
                    break;
                case PestanaTelefono.Mensajes:
                    ServicioMensajes mensajes = Registro<ServicioMensajes>.Actual;
                    if (mensajes != null && FirmaMensajes(mensajes) != mensajesMostrados)
                    {
                        Pintar();
                    }
                    break;
            }
        }

        private EstadoContratos CalcularEstadoContratos()
        {
            IReputacion reputacion = Registro<IReputacion>.Actual;
            if (reputacion != null && reputacion.Nivel(TipoReputacion.Corporaciones) <= -2)
            {
                return EstadoContratos.YaNoTeLlaman;
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            return reloj != null && reloj.EsDeNoche ? EstadoContratos.NadieContesta : EstadoContratos.Lista;
        }

        private void Pintar()
        {
            PintarPestana(textoPestanaContratos, config.TextoPestanaContratos, pestana == PestanaTelefono.Contratos);
            PintarPestana(textoPestanaMision, config.TextoPestanaMision, pestana == PestanaTelefono.Mision);
            PintarPestana(textoPestanaReputacion, config.TextoPestanaReputacion, pestana == PestanaTelefono.Reputacion);
            PintarPestana(textoPestanaFavores, config.TextoPestanaFavores, pestana == PestanaTelefono.Favores);
            if (textoPestanaMensajes != null)
            {
                PintarPestana(textoPestanaMensajes, config.TextoPestanaMensajes + SufijoSinLeer(), pestana == PestanaTelefono.Mensajes);
            }
            textoTitulo.text = config.TituloTelefono;
            textoTitulo.color = config.ColorTitulo;
            textoTitulo.fontSize = config.TamanoTitulo;
            textoAyuda.color = config.ColorNormal;
            textoAyuda.fontSize = config.TamanoAyuda;

            PantallaMisiones lista = Registro<PantallaMisiones>.Actual;
            bool mostrarLista = false;
            string texto = string.Empty;
            string ayuda = config.TextoAyudaTelefono;
            switch (pestana)
            {
                case PestanaTelefono.Contratos:
                    estadoContratos = CalcularEstadoContratos();
                    mostrarLista = estadoContratos == EstadoContratos.Lista && lista != null;
                    if (estadoContratos == EstadoContratos.NadieContesta)
                    {
                        texto = config.TextoNadieContesta;
                    }
                    else if (estadoContratos == EstadoContratos.YaNoTeLlaman)
                    {
                        texto = config.TextoYaNoTeLlaman;
                    }
                    ayuda = mostrarLista ? config.TextoAyudaListaEmbebida : config.TextoAyudaTelefono;
                    break;
                case PestanaTelefono.Mision:
                    texto = TextoMision();
                    ayuda = config.TextoAyudaMision;
                    break;
                case PestanaTelefono.Reputacion:
                    PintarBarras();
                    break;
                case PestanaTelefono.Favores:
                    texto = TextoFavores();
                    break;
                case PestanaTelefono.Mensajes:
                    texto = TextoMensajes();
                    break;
            }
            textoAyuda.text = ayuda;
            grupoReputacion.SetActive(pestana == PestanaTelefono.Reputacion);
            textoContenido.gameObject.SetActive(texto.Length > 0);
            textoContenido.text = texto;
            textoContenido.color = confirmando ? config.ColorAviso : config.ColorNormal;
            textoContenido.fontSize = config.TamanoTexto;

            if (mostrarLista)
            {
                if (!lista.Visible)
                {
                    lista.Mostrar(config.TituloContratos, OrigenMision.Telefono, null, Cerrar, true);
                }
            }
            else if (lista != null && lista.Visible && lista.Embebida)
            {
                lista.Ocultar();
            }
        }

        private string TextoMision()
        {
            ServicioMisiones misiones = Registro<ServicioMisiones>.Actual;
            if (misiones == null || !misiones.HayMision)
            {
                segundosMostrados = -1;
                string sinMision = config.TextoSinMision;
                if (misiones != null && misiones.UltimoResumen.FavorPerdido)
                {
                    ServicioFavores bolsillo = Registro<ServicioFavores>.Actual;
                    sinMision += "\n" + FormatoMisiones.AvisoFavorPerdido(config, bolsillo != null ? bolsillo.Maximo : 0);
                }
                return sinMision;
            }
            string texto = misiones.Titulo.ToUpperInvariant();
            PasoMision paso = misiones.PasoActual;
            if (paso != null)
            {
                texto += "\n" + string.Format(config.FormatoPaso, misiones.IndicePaso + 1, misiones.Actual.Pasos.Length, paso.Texto);
            }
            if (misiones.Actual.LimiteSegundos > 0f)
            {
                texto += "\n" + string.Format(config.FormatoTiempo, FormatoMisiones.Tiempo(misiones.SegundosRestantes));
            }
            segundosMostrados = Mathf.CeilToInt(misiones.SegundosRestantes);
            texto += "\n" + (confirmando ? config.TextoConfirmarAbandonar : config.TextoAbandonar);
            return texto;
        }

        // Los tres favores: nombre, cuántos lleva, su descripción y dónde se usan (T-187).
        private string TextoFavores()
        {
            ServicioFavores favores = Registro<ServicioFavores>.Actual;
            if (favores == null)
            {
                return config.TextoSinFavores;
            }
            string texto = string.Empty;
            foreach (TipoFavor tipo in System.Enum.GetValues(typeof(TipoFavor)))
            {
                DatosFavor datos = favores.Datos(tipo);
                if (texto.Length > 0)
                {
                    texto += "\n";
                }
                texto += FormatoMisiones.FilaFavor(config, tipo, datos, favores.Cantidad(tipo), favores.Maximo);
            }
            favoresMostrados = FirmaFavores(favores);
            return texto;
        }

        // T-449: los mensajes de aliados y profes, el más nuevo arriba. Al verlos se marcan como leídos.
        private string TextoMensajes()
        {
            ServicioMensajes servicio = Registro<ServicioMensajes>.Actual;
            if (servicio == null || servicio.Recibidos.Count == 0)
            {
                mensajesMostrados = -1;
                return config.TextoSinMensajes;
            }
            string texto = string.Empty;
            int mostrados = 0;
            for (int i = servicio.Recibidos.Count - 1; i >= 0 && mostrados < config.MensajesVisibles; i--, mostrados++)
            {
                ServicioMensajes.MensajeGuardado guardado = servicio.Recibidos[i];
                if (!ReglaMensajes.Buscar(guardado.id, out ReglaMensajes.Mensaje mensaje))
                {
                    continue;
                }
                texto += string.Format(config.FormatoMensaje, guardado.leido ? string.Empty : config.MarcaNoLeido, mensaje.Remitente, mensaje.Texto);
            }
            mensajesMostrados = FirmaMensajes(servicio);
            servicio.MarcarLeidos();
            return texto;
        }

        private static int FirmaMensajes(ServicioMensajes servicio) => servicio.Recibidos.Count;

        // El texto de la pestaña con cuántos mensajes hay sin leer: "MENSAJES (2)".
        private static string SufijoSinLeer()
        {
            ServicioMensajes servicio = Registro<ServicioMensajes>.Actual;
            int sinLeer = servicio != null ? servicio.SinLeer : 0;
            return sinLeer > 0 ? " (" + sinLeer + ")" : string.Empty;
        }

        private static int FirmaFavores(ServicioFavores favores)
        {
            int firma = 0;
            foreach (TipoFavor tipo in System.Enum.GetValues(typeof(TipoFavor)))
            {
                firma = firma * (favores.Maximo + 1) + favores.Cantidad(tipo);
            }
            return firma;
        }

        private void PintarBarras()
        {
            IReputacion reputacion = Registro<IReputacion>.Actual;
            int barrio = reputacion != null ? reputacion.Valor(TipoReputacion.Barrio) : 0;
            int corporaciones = reputacion != null ? reputacion.Valor(TipoReputacion.Corporaciones) : 0;
            barrioMostrado = barrio;
            corporacionesMostrado = corporaciones;
            PintarBarra(0, config.NombreBarrio, barrio, reputacion, TipoReputacion.Barrio);
            PintarBarra(1, config.NombreCorporaciones, corporaciones, reputacion, TipoReputacion.Corporaciones);
        }

        private void PintarBarra(int barra, string nombre, int valor, IReputacion reputacion, TipoReputacion tipo)
        {
            int minimo = reputacion != null ? reputacion.Minimo : -1;
            int maximo = reputacion != null ? reputacion.Maximo : 1;
            string nivel = reputacion != null ? reputacion.NombreNivel(tipo) : string.Empty;
            Text texto = textosBarras[barra];
            texto.text = string.Format(config.FormatoBarra, nombre, FormatoHud.Firmado(valor), nivel);
            texto.color = config.ColorNormal;
            texto.fontSize = config.TamanoTexto;
            huecosBarras[barra].color = config.ColorHuecoBarra;
            float posicion = FormatoMisiones.PosicionBarra(valor, minimo, maximo);
            RectTransform relleno = rellenosBarras[barra].rectTransform;
            relleno.anchorMin = new Vector2(Mathf.Min(0.5f, posicion), 0f);
            relleno.anchorMax = new Vector2(Mathf.Max(0.5f, posicion), 1f);
            relleno.offsetMin = Vector2.zero;
            relleno.offsetMax = Vector2.zero;
            rellenosBarras[barra].color = valor >= 0 ? config.ColorBarraPositiva : config.ColorBarraNegativa;
        }

        private void PintarPestana(Text texto, string nombre, bool elegida)
        {
            texto.text = elegida ? string.Format(config.FormatoPestanaElegida, nombre) : nombre;
            texto.color = elegida ? config.ColorElegido : config.ColorApagado;
            texto.fontSize = config.TamanoPestanas;
        }
    }
}
