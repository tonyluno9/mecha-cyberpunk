using System.Collections.Generic;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// HUD del jugador a pie: vida, mira, arma con balas, canción, objetivo (el rombo y la flecha del borde los dibuja MarcadorObjetivo, T-408) y aviso de interacción.
    /// Encuentra al jugador y el objetivo por <see cref="Registro{T}"/> (ahora o cuando se registren) y casi todo se
    /// actualiza con eventos; solo el ángulo, la distancia y el aviso de la puerta se calculan en cada frame.
    /// A pie muestra todo, en carro solo la canción y el objetivo y en el mecha nada (en la cabina todo es diegético).
    /// </summary>
    public class HudAPie : MonoBehaviour
    {
        [SerializeField] private ConfigHudAPie config;
        [Tooltip("Estilo del kit de UI (T-408); lo fija para todo el juego al nacer el HUD.")]
        [SerializeField] private EstiloCyberfix estilo;
        [Header("Grupos (se encienden y apagan por modo)")]
        [SerializeField] private GameObject grupoVida;
        [SerializeField] private GameObject grupoMira;
        [SerializeField] private GameObject grupoArma;
        [SerializeField] private GameObject grupoCancion;
        [Header("Elementos")]
        [SerializeField] private Image relleno;
        [SerializeField] private Image hueco;
        [SerializeField] private Text textoVida;
        [SerializeField] private Text textoArma;
        [SerializeField] private Text textoCancion;
        [SerializeField] private Image[] partesMira;
        [Header("Objetivo (arriba al centro)")]
        [SerializeField] private GameObject grupoObjetivo;
        [SerializeField] private Text textoObjetivo;
        [Header("Interacción (abajo al centro)")]
        [SerializeField] private GameObject grupoInteraccion;
        [SerializeField] private Text textoInteraccion;

        [Header("Llamado del mecha (arriba de la interacción)")]
        [SerializeField] private GameObject grupoLlamado;
        [SerializeField] private Text textoLlamado;

        [Header("Calor policial (bajo la vida; siempre visible, vacío con nivel 0)")]
        [SerializeField] private GameObject grupoCalor;
        [SerializeField] private Text textoCalor;
        [SerializeField] private Image[] marcasCalor;

        [Header("Panel del arma (T-521)")]
        [SerializeField] private Image iconoArma;
        [SerializeField] private Image[] casillasArma;
        [SerializeField] private Image resalteArma;

        [Header("Dinero y renta (T-110; solo a pie)")]
        [SerializeField] private GameObject grupoDinero;
        [SerializeField] private Text textoDinero;
        [Tooltip("Cifra que se ve junto al símbolo de Biits (T-521): el mismo dinero sin la Ƀ de texto. textoDinero queda apagado como fuente del dato.")]
        [SerializeField] private Text cifraDinero;
        [SerializeField] private GameObject grupoAvisoDinero;
        [SerializeField] private Text textoAvisoDinero;
        [SerializeField] private GameObject grupoRenta;
        [SerializeField] private Text textoRenta;

        [Header("Subir de liga (T-161; opcional; solo a pie)")]
        [SerializeField] private GameObject grupoAvisoLiga;
        [SerializeField] private Text textoAvisoLiga;
        [SerializeField] private Text textoAvisoLiga2;

        [Header("Reputación y misiones (T-186; opcional; solo a pie)")]
        [SerializeField] private GameObject grupoAvisoReputacion;
        [SerializeField] private Text textoAvisoReputacion;
        [SerializeField] private Text textoAvisoReputacion2;
        [SerializeField] private GameObject grupoAvisoMision;
        [SerializeField] private Text textoAvisoMision;

        [Header("Implante instalado (T-193; usa el aviso de misión; solo a pie)")]
        [Tooltip("{0} = nombre del implante.")]
        [SerializeField] private string formatoAvisoImplante = "IMPLANTE INSTALADO: {0}";

        [Header("OJOS a pie (T-195; opcional; solo a pie)")]
        [SerializeField] private GameObject grupoVidaRival;
        [SerializeField] private Image rellenoVidaRival;
        [SerializeField] private Text textoVidaRival;
        [SerializeField] private GameObject grupoGolpeRival;
        [SerializeField] private Text textoGolpeRival;

        private IReputacion reputacion;
        private ServicioGuardado guardado;
        private int valorBarrio;
        private int valorCorporaciones;
        private int deltaBarrioAviso;
        private int deltaCorporacionesAviso;
        private string nivelBarrioAviso = string.Empty;
        private string nivelCorporacionesAviso = string.Empty;
        private float avisoReputacionRestante;
        private bool reputacionPorPublicar;
        private ServicioMisiones misiones;
        private string avisoMision = string.Empty;
        private bool avisoMisionCumplida;
        private float avisoMisionRestante;
        private IProgresoLigas ligas;
        private int ligaPendiente;
        private float avisoLigaRestante;
        private ICartera cartera;
        private IRenta renta;
        private float avisoDineroRestante;
        private ICalor calor;
        private LlamadoMecha llamado;
        private string avisoLlamado = string.Empty;
        private float avisoLlamadoRestante;
        private IJugador jugador;
        private IVida vida;
        private ArmasAPie armas;
        private ControladorAPie controladorAPie;
        private ApuntadoAsistido apuntado;
        private bool miraEnBlanco;
        private bool aPieActual;
        private InteraccionAPie interaccion;
        private IObjetivos objetivos;
        private ServicioObjetivos servicio;
        private bool listo;
        private bool pendienteRefresco;
        private string avisoPuerta = string.Empty;
        private float avisoRestante;
        private float anguloFlecha;
        private string textoDistancia = string.Empty;
        private bool hayPosicionObjetivo;

        public GameObject GrupoVidaRival => grupoVidaRival;
        public Image RellenoVidaRival => rellenoVidaRival;
        public Text TextoVidaRival => textoVidaRival;
        public GameObject GrupoGolpeRival => grupoGolpeRival;
        public Text TextoGolpeRival => textoGolpeRival;
        public Image Relleno => relleno;
        public Text TextoVida => textoVida;
        public Text TextoArma => textoArma;
        public Text TextoCancion => textoCancion;
        public GameObject GrupoVida => grupoVida;
        public GameObject GrupoMira => grupoMira;
        public GameObject GrupoArma => grupoArma;
        public GameObject GrupoCancion => grupoCancion;
        public GameObject GrupoObjetivo => grupoObjetivo;
        public Text TextoObjetivo => textoObjetivo;
        /// <summary>Distancia al objetivo ("45 m"); el marcador (T-408) la dibuja. Vacía sin posición.</summary>
        public string TextoDistancia => textoDistancia;

        /// <summary>True si el objetivo tiene posición (el marcador lo dibuja en el mundo).</summary>
        public bool HayPosicionObjetivo => hayPosicionObjetivo;
        public GameObject GrupoInteraccion => grupoInteraccion;
        public Text TextoAvisoInteraccion => textoInteraccion;
        public GameObject GrupoCalor => grupoCalor;
        public Text TextoCalor => textoCalor;
        public Image[] MarcasCalor => marcasCalor;

        /// <summary>Cuántas marcas de calor están llenas (naranjas).</summary>
        public int MarcasCalorLlenas
        {
            get
            {
                int cuenta = 0;
                foreach (Image marca in marcasCalor)
                {
                    if (marca.color == config.ColorCalor) cuenta++;
                }
                return cuenta;
            }
        }

        public Image IconoArma => iconoArma;
        public Image[] CasillasArma => casillasArma;
        public Image ResalteArma => resalteArma;
        public Text CifraDinero => cifraDinero;

        public GameObject GrupoDinero => grupoDinero;
        public Text TextoDinero => textoDinero;
        public GameObject GrupoAvisoDinero => grupoAvisoDinero;
        public Text TextoAvisoDinero => textoAvisoDinero;
        public GameObject GrupoRenta => grupoRenta;
        public Text TextoRenta => textoRenta;

        public GameObject GrupoAvisoLiga => grupoAvisoLiga;
        public Text TextoAvisoLiga => textoAvisoLiga;
        public Text TextoAvisoLiga2 => textoAvisoLiga2;

        public GameObject GrupoAvisoReputacion => grupoAvisoReputacion;
        public Text TextoAvisoReputacion => textoAvisoReputacion;
        public Text TextoAvisoReputacion2 => textoAvisoReputacion2;
        public GameObject GrupoAvisoMision => grupoAvisoMision;
        public Text TextoAvisoMision => textoAvisoMision;

        public GameObject GrupoLlamado => grupoLlamado;
        public Text TextoLlamado => textoLlamado;

        /// <summary>Ángulo (°) de la cámara al objetivo: 0 enfrente, positivo a la derecha (la flecha giratoria se quitó en T-408).</summary>
        public float AnguloFlecha => anguloFlecha;

        private void Awake()
        {
            KitUI.FijarEstilo(estilo);
            if (GetComponent<Hitmarker>() == null)
            {
                gameObject.AddComponent<Hitmarker>();
            }
            if (GetComponent<TutorialLigero>() == null)
            {
                gameObject.AddComponent<TutorialLigero>();
            }
            if (GetComponent<AvisoMensaje>() == null)
            {
                gameObject.AddComponent<AvisoMensaje>();
            }
            if (GetComponent<ColaAvisos>() == null)
            {
                gameObject.AddComponent<ColaAvisos>();
            }
            if (GetComponent<HudImplantes>() == null)
            {
                gameObject.AddComponent<HudImplantes>();
            }
            if (config == null || grupoVida == null || grupoMira == null || grupoArma == null || grupoCancion == null
                || relleno == null || textoVida == null || textoArma == null || textoCancion == null
                || grupoObjetivo == null || textoObjetivo == null
                || grupoInteraccion == null || textoInteraccion == null || grupoLlamado == null || textoLlamado == null
                || grupoCalor == null || textoCalor == null || marcasCalor == null || marcasCalor.Length == 0
                || grupoDinero == null || textoDinero == null || grupoAvisoDinero == null || textoAvisoDinero == null
                || grupoRenta == null || textoRenta == null)
            {
                Debug.LogError("[UI] Faltan referencias en HudAPie de " + name, this);
                enabled = false;
                return;
            }

            // Una Image con relleno necesita sprite: se usa un blanco liso.
            if (relleno.sprite == null)
            {
                Texture2D blanco = Texture2D.whiteTexture;
                relleno.sprite = Sprite.Create(blanco, new Rect(0f, 0f, blanco.width, blanco.height), new Vector2(0.5f, 0.5f));
            }
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;

            if (hueco != null)
            {
                hueco.color = config.ColorHueco;
            }
            textoVida.color = KitUI.Estilo.Contorno;
            textoArma.color = config.ColorTexto;
            textoCancion.color = config.ColorTexto;
            textoObjetivo.color = KitUI.Estilo.Naranja;
            textoInteraccion.color = config.ColorTexto;
            textoLlamado.color = config.ColorTexto;
            textoDinero.color = config.ColorTexto;
            textoAvisoDinero.color = config.ColorTexto;
            textoRenta.color = config.ColorAviso;
            textoCalor.color = config.ColorCalor;
            textoCalor.text = config.TextoCalor;
            if (partesMira != null)
            {
                foreach (Image parte in partesMira)
                {
                    parte.color = config.ColorTexto;
                }
            }
            textoCancion.text = config.TextoCancionEspera;

            // T-523: los avisos de dinero, liga, reputación y misión ya no se dibujan en su sitio: salen por la cola del centro. Los grupos siguen
            // llevando el estado (texto y tiempo) para quien lo lea, pero sin dibujarse.
            NoDibujar(grupoAvisoDinero);
            NoDibujar(grupoAvisoLiga);
            NoDibujar(grupoAvisoReputacion);
            NoDibujar(grupoAvisoMision);

            listo = true;
            OcultarTodo();
        }

        private static void NoDibujar(GameObject grupo)
        {
            if (grupo == null)
            {
                return;
            }
            CanvasGroup lienzo = grupo.GetComponent<CanvasGroup>();
            if (lienzo == null)
            {
                lienzo = grupo.AddComponent<CanvasGroup>();
            }
            lienzo.alpha = 0f;
        }

        private void OnEnable()
        {
            EventosMontaje.Aviso += AlAvisoMontable;
            if (!listo)
            {
                return;
            }
            Registro<IJugador>.Agregado += AlAgregarJugador;
            Registro<IJugador>.Quitado += AlQuitarJugador;
            Registro<IObjetivos>.Agregado += AlAgregarObjetivos;
            Registro<IObjetivos>.Quitado += AlQuitarObjetivos;
            Registro<LlamadoMecha>.Agregado += AlAgregarLlamado;
            Registro<LlamadoMecha>.Quitado += AlQuitarLlamado;
            Registro<ICalor>.Agregado += AlAgregarCalor;
            Registro<ICalor>.Quitado += AlQuitarCalor;
            Registro<ICartera>.Agregado += AlAgregarCartera;
            Registro<ICartera>.Quitado += AlQuitarCartera;
            Registro<IRenta>.Agregado += AlAgregarRenta;
            Registro<IRenta>.Quitado += AlQuitarRenta;
            Registro<IProgresoLigas>.Agregado += AlAgregarLigas;
            Registro<IProgresoLigas>.Quitado += AlQuitarLigas;
            Registro<IReputacion>.Agregado += AlAgregarReputacion;
            Registro<IReputacion>.Quitado += AlQuitarReputacion;
            Registro<ServicioMisiones>.Agregado += AlAgregarMisiones;
            Registro<ServicioMisiones>.Quitado += AlQuitarMisiones;
            Registro<ServicioGuardado>.Agregado += AlAgregarGuardado;
            Registro<ServicioGuardado>.Quitado += AlQuitarGuardado;
            VincularGuardado(Registro<ServicioGuardado>.Actual);
            VincularReputacion(Registro<IReputacion>.Actual);
            VincularMisiones(Registro<ServicioMisiones>.Actual);
            VincularLigas(Registro<IProgresoLigas>.Actual);
            VincularCartera(Registro<ICartera>.Actual);
            VincularRenta(Registro<IRenta>.Actual);
            VincularCalor(Registro<ICalor>.Actual);
            VincularObjetivos(Registro<IObjetivos>.Actual);
            VincularLlamado(Registro<LlamadoMecha>.Actual);
            Vincular(Registro<IJugador>.Actual);
        }

        private void Update()
        {
            if (pendienteRefresco)
            {
                pendienteRefresco = false;
                ActualizarVida();
                ActualizarArma();
            }
            ActualizarHora();
            ActualizarMira();
            ActualizarAngulo();
            ActualizarInteraccion();
            ActualizarLlamado();
            ActualizarAvisoDinero();
            ActualizarAvisoLiga();
            ActualizarAvisosMision();
            ActualizarOjos();
        }

        private void OnDisable()
        {
            EventosMontaje.Aviso -= AlAvisoMontable;
            if (!listo)
            {
                return;
            }
            Registro<IJugador>.Agregado -= AlAgregarJugador;
            Registro<IJugador>.Quitado -= AlQuitarJugador;
            Registro<IObjetivos>.Agregado -= AlAgregarObjetivos;
            Registro<IObjetivos>.Quitado -= AlQuitarObjetivos;
            Registro<LlamadoMecha>.Agregado -= AlAgregarLlamado;
            Registro<LlamadoMecha>.Quitado -= AlQuitarLlamado;
            Registro<ICalor>.Agregado -= AlAgregarCalor;
            Registro<ICalor>.Quitado -= AlQuitarCalor;
            Registro<ICartera>.Agregado -= AlAgregarCartera;
            Registro<ICartera>.Quitado -= AlQuitarCartera;
            Registro<IRenta>.Agregado -= AlAgregarRenta;
            Registro<IRenta>.Quitado -= AlQuitarRenta;
            Registro<IProgresoLigas>.Agregado -= AlAgregarLigas;
            Registro<IProgresoLigas>.Quitado -= AlQuitarLigas;
            Registro<IReputacion>.Agregado -= AlAgregarReputacion;
            Registro<IReputacion>.Quitado -= AlQuitarReputacion;
            Registro<ServicioMisiones>.Agregado -= AlAgregarMisiones;
            Registro<ServicioMisiones>.Quitado -= AlQuitarMisiones;
            Registro<ServicioGuardado>.Agregado -= AlAgregarGuardado;
            Registro<ServicioGuardado>.Quitado -= AlQuitarGuardado;
            DesvincularGuardado();
            DesvincularReputacion();
            DesvincularMisiones();
            DesvincularLigas();
            DesvincularCartera();
            DesvincularRenta();
            DesvincularCalor();
            Desvincular();
            DesvincularObjetivos();
            DesvincularLlamado();
        }

        /// <summary>Cambió la canción de la radio: queda en el texto (sin dibujarse, T-523) y sale un aviso breve en la cola.</summary>
        public void MostrarCancion(string titulo)
        {
            textoCancion.text = string.IsNullOrEmpty(titulo) ? config.TextoCancionEspera : titulo;
            if (!string.IsNullOrEmpty(titulo))
            {
                ColaAvisos.Publicar("♪ " + titulo, PrioridadAviso.Baja, TipoAviso.Neutro);
            }
        }

        private void AlAgregarJugador(IJugador nuevo) => Vincular(nuevo);

        private void AlQuitarJugador(IJugador quitado)
        {
            if (quitado == jugador)
            {
                Vincular(Registro<IJugador>.Actual);
            }
        }

        private void Vincular(IJugador nuevo)
        {
            Desvincular();
            jugador = nuevo;
            if (jugador == null)
            {
                OcultarTodo();
                return;
            }

            Transform raiz = jugador.Raiz;
            vida = raiz != null ? raiz.GetComponent<IVida>() : null;
            armas = raiz != null ? raiz.GetComponent<ArmasAPie>() : null;
            controladorAPie = raiz != null ? raiz.GetComponent<ControladorAPie>() : null;
            apuntado = raiz != null ? raiz.GetComponent<ApuntadoAsistido>() : null;
            interaccion = raiz != null ? raiz.GetComponent<InteraccionAPie>() : null;
            jugador.ModoCambio += AlCambiarModo;
            if (vida != null)
            {
                vida.Cambio += ActualizarVida;
            }
            if (armas != null)
            {
                armas.Cambio += ActualizarArma;
            }
            if (interaccion != null)
            {
                interaccion.PuertaIntentada += AlIntentarPuerta;
                interaccion.BajarNegado += AlNegarBajar;
            }
            ActualizarVida();
            ActualizarArma();
            AplicarModo(jugador.Modo);
            // El jugador se registra en su OnEnable, quizá antes del Awake de su vida y sus armas: se repite al siguiente frame.
            pendienteRefresco = true;
        }

        private void Desvincular()
        {
            if (jugador != null)
            {
                jugador.ModoCambio -= AlCambiarModo;
            }
            if (vida != null)
            {
                vida.Cambio -= ActualizarVida;
            }
            if (armas != null)
            {
                armas.Cambio -= ActualizarArma;
            }
            if (interaccion != null)
            {
                interaccion.PuertaIntentada -= AlIntentarPuerta;
                interaccion.BajarNegado -= AlNegarBajar;
            }
            jugador = null;
            vida = null;
            armas = null;
            controladorAPie = null;
            apuntado = null;
            interaccion = null;
            avisoPuerta = string.Empty;
            avisoRestante = 0f;
        }

        // T-400: la retícula del centro solo se ve apuntando o con un arma de fuego en la mano.
        private bool MiraVisible()
        {
            bool apuntando = controladorAPie != null && controladorAPie.Apuntando;
            bool armaDeFuego = armas != null && armas.ArmaActiva != ArmaAPie.Punos;
            return apuntando || armaDeFuego;
        }

        private void ActualizarMira()
        {
            if (jugador == null || grupoMira == null)
            {
                return;
            }
            bool visible = aPieActual && MiraVisible();
            if (grupoMira.activeSelf != visible)
            {
                grupoMira.SetActive(visible);
            }
            bool enBlanco = visible && apuntado != null && apuntado.HayBlanco;
            if (enBlanco != miraEnBlanco && partesMira != null)
            {
                miraEnBlanco = enBlanco;
                foreach (Image parte in partesMira)
                {
                    parte.color = enBlanco ? config.ColorMiraBlanco : config.ColorTexto;
                }
            }
        }

        private void AlCambiarModo(ModoJugador modo) => AplicarModo(modo);

        private void AplicarModo(ModoJugador modo)
        {
            bool aPie = modo == ModoJugador.APie;
            aPieActual = aPie;
            grupoVida.SetActive(aPie);
            grupoMira.SetActive(aPie && MiraVisible());
            grupoArma.SetActive(aPie);
            grupoCancion.SetActive(false); // T-523: sin línea fija de radio
            RefrescarObjetivo();
            RefrescarCalor();
            RefrescarDinero();
            RefrescarRenta();
            RefrescarAvisoLiga();
            RefrescarAvisosMision();
            avisoRestante = 0f;
            grupoInteraccion.SetActive(false);
        }

        private void OcultarTodo()
        {
            if (grupoVidaRival != null)
            {
                grupoVidaRival.SetActive(false);
            }
            if (grupoGolpeRival != null)
            {
                grupoGolpeRival.SetActive(false);
            }
            grupoVida.SetActive(false);
            grupoMira.SetActive(false);
            grupoArma.SetActive(false);
            grupoCancion.SetActive(false);
            grupoObjetivo.SetActive(false);
            grupoInteraccion.SetActive(false);
            grupoLlamado.SetActive(false);
            grupoCalor.SetActive(false);
            grupoDinero.SetActive(false);
            grupoAvisoDinero.SetActive(false);
            grupoRenta.SetActive(false);
            if (grupoAvisoLiga != null)
            {
                grupoAvisoLiga.SetActive(false);
            }
            if (grupoAvisoReputacion != null)
            {
                grupoAvisoReputacion.SetActive(false);
            }
            if (grupoAvisoMision != null)
            {
                grupoAvisoMision.SetActive(false);
            }
        }

        // OJOS a pie (T-195): la vida del rival en la mira y el aviso de su golpe. Sin el implante no se muestra nada.
        private void ActualizarOjos()
        {
            if (grupoVidaRival == null || grupoGolpeRival == null || rellenoVidaRival == null)
            {
                return;
            }
            IImplantes implantes = Registro<IImplantes>.Actual;
            EfectosImplantes efectos = implantes != null ? implantes.Efectos : EfectosImplantes.Ninguno;
            bool leer = jugador != null && EstaAPie && (efectos.verVidaRival || efectos.anticipacionAtaque > 0f);
            ILecturaRival rival = leer ? ElegirRivalEnLaMira() : null;
            bool verVida = rival != null && efectos.verVidaRival;
            grupoVidaRival.SetActive(verVida);
            if (verVida)
            {
                IVida vidaRival = rival.Vida;
                float maxima = Mathf.Max(1f, vidaRival.Maxima);
                rellenoVidaRival.fillAmount = Mathf.Clamp01(vidaRival.Actual / maxima);
                if (textoVidaRival != null)
                {
                    textoVidaRival.gameObject.SetActive(efectos.vidaRivalEnNumeros);
                    textoVidaRival.text = string.Format(config.FormatoVidaRival, vidaRival.Actual, vidaRival.Maxima);
                }
            }
            bool avisar = rival != null && efectos.anticipacionAtaque > 0f
                && (rival.Preparando || (rival.SegundosParaAtacar >= 0f && rival.SegundosParaAtacar < efectos.anticipacionAtaque));
            grupoGolpeRival.SetActive(avisar);
        }

        // El rival a pie vivo que está en la mira (cerca del rayo de la cámara) a la distancia de lectura; el más cercano.
        private ILecturaRival ElegirRivalEnLaMira()
        {
            Camera camara = jugador.Camara;
            if (camara == null)
            {
                return null;
            }
            Vector3 origen = camara.transform.position;
            Vector3 adelante = camara.transform.forward;
            IReadOnlyList<ILecturaRival> todos = Registro<ILecturaRival>.Todos;
            ILecturaRival mejor = null;
            float mejorDistancia = config.DistanciaLecturaRival;
            for (int i = 0; i < todos.Count; i++)
            {
                ILecturaRival candidato = todos[i];
                if (candidato.EsMecha || candidato.Raiz == null || !candidato.Raiz.gameObject.activeInHierarchy
                    || candidato.Vida == null || candidato.Vida.EstaKO)
                {
                    continue;
                }
                Vector3 hacia = candidato.Raiz.position + Vector3.up * config.AlturaCentroRival - origen;
                float avance = Vector3.Dot(hacia, adelante);
                if (avance <= 0f || avance > mejorDistancia)
                {
                    continue;
                }
                if ((hacia - adelante * avance).magnitude <= config.RadioMiraRival)
                {
                    mejorDistancia = avance;
                    mejor = candidato;
                }
            }
            return mejor;
        }

        private void ActualizarVida()
        {
            if (vida == null)
            {
                return;
            }
            relleno.fillAmount = FormatoHud.Fraccion(vida.Actual, vida.Maxima);
            relleno.color = FormatoHud.EsVidaBaja(vida.Actual, vida.Maxima, config.UmbralVidaBaja) ? config.ColorVidaBaja : config.ColorVida;
            textoVida.text = FormatoHud.TextoVida(vida.Actual, config);
            textoVida.color = KitUI.Estilo.Contorno; // oscuro sobre la barra turquesa o roja
        }

        private void ActualizarArma()
        {
            if (armas == null)
            {
                textoArma.text = string.Empty;
                return;
            }
            textoArma.text = FormatoHud.TextoArma(armas.ArmaActiva, armas.Nivel, armas.Balas, armas.TamanoCargador, armas.Recargando, config);
            bool aviso = armas.ArmaActiva != ArmaAPie.Punos && (armas.Recargando || armas.Balas <= 0);
            textoArma.color = aviso ? config.ColorAviso : config.ColorTexto;
            ActualizarPanelArma();
        }

        // T-521: el icono del arma activa y las 5 casillas (puños, pistola, escopeta, fusil, sniper); la activa lleva el marco naranja.
        private void ActualizarPanelArma()
        {
            PiezasUI piezas = PiezasUI.Actual;
            if (piezas == null || armas == null)
            {
                return;
            }
            int activa = (int)armas.ArmaActiva;
            if (iconoArma != null)
            {
                // T-816: la imagen holográfica (H01-H04) si la hay, teñida con el material del holograma; los puños, su icono.
                PonerIconoArma(iconoArma, piezas, activa);
                iconoArma.enabled = iconoArma.sprite != null;
                // T-537: como el mockup: arma en naranja sobre un fondo azul muy oscuro.
                iconoArma.color = KitUI.Estilo.Naranja;
                PonerFondoArma(iconoArma.transform.parent);
            }
            if (casillasArma == null)
            {
                return;
            }
            for (int i = 0; i < casillasArma.Length; i++)
            {
                bool tiene = armas.TieneArma((ArmaAPie)i);
                PonerIconoArma(casillasArma[i], piezas, i);
                // T-537: la activa en naranja, las que tienes en turquesa, las que no en gris tenue.
                casillasArma[i].color = i == activa ? KitUI.Estilo.Naranja : tiene ? KitUI.Estilo.Turquesa : new Color(0.6f, 0.65f, 0.7f, 0.25f);
                if (i == activa && resalteArma != null)
                {
                    resalteArma.rectTransform.anchorMin = casillasArma[i].rectTransform.anchorMin;
                    resalteArma.rectTransform.anchorMax = casillasArma[i].rectTransform.anchorMax;
                    resalteArma.rectTransform.offsetMin = Vector2.zero;
                    resalteArma.rectTransform.offsetMax = Vector2.zero;
                }
            }
        }

        // Solo cambia sprite y material si son otros (cambiarlos reconstruye la Image).
        private static void PonerIconoArma(Image imagen, PiezasUI piezas, int arma)
        {
            Sprite sprite = piezas.ArmaHud(arma);
            Material material = piezas.MaterialArmaHud(arma);
            if (imagen.sprite != sprite)
            {
                imagen.sprite = sprite;
            }
            if (imagen.material != (material != null ? material : imagen.defaultMaterial))
            {
                imagen.material = material;
            }
        }

        private const string NombreFondoArma = "FondoArma_T537";
        private static readonly Color ColorFondoArma = new Color(0.02f, 0.04f, 0.09f, 0.82f);

        private static void PonerFondoArma(Transform grupo)
        {
            if (grupo == null || grupo.Find(NombreFondoArma) != null)
            {
                return;
            }
            Image fondo = KitUI.CrearFondoPantalla(grupo, NombreFondoArma);
            fondo.color = ColorFondoArma;
            fondo.raycastTarget = false;
            KitUI.Estirar(fondo.rectTransform);
            fondo.rectTransform.offsetMin = new Vector2(10f, 10f);
            fondo.rectTransform.offsetMax = new Vector2(-10f, -10f);
            fondo.transform.SetAsFirstSibling();
        }

        // ---- Subir de liga ----

        private void AlAgregarLigas(IProgresoLigas nuevas) => VincularLigas(nuevas);

        private void AlQuitarLigas(IProgresoLigas quitadas)
        {
            if (quitadas == ligas)
            {
                VincularLigas(Registro<IProgresoLigas>.Actual);
            }
        }

        private void VincularLigas(IProgresoLigas nuevas)
        {
            DesvincularLigas();
            ligas = nuevas;
            if (ligas != null)
            {
                ligas.CampeonVencido += AlVencerCampeon;
            }
        }

        private void DesvincularLigas()
        {
            if (ligas != null)
            {
                ligas.CampeonVencido -= AlVencerCampeon;
            }
            ligas = null;
        }

        // Si el jugador está en el mecha o en un carro, el aviso espera a que vuelva a pie.
        private void AlVencerCampeon(int liga)
        {
            if (grupoAvisoLiga == null || textoAvisoLiga == null || textoAvisoLiga2 == null)
            {
                return;
            }
            ligaPendiente = liga;
            avisoLigaRestante = config.SegundosAvisoLiga;
            if (ligas != null)
            {
                (string linea1, string linea2) = FormatoHud.AvisoLiga(liga, ligas, config);
                ColaAvisos.Publicar(linea2.Length > 0 ? linea1 + " · " + linea2 : linea1, PrioridadAviso.Alta, TipoAviso.Bueno, config.SegundosAvisoLiga);
            }
            RefrescarAvisoLiga();
        }

        private void RefrescarAvisoLiga()
        {
            if (grupoAvisoLiga == null)
            {
                return;
            }
            bool visible = ligaPendiente > 0 && avisoLigaRestante > 0f && EstaAPie && ligas != null;
            grupoAvisoLiga.SetActive(visible);
            if (!visible)
            {
                return;
            }
            (string primera, string segunda) = FormatoHud.AvisoLiga(ligaPendiente, ligas, config);
            textoAvisoLiga.text = primera;
            textoAvisoLiga.color = config.ColorAvisoLiga;
            textoAvisoLiga.fontSize = config.TamanoAvisoLiga;
            textoAvisoLiga2.text = segunda;
            textoAvisoLiga2.color = config.ColorAvisoLiga;
            textoAvisoLiga2.fontSize = config.TamanoAvisoLiga2;
            textoAvisoLiga2.gameObject.SetActive(segunda.Length > 0);
        }

        private void ActualizarAvisoLiga()
        {
            if (ligaPendiente <= 0 || !EstaAPie)
            {
                return;
            }
            avisoLigaRestante -= Time.deltaTime;
            if (avisoLigaRestante <= 0f)
            {
                ligaPendiente = 0;
                RefrescarAvisoLiga();
            }
        }

        // ---- Reputación y misiones (T-186) ----

        // Al cargar una partida la reputación avisa "Cambio" con lo guardado, que no es jugar: cuando el guardado termina de cargar
        // (en el mismo frame, antes de dibujar) se resincroniza y se descarta ese aviso (T-189).
        private void AlAgregarGuardado(ServicioGuardado nuevo) => VincularGuardado(nuevo);

        private void AlQuitarGuardado(ServicioGuardado quitado)
        {
            if (quitado == guardado)
            {
                VincularGuardado(Registro<ServicioGuardado>.Actual);
            }
        }

        private void VincularGuardado(ServicioGuardado nuevo)
        {
            DesvincularGuardado();
            guardado = nuevo;
            if (guardado != null)
            {
                guardado.Cargada += AlCargarPartida;
            }
        }

        private void DesvincularGuardado()
        {
            if (guardado != null)
            {
                guardado.Cargada -= AlCargarPartida;
            }
            guardado = null;
        }

        private void AlCargarPartida()
        {
            if (reputacion != null)
            {
                valorBarrio = reputacion.Valor(TipoReputacion.Barrio);
                valorCorporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
            }
            avisoReputacionRestante = 0f;
            deltaBarrioAviso = 0;
            deltaCorporacionesAviso = 0;
            nivelBarrioAviso = string.Empty;
            nivelCorporacionesAviso = string.Empty;
            RefrescarAvisoReputacion();
        }

        private void AlAgregarReputacion(IReputacion nueva) => VincularReputacion(nueva);

        private void AlQuitarReputacion(IReputacion quitada)
        {
            if (quitada == reputacion)
            {
                VincularReputacion(Registro<IReputacion>.Actual);
            }
        }

        private void VincularReputacion(IReputacion nueva)
        {
            DesvincularReputacion();
            reputacion = nueva;
            if (reputacion != null)
            {
                valorBarrio = reputacion.Valor(TipoReputacion.Barrio);
                valorCorporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
                reputacion.Cambio += AlCambiarReputacion;
                reputacion.NivelCambio += AlCambiarNivelReputacion;
            }
        }

        private void DesvincularReputacion()
        {
            if (reputacion != null)
            {
                reputacion.Cambio -= AlCambiarReputacion;
                reputacion.NivelCambio -= AlCambiarNivelReputacion;
            }
            reputacion = null;
            avisoReputacionRestante = 0f;
        }

        // Los cambios que llegan mientras el aviso sigue en pantalla se suman: "BARRIO −12 · CORPORACIONES +10".
        private void AlCambiarReputacion()
        {
            int barrio = reputacion.Valor(TipoReputacion.Barrio);
            int corporaciones = reputacion.Valor(TipoReputacion.Corporaciones);
            int deltaBarrio = barrio - valorBarrio;
            int deltaCorporaciones = corporaciones - valorCorporaciones;
            valorBarrio = barrio;
            valorCorporaciones = corporaciones;
            if (deltaBarrio == 0 && deltaCorporaciones == 0)
            {
                return;
            }
            IniciarAvisoReputacion();
            deltaBarrioAviso += deltaBarrio;
            deltaCorporacionesAviso += deltaCorporaciones;
            RefrescarAvisoReputacion();
        }

        private void AlCambiarNivelReputacion(TipoReputacion tipo, int nivel)
        {
            IniciarAvisoReputacion();
            string nombre = reputacion.NombreNivel(tipo);
            if (tipo == TipoReputacion.Barrio)
            {
                nivelBarrioAviso = nombre;
            }
            else
            {
                nivelCorporacionesAviso = nombre;
            }
            RefrescarAvisoReputacion();
        }

        private void IniciarAvisoReputacion()
        {
            reputacionPorPublicar = true;
            if (avisoReputacionRestante <= 0f)
            {
                deltaBarrioAviso = 0;
                deltaCorporacionesAviso = 0;
                nivelBarrioAviso = string.Empty;
                nivelCorporacionesAviso = string.Empty;
            }
            avisoReputacionRestante = config.SegundosAvisoReputacion;
        }

        private void RefrescarAvisoReputacion()
        {
            if (grupoAvisoReputacion == null || textoAvisoReputacion == null || textoAvisoReputacion2 == null)
            {
                return;
            }
            string primera = FormatoHud.TextoAvisoReputacion(deltaBarrioAviso, deltaCorporacionesAviso, config);
            string segunda = string.Empty;
            if (nivelBarrioAviso.Length > 0)
            {
                segunda = FormatoHud.TextoNivel(TipoReputacion.Barrio, nivelBarrioAviso, config);
            }
            if (nivelCorporacionesAviso.Length > 0)
            {
                string corporaciones = FormatoHud.TextoNivel(TipoReputacion.Corporaciones, nivelCorporacionesAviso, config);
                segunda = segunda.Length > 0 ? segunda + config.SeparadorAviso + corporaciones : corporaciones;
            }
            if (reputacionPorPublicar && avisoReputacionRestante > 0f && primera.Length + segunda.Length > 0)
            {
                reputacionPorPublicar = false;
                ColaAvisos.Publicar(segunda.Length > 0 ? primera + " · " + segunda : primera, PrioridadAviso.Normal, TipoAviso.Neutro,
                    config.SegundosAvisoReputacion, "reputacion");
            }
            textoAvisoReputacion.text = primera;
            textoAvisoReputacion.color = config.ColorAvisoReputacion;
            textoAvisoReputacion.fontSize = config.TamanoAvisoReputacion;
            textoAvisoReputacion2.text = segunda;
            textoAvisoReputacion2.color = config.ColorAvisoReputacion;
            textoAvisoReputacion2.fontSize = config.TamanoAvisoReputacion2;
            textoAvisoReputacion2.gameObject.SetActive(segunda.Length > 0);
            grupoAvisoReputacion.SetActive(avisoReputacionRestante > 0f && EstaAPie && primera.Length + segunda.Length > 0);
        }

        private void AlAgregarMisiones(ServicioMisiones nuevo) => VincularMisiones(nuevo);

        private void AlQuitarMisiones(ServicioMisiones quitado)
        {
            if (quitado == misiones)
            {
                VincularMisiones(Registro<ServicioMisiones>.Actual);
            }
        }

        private void VincularMisiones(ServicioMisiones nuevo)
        {
            DesvincularMisiones();
            misiones = nuevo;
            if (misiones != null)
            {
                misiones.Terminada += AlTerminarMision;
            }
        }

        private void DesvincularMisiones()
        {
            if (misiones != null)
            {
                misiones.Terminada -= AlTerminarMision;
            }
            misiones = null;
            avisoMisionRestante = 0f;
        }

        private void AlTerminarMision(DatosMision datos, bool cumplida, ResumenMision resumen)
        {
            if (grupoAvisoMision == null || textoAvisoMision == null)
            {
                return;
            }
            avisoMision = FormatoHud.TextoMisionTerminada(cumplida, resumen, config);
            avisoMisionCumplida = cumplida;
            avisoMisionRestante = config.SegundosAvisoMision;
            ColaAvisos.Publicar(avisoMision, PrioridadAviso.Alta, cumplida ? TipoAviso.Bueno : TipoAviso.Malo, config.SegundosAvisoMision);
            RefrescarAvisosMision();
        }

        /// <summary>Avisa "IMPLANTE INSTALADO: nombre" en el aviso de misión, el mismo tiempo que ese aviso (T-193).</summary>
        // T-532: el carro en el que vas se prende: aviso grande y rojo para bajarse.
        private static void AlAvisoMontable(IMontable montable, string texto)
        {
            ColaAvisos.Publicar(texto, PrioridadAviso.Alta, TipoAviso.Malo, 4f, "montable");
        }

        // T-537: la hora del juego bajo los Biits (se arma sola la primera vez, junto a la cifra del dinero).
        private Text textoHora;
        private float proximaHora;
        private const float SegundosEntreHoras = 0.5f;
        public Text TextoHora => textoHora;

        private void ActualizarHora()
        {
            if (Time.unscaledTime < proximaHora)
            {
                return;
            }
            proximaHora = Time.unscaledTime + SegundosEntreHoras;
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (textoHora == null)
            {
                if (cifraDinero == null || cifraDinero.transform.parent == null)
                {
                    return;
                }
                RectTransform grupo = (RectTransform)cifraDinero.transform.parent;
                textoHora = KitUI.CrearTexto(grupo.parent, "Hora", RolTexto.Hud, TextAnchor.MiddleLeft);
                RectTransform r = textoHora.rectTransform;
                r.anchorMin = grupo.anchorMin;
                r.anchorMax = grupo.anchorMax;
                r.pivot = grupo.pivot;
                r.sizeDelta = new Vector2(grupo.sizeDelta.x, grupo.rect.height * 0.7f);
                r.anchoredPosition = grupo.anchoredPosition + Vector2.down * (grupo.rect.height + 6f) + Vector2.right * (grupo.rect.width * 0.08f);
                textoHora.fontSize = Mathf.RoundToInt(cifraDinero.fontSize * 0.8f);
                textoHora.color = KitUI.Estilo.Turquesa;
            }
            textoHora.text = reloj != null ? FormatoHud.TextoHora(reloj.Hora) : string.Empty;
            textoHora.gameObject.SetActive(reloj != null && cifraDinero.gameObject.activeInHierarchy);
        }

        public void MostrarAvisoImplante(string nombre)
        {
            if (grupoAvisoMision == null || textoAvisoMision == null)
            {
                return;
            }
            avisoMision = string.Format(formatoAvisoImplante, nombre);
            avisoMisionCumplida = true;
            avisoMisionRestante = config.SegundosAvisoMision;
            ColaAvisos.Publicar(avisoMision, PrioridadAviso.Alta, TipoAviso.Bueno, config.SegundosAvisoMision);
            RefrescarAvisosMision();
        }

        private void RefrescarAvisosMision()
        {
            RefrescarAvisoReputacion();
            if (grupoAvisoMision == null || textoAvisoMision == null)
            {
                return;
            }
            bool visible = avisoMisionRestante > 0f && EstaAPie;
            grupoAvisoMision.SetActive(visible);
            if (!visible)
            {
                return;
            }
            textoAvisoMision.text = avisoMision;
            textoAvisoMision.color = avisoMisionCumplida ? config.ColorVida : config.ColorVidaBaja;
            textoAvisoMision.fontSize = config.TamanoAvisoMision;
        }

        private void ActualizarAvisosMision()
        {
            if (avisoReputacionRestante > 0f && EstaAPie)
            {
                avisoReputacionRestante -= Time.deltaTime;
                if (avisoReputacionRestante <= 0f)
                {
                    RefrescarAvisoReputacion();
                }
            }
            if (avisoMisionRestante > 0f && EstaAPie)
            {
                avisoMisionRestante -= Time.deltaTime;
                if (avisoMisionRestante <= 0f)
                {
                    RefrescarAvisosMision();
                }
            }
        }

        // ---- Dinero y renta ----

        private bool EstaAPie => jugador != null && jugador.Modo == ModoJugador.APie;

        private void AlAgregarCartera(ICartera nueva) => VincularCartera(nueva);

        private void AlQuitarCartera(ICartera quitada)
        {
            if (quitada == cartera)
            {
                VincularCartera(Registro<ICartera>.Actual);
            }
        }

        private void VincularCartera(ICartera nueva)
        {
            DesvincularCartera();
            cartera = nueva;
            if (cartera != null)
            {
                cartera.Cambio += AlCambiarDinero;
            }
            RefrescarDinero();
        }

        private void DesvincularCartera()
        {
            if (cartera != null)
            {
                cartera.Cambio -= AlCambiarDinero;
            }
            cartera = null;
            avisoDineroRestante = 0f;
        }

        private void AlCambiarDinero(int diferencia, MotivoDinero motivo)
        {
            RefrescarDinero();
            string aviso = FormatoHud.TextoAvisoDinero(diferencia, motivo, config);
            if (aviso.Length == 0 || !EstaAPie)
            {
                return;
            }
            ColaAvisos.Publicar(aviso, PrioridadAviso.Baja, diferencia > 0 ? TipoAviso.Dinero : TipoAviso.Malo, config.SegundosAvisoDinero);
            textoAvisoDinero.text = aviso;
            textoAvisoDinero.color = diferencia > 0 ? config.ColorTexto : config.ColorAviso;
            avisoDineroRestante = config.SegundosAvisoDinero;
            grupoAvisoDinero.SetActive(true);
        }

        // El dinero se ve solo a pie y solo si hay cartera.
        private void RefrescarDinero()
        {
            bool visible = EstaAPie && cartera != null;
            grupoDinero.SetActive(visible);
            if (!visible)
            {
                grupoAvisoDinero.SetActive(false);
                return;
            }
            textoDinero.text = FormatoDinero.Texto(cartera.Dinero);
            textoDinero.color = cartera.Dinero < 0 ? config.ColorDeuda : config.ColorTexto;
            if (cifraDinero != null)
            {
                cifraDinero.text = textoDinero.text.Replace(Moneda.Simbolo, string.Empty);
                cifraDinero.color = textoDinero.color;
            }
        }

        private void ActualizarAvisoDinero()
        {
            if (avisoDineroRestante <= 0f)
            {
                return;
            }
            avisoDineroRestante -= Time.deltaTime;
            grupoAvisoDinero.SetActive(avisoDineroRestante > 0f && EstaAPie && cartera != null);
        }

        private void AlAgregarRenta(IRenta nueva) => VincularRenta(nueva);

        private void AlQuitarRenta(IRenta quitada)
        {
            if (quitada == renta)
            {
                VincularRenta(Registro<IRenta>.Actual);
            }
        }

        private void VincularRenta(IRenta nueva)
        {
            DesvincularRenta();
            renta = nueva;
            if (renta != null)
            {
                renta.Cambio += RefrescarRenta;
            }
            RefrescarRenta();
        }

        private void DesvincularRenta()
        {
            if (renta != null)
            {
                renta.Cambio -= RefrescarRenta;
            }
            renta = null;
        }

        private void RefrescarRenta()
        {
            bool visible = EstaAPie && renta != null && renta.TallerBloqueado;
            grupoRenta.SetActive(visible);
            if (visible)
            {
                textoRenta.text = FormatoHud.TextoRentaVencida(renta.Adeudo, config);
            }
        }

        // ---- Calor policial ----

        private void AlAgregarCalor(ICalor nuevo) => VincularCalor(nuevo);

        private void AlQuitarCalor(ICalor quitado)
        {
            if (quitado == calor)
            {
                VincularCalor(Registro<ICalor>.Actual);
            }
        }

        private void VincularCalor(ICalor nuevo)
        {
            DesvincularCalor();
            calor = nuevo;
            if (calor != null)
            {
                calor.NivelCambio += AlCambiarNivelCalor;
            }
            RefrescarCalor();
        }

        private void DesvincularCalor()
        {
            if (calor != null)
            {
                calor.NivelCambio -= AlCambiarNivelCalor;
            }
            calor = null;
        }

        private void AlCambiarNivelCalor(int nivel)
        {
            // T-533: al subir el calor, la radio de la policía: "Unidades, sospechoso en <zona>".
            if (nivel > nivelCalorAnterior && nivel >= 1)
            {
                HudTerritorio territorio = FindFirstObjectByType<HudTerritorio>();
                string zona = territorio != null && territorio.NombreZona != null && !string.IsNullOrEmpty(territorio.NombreZona.text)
                    ? territorio.NombreZona.text : "el barrio";
                ColaAvisos.Publicar(TextoRadioPolicia(zona), PrioridadAviso.Alta, TipoAviso.Malo, 4f, "radio-policia");
            }
            nivelCalorAnterior = nivel;
            RefrescarCalor();
        }

        private int nivelCalorAnterior;

        /// <summary>Aviso de la radio de la policía al subir el calor (T-533).</summary>
        public static string TextoRadioPolicia(string zona) => "RADIO · Unidades, sospechoso en " + zona;

        // T-524: el calor se ve siempre a pie y en carro (vacío con nivel 0); en el mecha lo muestra la cabina.
        private void RefrescarCalor()
        {
            int nivel = calor != null ? calor.Nivel : 0;
            bool visible = jugador != null && jugador.Modo != ModoJugador.Mecha;
            grupoCalor.SetActive(visible);
            if (!visible)
            {
                return;
            }
            for (int i = 0; i < marcasCalor.Length; i++)
            {
                marcasCalor[i].color = i < nivel ? config.ColorCalor : config.ColorMarcaCalorApagada;
            }
        }

        // ---- Objetivo ----

        private void AlAgregarObjetivos(IObjetivos nuevo) => VincularObjetivos(nuevo);

        private void AlQuitarObjetivos(IObjetivos quitado)
        {
            if (quitado == objetivos)
            {
                VincularObjetivos(Registro<IObjetivos>.Actual);
            }
        }

        private void VincularObjetivos(IObjetivos nuevo)
        {
            DesvincularObjetivos();
            objetivos = nuevo;
            if (objetivos != null)
            {
                objetivos.Cambio += AlCambiarObjetivo;
                // Solo el ServicioObjetivos sabe de distancia y alejamiento; otra implementación muestra solo el texto.
                servicio = objetivos as ServicioObjetivos;
                if (servicio != null && servicio.Seguimiento != null)
                {
                    servicio.Seguimiento.SeAlejaCambio += AlCambiarSeAleja;
                }
            }
            RefrescarObjetivo();
        }

        private void DesvincularObjetivos()
        {
            if (objetivos != null)
            {
                objetivos.Cambio -= AlCambiarObjetivo;
            }
            if (servicio != null && servicio.Seguimiento != null)
            {
                servicio.Seguimiento.SeAlejaCambio -= AlCambiarSeAleja;
            }
            objetivos = null;
            servicio = null;
        }

        private void AlCambiarObjetivo(Objetivo objetivo) => RefrescarObjetivo();

        private void AlCambiarSeAleja(bool seAleja) => RefrescarObjetivo();

        // El objetivo se ve a pie y en carro; en mecha, o sin jugador, no.
        private void RefrescarObjetivo()
        {
            bool visible = jugador != null && jugador.Modo != ModoJugador.Mecha && objetivos != null && objetivos.HayObjetivo;
            grupoObjetivo.SetActive(visible);
            if (!visible)
            {
                return;
            }

            bool seAleja = servicio != null && servicio.SeAleja;
            string texto = objetivos.Actual.Texto ?? string.Empty;
            textoObjetivo.text = seAleja ? config.TextoRegresa + " · " + texto : texto;
            textoObjetivo.color = seAleja ? config.ColorRegresa : KitUI.Estilo.Naranja;
        }

        // El ángulo y la distancia se calculan cada frame (la cámara gira todo el tiempo); el marcador dibuja el objetivo.
        private void ActualizarAngulo()
        {
            hayPosicionObjetivo = grupoObjetivo.activeSelf && servicio != null && servicio.PosicionResuelta && jugador != null && jugador.Raiz != null;
            if (!hayPosicionObjetivo)
            {
                textoDistancia = string.Empty;
                return;
            }
            Camera camara = jugador.Camara;
            Transform origen = camara != null ? camara.transform : jugador.Raiz;
            anguloFlecha = CalculoFlechaObjetivo.Angulo(origen.position, origen.forward, servicio.Posicion);
            textoDistancia = CalculoFlechaObjetivo.TextoDistancia(servicio.Distancia);
        }

        // ---- Llamado del mecha ----

        private void AlAgregarLlamado(LlamadoMecha nuevo) => VincularLlamado(nuevo);

        private void AlQuitarLlamado(LlamadoMecha quitado)
        {
            if (quitado == llamado)
            {
                VincularLlamado(Registro<LlamadoMecha>.Actual);
            }
        }

        private void VincularLlamado(LlamadoMecha nuevo)
        {
            DesvincularLlamado();
            llamado = nuevo;
            if (llamado != null)
            {
                llamado.LlamadoIntentado += AlIntentarLlamado;
            }
        }

        private void DesvincularLlamado()
        {
            if (llamado != null)
            {
                llamado.LlamadoIntentado -= AlIntentarLlamado;
            }
            llamado = null;
            avisoLlamado = string.Empty;
            avisoLlamadoRestante = 0f;
        }

        // Solo la zona prohibida deja un aviso temporal; la llegada y el regreso se muestran mientras duran.
        private void AlIntentarLlamado(ResultadoLlamado resultado)
        {
            if (resultado == ResultadoLlamado.ZonaProhibida)
            {
                avisoLlamado = config.TextoZonaProhibida;
                avisoLlamadoRestante = config.DuracionAvisoLlamado;
            }
        }

        // A pie y en carro; en la cabina no (todo es diegético).
        private void ActualizarLlamado()
        {
            string texto = string.Empty;
            if (llamado != null && jugador != null && jugador.Modo != ModoJugador.Mecha)
            {
                if (llamado.Estado == EstadoLlamado.Llegando)
                {
                    texto = string.Format(config.TextoMechaEnCamino, Mathf.CeilToInt(llamado.SegundosParaLlegar));
                }
                else if (llamado.Estado == EstadoLlamado.Regresando)
                {
                    texto = config.TextoMechaRegresa;
                }
                else if (avisoLlamadoRestante > 0f)
                {
                    avisoLlamadoRestante -= Time.deltaTime;
                    texto = avisoLlamado;
                }
            }
            textoLlamado.text = texto;
            grupoLlamado.SetActive(texto.Length > 0);
        }

        // ---- Interacción ----

        private void AlIntentarPuerta(IPuerta puerta, ResultadoPuerta resultado)
        {
            ILugar lugar = BuscarLugar(puerta.Lugar);
            HorarioLugar? horario = lugar != null ? lugar.Horario : (HorarioLugar?)null;
            string aviso = TextoInteraccion.Aviso(resultado, horario, config);
            if (string.IsNullOrEmpty(aviso))
            {
                return;
            }
            avisoPuerta = aviso;
            avisoRestante = config.DuracionAvisoPuerta;
        }

        private void AlNegarBajar()
        {
            avisoPuerta = TextoInteraccion.AvisoNoPuedeBajar(config);
            avisoRestante = config.DuracionAvisoPuerta;
        }

        // El aviso temporal (puerta cerrada, "Frena para bajar") manda en cualquier modo menos el mecha; si no, a pie
        // se muestra lo que hace la E con lo que hay de frente.
        private void ActualizarInteraccion()
        {
            string texto = string.Empty;
            if (jugador != null && jugador.Modo != ModoJugador.Mecha)
            {
                if (avisoRestante > 0f)
                {
                    avisoRestante -= Time.deltaTime;
                    texto = avisoPuerta;
                }
                else if (jugador.Modo == ModoJugador.APie && interaccion != null)
                {
                    texto = TextoCandidato();
                }
            }

            textoInteraccion.text = texto;
            bool negado = jugador != null && jugador.Modo == ModoJugador.APie && avisoRestante <= 0f && interaccion != null
                && interaccion.InteractuableCandidato != null && !interaccion.InteractuableCandidato.PuedeUsar;
            textoInteraccion.color = negado ? config.ColorAviso : config.ColorTexto;
            grupoInteraccion.SetActive(texto.Length > 0);
        }

        private string TextoCandidato()
        {
            switch (interaccion.TipoCandidato)
            {
                case TipoCandidatoAPie.Puerta:
                    ILugar lugar = interaccion.LugarCandidato ?? BuscarLugar(interaccion.PuertaCandidata.Lugar);
                    return TextoInteraccion.Candidato(TipoCandidatoInteraccion.Puerta, lugar != null ? lugar.Nombre : null, config);
                case TipoCandidatoAPie.Montable:
                    return TextoInteraccion.Candidato(TipoCandidatoInteraccion.Montable, interaccion.MontableCandidato.Nombre, config);
                case TipoCandidatoAPie.Interactuable:
                    return TextoInteraccion.Interactuable(interaccion.InteractuableCandidato.Texto,
                        interaccion.InteractuableCandidato.PuedeUsar, config);
                default:
                    return string.Empty;
            }
        }

        private static ILugar BuscarLugar(IdLugar id)
        {
            IReadOnlyList<ILugar> lugares = Registro<ILugar>.Todos;
            for (int i = 0; i < lugares.Count; i++)
            {
                if (lugares[i] != null && lugares[i].Id == id)
                {
                    return lugares[i];
                }
            }
            return null;
        }
    }
}
