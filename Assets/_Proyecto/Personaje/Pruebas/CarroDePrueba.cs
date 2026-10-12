using System;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>
    /// Doble de prueba del carro de Alan (solo para escenas y pruebas de Tony): cumple <see cref="IMontable"/> como lo hará el
    /// carro de verdad. Una caja con asiento, cámara en tercera persona y punto de subida; mientras está ocupado lee sus
    /// propias flechas y avanza y gira de forma simple.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarroDePrueba : MonoBehaviour, IMontable
    {
        private const float SeparacionPiso = 0.05f;

        [SerializeField] private ConfigCarroDePrueba config;
        [Tooltip("Dónde se sienta el jugador.")]
        [SerializeField] private Transform asiento;
        [Tooltip("Cámara en tercera persona; debe tener un AudioListener.")]
        [SerializeField] private Camera camara;
        [Tooltip("Punto en el piso junto a la puerta del conductor.")]
        [SerializeField] private Transform puntoSubida;
        [Tooltip("Acción de manejo (Vector2: x gira, y acelera).")]
        [SerializeField] private InputActionReference conducir;
        [SerializeField] private string nombre = "Carro de prueba";

        private AudioListener oyente;
        private float velocidad;

        public ModoJugador Modo => ModoJugador.Carro;
        public string Nombre => nombre;
        public Vector3 PuntoSubida => puntoSubida != null ? puntoSubida.position : transform.position;
        public bool Ocupado { get; private set; }
        public bool PuedeSubir => isActiveAndEnabled && !Ocupado;
        public bool PuedeBajar => Ocupado && Mathf.Abs(velocidad) < config.VelocidadMaximaParaBajar;
        public Transform Asiento => asiento;
        public Camera Camara => camara;

        /// <summary>Velocidad hacia adelante (m/s, negativa en reversa).</summary>
        public float Velocidad => velocidad;

        public event Action<bool> OcupadoCambio;

        private void Awake()
        {
            if (config == null || asiento == null || camara == null || puntoSubida == null || conducir == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en CarroDePrueba de " + name, this);
                enabled = false;
                return;
            }
            oyente = camara.GetComponent<AudioListener>();
            Rigidbody cuerpo = GetComponent<Rigidbody>();
            cuerpo.isKinematic = true;
            camara.enabled = false;
            if (oyente != null)
            {
                oyente.enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<IMontable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IMontable>.Quitar(this);
            if (conducir != null)
            {
                conducir.action.Disable();
            }
        }

        private void Update()
        {
            if (!Ocupado)
            {
                return;
            }
            float dt = Time.deltaTime;
            Vector2 mando = conducir.action.ReadValue<Vector2>();

            float objetivo = mando.y > 0f ? mando.y * config.VelocidadMaxima : mando.y * config.VelocidadReversa;
            float ritmo = Mathf.Approximately(mando.y, 0f) ? config.Frenado : config.Aceleracion;
            velocidad = Mathf.MoveTowards(velocidad, objetivo, ritmo * dt);

            float giro = mando.x * config.GiroMaximo * Mathf.Clamp(velocidad / config.VelocidadGiroCompleto, -1f, 1f) * dt;
            transform.Rotate(0f, giro, 0f);
            transform.position += transform.forward * (velocidad * dt);
        }

        public bool IntentarSubir(IJugador jugador)
        {
            if (!PuedeSubir || jugador == null)
            {
                return false;
            }
            Ocupado = true;
            velocidad = 0f;
            camara.enabled = true;
            if (oyente != null)
            {
                oyente.enabled = true;
            }
            conducir.action.Enable();
            OcupadoCambio?.Invoke(true);
            return true;
        }

        public bool IntentarBajar(out Pose salida)
        {
            salida = Pose.identity;
            if (!Ocupado || !PuedeBajar)
            {
                return false;
            }
            Ocupado = false;
            velocidad = 0f;
            camara.enabled = false;
            if (oyente != null)
            {
                oyente.enabled = false;
            }
            conducir.action.Disable();
            salida = new Pose(PuntoSubida + Vector3.up * SeparacionPiso,Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            OcupadoCambio?.Invoke(false);
            return true;
        }
    }
}
