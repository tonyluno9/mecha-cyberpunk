using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Línea de ayuda de controles del HUD (T-134): la primera vez en la sesión que el jugador camina, maneja un carro o puede llamar
    /// a su mecha, muestra abajo a la izquierda la línea de ese contexto. Se va a los segundos máximos o cuando la persona ya hizo
    /// lo que dice (caminar o manejar unos segundos, pulsar la tecla de llamar). Lo visto se recuerda en <see cref="AyudasVistas"/>.
    /// En el mecha no muestra nada: ahí la ayuda es una pantalla de la cabina.
    /// </summary>
    public class AyudaControles : MonoBehaviour
    {
        [SerializeField] private ConfigAyudas config;
        [SerializeField] private ConfigHudAPie configHud;
        [SerializeField] private GameObject grupo;
        [SerializeField] private Text texto;

        private ProgresoAyuda progreso;
        private ContextoAyuda contextoActual;
        private bool hayAyuda;
        private IJugador jugador;
        private ModoJugador modoVisto;
        private Vector3 posicionAnterior;
        private bool posicionValida;
        private LlamadoMecha llamado;
        private bool suscrito;

        /// <summary>True mientras la línea de ayuda se ve.</summary>
        public bool Visible => hayAyuda;

        /// <summary>Contexto de la ayuda que se ve (solo vale si <see cref="Visible"/>).</summary>
        public ContextoAyuda Contexto => contextoActual;

        public Text Texto => texto;
        public GameObject Grupo => grupo;

        private void Awake()
        {
            if (config == null || configHud == null || grupo == null || texto == null)
            {
                Debug.LogError("[UI] Faltan referencias en AyudaControles de " + name, this);
                enabled = false;
                return;
            }
            texto.color = configHud.ColorTexto;
            grupo.SetActive(false);
        }

        private void OnEnable()
        {
            suscrito = true;
            Registro<LlamadoMecha>.Agregado += AlAgregarLlamado;
            Registro<LlamadoMecha>.Quitado += AlQuitarLlamado;
            VincularLlamado(Registro<LlamadoMecha>.Actual);
        }

        private void OnDisable()
        {
            if (!suscrito)
            {
                return;
            }
            suscrito = false;
            Registro<LlamadoMecha>.Agregado -= AlAgregarLlamado;
            Registro<LlamadoMecha>.Quitado -= AlQuitarLlamado;
            VincularLlamado(null);
            Ocultar();
        }

        private void Update()
        {
            IJugador actual = Registro<IJugador>.Actual;
            if (actual == null || actual.Raiz == null || actual.Modo == ModoJugador.Mecha)
            {
                jugador = actual;
                posicionValida = false;
                Ocultar();
                return;
            }

            float dt = Time.deltaTime;
            bool activo = MidioMovimiento(actual, dt);
            if (hayAyuda && !ContextoVigente(actual))
            {
                Ocultar();
            }
            if (!hayAyuda && TomarContexto(actual, out ContextoAyuda nuevo))
            {
                Mostrar(nuevo);
            }
            jugador = actual;
            modoVisto = actual.Modo;

            if (hayAyuda)
            {
                progreso.Avanzar(dt, activo);
                if (progreso.Terminada)
                {
                    Ocultar();
                }
            }
        }

        // La rapidez horizontal del jugador entre este frame y el anterior, ignorando saltos de posición (teletransportes).
        private bool MidioMovimiento(IJugador actual, float dt)
        {
            Vector3 posicion = actual.Raiz.position;
            bool activo = false;
            if (posicionValida && actual == jugador && actual.Modo == modoVisto && dt > 0f)
            {
                Vector3 delta = posicion - posicionAnterior;
                delta.y = 0f;
                float rapidez = delta.magnitude / dt;
                activo = rapidez >= config.RapidezMinima && rapidez <= config.RapidezMaxima;
            }
            posicionAnterior = posicion;
            posicionValida = true;
            return activo;
        }

        private bool ContextoVigente(IJugador actual)
        {
            switch (contextoActual)
            {
                case ContextoAyuda.APie:
                case ContextoAyuda.Llamar:
                    return actual.Modo == ModoJugador.APie;
                case ContextoAyuda.Carro:
                    return actual.Modo == ModoJugador.Carro;
                default:
                    return false;
            }
        }

        // La ayuda que toca mostrar ahora, si hay alguna que todavía no se ha visto.
        private bool TomarContexto(IJugador actual, out ContextoAyuda contexto)
        {
            contexto = ContextoAyuda.APie;
            if (actual.Modo == ModoJugador.Carro)
            {
                contexto = ContextoAyuda.Carro;
                return !AyudasVistas.YaVista(ContextoAyuda.Carro);
            }
            if (!AyudasVistas.YaVista(ContextoAyuda.APie))
            {
                return true;
            }
            if (!AyudasVistas.YaVista(ContextoAyuda.Llamar) && DebeOfrecerLlamar(actual))
            {
                contexto = ContextoAyuda.Llamar;
                return true;
            }
            return false;
        }

        // El mecha está en su taller y el objetivo pide llamarlo o el jugador está dentro de un punto de llamado.
        private bool DebeOfrecerLlamar(IJugador actual)
        {
            if (llamado == null || llamado.Estado != EstadoLlamado.EnTaller)
            {
                return false;
            }
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            if (objetivos != null && objetivos.HayObjetivo && !string.IsNullOrEmpty(config.PalabraObjetivoLlamar)
                && objetivos.Actual.Texto != null
                && objetivos.Actual.Texto.IndexOf(config.PalabraObjetivoLlamar, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            var puntos = Registro<IPuntoLlamadoMecha>.Todos;
            Vector3 posicion = actual.Raiz.position;
            for (int i = 0; i < puntos.Count; i++)
            {
                IPuntoLlamadoMecha punto = puntos[i];
                if (punto != null && punto.Habilitado
                    && ReglaLlamado.DistanciaHorizontal(posicion, punto.Llegada.position) <= punto.Radio)
                {
                    return true;
                }
            }
            return false;
        }

        private void Mostrar(ContextoAyuda contexto)
        {
            AyudasVistas.Marcar(contexto);
            contextoActual = contexto;
            hayAyuda = true;
            switch (contexto)
            {
                case ContextoAyuda.APie:
                    progreso = new ProgresoAyuda(config.SegundosMaximos, config.SegundosCaminando);
                    break;
                case ContextoAyuda.Carro:
                    progreso = new ProgresoAyuda(config.SegundosMaximos, config.SegundosManejando);
                    break;
                default:
                    progreso = new ProgresoAyuda(config.SegundosMaximos);
                    break;
            }
            texto.text = config.Texto(contexto);
            grupo.SetActive(true);
        }

        private void Ocultar()
        {
            hayAyuda = false;
            progreso = null;
            if (grupo != null)
            {
                grupo.SetActive(false);
            }
        }

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
            if (llamado != null)
            {
                llamado.LlamadoIntentado -= AlIntentarLlamar;
            }
            llamado = nuevo;
            if (llamado != null)
            {
                llamado.LlamadoIntentado += AlIntentarLlamar;
            }
        }

        // La persona pulsó M (con cualquier resultado): ya sabe la tecla.
        private void AlIntentarLlamar(ResultadoLlamado resultado)
        {
            if (hayAyuda && contextoActual == ContextoAyuda.Llamar)
            {
                progreso.Completar();
            }
            AyudasVistas.Marcar(ContextoAyuda.Llamar);
        }
    }
}
