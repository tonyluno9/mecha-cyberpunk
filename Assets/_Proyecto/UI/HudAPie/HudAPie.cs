using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// HUD mínimo del jugador a pie: vida, mira, arma con balas y canción. Encuentra al jugador por
    /// <see cref="Registro{T}"/> (ahora o cuando se registre), se actualiza con eventos y no en cada frame.
    /// A pie muestra todo, en carro solo la canción y en el mecha nada (en la cabina todo es diegético).
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

        private IJugador jugador;
        private IVida vida;
        private ArmasAPie armas;
        private bool listo;
        private bool pendienteRefresco;

        public Image Relleno => relleno;
        public Text TextoVida => textoVida;
        public Text TextoArma => textoArma;
        public Text TextoCancion => textoCancion;
        public GameObject GrupoVida => grupoVida;
        public GameObject GrupoMira => grupoMira;
        public GameObject GrupoArma => grupoArma;
        public GameObject GrupoCancion => grupoCancion;

        private void Awake()
        {
            if (config == null || grupoVida == null || grupoMira == null || grupoArma == null || grupoCancion == null
                || relleno == null || textoVida == null || textoArma == null || textoCancion == null)
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
            if (partesMira != null)
            {
                foreach (Image parte in partesMira)
                {
                    parte.color = config.ColorTexto;
                }
            }
            textoCancion.text = config.TextoCancionEspera;
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
            Vincular(Registro<IJugador>.Actual);
        }

        private void Update()
        {
            if (!pendienteRefresco)
            {
                return;
            }
            pendienteRefresco = false;
            ActualizarVida();
            ActualizarArma();
        }

        private void OnDisable()
        {
            if (!listo)
            {
                return;
            }
            Registro<IJugador>.Agregado -= AlAgregarJugador;
            Registro<IJugador>.Quitado -= AlQuitarJugador;
            Desvincular();
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
            jugador.ModoCambio += AlCambiarModo;
            if (vida != null)
            {
                vida.Cambio += ActualizarVida;
            }
            if (armas != null)
            {
                armas.Cambio += ActualizarArma;
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
            jugador = null;
            vida = null;
            armas = null;
        }

        private void AlCambiarModo(ModoJugador modo) => AplicarModo(modo);

        private void AplicarModo(ModoJugador modo)
        {
            bool aPie = modo == ModoJugador.APie;
            grupoVida.SetActive(aPie);
            grupoMira.SetActive(aPie);
            grupoArma.SetActive(aPie);
            grupoCancion.SetActive(modo != ModoJugador.Mecha);
        }

        private void OcultarTodo()
        {
            grupoVida.SetActive(false);
            grupoMira.SetActive(false);
            grupoArma.SetActive(false);
            grupoCancion.SetActive(false);
        }

        private void ActualizarVida()
        {
            if (vida == null)
            {
                return;
            }
            relleno.fillAmount = FormatoHud.Fraccion(vida.Actual, vida.Maxima);
            relleno.color = FormatoHud.EsVidaBaja(vida.Actual, vida.Maxima, config.UmbralVidaBaja) ? config.ColorVidaBaja : config.ColorVida;
            textoVida.text = FormatoHud.TextoVida(vida.Actual);
            textoVida.color = relleno.color;
        }

        private void ActualizarArma()
        {
            if (armas == null)
            {
                textoArma.text = string.Empty;
                return;
            }
            textoArma.text = FormatoHud.TextoArma(armas.ArmaActiva, armas.Balas, armas.TamanoCargador, armas.Recargando);
            bool aviso = armas.ArmaActiva == ArmaAPie.Pistola && (armas.Recargando || armas.Balas <= 0);
            textoArma.color = aviso ? config.ColorAviso : config.ColorTexto;
        }
    }
}
