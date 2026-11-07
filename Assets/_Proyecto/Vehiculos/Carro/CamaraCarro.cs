using MechaCyberpunk.Core;
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
        // T-584: con un rayo fino la cámara asomaba la pared por los lados; con esta esfera (m) el plano cercano no atraviesa muros.
        private const float RadioCamara = 0.3f;

        [SerializeField] private ConfigCamaraCarro config;
        [Tooltip("T-511: altura (m) del punto que mira la cámara en tercera persona; 0 usa la de la config. Cada modelo pone la suya según su altura.")]
        [Min(0f)] [SerializeField] private float alturaFocoPropia;
        [Tooltip("T-511: distancia (m) detrás del carro en tercera persona; 0 usa la de la config. Se ajusta al largo del modelo.")]
        [Min(0f)] [SerializeField] private float distanciaAtrasPropia;

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
        private float yawOrbita;
        private float inclinacionOrbita;
        private float rumbo;
        private float velocidadRumbo;

        /// <summary>True si la vista actual es tercera persona.</summary>
        public bool EsTerceraPersona { get; private set; } = true;

        /// <summary>Altura del foco de la tercera persona (la propia del modelo o la de la config).</summary>
        public float AlturaFocoActual => alturaFocoPropia > 0f ? alturaFocoPropia : config.AlturaFoco;

        /// <summary>Distancia detrás del carro en tercera persona (la propia del modelo o la de la config).</summary>
        public float DistanciaAtrasActual => distanciaAtrasPropia > 0f ? distanciaAtrasPropia : config.DistanciaAtras;

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
                yawOrbita = 0f;
                inclinacionOrbita = config.InclinacionReposo;
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
            // T-410: en pausa no se lee la entrada ni gira la cámara.
            if (!Camara.enabled || Pausa.Activa)
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
            if (mirar != null)
            {
                Vector2 delta = AjustarPorOpciones(mirar.ReadValue<Vector2>());
                if (EsTerceraPersona)
                {
                    ActualizarOrbita(delta, Time.deltaTime);
                }
                else
                {
                    ActualizarMirada(delta, Time.deltaTime);
                }
            }
        }

        // T-411: la sensibilidad de las opciones multiplica a la del carro y pueden invertir Y.
        private static Vector2 AjustarPorOpciones(Vector2 delta)
        {
            IOpciones opciones = Registro<IOpciones>.Actual;
            if (opciones == null)
            {
                return delta;
            }
            delta *= opciones.Sensibilidad;
            if (opciones.InvertirY)
            {
                delta.y = -delta.y;
            }
            return delta;
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

        // Tercera persona (T-401): el ratón orbita alrededor del carro; sin moverlo, vuelve detrás tras esperaRegreso.
        private void ActualizarOrbita(Vector2 delta, float dt)
        {
            if (delta.sqrMagnitude > 0f)
            {
                segundosSinMover = 0f;
                yawOrbita = Mathf.Repeat(yawOrbita + delta.x * config.Sensibilidad + 180f, 360f) - 180f;
                inclinacionOrbita = Mathf.Clamp(inclinacionOrbita - delta.y * config.Sensibilidad,
                    config.InclinacionMinOrbita, config.InclinacionMaxOrbita);
            }
            else
            {
                segundosSinMover += dt;
                yawOrbita = CalculoCamaraCarro.Regreso(yawOrbita, segundosSinMover, config.EsperaRegreso, config.VelocidadRegreso, dt);
                if (segundosSinMover >= config.EsperaRegreso)
                {
                    inclinacionOrbita = Mathf.MoveTowards(inclinacionOrbita, config.InclinacionReposo, config.VelocidadRegreso * dt);
                }
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
            Vector3 foco = t.position + Vector3.up * AlturaFocoActual;
            // T-658: el rumbo con el que se coloca la cámara sigue al del carro con un pequeño retraso (no gira de golpe al dar reversa o maniobrar).
            float rumboCarro = t.eulerAngles.y;
            rumbo = deGolpe ? rumboCarro : Mathf.SmoothDampAngle(rumbo, rumboCarro, ref velocidadRumbo, config.TiempoRumbo, Mathf.Infinity, Time.deltaTime);
            if (deGolpe)
            {
                velocidadRumbo = 0f;
            }
            Vector3 frenteCamara = Quaternion.Euler(0f, rumbo, 0f) * Vector3.forward;
            float kmh = cuerpo != null ? cuerpo.linearVelocity.magnitude * 3.6f : 0f;
            float distanciaOrbita = CalculoCamaraCarro.DistanciaPorRapidez(DistanciaAtrasActual, config.DistanciaExtraVelocidad, kmh, config.KmhDistanciaExtra);
            Vector3 ideal = CalculoCamaraCarro.PosicionOrbita(foco, frenteCamara, distanciaOrbita, yawOrbita, inclinacionOrbita);
            float paso = deGolpe ? 1f : 1f - Mathf.Exp(-config.Suavidad * Time.deltaTime);
            posicionTercera = deGolpe ? ideal : Vector3.Lerp(posicionTercera, ideal, paso);

            // El muro manda: la esfera del foco a la posición suavizada no puede chocar con nada que no sea el carro.
            Vector3 hacia = posicionTercera - foco;
            float distancia = hacia.magnitude;
            Vector3 direccion = distancia > 0.0001f ? hacia / distancia : Vector3.back;
            float distanciaMuro = float.MaxValue;
            int cantidad = Physics.SphereCastNonAlloc(foco, RadioCamara, direccion, impactos, distancia, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactos[i];
                if (impacto.distance <= 0f || EsDelCarro(impacto.collider))
                {
                    continue;
                }
                distanciaMuro = Mathf.Min(distanciaMuro, impacto.distance);
            }
            float libre = CalculoCamaraCarro.DistanciaLibre(distancia, distanciaMuro, config.MargenMuro, config.DistanciaMinima);
            transform.position = foco + direccion * libre;
            transform.rotation = Quaternion.LookRotation(foco + Vector3.up * config.AlturaMira - transform.position, Vector3.up);
        }

        private bool EsDelCarro(Collider colisionador)
        {
            return colisionador.attachedRigidbody == cuerpo || colisionador.transform.IsChildOf(carro.transform);
        }
    }
}
