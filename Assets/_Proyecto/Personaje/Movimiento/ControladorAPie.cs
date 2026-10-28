using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Mueve al jugador a pie: caminar, correr, saltar y mirar con el ratón, con cabeceo de cámara.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ControladorAPie : MonoBehaviour
    {
        private const float VueltaCompleta = Mathf.PI * 2f;
        private const float PendienteMaxima = 45f;
        private const float EscalonMaximo = 0.35f;

        [SerializeField] private DatosPersonaje datos;
        [SerializeField] private ConfigControlesAPie config;
        [Tooltip("Cámara hija del jugador. Se inclina para mirar y se mece al caminar.")]
        [SerializeField] private Transform camara;
        [SerializeField] private InputActionReference mover;
        [SerializeField] private InputActionReference mirar;
        [SerializeField] private InputActionReference correr;
        [SerializeField] private InputActionReference saltar;
        [SerializeField] private InputActionReference atacar;
        [SerializeField] private InputActionReference liberarCursor;

        private CharacterController controlador;
        private readonly MovimientoAPie movimiento = new MovimientoAPie();
        private readonly MiradaAPie mirada = new MiradaAPie();
        private readonly SeguidorImplantes implantes = new SeguidorImplantes();
        private float faseCabeceo;
        private bool cursorBloqueado;
        private bool controlActivo = true;
        private bool enPiso;

        /// <summary>Velocidad horizontal en el espacio del jugador (x lateral, y adelante, m/s).</summary>
        public Vector2 Velocidad => movimiento.VelocidadHorizontal;
        /// <summary>Velocidad vertical (m/s, positivo sube).</summary>
        public float VelocidadVertical => movimiento.VelocidadVertical;
        public bool EnPiso => enPiso;
        /// <summary>Inclinación vertical de la cámara (°, positivo mira hacia abajo).</summary>
        public float Inclinacion => mirada.Inclinacion;
        public Transform Camara => camara;
        public bool CursorBloqueado => cursorBloqueado;
        public bool ControlActivo => controlActivo;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            if (datos == null || config == null || camara == null || mover == null || mirar == null
                || correr == null || saltar == null || atacar == null || liberarCursor == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en ControladorAPie de " + name, this);
                enabled = false;
                return;
            }
            controlador.stepOffset = EscalonMaximo;
            controlador.slopeLimit = PendienteMaxima;
            controlador.minMoveDistance = 0f;
            Vector3 posicionCamara = camara.localPosition;
            posicionCamara.y = config.AlturaOjos;
            camara.localPosition = posicionCamara;
        }

        private void OnEnable()
        {
            mover.action.Enable();
            mirar.action.Enable();
            correr.action.Enable();
            saltar.action.Enable();
            atacar.action.Enable();
            liberarCursor.action.Enable();
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
            liberarCursor.action.Disable();
            implantes.Desactivar();
        }

        /// <summary>Congela o libera el control (lo usan el KO y el carro). Al congelar se detiene el movimiento.</summary>
        public void ActivarControl(bool activo)
        {
            controlActivo = activo;
            if (!activo)
            {
                movimiento.Reiniciar();
            }
        }

        /// <summary>Detiene el movimiento y la mirada pendiente; se usa tras teletransportar.</summary>
        public void ReiniciarMovimiento()
        {
            movimiento.Reiniciar();
        }

        private void Update()
        {
            if (!controlActivo)
            {
                return;
            }

            float dt = Time.deltaTime;
            ActualizarCursor();

            if (cursorBloqueado)
            {
                float giro = mirada.Actualizar(mirar.action.ReadValue<Vector2>(), config.SensibilidadRaton, config.LimiteMirarVertical);
                transform.Rotate(0f, giro, 0f);
            }

            Vector2 velocidad = movimiento.ActualizarHorizontal(mover.action.ReadValue<Vector2>(), correr.action.IsPressed(),
                enPiso, dt, datos.VelocidadCaminar, EfectosAPie.VelocidadCorrer(datos, implantes.Efectos), config.AceleracionPiso, config.FrenadoPiso, config.ControlAire);
            float vertical = movimiento.ActualizarVertical(enPiso, saltar.action.WasPressedThisFrame(), dt,
                EfectosAPie.AlturaSalto(datos, implantes.Efectos), config.Gravedad);

            Vector3 horizontal = transform.TransformDirection(new Vector3(velocidad.x, 0f, velocidad.y));
            controlador.Move(horizontal * dt + Vector3.up * vertical);
            enPiso = controlador.isGrounded;

            ActualizarCamara(dt, velocidad);
        }

        private void ActualizarCursor()
        {
            if (cursorBloqueado)
            {
                if (liberarCursor.action.WasPressedThisFrame())
                {
                    AplicarCursor(false);
                }
            }
            else if (atacar.action.WasPressedThisFrame())
            {
                AplicarCursor(true);
            }
        }

        private void AplicarCursor(bool bloquear)
        {
            cursorBloqueado = bloquear;
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
        }

        private void ActualizarCamara(float dt, Vector2 velocidad)
        {
            float proporcion = enPiso && datos.VelocidadCaminar > 0f ? Mathf.Min(velocidad.magnitude / datos.VelocidadCaminar, 1f) : 0f;
            faseCabeceo = Mathf.Repeat(faseCabeceo + proporcion * config.FrecuenciaCabeceo * VueltaCompleta * dt, VueltaCompleta);

            Vector3 posicion = camara.localPosition;
            posicion.y = config.AlturaOjos + Mathf.Sin(faseCabeceo) * config.AmplitudCabeceo * proporcion;
            camara.localPosition = posicion;
            camara.localRotation = Quaternion.Euler(mirada.Inclinacion, 0f, 0f);
        }
    }
}
