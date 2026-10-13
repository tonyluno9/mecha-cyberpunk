using System;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Carro viejo con física arcade: cuerpo rígido y suspensión por rayos (un rayo hacia abajo por rueda, sin WheelCollider).
    /// Solo lee la entrada cuando tiene control (<see cref="TomarControl"/>); sin control se queda estacionado.
    ///
    /// Es un <see cref="IMontable"/> (T-075), para el jugador de Tony (T-061) y la cámara libre de Alan:
    /// - Teclas del conductor (ControlesCarro): W o flecha arriba acelera, S o flecha abajo frena y da reversa,
    ///   A y D o flechas izquierda y derecha giran, Espacio es el freno de mano, C alterna primera y tercera persona,
    ///   el ratón mira en primera persona. La E es del jugador: el carro no la lee.
    /// - Subir: <c>IntentarSubir</c> funciona si no está ocupado. El jugador decide la distancia; el contrato sugiere
    ///   2.5 m o menos de <see cref="PuntoSubida"/> y 60° o menos de hacia dónde mira.
    /// - Bajar: <see cref="PuedeBajar"/> es true solo si está ocupado, su rapidez es menor que 0.5 m/s
    ///   (<see cref="ConfigCarro.RapidezMaximaParaBajar"/>) y hay una salida libre: junto a <see cref="PuntoSubida"/>
    ///   (lado del conductor) o, si ahí choca una cápsula de persona de 0.4 m de radio y 1.8 m de alto, del lado del copiloto.
    ///   Con el carro en marcha <c>IntentarBajar</c> devuelve false y no avisa nada.
    /// - Al bajar entrega una pose en el piso mirando como el carro.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarroViejo : MonoBehaviour, IMontable
    {
        private const string NombreMapa = "Carro";
        private const string AccionAcelerar = "Acelerar";
        private const string AccionFrenar = "Frenar";
        private const string AccionGirar = "Girar";
        private const string AccionFrenoMano = "FrenoMano";
        private const int RuedasTotales = 4;
        private const int RuedasMinimasParaGirar = 2;
        private const int MaxImpactosPorRayo = 8;
        private const float CompresionMaximaPermitida = 0.8f;
        private const int MaxImpactosSalida = 8;
        private const float AlturaRayoSalida = 1.5f;
        private const float LargoRayoSalida = 4f;

        [Header("Datos")]
        [Tooltip("Valores de física y sensación del carro.")]
        [SerializeField] private ConfigCarro config;
        [Tooltip("Controles del carro (ControlesCarro.inputactions).")]
        [SerializeField] private InputActionAsset controles;

        [Header("Montar")]
        [Tooltip("Asiento del conductor (lado izquierdo): a donde se pega el jugador.")]
        [SerializeField] private Transform asiento;
        [Tooltip("Punto en el piso, 1 m afuera de la puerta del conductor.")]
        [SerializeField] private Transform puntoSubida;
        [Tooltip("Cámara del carro, con su AudioListener; los dos apagados al empezar.")]
        [SerializeField] private CamaraCarro camaraCarro;

        [Header("Ruedas")]
        [Tooltip("Rueda delantera izquierda: punto en el eje, con el carro en reposo.")]
        [SerializeField] private Transform ruedaDelanteraIzquierda;
        [Tooltip("Rueda delantera derecha.")]
        [SerializeField] private Transform ruedaDelanteraDerecha;
        [Tooltip("Rueda trasera izquierda.")]
        [SerializeField] private Transform ruedaTraseraIzquierda;
        [Tooltip("Rueda trasera derecha.")]
        [SerializeField] private Transform ruedaTraseraDerecha;

        private Rigidbody cuerpo;
        private Transform[] ruedas;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactosPorRayo];
        private InputActionAsset controlesPropios;
        private InputAction acelerar;
        private InputAction frenar;
        private InputAction girar;
        private InputAction frenoMano;
        private ParametrosManejo parametros;
        private bool listo;
        private readonly RaycastHit[] impactosSalida = new RaycastHit[MaxImpactosSalida];

        /// <summary>Velocidad hacia adelante (m/s), negativa en reversa.</summary>
        public float Velocidad { get; private set; }

        /// <summary>Rapidez sin signo (m/s).</summary>
        public float Rapidez => cuerpo != null ? cuerpo.linearVelocity.magnitude : 0f;

        /// <summary>Cuántas de las 4 ruedas tocan el piso en este momento.</summary>
        public int RuedasEnPiso { get; private set; }

        /// <summary>Si el carro está leyendo la entrada del conductor.</summary>
        public bool TieneControl { get; private set; }

        /// <summary>Última entrada leída (para las ruedas visuales).</summary>
        public EntradaCarro EntradaActual { get; private set; }

        /// <summary>Acciones del carro (para que otros sistemas, como la cámara, lean C y el ratón). Puede ser null.</summary>
        public InputActionMap MapaControles => controlesPropios != null ? controlesPropios.FindActionMap(NombreMapa) : null;

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            if (config == null || controles == null || asiento == null || puntoSubida == null || camaraCarro == null
                || ruedaDelanteraIzquierda == null || ruedaDelanteraDerecha == null
                || ruedaTraseraIzquierda == null || ruedaTraseraDerecha == null)
            {
                Debug.LogError($"[Vehiculos] {name}: faltan la config, los controles, el asiento, el punto de subida, la cámara o alguna rueda.", this);
                enabled = false;
                return;
            }

            ruedas = new[] { ruedaDelanteraIzquierda, ruedaDelanteraDerecha, ruedaTraseraIzquierda, ruedaTraseraDerecha };
            parametros = config.Parametros;
            cuerpo.mass = config.Masa;
            cuerpo.centerOfMass = new Vector3(0f, config.AlturaCentroMasa, 0f);

            controlesPropios = Instantiate(controles);
            InputActionMap mapa = controlesPropios.FindActionMap(NombreMapa);
            if (mapa == null)
            {
                Debug.LogError($"[Vehiculos] {name}: los controles no tienen el mapa '{NombreMapa}'.", this);
                enabled = false;
                return;
            }
            acelerar = mapa.FindAction(AccionAcelerar);
            frenar = mapa.FindAction(AccionFrenar);
            girar = mapa.FindAction(AccionGirar);
            frenoMano = mapa.FindAction(AccionFrenoMano);
            listo = true;
        }

        private void OnEnable()
        {
            ActualizarMapa();
            Registro<IMontable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IMontable>.Quitar(this);
            controlesPropios?.Disable();
        }

        private void OnDestroy()
        {
            if (controlesPropios != null)
            {
                Destroy(controlesPropios);
            }
        }

        public ModoJugador Modo => ModoJugador.Carro;
        public string Nombre => "Carro viejo";
        public Vector3 PuntoSubida => puntoSubida.position;
        public bool Ocupado { get; private set; }
        public bool PuedeSubir => listo && !Ocupado;
        public Transform Asiento => asiento;
        public Camera Camara => camaraCarro.Camara;
        public event Action<bool> OcupadoCambio;

        /// <summary>La cámara del carro (para saber si va en tercera persona).</summary>
        public CamaraCarro CamaraDelCarro => camaraCarro;

        public bool PuedeBajar => Ocupado && CalculoSalida.VaLentoParaBajar(Rapidez, config.RapidezMaximaParaBajar)
            && CalculoSalida.Elegir(SalidaLibre(LadoSalida.Conductor, out _), SalidaLibre(LadoSalida.Copiloto, out _)) != LadoSalida.Ninguno;

        public bool IntentarSubir(IJugador jugador)
        {
            if (!PuedeSubir)
            {
                return false;
            }
            Ocupado = true;
            camaraCarro.Encender(true);
            TomarControl(true);
            OcupadoCambio?.Invoke(true);
            return true;
        }

        public bool IntentarBajar(out Pose salida)
        {
            salida = Pose.identity;
            if (!Ocupado || !CalculoSalida.VaLentoParaBajar(Rapidez, config.RapidezMaximaParaBajar))
            {
                return false;
            }
            LadoSalida lado = CalculoSalida.Elegir(SalidaLibre(LadoSalida.Conductor, out Pose conductor),
                SalidaLibre(LadoSalida.Copiloto, out Pose copiloto));
            if (lado == LadoSalida.Ninguno)
            {
                return false;
            }
            salida = lado == LadoSalida.Conductor ? conductor : copiloto;
            Ocupado = false;
            camaraCarro.Encender(false);
            TomarControl(false);
            OcupadoCambio?.Invoke(false);
            return true;
        }

        // Pose en el piso junto al carro (lado del conductor o espejo del lado del copiloto) y si cabe una persona ahí.
        private bool SalidaLibre(LadoSalida lado, out Pose pose)
        {
            Vector3 punto = puntoSubida.position;
            if (lado == LadoSalida.Copiloto)
            {
                Vector3 local = transform.InverseTransformPoint(punto);
                local.x = -local.x;
                punto = transform.TransformPoint(local);
            }
            Quaternion mirada = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            pose = new Pose(punto, mirada);

            Vector3 origen = punto + Vector3.up * AlturaRayoSalida;
            int cantidad = Physics.RaycastNonAlloc(origen, Vector3.down, impactosSalida, LargoRayoSalida,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            bool hayPiso = false;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactosSalida[i];
                if (impacto.collider.transform.IsChildOf(transform) || impacto.distance >= mejor)
                {
                    continue;
                }
                mejor = impacto.distance;
                punto = impacto.point;
                hayPiso = true;
            }
            if (!hayPiso)
            {
                return false;
            }
            pose = new Pose(punto, mirada);

            float radio = config.RadioSalida;
            Vector3 abajo = punto + Vector3.up * (config.HolguraSalida + radio);
            Vector3 arriba = punto + Vector3.up * (config.AlturaSalida - radio);
            return !Physics.CheckCapsule(abajo, arriba, radio, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Prende o apaga la lectura de la entrada. Sin control el carro se queda estacionado.</summary>
        public void TomarControl(bool tomar)
        {
            TieneControl = tomar;
            if (!tomar)
            {
                EntradaActual = default;
            }
            ActualizarMapa();
        }

        private void ActualizarMapa()
        {
            if (!listo)
            {
                return;
            }
            if (TieneControl && isActiveAndEnabled)
            {
                controlesPropios.Enable();
            }
            else
            {
                controlesPropios.Disable();
            }
        }

        private void FixedUpdate()
        {
            if (!listo)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            Transform t = transform;
            Vector3 arriba = t.up;
            Vector3 adelante = t.forward;
            Vector3 derecha = t.right;
            Vector3 centroMasa = cuerpo.worldCenterOfMass;

            Velocidad = Vector3.Dot(cuerpo.linearVelocity, adelante);
            EntradaActual = TieneControl ? LeerEntrada() : default;

            // Suspensión: un rayo por rueda. El motor y el agarre se reparten entre las ruedas que tocan piso.
            Vector3[] puntos = puntosPiso;
            int enPiso = 0;
            for (int i = 0; i < RuedasTotales; i++)
            {
                if (AplicarSuspension(ruedas[i], arriba, out Vector3 punto))
                {
                    puntos[enPiso] = punto;
                    enPiso++;
                }
            }
            RuedasEnPiso = enPiso;

            if (enPiso > 0)
            {
                AplicarManejo(puntos, enPiso, centroMasa, adelante, derecha, arriba, dt);
            }
            AplicarAntivuelco(arriba);
        }

        private readonly Vector3[] puntosPiso = new Vector3[RuedasTotales];

        private bool AplicarSuspension(Transform rueda, Vector3 arriba, out Vector3 puntoContacto)
        {
            Vector3 anclaje = rueda.position + arriba * config.LargoReposo;
            float largoRayo = config.LargoMaximo + config.RadioRueda;
            puntoContacto = anclaje;

            int cantidad = Physics.RaycastNonAlloc(anclaje, -arriba, impactos, largoRayo, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactos[i];
                if (impacto.collider.transform.root == transform.root || impacto.distance >= mejor)
                {
                    continue;
                }
                mejor = impacto.distance;
                puntoContacto = impacto.point;
                hay = true;
            }
            if (!hay)
            {
                return false;
            }

            float compresion = Mathf.Min(config.LargoMaximo - (mejor - config.RadioRueda), CompresionMaximaPermitida);
            float velocidadCompresion = -Vector3.Dot(cuerpo.GetPointVelocity(puntoContacto), arriba);
            float fuerza = CalculoSuspension.Fuerza(compresion, velocidadCompresion, config.Rigidez, config.Amortiguamiento);
            cuerpo.AddForceAtPosition(arriba * fuerza, puntoContacto);
            return true;
        }

        private void AplicarManejo(Vector3[] puntos, int enPiso, Vector3 centroMasa, Vector3 adelante, Vector3 derecha,
            Vector3 arriba, float dt)
        {
            Vector3 velocidad = cuerpo.linearVelocity;
            float vAdelante = Vector3.Dot(velocidad, adelante);
            float vLateral = Vector3.Dot(velocidad, derecha);
            float fuerzaAdelante;
            float fuerzaLateral;

            if (TieneControl)
            {
                EntradaCarro e = EntradaActual;
                fuerzaAdelante = CalculoManejo.FuerzaLongitudinal(parametros, e.Acelerar, e.Frenar, e.FrenoMano, vAdelante, dt);
                fuerzaLateral = CalculoManejo.FuerzaLateral(parametros, vLateral, e.FrenoMano, dt);
                if (enPiso >= RuedasMinimasParaGirar)
                {
                    float giroDeseado = CalculoManejo.VelocidadGiro(parametros, vAdelante, e.Girar);
                    float giroActual = Vector3.Dot(cuerpo.angularVelocity, arriba);
                    float cambio = (giroDeseado - giroActual) * Mathf.Min(1f, config.RespuestaGiro * dt);
                    cuerpo.AddTorque(arriba * cambio, ForceMode.VelocityChange);
                }
            }
            else
            {
                fuerzaAdelante = CalculoManejo.FuerzaEstacionamiento(parametros, vAdelante, dt);
                fuerzaLateral = CalculoManejo.FuerzaEstacionamiento(parametros, vLateral, dt);
                float giroActual = Vector3.Dot(cuerpo.angularVelocity, arriba);
                cuerpo.AddTorque(-arriba * giroActual * Mathf.Min(1f, config.RespuestaGiro * dt), ForceMode.VelocityChange);
            }

            // Cada rueda en el piso aporta una cuarta parte; se aplica a la altura del centro de masa para no cabecear ni ladear.
            Vector3 fuerzaPorRueda = (adelante * fuerzaAdelante + derecha * fuerzaLateral) / RuedasTotales;
            for (int i = 0; i < enPiso; i++)
            {
                Vector3 punto = puntos[i];
                punto += arriba * Vector3.Dot(centroMasa - punto, arriba);
                cuerpo.AddForceAtPosition(fuerzaPorRueda, punto);
            }
        }

        private void AplicarAntivuelco(Vector3 arriba)
        {
            Vector3 eje = Vector3.Cross(arriba, Vector3.up);
            cuerpo.AddTorque(eje * config.FuerzaAntivuelco);
        }

        private EntradaCarro LeerEntrada()
        {
            return new EntradaCarro(
                Mathf.Clamp01(acelerar.ReadValue<float>()),
                Mathf.Clamp01(frenar.ReadValue<float>()),
                Mathf.Clamp(girar.ReadValue<float>(), -1f, 1f),
                Mathf.Clamp01(frenoMano.ReadValue<float>()));
        }
    }
}
