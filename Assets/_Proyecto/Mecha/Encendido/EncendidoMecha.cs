using System;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Montaje;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mecha.Encendido
{
    /// <summary>
    /// Encendido de la cabina en 3 pasos cada vez que el jugador sube al mecha (T-065). Mientras no termina: el piloto no tiene
    /// control, no se puede bajar y la cabina está apagada. Un mecha Directo, o una pelea preparada, queda encendido al instante.
    /// </summary>
    [RequireComponent(typeof(MontajeMecha))]
    [RequireComponent(typeof(PeleadorMecha))]
    public class EncendidoMecha : MonoBehaviour
    {
        [SerializeField] private ConfigEncendido config;
        [SerializeField] private InputActionReference confirmar;
        [SerializeField] private InputActionReference levantar;
        [SerializeField] private InputActionReference cristal;
        [Tooltip("Objeto Sacudida que contiene la cabina y la cámara: se baja mientras el mecha está agachado.")]
        [SerializeField] private SacudidaCabina cabina;
        [Tooltip("Panel oscuro que tapa el cristal hasta el paso 3.")]
        [SerializeField] private Transform persiana;
        [Tooltip("Piezas de neón de la cabina: apagadas hasta el paso 1.")]
        [SerializeField] private Renderer[] luces;
        [Tooltip("Pantalla de vida del tablero: apagada hasta el paso 1.")]
        [SerializeField] private GameObject pantallaVida;

        private MontajeMecha montaje;
        private PeleadorMecha peleador;
        private SecuenciaEncendido secuencia;
        private Vector3 reposoPersiana;
        private float alturaActual;

        public ConfigEncendido Config => config;
        public PasoEncendido Paso => secuencia != null ? secuencia.Paso : PasoEncendido.Apagado;
        public float Progreso => secuencia != null ? secuencia.Progreso : 0f;
        public bool EstaEncendido => secuencia != null && secuencia.EstaEncendido;
        public bool Ocupado => montaje != null && montaje.Ocupado;

        /// <summary>De 0 (agachado) a 1 (a su altura).</summary>
        public float Altura => alturaActual;

        public event Action<PasoEncendido> PasoCambio;
        public event Action Completado;
        public event Action TeclaIncorrecta;

        private void Awake()
        {
            montaje = GetComponent<MontajeMecha>();
            peleador = GetComponent<PeleadorMecha>();
            if (config == null || confirmar == null || levantar == null || cristal == null || cabina == null
                || persiana == null || pantallaVida == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en EncendidoMecha de " + name, this);
                enabled = false;
                return;
            }
            reposoPersiana = persiana.localPosition;
            secuencia = new SecuenciaEncendido(config.SegundosPaso1, config.SegundosMantenerPaso2, config.SegundosPaso3,
                config.PausaEntrePasos);
            secuencia.PasoCambio += AlCambiarPaso;
            secuencia.Completado += AlCompletar;
            secuencia.TeclaIncorrecta += AlEquivocarse;

            if (montaje.Estacionado)
            {
                AplicarVisual();
            }
            else
            {
                secuencia.Completar();
            }
        }

        private void OnEnable()
        {
            if (secuencia == null)
            {
                return;
            }
            montaje.OcupadoCambio += AlCambiarOcupado;
            peleador.PeleaPreparada += secuencia.Completar;
        }

        private void OnDisable()
        {
            if (secuencia == null)
            {
                return;
            }
            montaje.OcupadoCambio -= AlCambiarOcupado;
            peleador.PeleaPreparada -= secuencia.Completar;
            DesactivarAcciones();
        }

        private void AlCambiarOcupado(bool ocupado)
        {
            if (!ocupado)
            {
                DesactivarAcciones();
                return;
            }
            if (!montaje.Estacionado)
            {
                return;
            }
            alturaActual = 0f;
            peleador.FijarPermisoPiloto(false);
            montaje.FijarBajadaBloqueada(true);
            confirmar.action.Enable();
            levantar.action.Enable();
            cristal.action.Enable();
            secuencia.Reiniciar();
            AplicarVisual();
        }

        private void Update()
        {
            if (!montaje.Ocupado || secuencia.EstaEncendido)
            {
                return;
            }
            secuencia.Avanzar(Time.deltaTime, confirmar.action.WasPressedThisFrame(), levantar.action.IsPressed(),
                cristal.action.WasPressedThisFrame());
            if (!secuencia.EstaEncendido)
            {
                float objetivo = secuencia.Paso == PasoEncendido.Paso1 ? 0f
                    : secuencia.Paso == PasoEncendido.Paso2 ? secuencia.Progreso : 1f;
                alturaActual = Mathf.MoveTowards(alturaActual, objetivo, Time.deltaTime / Mathf.Max(config.SegundosMantenerPaso2, Mathf.Epsilon));
                AplicarVisual();
            }
        }

        private void AlCambiarPaso(PasoEncendido paso)
        {
            PasoCambio?.Invoke(paso);
        }

        private void AlEquivocarse()
        {
            TeclaIncorrecta?.Invoke();
        }

        private void AlCompletar()
        {
            alturaActual = 1f;
            DesactivarAcciones();
            AplicarVisual();
            if (montaje.Ocupado)
            {
                montaje.FijarBajadaBloqueada(false);
                peleador.FijarPermisoPiloto(true);
            }
            Completado?.Invoke();
        }

        private void DesactivarAcciones()
        {
            confirmar.action.Disable();
            levantar.action.Disable();
            cristal.action.Disable();
        }

        // Pone la cabina como corresponde al paso: luces, altura de la cámara y persiana.
        private void AplicarVisual()
        {
            bool encendido = secuencia.EstaEncendido;
            bool luzPrendida = encendido || secuencia.EntradaConfirmada;
            foreach (Renderer luz in luces)
            {
                if (luz != null)
                {
                    luz.enabled = luzPrendida;
                }
            }
            pantallaVida.SetActive(luzPrendida);

            float altura = Mathf.SmoothStep(0f, 1f, alturaActual);
            cabina.FijarDesplazamientoBase(Vector3.up * (config.DesplazamientoAgachado * (1f - altura)));

            float apertura = encendido ? 1f : secuencia.Paso == PasoEncendido.Paso3 ? secuencia.Progreso : 0f;
            persiana.gameObject.SetActive(apertura < 1f);
            persiana.localPosition = reposoPersiana + Vector3.up * (config.AlturaPersiana * Mathf.SmoothStep(0f, 1f, apertura));
        }
    }
}
