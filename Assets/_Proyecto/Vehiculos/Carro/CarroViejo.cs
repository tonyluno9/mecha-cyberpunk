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
    ///   (lado del conductor) o, si ahí choca una cápsula de persona de 0.4 m de radio y 1.8 m de alto, del lado del copiloto, por atrás
    ///   y, como último recurso (T-657), sobre el techo.
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
        private const string AccionLuces = "Luces";
        private const int RuedasTotales = 4;
        private const float UmbralReposo = 0.05f;
        private const float SegundosParaDormir = 1.5f;
        private const int RuedasMinimasParaGirar = 2;
        private const int MaxImpactosPorRayo = 8;
        private const float CompresionMaximaPermitida = 0.8f;
        private const int MaxImpactosSalida = 8;
        private const float AlturaRayoSalida = 1.5f;
        private const float LargoRayoSalida = 4f;
        private const float SeparacionSalidaAtras = 1f;
        private const float MargenTecho = 0.05f;
        private const int MaxCercanosSalida = 16;

        [Header("Datos")]
        [Tooltip("Valores de física y sensación del carro.")]
        [SerializeField] private ConfigCarro config;
        [Tooltip("T-532: nombre del modelo para el HUD (\"Sedán viejo\", \"Patrulla\"...). Vacío: \"Carro viejo\".")]
        [SerializeField] private string nombreModelo = "";
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
        private InputAction luces;
        private LucesCarro lucesCarro;
        private ParametrosManejo parametros;
        private bool listo;
        private float segundosEnReposo;
        private float velocidadEnTrafico;
        private Vector3 posicionAnterior;
        private bool salidaForzada;
        private readonly RaycastHit[] impactosSalida = new RaycastHit[MaxImpactosSalida];
        private readonly CalculoMarcha marcha = new CalculoMarcha();
        private float volante;
        private readonly Collider[] cercanosSalida = new Collider[MaxCercanosSalida];
        private Bounds limitesLocales;
        private bool limitesListos;

        /// <summary>Velocidad hacia adelante (m/s), negativa en reversa.</summary>
        public float Velocidad { get; private set; }

        /// <summary>Rapidez sin signo (m/s).</summary>
        public float Rapidez => cuerpo != null ? cuerpo.linearVelocity.magnitude : 0f;

        /// <summary>True mientras el carro anda por la calle como tráfico (T-511): cuerpo cinemático que mueve otro script; no maneja su física.</summary>
        public bool EstaEnTrafico => cuerpo != null && cuerpo.isKinematic;

        /// <summary>True si el cuerpo está dormido (carro estacionado en reposo: no gasta física).</summary>
        public bool EstaDormido => cuerpo != null && cuerpo.IsSleeping();

        /// <summary>Cuántas de las 4 ruedas tocan el piso en este momento.</summary>
        public int RuedasEnPiso { get; private set; }

        /// <summary>Si el carro está leyendo la entrada del conductor.</summary>
        public bool TieneControl { get; private set; }

        /// <summary>T-986: los valores de manejo del carro, para que un conductor IA los use.</summary>
        public ParametrosManejo ParametrosManejo => parametros;

        private Func<EntradaCarro> conductorIA;

        /// <summary>T-986: true si lo maneja un conductor IA (<see cref="ConducirConIA"/>): usa la misma física que con el jugador.</summary>
        public bool ConIA => conductorIA != null;

        /// <summary>T-986: alguien lo maneja, el jugador o la IA.</summary>
        private bool Manejado => TieneControl || conductorIA != null;

        /// <summary>Posición del volante (-1 a 1, suavizada): para que las ruedas delanteras doblen (T-742).</summary>
        public float Volante => volante;

        /// <summary>Última entrada leída (para las ruedas visuales).</summary>
        public EntradaCarro EntradaActual { get; private set; }

        /// <summary>Acciones del carro (para que otros sistemas, como la cámara, lean C y el ratón). Puede ser null.</summary>
        public InputActionMap MapaControles => controlesPropios != null ? controlesPropios.FindActionMap(NombreMapa) : null;

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            if (GetComponent<DanioCarro>() == null)
            {
                gameObject.AddComponent<DanioCarro>();
            }
            // T-660: las motos no llevan faros ni calaveras (se veían raras).
            if (GetComponent<LucesCarro>() == null && GetComponent<ManejoMoto>() == null)
            {
                gameObject.AddComponent<LucesCarro>();
            }
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
            luces = mapa.FindAction(AccionLuces);
            lucesCarro = GetComponent<LucesCarro>();
            listo = true;
        }

        // La tecla de luces (L) solo la lee quien maneja.
        private void Update()
        {
            if (listo && TieneControl && luces != null && lucesCarro != null && luces.WasPressedThisFrame())
            {
                lucesCarro.Alternar();
            }
        }

        private void OnEnable()
        {
            ActualizarMapa();
            Registro<IMontable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IMontable>.Quitar(this);
            DetenerDerrape();
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
        public string Nombre => string.IsNullOrEmpty(nombreModelo) ? "Carro viejo" : nombreModelo;
        public Vector3 PuntoSubida => puntoSubida.position;
        public bool Ocupado { get; private set; }
        public bool PuedeSubir => listo && !Ocupado && (!EstaEnTrafico || velocidadEnTrafico < config.RapidezMaximaParaBajar)
            && (conductorIA == null || Rapidez < config.RapidezMaximaParaBajar);
        public Transform Asiento => asiento;
        public Camera Camara => camaraCarro.Camara;
        public event Action<bool> OcupadoCambio;

        /// <summary>La cámara del carro (para saber si va en tercera persona).</summary>
        public CamaraCarro CamaraDelCarro => camaraCarro;

        public bool PuedeBajar => Ocupado && CalculoSalida.VaLentoParaBajar(Rapidez, config.RapidezMaximaParaBajar)
            && BuscarSalida(out _) != LadoSalida.Ninguno;

        public bool IntentarSubir(IJugador jugador)
        {
            if (!PuedeSubir)
            {
                return false;
            }
            SoltarIA(); // T-986: al subir el jugador manda él
            Ocupado = true;
            Sonidos.ReproducirEn("VEH_VIEJO_ARRANQUE", transform.position);
            camaraCarro.Encender(true);
            TomarControl(true);
            OcupadoCambio?.Invoke(true);
            return true;
        }

        public bool IntentarBajar(out Pose salida)
        {
            salida = Pose.identity;
            if (!Ocupado || !(salidaForzada || CalculoSalida.VaLentoParaBajar(Rapidez, config.RapidezMaximaParaBajar)))
            {
                return false;
            }
            LadoSalida lado = BuscarSalida(out Pose pose);
            if (lado == LadoSalida.Ninguno && !salidaForzada)
            {
                return false;
            }
            salida = pose;
            salidaForzada = false;
            Ocupado = false;
            DetenerDerrape();
            camaraCarro.Encender(false);
            TomarControl(false);
            OcupadoCambio?.Invoke(false);
            return true;
        }

        // Elige por dónde baja (T-657): conductor, copiloto, atrás y, al final, desde arriba. Devuelve el lado y su pose.
        private LadoSalida BuscarSalida(out Pose pose)
        {
            bool conductor = SalidaLibre(LadoSalida.Conductor, out Pose poseConductor);
            bool copiloto = SalidaLibre(LadoSalida.Copiloto, out Pose poseCopiloto);
            bool atras = SalidaLibre(LadoSalida.Atras, out Pose poseAtras);
            bool arriba = SalidaLibre(LadoSalida.Arriba, out Pose poseArriba);
            LadoSalida lado = CalculoSalida.Elegir(conductor, copiloto, atras, arriba);
            switch (lado)
            {
                case LadoSalida.Copiloto: pose = poseCopiloto; break;
                case LadoSalida.Atras: pose = poseAtras; break;
                case LadoSalida.Arriba: pose = poseArriba; break;
                default: pose = poseConductor; break;
            }
            return lado;
        }

        // Cajas de choque del carro en su espacio local (se miden una vez: el modelo no cambia).
        private Bounds LimitesLocales()
        {
            if (limitesListos)
            {
                return limitesLocales;
            }
            bool hay = false;
            foreach (BoxCollider c in GetComponentsInChildren<BoxCollider>(true))
            {
                if (c.isTrigger)
                {
                    continue;
                }
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = c.center + Vector3.Scale(c.size * 0.5f,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = transform.InverseTransformPoint(c.transform.TransformPoint(esquina));
                    if (!hay)
                    {
                        limitesLocales = new Bounds(local, Vector3.zero);
                        hay = true;
                    }
                    else
                    {
                        limitesLocales.Encapsulate(local);
                    }
                }
            }
            if (!hay)
            {
                limitesLocales = new Bounds(Vector3.up, new Vector3(2f, 2f, 4f));
            }
            limitesListos = true;
            return limitesLocales;
        }

        // Pose en el piso junto al carro (lado del conductor, espejo del copiloto o detrás) o sobre el techo, y si cabe una persona ahí.
        private bool SalidaLibre(LadoSalida lado, out Pose pose)
        {
            Quaternion mirada = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (lado == LadoSalida.Arriba)
            {
                return SalidaEnTecho(mirada, out pose);
            }
            Vector3 punto = puntoSubida.position;
            if (lado == LadoSalida.Copiloto)
            {
                Vector3 local = transform.InverseTransformPoint(punto);
                local.x = -local.x;
                punto = transform.TransformPoint(local);
            }
            else if (lado == LadoSalida.Atras)
            {
                Bounds caja = LimitesLocales();
                punto = transform.TransformPoint(new Vector3(caja.center.x, 0f, caja.min.z - SeparacionSalidaAtras));
            }
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

        // T-657, último recurso: de pie sobre el techo del carro (la cápsula de persona no debe chocar con nada que no sea el propio carro).
        private bool SalidaEnTecho(Quaternion mirada, out Pose pose)
        {
            Bounds caja = LimitesLocales();
            Vector3 punto = transform.TransformPoint(new Vector3(caja.center.x, caja.max.y + MargenTecho, caja.center.z));
            pose = new Pose(punto, mirada);
            float radio = config.RadioSalida;
            Vector3 abajo = punto + Vector3.up * (config.HolguraSalida + radio);
            Vector3 arriba = punto + Vector3.up * (config.AlturaSalida - radio);
            int cantidad = Physics.OverlapCapsuleNonAlloc(abajo, arriba, radio, cercanosSalida, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                if (!cercanosSalida[i].transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>La siguiente <see cref="IntentarBajar"/> ignora la rapidez y el lugar libre (el carro explota y hay que salir ya) (T-444).</summary>
        public void PermitirBajarYa()
        {
            salidaForzada = true;
        }

        /// <summary>Prende o apaga la lectura de la entrada. Sin control el carro se queda estacionado.</summary>
        public void TomarControl(bool tomar)
        {
            TieneControl = tomar;
            if (tomar && cuerpo != null && !cuerpo.isKinematic)
            {
                cuerpo.WakeUp();
                segundosEnReposo = 0f;
            }
            if (!tomar)
            {
                EntradaActual = default;
                marcha.Reiniciar();
                volante = 0f;
            }
            ActualizarMapa();
        }

        /// <summary>
        /// T-986: un conductor IA maneja el carro: cada paso de física pide su <see cref="EntradaCarro"/> a <paramref name="conductor"/> (con el volante
        /// suavizado igual que el del jugador) y no hace falta tomar el control del jugador ni habilitar sus controles.
        /// </summary>
        public void ConducirConIA(Func<EntradaCarro> conductor)
        {
            conductorIA = conductor;
            if (conductor != null && cuerpo != null && !cuerpo.isKinematic)
            {
                cuerpo.WakeUp();
                segundosEnReposo = 0f;
            }
        }

        /// <summary>T-986: el conductor IA deja el carro (queda estacionado, o lo toma el jugador).</summary>
        public void SoltarIA()
        {
            if (conductorIA == null)
            {
                return;
            }
            conductorIA = null;
            if (!TieneControl)
            {
                EntradaActual = default;
                marcha.Reiniciar();
                volante = 0f;
            }
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

        private const float TonoMinimoMotor = 0.8f;
        private const float TonoMaximoMotor = 2f;
        private const float MetrosPorSegundoPorTono = 20f;
        private const float AmplitudOscilacionMotor = 0.04f;
        private const float FrecuenciaOscilacionMotor = 0.6f;
        private const float RangoFaseMotor = 100f;
        private float faseMotor = -1f;
        private int bucleMotor = Sonidos.SinBucle;

        // Motor en marcha mínima mientras alguien maneja; el tono sube con la rapidez.
        private void ActualizarMotor()
        {
            bool suena = Ocupado || conductorIA != null;
            if (suena && bucleMotor == Sonidos.SinBucle)
            {
                bucleMotor = Sonidos.ReproducirBucle("VEH_VIEJO_MOTOR_RALENTI", transform);
            }
            else if (!suena && bucleMotor != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleMotor);
                bucleMotor = Sonidos.SinBucle;
            }
            if (bucleMotor != Sonidos.SinBucle)
            {
                if (faseMotor < 0f)
                {
                    faseMotor = UnityEngine.Random.value * RangoFaseMotor;
                }
                Sonidos.FijarTono(bucleMotor, ReglaMotorSonido.Tono(Rapidez, Time.time, faseMotor, TonoMinimoMotor, TonoMaximoMotor,
                    MetrosPorSegundoPorTono, AmplitudOscilacionMotor, FrecuenciaOscilacionMotor));
            }
        }

        private void FixedUpdate()
        {
            if (!listo)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            if (cuerpo.isKinematic)
            {
                // Tráfico: otro script lo mueve; solo se estima su rapidez (para saber si está detenido y se puede subir).
                velocidadEnTrafico = (transform.position - posicionAnterior).magnitude / dt;
                posicionAnterior = transform.position;
                return;
            }
            if (!Manejado && cuerpo.IsSleeping())
            {
                return;
            }
            Transform t = transform;
            Vector3 arriba = t.up;
            Vector3 adelante = t.forward;
            Vector3 derecha = t.right;
            Vector3 centroMasa = cuerpo.worldCenterOfMass;

            Velocidad = Vector3.Dot(cuerpo.linearVelocity, adelante);
            ActualizarMotor();
            EntradaActual = TieneControl ? LeerEntrada() : (conductorIA != null ? LeerConductorIA() : default);

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
            DormirSiEstaQuieto(dt);
        }

        private readonly Vector3[] puntosPiso = new Vector3[RuedasTotales];

        // T-511: un carro sin conductor que lleva un rato quieto sobre sus ruedas se duerme: dormido no cuesta física (hay decenas estacionados).
        private void DormirSiEstaQuieto(float dt)
        {
            bool quieto = !Manejado && !Ocupado && RuedasEnPiso >= RuedasMinimasParaGirar
                && cuerpo.linearVelocity.sqrMagnitude < UmbralReposo * UmbralReposo && cuerpo.angularVelocity.sqrMagnitude < UmbralReposo * UmbralReposo;
            segundosEnReposo = quieto ? segundosEnReposo + dt : 0f;
            if (segundosEnReposo >= SegundosParaDormir)
            {
                cuerpo.Sleep();
                segundosEnReposo = 0f;
            }
        }

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
                // T-986: solo cuentan como suyos los colliders de este carro; la patrulla cuelga del barrio y no debe ignorar el piso que comparte raíz con ella
                if (impacto.collider.attachedRigidbody == cuerpo || impacto.collider.transform.IsChildOf(transform) || impacto.distance >= mejor)
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

            ActualizarDerrape(vAdelante, vLateral, dt);
            if (Manejado)
            {
                EntradaCarro e = EntradaActual;
                float factorMarcha = marcha.Factor(parametros, e.Acelerar, e.Frenar, vAdelante, dt);
                fuerzaAdelante = CalculoManejo.FuerzaLongitudinal(parametros, e.Acelerar, e.Frenar, e.FrenoMano, vAdelante, dt, factorMarcha);
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

        /// <summary>T-977: el ID de las llantas que rechinan.</summary>
        public const string SonidoDerrape = "VEH_FRENO_DERRAPE";

        private int bucleDerrape = Sonidos.SinBucle;
        private bool rechina;
        private float desdeCambioDerrape = ReglaDerrapeSonido.SegundosDescanso;

        /// <summary>T-977: si las llantas rechinan ahora (para las pruebas).</summary>
        public bool Rechina => rechina;

        // T-977: solo el carro que maneja el jugador; el archivo dura 11.6 s, así que va en bucle y se corta al terminar el derrape.
        private void ActualizarDerrape(float vAdelante, float vLateral, float dt)
        {
            desdeCambioDerrape += dt;
            EntradaCarro e = EntradaActual;
            bool ahora = TieneControl
                && ReglaDerrapeSonido.Rechina(vAdelante, vLateral, e.Frenar, e.FrenoMano, rechina, desdeCambioDerrape);
            if (ahora == rechina)
            {
                return;
            }
            rechina = ahora;
            desdeCambioDerrape = 0f;
            if (ahora)
            {
                bucleDerrape = Sonidos.ReproducirBucle(SonidoDerrape, transform);
            }
            else
            {
                DetenerDerrape();
            }
        }

        private void DetenerDerrape()
        {
            rechina = false;
            if (bucleDerrape != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleDerrape);
                bucleDerrape = Sonidos.SinBucle;
            }
        }

        private void AplicarAntivuelco(Vector3 arriba)
        {
            Vector3 eje = Vector3.Cross(arriba, Vector3.up);
            cuerpo.AddTorque(eje * config.FuerzaAntivuelco);
        }

        // T-986: la entrada de la IA pasa por el mismo volante con velocidad limitada que el del jugador.
        private EntradaCarro LeerConductorIA()
        {
            EntradaCarro e = conductorIA();
            volante = CalculoDireccion.Suavizar(volante, Mathf.Clamp(e.Girar, -1f, 1f), Time.fixedDeltaTime,
                config.VelocidadVolante, config.VelocidadCentradoVolante);
            return new EntradaCarro(Mathf.Clamp01(e.Acelerar), Mathf.Clamp01(e.Frenar), volante, Mathf.Clamp01(e.FrenoMano));
        }

        private EntradaCarro LeerEntrada()
        {
            // T-658: el volante sigue a la tecla a una velocidad limitada (sin saltos de un lado al otro).
            volante = CalculoDireccion.Suavizar(volante, Mathf.Clamp(girar.ReadValue<float>(), -1f, 1f), Time.fixedDeltaTime,
                config.VelocidadVolante, config.VelocidadCentradoVolante);
            return new EntradaCarro(
                Mathf.Clamp01(acelerar.ReadValue<float>()),
                Mathf.Clamp01(frenar.ReadValue<float>()),
                volante,
                Mathf.Clamp01(frenoMano.ReadValue<float>()));
        }
    }
}
