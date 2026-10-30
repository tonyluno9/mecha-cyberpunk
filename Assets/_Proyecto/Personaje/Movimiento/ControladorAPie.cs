using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Mueve al jugador a pie: caminar, correr, saltar y mirar con el ratón, con cabeceo de cámara.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ControladorAPie : MonoBehaviour
    {
        private const float PendienteMaxima = 45f;
        private const float EscalonMaximo = 0.35f;
        private const float UmbralPaso = 0.5f;
        private const float SegundosPasoCaminar = 0.5f;
        private const float SegundosPasoCorrer = 0.32f;
        private const float SegundosMinimosAire = 0.25f;
        private float proximoPaso;
        private float tiempoAire;

        [SerializeField] private DatosPersonaje datos;
        [SerializeField] private ConfigControlesAPie config;
        [Tooltip("Cámara hija del jugador. Se inclina para mirar y se mece al caminar.")]
        [SerializeField] private Transform camara;
        [SerializeField] private InputActionReference mover;
        [SerializeField] private InputActionReference mirar;
        [SerializeField] private InputActionReference correr;
        [SerializeField] private InputActionReference saltar;
        [SerializeField] private InputActionReference atacar;
        [Tooltip("Clic derecho: apuntar (T-400). Opcional.")]
        [SerializeField] private InputActionReference apuntar;
        [Tooltip("Cámara de hombro: límites de inclinación y giro del cuerpo (T-400). Opcional.")]
        [SerializeField] private ConfigCamaraHombro configCamara;

        private CharacterController controlador;
        private readonly MovimientoAPie movimiento = new MovimientoAPie();
        private readonly MiradaAPie mirada = new MiradaAPie();
        private readonly SeguidorImplantes implantes = new SeguidorImplantes();
        private bool cursorBloqueado;
        private bool controlActivo = true;
        private bool enPiso;
        private float giroCamara;
        private bool apuntando;

        /// <summary>Velocidad horizontal en el espacio del jugador (x lateral, y adelante, m/s).</summary>
        public Vector2 Velocidad => movimiento.VelocidadHorizontal;
        /// <summary>Velocidad vertical (m/s, positivo sube).</summary>
        public float VelocidadVertical => movimiento.VelocidadVertical;
        public bool EnPiso => enPiso;
        /// <summary>Inclinación vertical de la cámara (°, positivo mira hacia abajo).</summary>
        public float Inclinacion => mirada.Inclinacion;
        public Transform Camara => camara;
        /// <summary>Giro horizontal de la cámara de hombro (° en el mundo). Movimiento y apuntado son relativos a él.</summary>
        public float GiroCamara => giroCamara;
        /// <summary>True mientras se mantiene el clic derecho con el control activo.</summary>
        public bool Apuntando => apuntando;
        /// <summary>Multiplica la sensibilidad del ratón (T-416: baja con el zoom del sniper). 1 = normal.</summary>
        public float FactorSensibilidad { get; set; } = 1f;
        public bool CursorBloqueado => cursorBloqueado;
        public bool ControlActivo => controlActivo;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            if (datos == null || config == null || camara == null || mover == null || mirar == null
                || correr == null || saltar == null || atacar == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en ControladorAPie de " + name, this);
                enabled = false;
                return;
            }
            controlador.stepOffset = EscalonMaximo;
            controlador.slopeLimit = PendienteMaxima;
            controlador.minMoveDistance = 0f;
            giroCamara = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            mover.action.Enable();
            mirar.action.Enable();
            correr.action.Enable();
            saltar.action.Enable();
            atacar.action.Enable();
            apuntar?.action.Enable();
            implantes.Activar();
            AplicarCursor(true);
        }

        private void OnDisable()
        {
            mover.action.Disable();
            mirar.action.Disable();
            correr.action.Disable();
            saltar.action.Disable();
            atacar.action.Disable();
            apuntar?.action.Disable();
            implantes.Desactivar();
        }

        /// <summary>Congela o libera el control (lo usan el KO y el carro). Al congelar se detiene el movimiento.</summary>
        public void ActivarControl(bool activo)
        {
            controlActivo = activo;
            if (!activo)
            {
                apuntando = false;
                movimiento.Reiniciar();
            }
        }

        /// <summary>Detiene el movimiento y la mirada pendiente; se usa tras teletransportar.</summary>
        public void ReiniciarMovimiento()
        {
            movimiento.Reiniciar();
            giroCamara = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (!controlActivo)
            {
                return;
            }

            float dt = Time.deltaTime;

            if (cursorBloqueado)
            {
                float minima = configCamara != null ? configCamara.InclinacionMinima : -40f;
                float maxima = configCamara != null ? configCamara.InclinacionMaxima : 60f;
                float giro = mirada.Actualizar(mirar.action.ReadValue<Vector2>(), config.SensibilidadRaton * FactorSensibilidad, Mathf.Max(Mathf.Abs(minima), maxima));
                giroCamara = Mathf.Repeat(giroCamara + giro, 360f);
                mirada.Limitar(minima, maxima);
            }
            apuntando = apuntar != null && apuntar.action.IsPressed();

            Vector2 entrada = mover.action.ReadValue<Vector2>();
            Vector2 velocidad = movimiento.ActualizarHorizontal(entrada, correr.action.IsPressed(),
                enPiso, dt, datos.VelocidadCaminar, EfectosAPie.VelocidadCorrer(datos, implantes.Efectos), config.AceleracionPiso, config.FrenadoPiso, config.ControlAire);
            bool quiereSaltar = saltar.action.WasPressedThisFrame();
            float vertical = movimiento.ActualizarVertical(enPiso, quiereSaltar, dt,
                EfectosAPie.AlturaSalto(datos, implantes.Efectos), config.Gravedad);
            if (enPiso && quiereSaltar && vertical > 0f)
            {
                Sonidos.Reproducir("JUG_SALTO");
            }

            // Tercera persona: el movimiento es relativo a la cámara; el cuerpo gira hacia donde camina, o hacia donde mira si apunta.
            Quaternion marco = Quaternion.Euler(0f, giroCamara, 0f);
            Vector3 horizontal = marco * new Vector3(velocidad.x, 0f, velocidad.y);
            GirarCuerpo(dt, horizontal);
            controlador.Move(horizontal * dt + Vector3.up * vertical);
            enPiso = controlador.isGrounded;
            if (enPiso && tiempoAire > SegundosMinimosAire)
            {
                Sonidos.ReproducirEn("JUG_CAER", transform.position);
            }
            tiempoAire = enPiso ? 0f : tiempoAire + dt;
            if (enPiso && velocidad.sqrMagnitude > UmbralPaso * UmbralPaso && Time.time >= proximoPaso)
            {
                proximoPaso = Time.time + (correr.action.IsPressed() ? SegundosPasoCorrer : SegundosPasoCaminar);
                Sonidos.ReproducirEn("JUG_PASO_ASFALTO", transform.position);
            }
        }

        private void GirarCuerpo(float dt, Vector3 horizontal)
        {
            float giroMaximo = (configCamara != null ? configCamara.GiroCuerpo : 720f) * dt;
            float objetivo = transform.eulerAngles.y;
            if (apuntando)
            {
                objetivo = giroCamara;
            }
            else if (horizontal.sqrMagnitude > 0.01f)
            {
                objetivo = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
            }
            else
            {
                return;
            }
            float angulo = Mathf.MoveTowardsAngle(transform.eulerAngles.y, objetivo, giroMaximo);
            transform.rotation = Quaternion.Euler(0f, angulo, 0f);
        }

        // T-410: Esc ya no suelta el cursor; lo abre la pausa (el kit de UI muestra y bloquea el cursor al abrir y cerrar menús).
        private void AplicarCursor(bool bloquear)
        {
            cursorBloqueado = bloquear;
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
        }
    }
}
