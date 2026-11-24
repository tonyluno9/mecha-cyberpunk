using System;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Golpe simple del jugador a pie. Con Atacar y los puños en la mano arranca un <see cref="CicloGolpe"/>; en el impacto
    /// lanza una esfera desde el pecho hacia donde mira el cuerpo (<see cref="BarridoGolpe"/>) y daña al primer
    /// <see cref="IRecibeDanio"/> que no sea el propio jugador. En KO no ataca. El atacante es null: la persona no es IPeleador.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(VidaAPie))]
    public class GolpeAPie : MonoBehaviour
    {
        private const int MaxImpactosPorGolpe = 16;
        private const float AlturaPecho = 1.2f;
        private const int MaxCercanos = 32;

        [SerializeField] private DatosPersonaje datos;
        [SerializeField] private InputActionReference atacar;
        [Tooltip("T-1487: asistencia, combo, hitstop, temblor y empuje del golpe. Vacío: valores de código (ConfigGolpeAPie.PorDefecto).")]
        [SerializeField] private ConfigGolpeAPie config;
        [Tooltip("Cubo que hace de puño, hijo de Manos. Avanza durante la preparación y regresa en la recarga.")]
        [SerializeField] private Transform puno;
        [Tooltip("Cuánto avanza el puño hacia adelante en el impacto (m).")]
        [Min(0f)] [SerializeField] private float avancePuno = 0.6f;

        private ControladorAPie controlador;
        private VidaAPie vida;
        private CicloGolpe ciclo;
        private ArsenalJugador arsenal;
        private VisualPersonaje visual;
        private float recargaActual;
        private readonly SeguidorImplantes implantes = new SeguidorImplantes();
        private Vector3 posicionReposo;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactosPorGolpe];
        private readonly Collider[] cercanos = new Collider[MaxCercanos];
        private readonly RaycastHit[] estorbos = new RaycastHit[MaxImpactosPorGolpe];
        private readonly System.Collections.Generic.List<Component> blancos = new System.Collections.Generic.List<Component>();
        private readonly System.Collections.Generic.List<Vector2> posicionesBlancos = new System.Collections.Generic.List<Vector2>();
        private bool girando;
        private float anguloAutoGiro;
        // T-1327: el golpe dirigido también acerca un poco al jugador al blanco.
        private float acercamientoPendiente;
        private Vector3 direccionAcercamiento;
        private CharacterController cuerpo;
        // T-1487: combo con buffer, y feedback al conectar.
        private ReglaComboGolpe combo;
        private SensacionGolpe sensacion;

        /// <summary>T-1487: el golpe en curso es el último del combo (la animación usa GolpeFuerte).</summary>
        public bool EsGolpeFinal => combo != null && combo.EsGolpeFinal;
        /// <summary>T-1487: índice del golpe del combo en curso (-1 si no hay).</summary>
        public int PasoCombo => combo != null ? combo.Paso : -1;
        public ConfigGolpeAPie Config => config;

        public DatosPersonaje Datos => datos;
        public FaseGolpe Fase => ciclo != null ? ciclo.Fase : FaseGolpe.Listo;
        public float Progreso => ciclo != null ? ciclo.Progreso : 0f;
        public Transform Puno => puno;
        /// <summary>Arma en la mano. T-053 la alterna entre puños y pistola; solo los puños golpean.</summary>
        public ArmaAPie ArmaActiva { get; set; } = ArmaAPie.Punos;
        /// <summary>Se dispara en cada impacto, haya dado a alguien o no.</summary>
        public event Action Impacto;
        /// <summary>Se dispara cuando el golpe alcanza a alguien; el argumento es si lo dejó en KO (T-420).</summary>
        public event Action<bool> Conecto;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            cuerpo = GetComponent<CharacterController>();
            if (datos == null || atacar == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en GolpeAPie de " + name, this);
                enabled = false;
                return;
            }
            if (config == null)
            {
                config = ConfigGolpeAPie.PorDefecto;
            }
            combo = new ReglaComboGolpe(config.GolpesCombo, config.SegundosBuffer, config.VentanaCombo, config.SegundosReinicioCombo);
            sensacion = GetComponent<SensacionGolpe>();
            if (sensacion == null)
            {
                sensacion = gameObject.AddComponent<SensacionGolpe>();
            }
            sensacion.Configurar(config);
            recargaActual = datos.RecargaGolpe;
            ciclo = new CicloGolpe(datos.PreparacionGolpe * ReglasAnimacion.EscalaTiempoGolpe, recargaActual * ReglasAnimacion.EscalaTiempoGolpe);
            if (puno != null)
            {
                posicionReposo = puno.localPosition;
            }
        }

        private void OnEnable()
        {
            atacar.action.Enable();
            implantes.Activar();
        }

        private void OnDisable()
        {
            implantes.Desactivar();
            ciclo?.Reiniciar();
            MoverPuno();
        }

        private void Update()
        {
            if (vida.EstaKO || !controlador.ControlActivo || controlador.Derribado)
            {
                girando = false;
                acercamientoPendiente = 0f;
                ciclo.Reiniciar();
                combo.Reiniciar(); // T-1487: cancelación limpia: sin buffer ni combo en KO o derribo
                MoverPuno();
                return;
            }
            if (ArmaActiva != ArmaAPie.Punos)
            {
                combo.Reiniciar();
            }
            if (ciclo.Fase == FaseGolpe.Listo)
            {
                AjustarRecarga();
            }
            // T-1487: el clic se recuerda (buffer); el combo encadena el siguiente golpe sin volver a Listo.
            // Entre golpes de un combo no se espera a que acabe la animación; fuera del combo sí (T-675, sin spam).
            if (atacar.action.WasPressedThisFrame() && ArmaActiva == ArmaAPie.Punos && !controlador.EntradaBloqueada)
            {
                combo.RegistrarEntrada();
            }
            float dt = Time.deltaTime;
            if (sensacion.EnHitstop)
            {
                // Hitstop: la animación está congelada y la lógica espera con ella (el buffer queda pausado).
                combo.Avanzar(0f, ciclo.Fase, ciclo.Progreso, false);
                MoverPuno();
                return;
            }
            bool enCombo = combo.Paso >= 0 && !combo.EsGolpeFinal;
            bool animacionLibre = (enCombo || !AnimacionOcupada()) && GuardiaListaParaGolpear();
            if (combo.Avanzar(dt, ciclo.Fase, ciclo.Progreso, animacionLibre))
            {
                AjustarRecarga(); // T-1658: BRAZOS activa o no cambia la rapidez entre golpes del combo
                ciclo.Reiniciar();
                ciclo.IntentarGolpear();
                IniciarAutoGiro();
            }
            if (girando)
            {
                AvanzarAutoGiro(dt);
            }
            Acercar(dt);
            if (ciclo.Avanzar(dt))
            {
                if (girando)
                {
                    // Si la preparación terminó antes que el giro, se termina de girar para que el golpe conecte.
                    transform.rotation = Quaternion.Euler(0f, anguloAutoGiro, 0f);
                    girando = false;
                }
                Impactar();
            }
            MoverPuno();
        }

        // T-1003: al golpear, el cuerpo se gira hacia el oponente vivo más cercano al frente, sin muro en medio. Apuntando no cambia nada.
        private void IniciarAutoGiro()
        {
            girando = false;
            acercamientoPendiente = 0f;
            if (controlador.Apuntando || config.AlcanceAsistencia <= 0f)
            {
                return;
            }
            Vector3 centro = transform.position;
            Vector3 origen = centro + Vector3.up * AlturaPecho;
            Vector3 frente3 = controlador.Camara != null ? controlador.Camara.forward : transform.forward;
            Vector2 frente = new Vector2(frente3.x, frente3.z);
            if (frente.sqrMagnitude < 0.0001f)
            {
                frente = new Vector2(transform.forward.x, transform.forward.z);
            }
            blancos.Clear();
            posicionesBlancos.Clear();
            int cantidad = Physics.OverlapSphereNonAlloc(centro, config.AlcanceAsistencia, cercanos, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                Collider c = cercanos[i];
                if (c.transform.root == transform.root)
                {
                    continue;
                }
                Component componente = c.GetComponentInParent<IRecibeDanio>() as Component;
                if (componente == null || blancos.Contains(componente))
                {
                    continue;
                }
                IVida vidaBlanco = componente.GetComponentInParent<IVida>();
                if ((vidaBlanco != null && vidaBlanco.EstaKO) || HayMuroEntre(origen, c, componente.transform.root))
                {
                    continue;
                }
                Vector3 p = componente.transform.position;
                blancos.Add(componente);
                posicionesBlancos.Add(new Vector2(p.x, p.z));
            }
            Vector2 yo = new Vector2(centro.x, centro.z);
            int elegido = SelectorAutoGiro.Elegir(yo, frente, posicionesBlancos, config.AlcanceAsistencia, config.AnguloAsistencia);
            if (elegido < 0)
            {
                return;
            }
            Vector2 haciaBlanco = posicionesBlancos[elegido] - yo;
            anguloAutoGiro = SelectorAutoGiro.AnguloYaw(haciaBlanco);
            girando = true;
            acercamientoPendiente = SelectorAutoGiro.Acercamiento(haciaBlanco.magnitude, config.DistanciaParada, config.AcercamientoMaximo);
            direccionAcercamiento = new Vector3(haciaBlanco.x, 0f, haciaBlanco.y).normalized;
        }

        /// <summary>T-1327: metros que le faltan por acercarse al blanco del golpe (para las pruebas).</summary>
        public float AcercamientoPendiente => acercamientoPendiente;

        private void Acercar(float dt)
        {
            if (acercamientoPendiente <= 0f || cuerpo == null || !cuerpo.enabled)
            {
                return;
            }
            float paso = Mathf.Min(acercamientoPendiente, config.RapidezAcercamiento * dt);
            acercamientoPendiente -= paso;
            cuerpo.Move(direccionAcercamiento * paso);
        }

        private bool HayMuroEntre(Vector3 origen, Collider destino, Transform raizBlanco)
        {
            Vector3 delta = destino.bounds.center - origen;
            float distancia = delta.magnitude;
            if (distancia < 0.01f)
            {
                return false;
            }
            int n = Physics.RaycastNonAlloc(origen, delta / distancia, estorbos, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform raiz = estorbos[i].collider.transform.root;
                if (raiz != transform.root && raiz != raizBlanco)
                {
                    return true;
                }
            }
            return false;
        }

        private void AvanzarAutoGiro(float dt)
        {
            float angulo = Mathf.MoveTowardsAngle(transform.eulerAngles.y, anguloAutoGiro, config.VelocidadGiro * dt);
            transform.rotation = Quaternion.Euler(0f, angulo, 0f);
            if (Mathf.Approximately(Mathf.DeltaAngle(angulo, anguloAutoGiro), 0f))
            {
                girando = false;
            }
        }

        // T-1561 (decisión de Tony, 21-nov): golpear corriendo primero sube la guardia (0.2 s) y luego golpea. Parado o caminando despacio el golpe sale de una vez.
        private float esperaGuardia;

        private bool GuardiaListaParaGolpear()
        {
            if (!combo.HayBuffer)
            {
                esperaGuardia = 0f;
                return true;
            }
            if (visual == null)
            {
                visual = GetComponentInChildren<VisualPersonaje>(true);
            }
            if (visual == null || !visual.gameObject.activeInHierarchy)
            {
                return true;
            }
            float rapidez = controlador.Velocidad.magnitude;
            bool arriba = visual.GuardiaArriba;
            if (!ReglasTransicionesT1561.NecesitaSubirGuardia(rapidez, arriba))
            {
                esperaGuardia = 0f;
                return true;
            }
            visual.SubirGuardia();
            esperaGuardia += Time.deltaTime;
            return ReglasTransicionesT1561.GuardiaLista(rapidez, arriba, esperaGuardia);
        }

        /// <summary>T-1561: el daño recibido corta el golpe en curso y el siguiente del combo (vuelve a Listo).</summary>
        public void InterrumpirPorDano()
        {
            ciclo?.Reiniciar();
            combo?.Reiniciar();
            girando = false;
            acercamientoPendiente = 0f;
            MoverPuno();
        }

        private bool AnimacionOcupada()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<VisualPersonaje>(true);
            }
            return visual != null && visual.gameObject.activeInHierarchy && visual.GolpeEnCurso;
        }

        // T-403: los puños usan el daño y la recarga de su nivel. Sin arsenal, los de DatosPersonaje.
        private DatosArma DatosPunos()
        {
            arsenal = arsenal != null ? arsenal : Registro<ArsenalJugador>.Actual;
            return arsenal != null ? arsenal.Datos(ArmaAPie.Punos) : null;
        }

        private MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis habilidades;
        private MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis Habilidades =>
            habilidades != null ? habilidades : (habilidades = GetComponent<MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis>());

        /// <summary>Daño del puño contra <paramref name="objetivo"/> (nivel, implantes, habilidades y enemigos). VR2 lo usa para los golpes con las manos.</summary>
        public float DanioDelGolpe(Component objetivo)
        {
            DatosArma punos = DatosPunos();
            float basico = punos != null ? punos.Danio : datos.DanioGolpe;
            float total = basico * implantes.Efectos.multiplicadorGolpeAPie * (Habilidades != null ? Habilidades.FactorGolpe : 1f);
            // T-1490: los puños pegan menos a los enemigos (ILecturaRival); con muchos hay que usar un arma. Las armas no pasan por aquí.
            return ReglaDanioPunos.Danio(total, config.MultiplicadorPunosContraEnemigos, objetivo != null && objetivo.GetComponentInParent<ILecturaRival>() != null);
        }

        private float factorRapidezActual = 1f;
        /// <summary>T-1658: rapidez extra del golpe ahora (1 normal; con BRAZOS activa, el factor de la config).</summary>
        public float FactorRapidezActual => factorRapidezActual;

        private void AjustarRecarga()
        {
            DatosArma punos = DatosPunos();
            float recarga = punos != null ? punos.Recarga : datos.RecargaGolpe;
            float factor = Habilidades != null ? Habilidades.FactorRapidezGolpe : 1f;
            if (!Mathf.Approximately(recarga, recargaActual) || !Mathf.Approximately(factor, factorRapidezActual))
            {
                recargaActual = recarga;
                factorRapidezActual = factor;
                float escala = ReglasAnimacion.EscalaTiempoGolpe / factor; // T-1657: golpe 0.75x más lento; T-1658: BRAZOS lo acelera
                ciclo = new CicloGolpe(datos.PreparacionGolpe * escala, recarga * escala);
                if (visual == null)
                {
                    visual = GetComponentInChildren<VisualPersonaje>(true);
                }
                if (visual != null)
                {
                    visual.RapidezGolpes = ReglasAnimacion.RapidezGolpeJugador * factor;
                }
            }
        }

        /// <summary>Depuración (T-1657/T-1658): tiempos del golpe y rapidez de los clips. Invocable por reflexión desde Play.</summary>
        public static string DepurarGolpe()
        {
            GolpeAPie g = FindAnyObjectByType<GolpeAPie>();
            if (g == null)
            {
                return "[Personaje] No hay GolpeAPie en escena.";
            }
            VisualPersonaje v = g.GetComponentInChildren<VisualPersonaje>(true);
            return "[Personaje] GolpeAPie: fase=" + g.Fase + " rapidezClips=" + (v != null ? v.RapidezGolpes.ToString("0.000") : "sin visual")
                + " (base " + ReglasAnimacion.RapidezGolpeJugador.ToString("0.000") + ") factorBrazos=" + g.factorRapidezActual.ToString("0.00")
                + " preparacion=" + (g.datos.PreparacionGolpe * ReglasAnimacion.EscalaTiempoGolpe / g.factorRapidezActual).ToString("0.000")
                + "s recarga=" + (g.recargaActual * ReglasAnimacion.EscalaTiempoGolpe / g.factorRapidezActual).ToString("0.000") + "s";
        }

        private void Impactar()
        {
            // Tercera persona: el golpe sale del pecho hacia donde mira el cuerpo (apuntando, hacia donde mira la cámara).
            Vector3 origen = transform.position + Vector3.up * AlturaPecho;
            Vector3 direccion = controlador.Apuntando ? controlador.Camara.forward : transform.forward;
            IRecibeDanio objetivo = BarridoGolpe.Buscar(transform, origen, direccion, datos.AlcanceGolpe,
                datos.RadioGolpe, impactos, out Vector3 punto);
            if (objetivo != null)
            {
                ResultadoDanio resultado = objetivo.RecibirDanio(new InfoDanio(DanioDelGolpe(objetivo as Component), TipoDanio.Golpe, punto, direccion, null));
                Conecto?.Invoke(resultado.CausoKO);
                sensacion.Conectar(objetivo as Component, direccion, combo.EsGolpeFinal);
                // T-992: con BRAZOS activa el golpe empuja a lo que tenga cuerpo con física.
                float empuje = Habilidades != null ? Habilidades.EmpujeGolpe : 0f;
                Rigidbody cuerpo = empuje > 0f && objetivo is Component componente ? componente.GetComponentInParent<Rigidbody>() : null;
                if (cuerpo != null && !cuerpo.isKinematic)
                {
                    // T-1658: un cuerpo pesado (carro) solo se mueve unos centímetros; no sale volando.
                    bool pesado = cuerpo.mass >= Habilidades.MasaCuerpoPesado;
                    float velocidad = pesado ? Habilidades.EmpujeCuerpoPesado : empuje;
                    Vector3 plano = Vector3.ProjectOnPlane(direccion, Vector3.up);
                    cuerpo.AddForce((pesado ? plano : direccion).normalized * velocidad, ForceMode.VelocityChange);
                }
            }
            if (objetivo == null)
            {
                Sonidos.ReproducirEn(ReglaSonidosJugador.IdGolpe(false), origen); // al conectar suena SensacionGolpe
            }
            Impacto?.Invoke();
        }

        // Preparando: el puño va de reposo al máximo avance; Recuperando: regresa.
        private void MoverPuno()
        {
            if (puno == null)
            {
                return;
            }
            float avance = 0f;
            if (ciclo != null)
            {
                if (ciclo.Fase == FaseGolpe.Preparando)
                {
                    avance = ciclo.Progreso;
                }
                else if (ciclo.Fase == FaseGolpe.Recuperando)
                {
                    avance = 1f - ciclo.Progreso;
                }
            }
            puno.localPosition = posicionReposo + Vector3.forward * (avancePuno * avance);
        }
    }
}
