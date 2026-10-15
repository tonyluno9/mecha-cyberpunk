using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Progresion;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// HUD del jugador a pie: vida, mira, arma con balas, canción, objetivo con flecha y aviso de interacción.
    /// Encuentra al jugador y el objetivo por <see cref="Registro{T}"/> (ahora o cuando se registren) y casi todo se
    /// actualiza con eventos; solo la flecha, la distancia y el aviso de la puerta se calculan en cada frame.
    /// A pie muestra todo, en carro solo la canción y el objetivo y en el mecha nada (en la cabina todo es diegético).
    /// </summary>
    public class HudAPie : MonoBehaviour
    {
        [SerializeField] private ConfigHudAPie config;
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
        [SerializeField] private Image flecha;
        [SerializeField] private Text textoDistancia;
        [Header("Interacción (abajo al centro)")]
        [SerializeField] private GameObject grupoInteraccion;
        [SerializeField] private Text textoInteraccion;

        [Header("Llamado del mecha (arriba de la interacción)")]
        [SerializeField] private GameObject grupoLlamado;
        [SerializeField] private Text textoLlamado;

        [Header("Calor policial (arriba a la derecha; solo con nivel mayor que 0)")]
        [SerializeField] private GameObject grupoCalor;
        [SerializeField] private Text textoCalor;
        [SerializeField] private Image[] marcasCalor;

        private ICalor calor;
        private LlamadoMecha llamado;
        private string avisoLlamado = string.Empty;
        private float avisoLlamadoRestante;
        private IJugador jugador;
        private IVida vida;
        private ArmasAPie armas;
        private InteraccionAPie interaccion;
        private IObjetivos objetivos;
        private ServicioObjetivos servicio;
        private bool listo;
        private bool pendienteRefresco;
        private Texture2D texturaFlecha;
        private Sprite spriteFlecha;
        private string avisoPuerta = string.Empty;
        private float avisoRestante;
        private float anguloFlecha;

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
        public Image Flecha => flecha;
        public Text TextoDistancia => textoDistancia;
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

        public GameObject GrupoLlamado => grupoLlamado;
        public Text TextoLlamado => textoLlamado;

        /// <summary>Último ángulo (°) calculado para la flecha: 0 enfrente, positivo a la derecha.</summary>
        public float AnguloFlecha => anguloFlecha;

        private void Awake()
        {
            if (config == null || grupoVida == null || grupoMira == null || grupoArma == null || grupoCancion == null
                || relleno == null || textoVida == null || textoArma == null || textoCancion == null
                || grupoObjetivo == null || textoObjetivo == null || flecha == null || textoDistancia == null
                || grupoInteraccion == null || textoInteraccion == null || grupoLlamado == null || textoLlamado == null
                || grupoCalor == null || textoCalor == null || marcasCalor == null || marcasCalor.Length == 0)
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
            textoVida.color = config.ColorVida;
            textoArma.color = config.ColorTexto;
            textoCancion.color = config.ColorTexto;
            textoObjetivo.color = config.ColorTexto;
            textoDistancia.color = config.ColorTexto;
            textoInteraccion.color = config.ColorTexto;
            textoLlamado.color = config.ColorTexto;
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

            // La flecha es un triángulo dibujado por código, sin asset.
            texturaFlecha = CrearTexturaFlecha();
            spriteFlecha = Sprite.Create(texturaFlecha, new Rect(0f, 0f, texturaFlecha.width, texturaFlecha.height), new Vector2(0.5f, 0.5f));
            flecha.sprite = spriteFlecha;
            flecha.color = config.ColorTexto;

            listo = true;
            OcultarTodo();
        }

        private void OnEnable()
        {
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
            ActualizarFlecha();
            ActualizarInteraccion();
            ActualizarLlamado();
        }

        private void OnDisable()
        {
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
            DesvincularCalor();
            Desvincular();
            DesvincularObjetivos();
            DesvincularLlamado();
        }

        private void OnDestroy()
        {
            if (spriteFlecha != null)
            {
                Destroy(spriteFlecha);
            }
            if (texturaFlecha != null)
            {
                Destroy(texturaFlecha);
            }
        }

        /// <summary>Muestra la canción que suena. Con texto vacío vuelve al texto de espera.</summary>
        public void MostrarCancion(string titulo)
        {
            textoCancion.text = string.IsNullOrEmpty(titulo) ? config.TextoCancionEspera : titulo;
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
            interaccion = null;
            avisoPuerta = string.Empty;
            avisoRestante = 0f;
        }

        private void AlCambiarModo(ModoJugador modo) => AplicarModo(modo);

        private void AplicarModo(ModoJugador modo)
        {
            bool aPie = modo == ModoJugador.APie;
            grupoVida.SetActive(aPie);
            grupoMira.SetActive(aPie);
            grupoArma.SetActive(aPie);
            grupoCancion.SetActive(modo != ModoJugador.Mecha);
            RefrescarObjetivo();
            RefrescarCalor();
            avisoRestante = 0f;
            grupoInteraccion.SetActive(false);
        }

        private void OcultarTodo()
        {
            grupoVida.SetActive(false);
            grupoMira.SetActive(false);
            grupoArma.SetActive(false);
            grupoCancion.SetActive(false);
            grupoObjetivo.SetActive(false);
            grupoInteraccion.SetActive(false);
            grupoLlamado.SetActive(false);
            grupoCalor.SetActive(false);
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
            textoVida.color = relleno.color;
        }

        private void ActualizarArma()
        {
            if (armas == null)
            {
                textoArma.text = string.Empty;
                return;
            }
            textoArma.text = FormatoHud.TextoArma(armas.ArmaActiva, armas.Balas, armas.TamanoCargador, armas.Recargando, config);
            bool aviso = armas.ArmaActiva == ArmaAPie.Pistola && (armas.Recargando || armas.Balas <= 0);
            textoArma.color = aviso ? config.ColorAviso : config.ColorTexto;
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

        private void AlCambiarNivelCalor(int nivel) => RefrescarCalor();

        // El calor se ve a pie y en carro, y solo si hay (nivel mayor que 0); en el mecha lo muestra la cabina.
        private void RefrescarCalor()
        {
            int nivel = calor != null ? calor.Nivel : 0;
            bool visible = jugador != null && jugador.Modo != ModoJugador.Mecha && nivel > 0;
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
            Color color = seAleja ? config.ColorRegresa : config.ColorTexto;
            textoObjetivo.color = color;
            flecha.color = color;
            if (!seAleja)
            {
                flecha.rectTransform.localScale = Vector3.one;
            }
        }

        // La flecha y la distancia sí se calculan cada frame: la cámara gira todo el tiempo.
        private void ActualizarFlecha()
        {
            if (!grupoObjetivo.activeSelf)
            {
                return;
            }

            bool conPosicion = servicio != null && servicio.PosicionResuelta && jugador != null && jugador.Raiz != null;
            flecha.gameObject.SetActive(conPosicion);
            textoDistancia.gameObject.SetActive(conPosicion);
            if (!conPosicion)
            {
                return;
            }

            Camera camara = jugador.Camara;
            Transform origen = camara != null ? camara.transform : jugador.Raiz;
            anguloFlecha = CalculoFlechaObjetivo.Angulo(origen.position, origen.forward, servicio.Posicion);
            // En la UI un giro positivo va a la izquierda y el ángulo positivo es a la derecha: se invierte.
            flecha.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -anguloFlecha);
            textoDistancia.text = CalculoFlechaObjetivo.TextoDistancia(servicio.Distancia);

            if (servicio.SeAleja)
            {
                float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * config.VelocidadPulsoFlecha * Mathf.PI * 2f);
                flecha.rectTransform.localScale = Vector3.one * (1f + config.AmplitudPulsoFlecha * pulso);
            }
        }

        private static Texture2D CrearTexturaFlecha()
        {
            const int lado = 64;
            Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            textura.filterMode = FilterMode.Bilinear;
            textura.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixeles = new Color32[lado * lado];
            // Triángulo con la punta arriba: la fila y = lado - 1 es la de arriba y el ancho crece hacia abajo.
            for (int y = 0; y < lado; y++)
            {
                float ancho = (1f - (float)y / (lado - 1)) * lado;
                float inicio = (lado - ancho) / 2f;
                for (int x = 0; x < lado; x++)
                {
                    bool dentro = x >= inicio && x <= inicio + ancho;
                    pixeles[y * lado + x] = dentro ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply();
            return textura;
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
