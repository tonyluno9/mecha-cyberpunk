using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mecha.Movimiento
{
    /// <summary>Mueve el mecha del jugador: caminar con inercia, giro del cuerpo, mirada y cabeceo de cámara.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ControladorMecha : MonoBehaviour
    {
        private const float VueltaCompleta = Mathf.PI * 2f;
        private const float VelocidadVerticalEnPiso = -2f;

        [SerializeField] private DatosMecha datos;
        [SerializeField] private ConfigControlesMecha config;
        [Tooltip("Cámara hija del mecha. Se inclina para mirar y se mece al caminar.")]
        [SerializeField] private Transform camara;
        [SerializeField] private InputActionReference mover;
        [SerializeField] private InputActionReference mirar;
        [SerializeField] private InputActionReference golpear;
        [SerializeField] private InputActionReference liberarCursor;

        private CharacterController controlador;
        private Vector2 velocidadLocal;
        private float giroPendiente;
        private float inclinacionVertical;
        private float velocidadVertical;
        private float faseCabeceo;
        private float alturaCamaraBase;
        private bool cursorBloqueado;

        /// <summary>Velocidad horizontal en el espacio del mecha (x lateral, y adelante, m/s).</summary>
        public Vector2 VelocidadLocal => velocidadLocal;
        /// <summary>Giro del cuerpo que falta por hacer (°).</summary>
        public float GiroPendiente => giroPendiente;
        /// <summary>Inclinación vertical de la cámara (°, positivo mira hacia abajo).</summary>
        public float InclinacionVertical => inclinacionVertical;
        public bool CursorBloqueado => cursorBloqueado;
        public Transform Camara => camara;
        public float AlturaCamaraBase => alturaCamaraBase;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            if (datos == null || config == null || camara == null || mover == null || mirar == null
                || golpear == null || liberarCursor == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en ControladorMecha de " + name, this);
                enabled = false;
                return;
            }
            alturaCamaraBase = camara.localPosition.y;
        }

        private void OnEnable()
        {
            mover.action.Enable();
            mirar.action.Enable();
            golpear.action.Enable();
            liberarCursor.action.Enable();
            AplicarCursor(true);
        }

        private void OnDisable()
        {
            mover.action.Disable();
            mirar.action.Disable();
            liberarCursor.action.Disable();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            ActualizarCursor();

            Vector2 entrada = mover.action.ReadValue<Vector2>();
            velocidadLocal = MovimientoPesado.Calcular(entrada, velocidadLocal, dt,
                datos.VelocidadMaxima, datos.Aceleracion, datos.Desaceleracion);

            if (cursorBloqueado)
            {
                Vector2 delta = mirar.action.ReadValue<Vector2>();
                giroPendiente += delta.x * config.SensibilidadRaton;
                inclinacionVertical = Mathf.Clamp(inclinacionVertical - delta.y * config.SensibilidadRaton,
                    -config.LimiteMirarVertical, config.LimiteMirarVertical);
            }

            float giro = GiroPesado.Calcular(giroPendiente, dt, datos.VelocidadGiroMaxima);
            giroPendiente -= giro;
            transform.Rotate(0f, giro, 0f);

            if (controlador.isGrounded)
            {
                velocidadVertical = VelocidadVerticalEnPiso;
            }
            else
            {
                velocidadVertical += Physics.gravity.y * dt;
            }

            Vector3 horizontal = transform.TransformDirection(new Vector3(velocidadLocal.x, 0f, velocidadLocal.y));
            controlador.Move((horizontal + Vector3.up * velocidadVertical) * dt);

            ActualizarCamara(dt);
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
            else if (golpear.action.WasPressedThisFrame())
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

        private void ActualizarCamara(float dt)
        {
            float proporcion = datos.VelocidadMaxima > 0f ? velocidadLocal.magnitude / datos.VelocidadMaxima : 0f;
            faseCabeceo = Mathf.Repeat(faseCabeceo + proporcion * config.PasosPorSegundo * VueltaCompleta * dt, VueltaCompleta);
            float desplazamiento = Mathf.Sin(faseCabeceo) * config.AmplitudCabeceo * proporcion;

            Vector3 posicion = camara.localPosition;
            posicion.y = alturaCamaraBase + desplazamiento;
            camara.localPosition = posicion;
            camara.localRotation = Quaternion.Euler(inclinacionVertical, 0f, 0f);
        }
    }
}
