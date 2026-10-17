using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Montaje;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla de ayuda de controles del tablero (T-134). La primera pelea de la sesión muestra la línea de pelea hasta que la
    /// persona dio los golpes y subió la guardia, o hasta que pasa su tiempo máximo; la primera vez que el mecha queda encendido en
    /// la calle muestra la línea de calle unos segundos. Lo visto se recuerda en <see cref="AyudasVistas"/>. Solo se ve con alguien
    /// dentro del mecha y la cabina encendida.
    /// </summary>
    public class PantallaAyudaCabina : MonoBehaviour
    {
        [SerializeField] private ConfigAyudas config;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Image fondo;
        [SerializeField] private Text texto;
        [SerializeField] private InputActionReference golpear;
        [SerializeField] private InputActionReference bloquear;

        private MontajeMecha montaje;
        private EncendidoMecha encendido;
        private ProgresoAyuda progreso;
        private ContextoAyuda contextoActual;
        private bool hayAyuda;
        private bool secuenciaReal;
        private bool suscrito;

        // Un mecha que no nace estacionado ya tiene piloto desde el inicio (arena, pruebas).
        private bool EstaPilotado => !montaje.Estacionado || montaje.Ocupado;

        public Canvas Lienzo => lienzo;
        public Text Texto => texto;

        /// <summary>True mientras la pantalla muestra una ayuda.</summary>
        public bool Visible => lienzo != null && lienzo.enabled;

        /// <summary>Contexto de la ayuda que se ve (solo vale si <see cref="Visible"/>).</summary>
        public ContextoAyuda Contexto => contextoActual;

        private void Awake()
        {
            montaje = GetComponentInParent<MontajeMecha>();
            encendido = GetComponentInParent<EncendidoMecha>();
            if (config == null || lienzo == null || fondo == null || texto == null || golpear == null || bloquear == null
                || montaje == null || encendido == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaAyudaCabina de " + name, this);
                enabled = false;
                return;
            }
            fondo.color = config.ColorFondoCabina;
            texto.color = config.ColorTextoCabina;
            lienzo.enabled = false;
        }

        private void OnEnable()
        {
            if (encendido == null)
            {
                return;
            }
            suscrito = true;
            EventosPelea.PeleaIniciada += AlIniciarPelea;
            EventosPelea.PeleaTerminada += AlTerminarPelea;
            encendido.PasoCambio += AlCambiarPaso;
            encendido.Completado += AlCompletarEncendido;
            montaje.OcupadoCambio += AlCambiarOcupado;
        }

        private void OnDisable()
        {
            if (!suscrito)
            {
                return;
            }
            suscrito = false;
            EventosPelea.PeleaIniciada -= AlIniciarPelea;
            EventosPelea.PeleaTerminada -= AlTerminarPelea;
            encendido.PasoCambio -= AlCambiarPaso;
            encendido.Completado -= AlCompletarEncendido;
            montaje.OcupadoCambio -= AlCambiarOcupado;
            Ocultar();
        }

        private void Update()
        {
            if (!hayAyuda)
            {
                return;
            }
            if (contextoActual == ContextoAyuda.MechaPelea)
            {
                if (golpear.action.WasPressedThisFrame())
                {
                    progreso.RegistrarGolpe();
                }
                if (bloquear.action.WasPressedThisFrame())
                {
                    progreso.RegistrarGuardia();
                }
            }
            progreso.Avanzar(Time.deltaTime, false);
            if (progreso.Terminada)
            {
                Ocultar();
                return;
            }
            lienzo.enabled = EstaPilotado && encendido.EstaEncendido;
        }

        private void AlCambiarPaso(PasoEncendido paso)
        {
            // Solo la secuencia que juega la persona pasa por el paso 1; la pelea y el arranque directo la saltan.
            if (paso == PasoEncendido.Paso1)
            {
                secuenciaReal = true;
            }
        }

        private void AlCompletarEncendido()
        {
            bool eraReal = secuenciaReal;
            secuenciaReal = false;
            if (eraReal && EstaPilotado && !hayAyuda && !AyudasVistas.YaVista(ContextoAyuda.MechaCalle))
            {
                Mostrar(ContextoAyuda.MechaCalle);
            }
        }

        private void AlIniciarPelea()
        {
            if (!EstaPilotado || AyudasVistas.YaVista(ContextoAyuda.MechaPelea))
            {
                return;
            }
            Mostrar(ContextoAyuda.MechaPelea);
        }

        private void AlTerminarPelea(ResultadoPelea resultado)
        {
            if (hayAyuda && contextoActual == ContextoAyuda.MechaPelea)
            {
                Ocultar();
            }
        }

        private void AlCambiarOcupado(bool ocupado)
        {
            if (!ocupado)
            {
                secuenciaReal = false;
                Ocultar();
            }
        }

        private void Mostrar(ContextoAyuda contexto)
        {
            AyudasVistas.Marcar(contexto);
            contextoActual = contexto;
            hayAyuda = true;
            progreso = contexto == ContextoAyuda.MechaPelea
                ? new ProgresoAyuda(config.SegundosMaximosPelea, 0f, config.GolpesNecesarios, config.GuardiasNecesarias)
                : new ProgresoAyuda(config.SegundosMaximos);
            texto.text = config.Texto(contexto);
            lienzo.enabled = EstaPilotado && encendido.EstaEncendido;
        }

        private void Ocultar()
        {
            hayAyuda = false;
            progreso = null;
            if (lienzo != null)
            {
                lienzo.enabled = false;
            }
        }
    }
}
