using MechaCyberpunk.Personaje.Combate;
using Unity.Cinemachine;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// Cámara de hombro (T-400), hecha con Cinemachine desde T-723. Va en el objeto de la cámara. Al despertar le pone un
    /// <see cref="CinemachineBrain"/> y crea un <see cref="CinemachineCamera"/> "VcamAPie" (hijo del jugador) con
    /// <see cref="CinemachineThirdPersonFollow"/> (detrás y arriba del hombro, con su colisión contra muros) y
    /// <see cref="CinemachineDeoccluder"/>. Este script solo le da los datos cada frame: el giro y la inclinación de
    /// <see cref="ControladorAPie"/> van en el punto que sigue la vcam, el techo baja el punto de giro, y apuntar o el
    /// zoom del sniper cambian el campo de visión (lo único que cambia al apuntar). Es la única cámara del jugador a pie:
    /// si la cámara está apagada (carro) o el control está quitado (menús, cinemáticas), el Brain se apaga y no toca la cámara.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class CamaraHombro : MonoBehaviour
    {
        private const int MaxChoques = 16;
        public const string EtiquetaJugador = "Player";

        [SerializeField] private ConfigCamaraHombro config;
        [SerializeField] private ControladorAPie controlador;
        [SerializeField] private Camera camara;

        private readonly RaycastHit[] choques = new RaycastHit[MaxChoques];
        private float lateral;
        private float altura;
        private float distancia;
        private float fov;
        private bool iniciada;
        private ZoomSniper zoom;
        private SensacionDisparo sensacion;
        private CinemachineBrain brain;
        private CinemachineCamera vcam;
        private CinemachineThirdPersonFollow seguir;
        private CinemachineDeoccluder desocluir;
        private Transform punto;

        public float DistanciaActual => distancia;

        /// <summary>La cámara virtual de Cinemachine del jugador a pie.</summary>
        public CinemachineCamera Vcam => vcam;

        /// <summary>El Brain de Cinemachine que mueve la cámara del jugador.</summary>
        public CinemachineBrain Brain => brain;

        /// <summary>La parte de Cinemachine que coloca la cámara detrás y arriba del hombro.</summary>
        public CinemachineThirdPersonFollow Seguir => seguir;

        /// <summary>Campo de visión que pide la cámara ahora mismo (°), con el zoom del sniper.</summary>
        public float FovActual => vcam != null ? vcam.Lens.FieldOfView : 0f;

        private void Awake()
        {
            if (camara == null)
            {
                camara = GetComponent<Camera>();
            }
            if (controlador == null)
            {
                controlador = GetComponentInParent<ControladorAPie>();
            }
            zoom = controlador != null ? controlador.GetComponent<ZoomSniper>() : null;
            if (config == null || controlador == null || camara == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en CamaraHombro de " + name, this);
                enabled = false;
                return;
            }
            Construir();
        }

        /// <summary>Crea el Brain y la vcam. La vcam y el punto cuelgan del jugador, no de la cámara.</summary>
        private void Construir()
        {
            // Los colliders del jugador llevan la etiqueta Player: Cinemachine los ignora (no hay capas libres para eso).
            foreach (Collider colisionador in controlador.GetComponentsInChildren<Collider>(true))
            {
                colisionador.gameObject.tag = EtiquetaJugador;
            }

            brain = camara.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = camara.gameObject.AddComponent<CinemachineBrain>();
            }
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            brain.ShowDebugText = false;
            brain.enabled = false;

            punto = new GameObject("PuntoCamara").transform;
            punto.SetParent(controlador.transform, false);

            GameObject objeto = new GameObject("VcamAPie");
            objeto.SetActive(false);
            objeto.transform.SetParent(controlador.transform, false);
            vcam = objeto.AddComponent<CinemachineCamera>();
            vcam.Follow = punto;
            vcam.Priority.Enabled = true;
            vcam.Priority.Value = 10;
            LensSettings lente = vcam.Lens;
            lente.FieldOfView = config.FovNormal;
            lente.NearClipPlane = camara.nearClipPlane;
            lente.FarClipPlane = camara.farClipPlane;
            vcam.Lens = lente;

            seguir = objeto.AddComponent<CinemachineThirdPersonFollow>();
            seguir.CameraSide = 1f;
            seguir.Damping = Vector3.zero;
            CinemachineThirdPersonFollow.ObstacleSettings obstaculos = seguir.AvoidObstacles;
            obstaculos.Enabled = true;
            obstaculos.CollisionFilter = ~0;
            obstaculos.IgnoreTag = EtiquetaJugador;
            obstaculos.CameraRadius = config.RadioColision;
            obstaculos.DampingIntoCollision = 0f;
            obstaculos.DampingFromCollision = 0.2f;
            seguir.AvoidObstacles = obstaculos;

            desocluir = objeto.AddComponent<CinemachineDeoccluder>();
            desocluir.CollideAgainst = ~0;
            desocluir.IgnoreTag = EtiquetaJugador;
            desocluir.TransparentLayers = 0;
            desocluir.MinimumDistanceFromTarget = 0.15f;
            desocluir.AvoidObstacles.Enabled = true;
            desocluir.AvoidObstacles.DistanceLimit = 0f;
            desocluir.AvoidObstacles.MinimumOcclusionTime = 0f;
            desocluir.AvoidObstacles.CameraRadius = config.RadioColision;
            desocluir.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            desocluir.AvoidObstacles.MaximumEffort = 4;
            desocluir.AvoidObstacles.SmoothingTime = 0f;
            desocluir.AvoidObstacles.Damping = 0f;
            desocluir.AvoidObstacles.DampingWhenOccluded = 0f;
            desocluir.AvoidObstacles.UseFollowTarget.Enabled = true;
            desocluir.AvoidObstacles.UseFollowTarget.YOffset = 0f;

            objeto.SetActive(true);
        }

        private void OnDisable()
        {
            if (brain != null)
            {
                brain.enabled = false;
            }
        }

        private void LateUpdate()
        {
            bool activa = camara.enabled && controlador.ControlActivo;
            if (!activa)
            {
                iniciada = false;
                if (brain.enabled)
                {
                    brain.enabled = false;
                }
                return;
            }
            bool apuntando = controlador.Apuntando;
            float objetivoLateral = apuntando ? config.LateralApuntando : config.LateralNormal;
            float objetivoAltura = apuntando ? config.AlturaApuntando : config.AlturaNormal;
            float objetivoDistancia = apuntando ? config.DistanciaApuntando : config.DistanciaNormal;
            float objetivoFov = apuntando ? config.FovApuntando : config.FovNormal;
            if (!iniciada)
            {
                iniciada = true;
                lateral = objetivoLateral;
                altura = objetivoAltura;
                distancia = objetivoDistancia;
                fov = objetivoFov;
            }
            else
            {
                float paso = 1f - Mathf.Exp(-config.Suavizado * Time.deltaTime);
                lateral = Mathf.Lerp(lateral, objetivoLateral, paso);
                altura = Mathf.Lerp(altura, objetivoAltura, paso);
                distancia = Mathf.Lerp(distancia, objetivoDistancia, paso);
                fov = Mathf.Lerp(fov, objetivoFov, paso);
            }

            if (sensacion == null)
            {
                sensacion = controlador.GetComponent<SensacionDisparo>();
            }
            float patada = sensacion != null ? sensacion.Patada : 0f;
            float desvio = sensacion != null ? sensacion.Desvio : 0f;
            Quaternion giro = Quaternion.Euler(controlador.Inclinacion - (zoom != null ? zoom.Sacudida : 0f) - patada, controlador.GiroCamara + desvio, 0f);

            // El techo baja la cámara y el punto de giro (casas, túneles, cuartos): la cámara no queda sobre el techo.
            float pivote = altura - controlador.AjusteAlturaCamara;
            float total = LibreArriba(pivote + config.BrazoVertical);
            float pivoteLibre = Mathf.Min(pivote, total);
            float brazo = Mathf.Max(0f, total - pivoteLibre);

            punto.SetPositionAndRotation(controlador.transform.position + Vector3.up * pivoteLibre, giro);
            seguir.ShoulderOffset = new Vector3(lateral, 0f, 0f);
            seguir.VerticalArmLength = brazo;
            seguir.CameraDistance = distancia * (1f - config.AcercarMirandoAbajo * Mathf.Clamp01(controlador.Inclinacion / 30f));
            LensSettings lente = vcam.Lens;
            lente.FieldOfView = zoom != null ? Mathf.Lerp(fov, config.FovSniper, zoom.Mezcla) : fov;
            vcam.Lens = lente;

            if (!brain.enabled)
            {
                // Al volver el control (de un menú o de bajar del carro) la cámara salta a su sitio, sin deslizarse.
                brain.enabled = true;
                vcam.PreviousStateIsValid = false;
            }
        }

        /// <summary>
        /// T-699: la cámara va alta (más que la cabeza); bajo un techo (casas, túneles) la altura se recorta hasta antes de lo que
        /// haya encima del jugador. T-723: se mide desde el pecho del jugador para que un techo bajo (2 m) también cuente.
        /// </summary>
        private float LibreArriba(float alturaDeseada)
        {
            const float desdeAltura = 1.0f;
            if (alturaDeseada <= desdeAltura)
            {
                return alturaDeseada;
            }
            Vector3 desde = controlador.transform.position + Vector3.up * desdeAltura;
            float largo = alturaDeseada - desdeAltura;
            int cantidad = Physics.SphereCastNonAlloc(desde, config.RadioColision, Vector3.up, choques, largo, ~0, QueryTriggerInteraction.Ignore);
            float minima = largo;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit choque = choques[i];
                if (choque.collider.transform.root == controlador.transform.root || choque.distance <= 0f)
                {
                    continue;
                }
                minima = Mathf.Min(minima, choque.distance);
            }
            return desdeAltura + minima;
        }
    }
}
