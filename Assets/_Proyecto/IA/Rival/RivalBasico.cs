using MechaCyberpunk.Combate.Golpe;
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
    public class RivalBasico : MonoBehaviour
    {
        private const int MaxImpactosPorGolpe = 16;
        private const float DistanciaMinimaParaGirar = 0.01f;
        private const float VelocidadVerticalEnPiso = -2f;
        private const float MitadDeLaDistancia = 0.5f;
        private const float FactorFrenado = 2f;

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

        private CharacterController controlador;
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
        private bool brazoEnAviso;
        private bool inicioHecho;
        // Alguien (el director de pelea) ya le quitó el control antes de su Start: no se lo vuelve a quitar.
        private bool controlDecidido;

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

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            propio = GetComponent<IPeleador>();
            if (datos == null || datos.Mecha == null || config == null || brazo == null
                || piezasBrazo == null || piezasBrazo.Length == 0 || propio == null)
            {
                Debug.LogError("[IA] Faltan referencias en RivalBasico de " + name, this);
                enabled = false;
                return;
            }
            cerebro = new CerebroRival(datos);
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

            Vector3 haciaObjetivo = Vector3.zero;
            bool hayObjetivo = peleadorObjetivo != null && peleadorObjetivo.Raiz != null;
            if (hayObjetivo)
            {
                haciaObjetivo = peleadorObjetivo.Raiz.position - transform.position;
                haciaObjetivo.y = 0f;
            }
            float distancia = haciaObjetivo.magnitude;
            bool objetivoKO = hayObjetivo && peleadorObjetivo.Vida != null && peleadorObjetivo.Vida.EstaKO;

            DecisionRival decision = DecisionRival.Esperar;
            if (hayObjetivo && ciclo.Fase == FaseGolpe.Listo)
            {
                cerebro.MultiplicadorEspera = MultiplicadorEspera;
                decision = cerebro.Decidir(distancia, objetivoKO, true, dt);
            }
            if (decision == DecisionRival.EmpezarGolpe)
            {
                ciclo.IntentarGolpear();
            }
            if (ciclo.Avanzar(dt))
            {
                Impactar();
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
                velocidadDeseada = VelocidadParaFrenar(distancia, mecha) * alineacion * MultiplicadorVelocidad;
            }
            float cambio = (velocidadDeseada > velocidad ? mecha.Aceleracion : mecha.Desaceleracion) * MultiplicadorVelocidad;
            velocidad = Mathf.MoveTowards(velocidad, velocidadDeseada, cambio * dt);

            // Nunca pasa de la distancia mínima aunque la inercia lo empuje.
            float avance = Mathf.Min(velocidad * dt, Mathf.Max(0f, distancia - datos.DistanciaMinima));
            if (controlador.isGrounded)
            {
                velocidadVertical = VelocidadVerticalEnPiso;
            }
            else
            {
                velocidadVertical += Physics.gravity.y * dt;
            }
            controlador.Move(transform.forward * avance + Vector3.up * (velocidadVertical * dt));

            ActualizarBrazo();
        }

        // Velocidad desde la que todavía puede frenar antes de la mitad entre la distancia mínima y la de ataque.
        private float VelocidadParaFrenar(float distancia, DatosMecha mecha)
        {
            float parada = (datos.DistanciaMinima + datos.DistanciaAtaque) * MitadDeLaDistancia;
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
            if (recibe != null)
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
                PintarBrazo(true);
            }
            else
            {
                PonerBrazoEnReposo();
            }
        }

        private void PonerBrazoEnReposo()
        {
            brazo.localRotation = giroReposoBrazo;
            PintarBrazo(false);
        }

        private void PintarBrazo(bool aviso)
        {
            if (aviso == brazoEnAviso)
            {
                return;
            }
            brazoEnAviso = aviso;
            for (int i = 0; i < piezasBrazo.Length; i++)
            {
                piezasBrazo[i].GetPropertyBlock(bloque);
                bloque.SetColor(IdColorBase, aviso ? config.ColorAviso : coloresReposo[i]);
                bloque.SetColor(IdColorEmision, aviso ? config.ColorAviso : Color.black);
                piezasBrazo[i].SetPropertyBlock(bloque);
            }
        }
    }
}
