using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Cámara del carro. Primera persona por defecto (ojos del conductor, el ratón mira); la tecla C alterna con tercera
    /// persona (detrás y arriba, con suavidad y sin atravesar muros). Solo trabaja mientras su cámara está prendida
    /// (la prende <see cref="CarroViejo"/> al subir). La elección de vista se recuerda entre una subida y otra.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraCarro : MonoBehaviour
    {
        private const int MaxImpactos = 8;

        [SerializeField] private ConfigCamaraCarro config;

        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactos];
        private CarroViejo carro;
        private Camera camara;
        private AudioListener oyente;
        private Rigidbody cuerpo;
        private InputAction cambiar;
        private InputAction mirar;
        private float yaw;
        private float inclinacion;
        private float segundosSinMover;
        private Vector3 posicionTercera;

        /// <summary>True si la vista actual es tercera persona.</summary>
        public bool EsTerceraPersona { get; private set; }

        /// <summary>La cámara de este objeto.</summary>
        public Camera Camara
        {
            get
            {
                if (camara == null)
                {
                    camara = GetComponent<Camera>();
                }
                return camara;
            }
        }

        /// <summary>Mirada actual a los lados (°) en primera persona; 0 es al frente.</summary>
        public float Yaw => yaw;

        /// <summary>Mirada actual arriba o abajo (°) en primera persona; positivo es hacia abajo.</summary>
        public float Inclinacion => inclinacion;

        private void Awake()
        {
            camara = GetComponent<Camera>();
            oyente = GetComponent<AudioListener>();
            carro = GetComponentInParent<CarroViejo>();
            cuerpo = carro != null ? carro.GetComponent<Rigidbody>() : null;
            if (config == null || carro == null)
            {
                Debug.LogError($"[Vehiculos] {name}: falta la config o el CarroViejo padre.", this);
                enabled = false;
            }
        }

        /// <summary>Prende o apaga la cámara y su AudioListener. Al prender, la vista empieza al frente y bien colocada.</summary>
        public void Encender(bool encender)
        {
            if (encender)
            {
                yaw = 0f;
                inclinacion = 0f;
                segundosSinMover = 0f;
                Camara.enabled = true;
                if (oyente != null)
                {
                    oyente.enabled = true;
                }
                Colocar(true);
            }
            else
            {
                Camara.enabled = false;
                if (oyente != null)
                {
                    oyente.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (!Camara.enabled)
            {
                return;
            }
            if (cambiar == null)
            {
                InputActionMap mapa = carro.MapaControles;
                if (mapa == null)
                {
                    return;
                }
                cambiar = mapa.FindAction("CambiarCamara");
                mirar = mapa.FindAction("Mirar");
            }
            if (cambiar != null && cambiar.WasPressedThisFrame())
            {
                EsTerceraPersona = !EsTerceraPersona;
                Colocar(true);
            }
            if (!EsTerceraPersona && mirar != null)
            {
                ActualizarMirada(mirar.ReadValue<Vector2>(), Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (Camara.enabled)
            {
                Colocar(false);
            }
        }

        private void ActualizarMirada(Vector2 delta, float dt)
        {
            if (delta.sqrMagnitude > 0f)
            {
                segundosSinMover = 0f;
                yaw = CalculoCamaraCarro.LimitarMirada(yaw, delta.x * config.Sensibilidad, config.LimiteLados);
                inclinacion = CalculoCamaraCarro.LimitarMirada(inclinacion, -delta.y * config.Sensibilidad, config.LimiteVertical);
            }
            else
            {
                segundosSinMover += dt;
                yaw = CalculoCamaraCarro.Regreso(yaw, segundosSinMover, config.EsperaRegreso, config.VelocidadRegreso, dt);
                inclinacion = CalculoCamaraCarro.Regreso(inclinacion, segundosSinMover, config.EsperaRegreso, config.VelocidadRegreso, dt);
            }
        }

        private void Colocar(bool deGolpe)
        {
            if (EsTerceraPersona)
            {
                ColocarTercera(deGolpe);
            }
            else
            {
                Transform asiento = carro.Asiento;
                Transform t = carro.transform;
                transform.SetPositionAndRotation(asiento.position + t.up * config.AlturaOjos,
                    t.rotation * Quaternion.Euler(inclinacion, yaw, 0f));
            }
        }

        private void ColocarTercera(bool deGolpe)
        {
            Transform t = carro.transform;
            Vector3 foco = t.position + Vector3.up * config.AlturaFoco;
            Vector3 ideal = CalculoCamaraCarro.PosicionTercera(foco, t.forward, config.DistanciaAtras, config.AlturaTercera);
            float paso = deGolpe ? 1f : 1f - Mathf.Exp(-config.Suavidad * Time.deltaTime);
            posicionTercera = deGolpe ? ideal : Vector3.Lerp(posicionTercera, ideal, paso);

            // El muro manda: el rayo del foco a la posición suavizada no puede chocar con nada que no sea el carro.
            Vector3 hacia = posicionTercera - foco;
            float distancia = hacia.magnitude;
            Vector3 direccion = distancia > 0.0001f ? hacia / distancia : Vector3.back;
            float distanciaMuro = float.MaxValue;
            int cantidad = Physics.RaycastNonAlloc(foco, direccion, impactos, distancia, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactos[i];
                if (EsDelCarro(impacto.collider))
                {
                    continue;
                }
                distanciaMuro = Mathf.Min(distanciaMuro, impacto.distance);
            }
            float libre = CalculoCamaraCarro.DistanciaLibre(distancia, distanciaMuro, config.MargenMuro, config.DistanciaMinima);
            transform.position = foco + direccion * libre;
            transform.rotation = Quaternion.LookRotation(foco - transform.position, Vector3.up);
        }

        private bool EsDelCarro(Collider colisionador)
        {
            return colisionador.attachedRigidbody == cuerpo || colisionador.transform.IsChildOf(carro.transform);
        }
    }
}
