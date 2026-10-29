using System.Collections.Generic;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// El rival greybox: busca a su objetivo, camina hacia él con inercia, avisa cada golpe con el brazo echado atrás y
    /// en naranja, y pega por el contrato de Core. Va en la lista <c>controlados</c> de su <c>PeleadorMecha</c>: sin
    /// control no se mueve ni ataca. Sin NavMesh (D-015): camina en línea recta.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class RivalBasico : MonoBehaviour, ILecturaRival
    {
        private const int MaxImpactosPorGolpe = 16;
        private const float DistanciaMinimaParaGirar = 0.01f;
        private const float VelocidadVerticalEnPiso = -2f;
        private const float MitadDeLaDistancia = 0.5f;
        private const float FactorFrenado = 2f;
        // Tolerancia al medir si ya llegó al anillo de espera (m).
        private const float MargenAnillo = 0.25f;
        private const float ProbabilidadRodeoInverso = 0.5f;
        private const float VelocidadMinimaAvance = 0.2f;
        private const float TiempoParaRodearEstorbo = 0.5f;
        private const float TinteAviso = -1f;

        private static readonly List<RivalBasico> Grupo = new List<RivalBasico>();
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColorEmision = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Distancias y esperas del rival (DatosRival_Prueba). Su mecha da velocidad, daño y tiempos.")]
        [SerializeField] private DatosRival datos;
        [Tooltip("Colores y ángulo del aviso del golpe.")]
        [SerializeField] private ConfigPresentacionRival config;
        [Tooltip("A quién persigue y golpea. Si está vacío, busca al peleador del lado contrario al arrancar.")]
        [SerializeField] private GameObject objetivo;
        [Tooltip("Pivote del hombro del brazo que golpea. Se echa atrás al preparar el golpe.")]
        [SerializeField] private Transform brazo;
        [Tooltip("Piezas del brazo que se pintan con el color de aviso.")]
        [SerializeField] private Renderer[] piezasBrazo;
        [Tooltip("Si es falso, el rival arranca sin control hasta que alguien llame a ActivarControl(true).")]
        [SerializeField] private bool controlAlArrancar;
        [Tooltip("Radio de la esfera del golpe (m). El alcance llega hasta su borde.")]
        [Min(0.01f)] [SerializeField] private float radioBusqueda = 0.25f;
        [Tooltip("Pandilla: solo uno por lado prepara un golpe a la vez; los demás esperan rodeando. Falso: el rival de arena.")]
        [SerializeField] private bool usarTurnos;
        [Tooltip("Sin turno, espera a su distancia de ataque más esto (m).")]
        [Min(0f)] [SerializeField] private float margenEspera = 2f;
        [Tooltip("Velocidad con la que rodea al objetivo mientras espera su turno (m/s).")]
        [Min(0f)] [SerializeField] private float velocidadRodeo = 1.2f;
        [Tooltip("Cada cuánto busca al objetivo si no tiene (s).")]
        [Min(0.05f)] [SerializeField] private float intervaloBusqueda = 0.5f;
        [Tooltip("Pide el turno cuando le faltan esto o menos para poder golpear (s): así se turnan en vez de acaparar.")]
        [Min(0f)] [SerializeField] private float anticipoTurno = 0.6f;

        private CharacterController controlador;
        private float esperaBusqueda;
        private float sentidoRodeo = 1f;
        private float tiempoAtorado;
        private bool enGrupo;
        private IPeleador propio;
        private IPeleador peleadorObjetivo;
        private CerebroRival cerebro;
        private CicloGolpe ciclo;
        private MaterialPropertyBlock bloque;
        private Color[] coloresReposo;
        private Quaternion giroReposoBrazo;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactosPorGolpe];
        private float velocidad;
        private float velocidadVertical;
        private bool avisoSinObjetivo;
        // Qué tinte tiene el brazo: -1 aviso del golpe, 0 reposo, de 0 a 1 la guardia subiendo (T-201).
        private float tinteBrazo;
        private GuardiaMecha guardia;
        private bool inicioHecho;
        // Alguien (el director de pelea) ya le quitó el control antes de su Start: no se lo vuelve a quitar.
        private bool controlDecidido;
        private HabilidadRival habilidad;
        private float distanciaLectura = -1f;
        private bool objetivoKOLectura;

        /// <summary>El rival está en la preparación del golpe: brazo atrás y en color de aviso.</summary>
        public bool EnAviso => ciclo != null && ciclo.Fase == FaseGolpe.Preparando;
        public FaseGolpe Fase => ciclo != null ? ciclo.Fase : FaseGolpe.Listo;
        /// <summary>Velocidad de avance actual (m/s).</summary>
        public float Velocidad => velocidad;
        public GameObject Objetivo => objetivo;
        public Transform Brazo => brazo;
        public Renderer[] PiezasBrazo => piezasBrazo;
        public DatosRival Datos => datos;
        /// <summary>Multiplica el daño de sus golpes (1 = normal). Lo cambia PiedadRival.</summary>
        public float MultiplicadorDanio { get; set; } = 1f;
        /// <summary>Multiplica la espera entre golpes (1 = normal). Lo cambia PiedadRival.</summary>
        public float MultiplicadorEspera { get; set; } = 1f;
        /// <summary>Multiplica su velocidad y aceleración (1 = normal). Lo cambia PiedadRival.</summary>
        public float MultiplicadorVelocidad { get; set; } = 1f;
        /// <summary>Si existe, recibe el daño del golpe y devuelve el que se pide de verdad. Lo pone PiedadRival.</summary>
        public System.Func<float, float> LimitarDanio { get; set; }
        /// <summary>Se dispara en cada impacto, haya dado a alguien o no.</summary>
        public event System.Action Impacto;
        /// <summary>
        /// Si es true se queda quieto (sin caminar, rodear ni empezar golpes) pero sigue mirando al objetivo. Lo pone HabilidadRival
        /// durante el aviso y la embestida.
        /// </summary>
        public bool Detenido { get; set; }

        // ---- ILecturaRival (T-194, D-tony-168) ----
        string ILecturaRival.Nombre => propio != null ? propio.Nombre : name;
        Transform ILecturaRival.Raiz => transform;
        IVida ILecturaRival.Vida => propio?.Vida;
        bool ILecturaRival.EsMecha => true;
        bool ILecturaRival.Preparando => EnAviso || (habilidad != null && habilidad.EnAviso);

        float ILecturaRival.SegundosParaAtacar
        {
            get
            {
                float golpe = SegundosParaGolpe();
                float habil = habilidad != null ? habilidad.SegundosParaAviso : -1f;
                if (habil >= 0f && (golpe < 0f || habil < golpe || habilidad.EnAviso))
                {
                    return habil;
                }
                return golpe;
            }
        }

        TipoAtaqueLeido ILecturaRival.Proximo
        {
            get
            {
                float golpe = SegundosParaGolpe();
                float habil = habilidad != null ? habilidad.SegundosParaAviso : -1f;
                return habil >= 0f && (golpe < 0f || habil < golpe || habilidad.EnAviso) ? TipoAtaqueLeido.Habilidad : TipoAtaqueLeido.Golpe;
            }
        }

        // Segundos hasta que empiece el aviso de su próximo golpe; -1 si no se sabe.
        private float SegundosParaGolpe()
        {
            if (ciclo == null || !isActiveAndEnabled)
            {
                return -1f;
            }
            if (ciclo.Fase == FaseGolpe.Preparando)
            {
                return 0f;
            }
            if (distanciaLectura < 0f || objetivoKOLectura)
            {
                return -1f;
            }
            if (ciclo.Fase == FaseGolpe.Recuperando)
            {
                return datos.Mecha.RecargaGolpe * (1f - ciclo.Progreso) + Mathf.Max(0f, cerebro.EsperaRestante);
            }
            if (distanciaLectura > datos.DistanciaAtaque || (usarTurnos && !TurnosAtaque.Compartido.Tiene(propio)))
            {
                return -1f;
            }
            return Mathf.Max(0f, cerebro.EsperaRestante);
        }

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            propio = GetComponent<IPeleador>();
            habilidad = GetComponent<HabilidadRival>();
            guardia = GetComponent<GuardiaMecha>();
            // Se registra en Awake y no en OnEnable: este componente se apaga cada vez que el rival pierde el control (cuenta atrás,
            // aturdimiento) y los ojos no deben perderlo de vista por eso.
            Registro<ILecturaRival>.Agregar(this);
            if (datos == null || datos.Mecha == null || config == null || brazo == null
                || piezasBrazo == null || piezasBrazo.Length == 0 || propio == null)
            {
                Debug.LogError("[IA] Faltan referencias en RivalBasico de " + name, this);
                enabled = false;
                return;
            }
            cerebro = new CerebroRival(datos);
            sentidoRodeo = Random.value < ProbabilidadRodeoInverso ? -1f : 1f;
            PeleadorMecha peleadorMecha = GetComponent<PeleadorMecha>();
            if (peleadorMecha != null)
            {
                peleadorMecha.FijarNombre(datos.Alias);
            }
            ciclo = new CicloGolpe(datos.Mecha.PreparacionGolpe, datos.Mecha.RecargaGolpe);
            bloque = new MaterialPropertyBlock();
            giroReposoBrazo = brazo.localRotation;
            coloresReposo = new Color[piezasBrazo.Length];
            for (int i = 0; i < piezasBrazo.Length; i++)
            {
                coloresReposo[i] = piezasBrazo[i].sharedMaterial.GetColor(IdColorBase);
            }
        }

        private void Start()
        {
            if (datos == null)
            {
                return;
            }
            if (objetivo == null)
            {
                objetivo = BuscarObjetivo();
            }
            AsignarObjetivo(objetivo);
            inicioHecho = true;
            if (!controlAlArrancar && !controlDecidido)
            {
                propio.ActivarControl(false);
            }
        }

        private void OnEnable()
        {
            if (usarTurnos && controlador != null && !enGrupo)
            {
                RegistrarEnGrupo();
            }
        }

        private void OnDestroy()
        {
            Registro<ILecturaRival>.Quitar(this);
            Grupo.Remove(this);
        }

        // La pandilla no se estorba físicamente: ni entre vivos ni con los caídos. Así nadie queda atorado entre cuerpos
        // y los turnos son lo único que ordena el ataque. El jugador sí los bloquea.
        private void RegistrarEnGrupo()
        {
            enGrupo = true;
            Collider[] propios = GetComponentsInChildren<Collider>();
            foreach (RivalBasico otro in Grupo)
            {
                foreach (Collider ajeno in otro.GetComponentsInChildren<Collider>())
                {
                    foreach (Collider propio in propios)
                    {
                        Physics.IgnoreCollision(propio, ajeno, true);
                    }
                }
            }
            Grupo.Add(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarGrupo() => Grupo.Clear();

        private void OnDisable()
        {
            if (ciclo == null)
            {
                return;
            }
            // Apagarlo antes de su Start es una decisión de control de otro (PrepararPelea / ActivarControl(false)).
            // Ese Start corre al devolverle el control, y entonces no debe quitárselo otra vez.
            if (!inicioHecho)
            {
                controlDecidido = true;
            }
            ciclo.Reiniciar();
            TurnosAtaque.Compartido.Soltar(propio);
            velocidad = 0f;
            PonerBrazoEnReposo();
        }

        /// <summary>Cambia a quién persigue. Debe tener un <see cref="IPeleador"/>; null lo deja sin objetivo.</summary>
        public void AsignarObjetivo(GameObject nuevoObjetivo)
        {
            objetivo = nuevoObjetivo;
            peleadorObjetivo = nuevoObjetivo != null ? nuevoObjetivo.GetComponent<IPeleador>() : null;
            if (peleadorObjetivo == null && !avisoSinObjetivo)
            {
                avisoSinObjetivo = true;
                Debug.LogWarning("[IA] RivalBasico de " + name + " no tiene objetivo; se queda quieto.", this);
            }
        }

        // Se llama una sola vez, en Start: busca al peleador del lado contrario.
        private GameObject BuscarObjetivo()
        {
            foreach (MonoBehaviour candidato in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidato is IPeleador peleador && peleador.Lado != propio.Lado)
                {
                    return candidato.gameObject;
                }
            }
            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            DatosMecha mecha = datos.Mecha;

            BuscarSiHaceFalta(dt);

            Vector3 haciaObjetivo = Vector3.zero;
            bool hayObjetivo = objetivo != null && peleadorObjetivo != null && peleadorObjetivo.Raiz != null;
            if (hayObjetivo)
            {
                haciaObjetivo = peleadorObjetivo.Raiz.position - transform.position;
                haciaObjetivo.y = 0f;
            }
            float distancia = haciaObjetivo.magnitude;
            bool objetivoKO = hayObjetivo && peleadorObjetivo.Vida != null && peleadorObjetivo.Vida.EstaKO;
            distanciaLectura = hayObjetivo ? distancia : -1f;
            objetivoKOLectura = objetivoKO;
            float anillo = datos.DistanciaAtaque + margenEspera;
            bool sinTurno = false;
            bool rodeando = false;

            DecisionRival decision = DecisionRival.Esperar;
            if (hayObjetivo && ciclo.Fase == FaseGolpe.Listo)
            {
                cerebro.MultiplicadorEspera = MultiplicadorEspera;
                decision = cerebro.Decidir(distancia, objetivoKO, true, dt);
                if (usarTurnos)
                {
                    sinTurno = !ConseguirTurno(distancia, anillo, objetivoKO);
                    if (sinTurno)
                    {
                        rodeando = distancia <= anillo + MargenAnillo;
                        decision = rodeando ? DecisionRival.Esperar : DecisionRival.Acercarse;
                    }
                }
            }
            else if (usarTurnos && !hayObjetivo)
            {
                TurnosAtaque.Compartido.Soltar(propio);
            }
            if (Detenido)
            {
                decision = DecisionRival.Esperar;
                rodeando = false;
                velocidad = 0f;
            }
            // Con la guardia levantada (o pedida) no empieza golpes, pero sigue girando y caminando (T-201).
            if (decision == DecisionRival.EmpezarGolpe && guardia != null && (guardia.Levantada || guardia.Pedida))
            {
                decision = DecisionRival.Esperar;
            }
            if (decision == DecisionRival.EmpezarGolpe)
            {
                ciclo.IntentarGolpear();
            }
            bool ocupado = ciclo.Fase != FaseGolpe.Listo;
            if (ciclo.Avanzar(dt))
            {
                Impactar();
            }
            if (usarTurnos && ocupado && ciclo.Fase == FaseGolpe.Listo)
            {
                // Terminó la recarga: el turno pasa al siguiente.
                TurnosAtaque.Compartido.Soltar(propio);
            }

            float alineacion = 0f;
            if (hayObjetivo && !objetivoKO && distancia > DistanciaMinimaParaGirar)
            {
                Vector3 direccion = haciaObjetivo / distancia;
                float yawObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;
                float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, yawObjetivo, mecha.VelocidadGiroMaxima * dt);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                alineacion = Mathf.Clamp01(Vector3.Dot(transform.forward, direccion));
            }

            float velocidadDeseada = 0f;
            if (decision == DecisionRival.Acercarse)
            {
                float parada = sinTurno ? anillo : (datos.DistanciaMinima + datos.DistanciaAtaque) * MitadDeLaDistancia;
                velocidadDeseada = VelocidadParaFrenar(distancia, mecha, parada) * alineacion * MultiplicadorVelocidad;
            }
            float cambio = (velocidadDeseada > velocidad ? mecha.Aceleracion : mecha.Desaceleracion) * MultiplicadorVelocidad;
            velocidad = Mathf.MoveTowards(velocidad, velocidadDeseada, cambio * dt);

            // Nunca pasa de la distancia mínima (o del anillo de espera, si no tiene turno) aunque la inercia lo empuje.
            float limite = sinTurno ? anillo : datos.DistanciaMinima;
            float avance = Mathf.Min(velocidad * dt, Mathf.Max(0f, distancia - limite));
            Vector3 rodeo = Vector3.zero;
            if (rodeando)
            {
                // Se mueve de lado alrededor del objetivo y se corrige hacia el anillo para no abrirse ni cerrarse.
                float error = Mathf.Clamp(distancia - anillo, -1f, 1f);
                rodeo = (transform.right * sentidoRodeo + transform.forward * error) * (velocidadRodeo * MultiplicadorVelocidad * dt);
            }
            // Si quiere avanzar pero algo lo frena (un mecha caído, otro pandillero), rodea el estorbo de lado.
            Vector3 velocidadReal = controlador.velocity;
            velocidadReal.y = 0f;
            bool atorado = avance > 0f && velocidad > VelocidadMinimaAvance && Vector3.Dot(velocidadReal, transform.forward) < VelocidadMinimaAvance;
            tiempoAtorado = atorado ? tiempoAtorado + dt : 0f;
            if (tiempoAtorado > TiempoParaRodearEstorbo)
            {
                rodeo += transform.right * (SentidoDeEstorbo() * velocidadRodeo * MultiplicadorVelocidad * dt);
            }
            if (controlador.isGrounded)
            {
                velocidadVertical = VelocidadVerticalEnPiso;
            }
            else
            {
                velocidadVertical += Physics.gravity.y * dt;
            }
            controlador.Move(transform.forward * avance + rodeo + Vector3.up * (velocidadVertical * dt));

            ActualizarBrazo();
        }

        // Al atorarse prueba de un lado y, si sigue sin avanzar, del otro.
        private float SentidoDeEstorbo()
        {
            int intento = Mathf.FloorToInt((tiempoAtorado - TiempoParaRodearEstorbo) / TiempoParaRodearEstorbo);
            return intento % 2 == 0 ? sentidoRodeo : -sentidoRodeo;
        }

        // Pide el turno cuando ya está en el anillo de espera y su espera casi terminó. Devuelve si es suyo.
        private bool ConseguirTurno(float distancia, float anillo, bool objetivoKO)
        {
            TurnosAtaque turnos = TurnosAtaque.Compartido;
            if (objetivoKO)
            {
                turnos.Soltar(propio);
                return false;
            }
            if (turnos.Tiene(propio))
            {
                return true;
            }
            bool listoParaPedir = distancia <= anillo + MargenAnillo && cerebro.EsperaRestante <= anticipoTurno;
            return listoParaPedir && turnos.Pedir(propio);
        }

        // Sin objetivo, o si el suyo se destruyó, lo busca otra vez cada intervaloBusqueda segundos.
        private void BuscarSiHaceFalta(float dt)
        {
            if (objetivo != null)
            {
                return;
            }
            peleadorObjetivo = null;
            esperaBusqueda -= dt;
            if (esperaBusqueda > 0f)
            {
                return;
            }
            esperaBusqueda = intervaloBusqueda;
            GameObject encontrado = BuscarObjetivo();
            if (encontrado != null)
            {
                AsignarObjetivo(encontrado);
            }
        }

        // Velocidad desde la que todavía puede frenar antes de llegar a la distancia de parada.
        private float VelocidadParaFrenar(float distancia, DatosMecha mecha, float parada)
        {
            float libre = Mathf.Max(0f, distancia - parada);
            return Mathf.Min(mecha.VelocidadMaxima, Mathf.Sqrt(FactorFrenado * mecha.Desaceleracion * libre));
        }

        private void Impactar()
        {
            DatosMecha mecha = datos.Mecha;
            Vector3 origen = transform.TransformPoint(controlador.center);
            Vector3 direccion = transform.forward;
            IRecibeDanio recibe = BarridoGolpe.Buscar(transform, origen, direccion, mecha.AlcanceGolpe,
                radioBusqueda, impactos, out Vector3 punto);
            // La pandilla no se pega entre ella: si lo que encontró es un peleador de su lado, no hace daño.
            Component componente = recibe as Component;
            IPeleador golpeado = componente != null ? componente.GetComponentInParent<IPeleador>() : null;
            bool esAliado = golpeado != null && golpeado.Lado == propio.Lado;
            if (recibe != null && !esAliado)
            {
                float danio = mecha.DanioGolpe * MultiplicadorDanio;
                if (LimitarDanio != null)
                {
                    danio = LimitarDanio(danio);
                }
                if (danio > 0f)
                {
                    recibe.RecibirDanio(new InfoDanio(danio, TipoDanio.Golpe, punto, direccion, propio));
                }
            }
            Impacto?.Invoke();
        }

        private void ActualizarBrazo()
        {
            if (ciclo.Fase == FaseGolpe.Preparando)
            {
                brazo.localRotation = giroReposoBrazo * Quaternion.Euler(-config.AnguloBrazoAtras * ciclo.Progreso, 0f, 0f);
                PintarBrazo(TinteAviso);
                return;
            }
            float subida = guardia != null ? guardia.Progreso : 0f;
            if (subida > 0f)
            {
                brazo.localRotation = giroReposoBrazo * Quaternion.Euler(-config.AnguloBrazoGuardia * subida, 0f, 0f);
                PintarBrazo(subida);
            }
            else
            {
                PonerBrazoEnReposo();
            }
        }

        private void PonerBrazoEnReposo()
        {
            brazo.localRotation = giroReposoBrazo;
            PintarBrazo(0f);
        }

        private void PintarBrazo(float tinte)
        {
            if (Mathf.Approximately(tinte, tinteBrazo))
            {
                return;
            }
            tinteBrazo = tinte;
            bool aviso = tinte < 0f;
            for (int i = 0; i < piezasBrazo.Length; i++)
            {
                piezasBrazo[i].GetPropertyBlock(bloque);
                Color color = aviso ? config.ColorAviso : Color.Lerp(coloresReposo[i], config.ColorGuardia, tinte);
                Color emision = aviso ? config.ColorAviso : Color.Lerp(Color.black, config.ColorGuardia, tinte);
                bloque.SetColor(IdColorBase, color);
                bloque.SetColor(IdColorEmision, emision);
                piezasBrazo[i].SetPropertyBlock(bloque);
            }
        }
    }
}
