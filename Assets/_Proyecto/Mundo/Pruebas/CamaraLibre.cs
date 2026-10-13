using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// Cámara libre de pruebas de Alan: camina por el barrio con CharacterController y, con F, vuela sin colisiones.
    /// Implementa <see cref="IJugador"/> (modo a pie) para probar puertas y zonas sin el jugador de Tony.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CamaraLibre : MonoBehaviour, IJugador
    {
        private const float PegadoAlSuelo = 2f;

        [SerializeField] private ConfigCamaraLibre config;
        [Tooltip("Asset ControlesCamaraLibre con las acciones Mover, Mirar, Correr, AlternarVuelo, Subir y Bajar.")]
        [SerializeField] private InputActionAsset controles;
        [SerializeField] private Camera camara;

        private CharacterController personaje;
        private InputAction mover;
        private InputAction mirar;
        private InputAction correr;
        private InputAction alternarVuelo;
        private InputAction subir;
        private InputAction bajar;
        private InputAction interactuar;
        private float yaw;
        private float inclinacion;
        private float velocidadVertical;
        private bool volando;
        private AudioListener oyente;
        private IMontable montable;

        public Transform Raiz => transform;
        public ModoJugador Modo { get; private set; } = ModoJugador.APie;
        public Camera Camara => montable != null ? montable.Camara : camara;
        public bool EstaVolando => volando;
        public float Yaw => yaw;
        public float Inclinacion => inclinacion;

        /// <summary>Resultado de la última puerta con la que se interactuó (null si todavía ninguna).</summary>
        public ResultadoPuerta? UltimoResultadoPuerta { get; private set; }

        /// <summary>Lugar de la última puerta con la que se interactuó.</summary>
        public IdLugar UltimaPuerta { get; private set; }

        /// <summary>Avisa cada vez que se interactúa con una puerta, con su lugar y lo que respondió.</summary>
        public event Action<IdLugar, ResultadoPuerta> InteractuoConPuerta;

        public event Action<ModoJugador> ModoCambio;

        private void Awake()
        {
            personaje = GetComponent<CharacterController>();
            if (config == null || controles == null || camara == null)
            {
                Debug.LogError("[Mundo] CamaraLibre en '" + name + "' necesita config, controles y cámara.", this);
                enabled = false;
                return;
            }
            mover = controles.FindAction("Mover", true);
            mirar = controles.FindAction("Mirar", true);
            correr = controles.FindAction("Correr", true);
            alternarVuelo = controles.FindAction("AlternarVuelo", true);
            subir = controles.FindAction("Subir", true);
            bajar = controles.FindAction("Bajar", true);
            interactuar = controles.FindAction("Interactuar", true);
            oyente = camara.GetComponent<AudioListener>();
            yaw = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            if (mover == null)
            {
                return;
            }
            controles.Enable();
            Registro<IJugador>.Agregar(this);
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            Registro<IJugador>.Quitar(this);
            if (controles != null)
            {
                controles.Disable();
            }
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            if (montable != null)
            {
                // Dentro de un vehículo solo se lee la E: el vehículo lee su propia entrada y la raíz lo sigue en LateUpdate.
                if (interactuar.WasPressedThisFrame())
                {
                    Bajar();
                }
                return;
            }

            if (alternarVuelo.WasPressedThisFrame())
            {
                AlternarVuelo();
            }

            Mirar();
            if (interactuar.WasPressedThisFrame())
            {
                Interactuar();
            }
            if (montable != null)
            {
                return;
            }
            if (volando)
            {
                Volar(Time.deltaTime);
            }
            else
            {
                Caminar(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (montable != null)
            {
                transform.SetPositionAndRotation(montable.Asiento.position, montable.Asiento.rotation);
            }
        }

        private void Mirar()
        {
            Vector2 delta = mirar.ReadValue<Vector2>() * config.Sensibilidad;
            yaw += delta.x;
            inclinacion = CalculoCamaraLibre.LimitarInclinacion(inclinacion, -delta.y, config.LimiteInclinacion);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            camara.transform.localRotation = Quaternion.Euler(inclinacion, 0f, 0f);
        }

        private void Caminar(float dt)
        {
            float rapidez = CalculoCamaraLibre.Rapidez(false, correr.IsPressed(), config.VelocidadAPie, config.VelocidadCorriendo, config.VelocidadVolando);
            Vector3 horizontal = CalculoCamaraLibre.DireccionHorizontal(mover.ReadValue<Vector2>(), yaw) * rapidez;
            velocidadVertical = CalculoCamaraLibre.VelocidadVertical(velocidadVertical, personaje.isGrounded, config.Gravedad, dt, PegadoAlSuelo);
            personaje.Move((horizontal + Vector3.up * velocidadVertical) * dt);
        }

        private void Volar(float dt)
        {
            Vector2 entrada = mover.ReadValue<Vector2>();
            Vector3 direccion = camara.transform.rotation * new Vector3(entrada.x, 0f, entrada.y);
            float vertical = (subir.IsPressed() ? 1f : 0f) - (bajar.IsPressed() ? 1f : 0f);
            direccion += Vector3.up * vertical;
            if (direccion.sqrMagnitude > 1f)
            {
                direccion.Normalize();
            }
            float rapidez = CalculoCamaraLibre.Rapidez(true, false, config.VelocidadAPie, config.VelocidadCorriendo, config.VelocidadVolando);
            transform.position += direccion * (rapidez * dt);
        }

        // Lanza un rayo al frente; si da con una puerta (IPuerta), le pide abrir y escribe el resultado en la consola.
        private void Interactuar()
        {
            Transform ojos = camara.transform;
            IPuerta puerta = null;
            if (Physics.Raycast(ojos.position, ojos.forward, out RaycastHit golpe, config.DistanciaInteraccion, ~0, QueryTriggerInteraction.Ignore))
            {
                puerta = golpe.collider.GetComponentInParent<IPuerta>();
            }
            if (puerta == null)
            {
                IntentarSubir();
                return;
            }
            ResultadoPuerta resultado = puerta.IntentarAbrir();
            UltimaPuerta = puerta.Lugar;
            UltimoResultadoPuerta = resultado;
            Debug.Log("[Mundo] Puerta " + puerta.Lugar + ": " + resultado);
            InteractuoConPuerta?.Invoke(puerta.Lugar, resultado);
        }

        // Busca el montable que se puede subir más cercano al frente (por Registro<IMontable>, sin conocer su clase) y se sube.
        private void IntentarSubir()
        {
            IMontable mejor = null;
            float mejorDistancia = float.MaxValue;
            foreach (IMontable candidato in Registro<IMontable>.Todos)
            {
                if (!candidato.PuedeSubir || !CalculoCamaraLibre.EnRangoDeSubida(transform.position, camara.transform.forward,
                        candidato.PuntoSubida, config.DistanciaSubida, config.AnguloSubida))
                {
                    continue;
                }
                float distancia = (candidato.PuntoSubida - transform.position).sqrMagnitude;
                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejor = candidato;
                }
            }
            if (mejor == null || !mejor.IntentarSubir(this))
            {
                return;
            }
            montable = mejor;
            camara.enabled = false;
            if (oyente != null)
            {
                oyente.enabled = false;
            }
            personaje.enabled = false;
            Modo = mejor.Modo;
            ModoCambio?.Invoke(Modo);
        }

        private void Bajar()
        {
            if (!montable.IntentarBajar(out Pose salida))
            {
                Debug.Log("[Mundo] Frena para bajar");
                return;
            }
            montable = null;
            Colocar(salida);
            camara.enabled = true;
            if (oyente != null)
            {
                oyente.enabled = true;
            }
            Modo = ModoJugador.APie;
            ModoCambio?.Invoke(Modo);
        }

        private void AlternarVuelo()
        {
            volando = !volando;
            personaje.enabled = !volando;
            velocidadVertical = 0f;
        }

        /// <summary>Teletransporta sin atravesar nada: apaga el CharacterController, mueve y lo vuelve a prender (si no vuela).</summary>
        public void Colocar(Pose pose)
        {
            personaje.enabled = false;
            transform.SetPositionAndRotation(pose.position, Quaternion.Euler(0f, pose.rotation.eulerAngles.y, 0f));
            yaw = pose.rotation.eulerAngles.y;
            inclinacion = 0f;
            camara.transform.localRotation = Quaternion.identity;
            velocidadVertical = 0f;
            personaje.enabled = !volando;
        }
    }
}
