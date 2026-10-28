using System;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Montaje
{
    /// <summary>
    /// El mecha como montable (<see cref="IMontable"/>, modo Mecha). En arranque Estacionado nace vacío, con cámara, oído y
    /// permiso de piloto apagados, y se registra en Registro&lt;IMontable&gt;. En arranque Directo no hace nada: el mecha
    /// funciona como siempre (arena, pruebas) y no se registra.
    /// </summary>
    [RequireComponent(typeof(ControladorMecha))]
    [RequireComponent(typeof(PeleadorMecha))]
    public class MontajeMecha : MonoBehaviour, IMontable
    {
        private const int MaxColisionadores = 16;
        private static readonly float[] GirosSalida = { 0f, -90f, 90f, 180f };

        [SerializeField] private ConfigMontajeMecha config;
        [SerializeField] private ArranqueMontaje arranque = ArranqueMontaje.Directo;
        [SerializeField] private string nombre = "Mecha";
        [Tooltip("Dónde va el jugador: dentro del cuerpo, a la altura de la cabina.")]
        [SerializeField] private Transform asiento;
        [Tooltip("La cámara del mecha (con su AudioListener).")]
        [SerializeField] private Camera camara;
        [Tooltip("Punto en el piso junto a la escotilla, al frente del mecha.")]
        [SerializeField] private Transform puntoSubida;

        private ControladorMecha controlador;
        private PeleadorMecha peleador;
        private VidaMecha vida;
        private LlamadoMecha llamado;
        private AudioListener oyente;
        private bool bajadaBloqueada;
        private bool bajadaForzada;
        private readonly Collider[] colisionadores = new Collider[MaxColisionadores];

        public ModoJugador Modo => ModoJugador.Mecha;
        public string Nombre => nombre;
        public Vector3 PuntoSubida => puntoSubida != null ? puntoSubida.position : transform.position;
        public bool Ocupado { get; private set; }
        public bool Estacionado => arranque == ArranqueMontaje.Estacionado;
        public bool PuedeSubir => Estacionado && !Ocupado && isActiveAndEnabled && vida != null && !vida.EstaKO
            && (llamado == null || llamado.PermiteSubir);
        public bool PuedeBajar => Ocupado && !bajadaBloqueada && controlador.VelocidadLocal.magnitude < config.VelocidadMaximaParaBajar;
        public Transform Asiento => asiento;
        public Camera Camara => camara;

        public event Action<bool> OcupadoCambio;

        private void Awake()
        {
            controlador = GetComponent<ControladorMecha>();
            peleador = GetComponent<PeleadorMecha>();
            vida = GetComponent<VidaMecha>();
            llamado = GetComponent<LlamadoMecha>();
            if (config == null || asiento == null || camara == null || puntoSubida == null || vida == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en MontajeMecha de " + name, this);
                enabled = false;
                return;
            }
            oyente = camara.GetComponent<AudioListener>();
            if (Estacionado)
            {
                camara.enabled = false;
                if (oyente != null)
                {
                    oyente.enabled = false;
                }
                peleador.FijarPermisoPiloto(false);
            }
        }

        private void OnEnable()
        {
            if (Estacionado)
            {
                Registro<IMontable>.Agregar(this);
            }
        }

        private void OnDisable()
        {
            Registro<IMontable>.Quitar(this);
        }

        /// <summary>El encendido (T-065) impide bajar mientras la cabina no está encendida.</summary>
        public void FijarBajadaBloqueada(bool bloqueada)
        {
            bajadaBloqueada = bloqueada;
        }

        public bool IntentarSubir(IJugador jugador)
        {
            if (!PuedeSubir || jugador == null)
            {
                return false;
            }
            Ocupado = true;
            camara.enabled = true;
            if (oyente != null)
            {
                oyente.enabled = true;
            }
            peleador.FijarPermisoPiloto(true);
            OcupadoCambio?.Invoke(true);
            return true;
        }

        /// <summary>
        /// La siguiente <see cref="IntentarBajar"/> (y solo esa) ignora el encendido y la velocidad, y si no hay un lugar libre alrededor
        /// deja al jugador frente al mecha. La usa el doctor de la clínica (T-196) cuando el mecha cayó en KO con el jugador adentro.
        /// </summary>
        public void ForzarSiguienteBajada()
        {
            bajadaForzada = true;
        }

        public bool IntentarBajar(out Pose salida)
        {
            salida = Pose.identity;
            bool forzada = bajadaForzada;
            bajadaForzada = false;
            if (!Ocupado || (!forzada && !PuedeBajar))
            {
                return false;
            }
            Pose libre;
            if (!BuscarSalida(out libre))
            {
                if (!forzada)
                {
                    return false;
                }
                libre = new Pose(transform.position + transform.forward * config.DistanciaSalida, transform.rotation);
            }
            Ocupado = false;
            peleador.FijarPermisoPiloto(false);
            camara.enabled = false;
            if (oyente != null)
            {
                oyente.enabled = false;
            }
            salida = libre;
            OcupadoCambio?.Invoke(false);
            return true;
        }

        // Prueba frente, izquierda, derecha y atrás; devuelve la primera pose donde cabe una persona.
        private bool BuscarSalida(out Pose pose)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < GirosSalida.Length; i++)
            {
                Vector3 direccion = Quaternion.Euler(0f, GirosSalida[i], 0f) * transform.forward;
                Vector3 candidato = transform.position + direccion * config.DistanciaSalida;
                if (PoseLibre(candidato, out pose))
                {
                    return true;
                }
            }
            pose = Pose.identity;
            return false;
        }

        private bool PoseLibre(Vector3 candidato, out Pose pose)
        {
            pose = Pose.identity;
            Vector3 origen = candidato + Vector3.up * config.AlturaBusquedaPiso;
            float y = transform.position.y;
            if (Physics.Raycast(origen, Vector3.down, out RaycastHit golpe, config.AlcanceBusquedaPiso,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && golpe.transform.root != transform.root)
            {
                y = golpe.point.y;
            }
            Vector3 pies = new Vector3(candidato.x, y + config.SeparacionPiso, candidato.z);
            Vector3 bajo = pies + Vector3.up * config.RadioPersona;
            Vector3 alto = pies + Vector3.up * (config.AlturaPersona - config.RadioPersona);
            int cuenta = Physics.OverlapCapsuleNonAlloc(bajo, alto, config.RadioPersona, colisionadores,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cuenta; i++)
            {
                if (colisionadores[i].transform.root != transform.root)
                {
                    return false;
                }
            }
            Vector3 haciaMecha = transform.position - pies;
            haciaMecha.y = 0f;
            Quaternion giro = haciaMecha.sqrMagnitude > 0f ? Quaternion.LookRotation(haciaMecha) : transform.rotation;
            pose = new Pose(pies, giro);
            return true;
        }
    }
}
