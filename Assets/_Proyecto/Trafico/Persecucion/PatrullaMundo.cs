using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Una patrulla (T-341, T-587): recorre el <see cref="GrafoCalles"/> hacia el cruce más cercano al jugador, siempre sobre el eje de las calles.
    /// Solo sabe dónde estás mientras te ve (a menos de <see cref="ConfigPersecucion.RadioVista"/> m y sin pared en medio); si te pierde de vista va a donde
    /// te vio por última vez. Las que "cortan el paso" se adelantan al cruce hacia donde corres. Al bajar el calor <see cref="Retirar"/> la manda por la calle
    /// hasta salir de la vista, donde desaparece. Sus luces rojas y azules alternan con <see cref="ConfigPersecucion.PeriodoSirena"/>.
    /// T-707: es un carro de verdad (el de Vehiculos, con cuerpo cinemático y colliders, igual que los del tráfico): es sólido y se puede robar. Al subir el
    /// jugador deja de ser patrulla (pasa a ser carro suyo) y, mientras vaya en ella, la policía lo ubica siempre (GPS).
    /// T-933: también puede ir de ronda (<see cref="PrepararRonda"/>): circula sin sirena y sin saber dónde estás; si ve a un criminal atacando, va y baja a sus policías.
    /// </summary>
    public class PatrullaMundo : MonoBehaviour, Testigos.ITestigoPolicia
    {
        // T-533: de cerca deja la calle y va directo hacia el jugador (como en GTA), y por el megáfono le grita cada tanto.
        private const float DistanciaDirecta = 28f;
        private const float DistanciaMegafono = 35f;
        private const float SegundosEntreMegafono = 7f;
        // T-581: de cerca solo va directo si no hay una pared en medio; si no, regresa por donde vino hasta la calle.
        private const float AlturaVista = 1f;
        private const float AlturaOjos = 2.2f;
        private const float MargenAlJugador = 3f;
        private const float DistanciaEntreMigas = 2f;
        private const float MargenDelante = 1.5f;
        private const int MaximoDeMigas = 60;
        // T-587: velocidad del jugador suavizada y alcance de la cámara para saber si la patrulla se ve.
        private const float SuavizadoVelocidad = 5f;
        private const float VelocidadImposible = 80f;
        private const float AlcanceCamara = 400f;
        private const float SegundosMinimosDeRetiro = 1f;
        // T-933: ronda.
        private const float SegundosEntreVistazos = 0.5f;
        private const float AlturaOjosPolicia = 1.6f;
        private const float AlturaCriminal = 1.2f;
        private static readonly List<PatrullaMundo> ActivasLista = new List<PatrullaMundo>();
        // T-666: conducción natural: carril a la derecha, esquiva obstáculos y no se encima con otras patrullas.
        private const float AnchoCarril = 1.6f;
        private const float VelocidadCarril = 6f;
        private const float DistanciaObstaculo = 20f;
        private const float RadioObstaculo = 0.7f;
        private const float AlturaObstaculo = 1.4f;
        private const float DistanciaFrenoPatrullas = 12f;
        private const float DistanciaMinimaEntrePatrullas = 5f;
        private const float AnchoDePasoPatrullas = 2.8f;
        private const float FactorObstaculoTotal = 0.15f;
        private const float RapidezDeFreno = 3f;
        // T-986: maneja con la física del carro; esto es lo que se mira para quitarla si queda volcada y para no esperar por siempre un obstáculo.
        private const float MinimoArribaVolcada = 0.3f;
        private const float DistanciaMinimaDeMira = 1f;
        private static readonly float[] CarrilesPosibles = { AnchoCarril, -AnchoCarril, 2.75f * AnchoCarril, -2.75f * AnchoCarril };
        private static readonly float[] GradosParaEsquivar = { 35f, -35f, 70f, -70f, 105f, -105f };
        private static readonly RaycastHit[] impactos = new RaycastHit[16];
        private float factorVelocidad = 1f;
        private float desvioCarril;
        private int bucleSirena = Sonidos.SinBucle;
        private float proximoMegafono;

        [Tooltip("T-707: altura (m) a la que va la raíz sobre la calle si no se puede medir el modelo.")]
        [Min(0f)] [SerializeField] private float alturaRaiz = 0.3f;
        [Tooltip("T-709: el policía a pie que se baja al llegar (IA/Prefabs/PoliciaAPie). Sin él no se baja nadie.")]
        [SerializeField] private GameObject prefabPolicia;
        [Tooltip("Luces de la sirena: la primera es roja y la segunda azul; alternan.")]
        [SerializeField] private Light luzRoja;
        [SerializeField] private Light luzAzul;

        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private GrafoCalles calles;
        private Func<Vector3?> posicionJugador;
        private readonly List<int> camino = new List<int>();
        private int indiceEnCamino;
        private int nodoMeta = -1;
        private Vector3 posicion;
        // T-986: el conductor IA (puro) y lo que el cerebro le pasa cada paso de física: a dónde mirar y qué velocidad llevar.
        private CarroViejo carro;
        private ConductorCarroIA conductor;
        private Vector3 puntoDeMira;
        private float velocidadDeseada;
        private ParametrosConductorIA parametrosConductor;
        private bool orientarAlSalir;
        private float segundosVolcada;
        private float segundosObstaculoTotal;
        private bool estorboEnMiCarril;
        private readonly List<Vector3> rastro = new List<Vector3>();
        private Vector3 ultimaVista;
        private Vector3 velocidadJugador;
        private Vector3? jugadorAnterior;
        private int nodoRetiro = -1;
        private float segundosRetirandose;
        private ICriminalCalle criminalVisto;
        private System.Random azarRonda;
        private float proximoVistazo;
        private float segundosSinCriminal;

        /// <summary>T-933: true si anda de ronda (sin sirena y sin perseguir a nadie).</summary>
        public bool EnRonda { get; private set; }

        /// <summary>T-933: true si de ronda vio a un criminal atacando y va por él.</summary>
        public bool AtiendeCriminal => CriminalVivo();

        public static IReadOnlyList<PatrullaMundo> Activas => ActivasLista;

        // T-707: las patrullas robadas con el jugador adentro (mientras haya una, la policía lo ubica con el GPS).
        private static readonly List<PatrullaMundo> ConJugadorLista = new List<PatrullaMundo>();

        /// <summary>T-707: True mientras el jugador va dentro de una patrulla robada: la policía siempre sabe dónde está.</summary>
        public static bool JugadorEnPatrulla => ConJugadorLista.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            ActivasLista.Clear();
            ConJugadorLista.Clear();
        }

        /// <summary>T-707: True si el jugador ya se la robó: deja de ser de la policía y es un carro suyo.</summary>
        public bool Robada { get; private set; }

        /// <summary>T-707: True mientras el jugador va adentro.</summary>
        public bool ConJugador => montable != null && montable.Ocupado;

        private Rigidbody cuerpo;
        private IMontable montable;
        private Renderer[] visuales;
        private float raizABajo = -1f;
        private readonly RaycastHit[] golpesSuelo = new RaycastHit[8];
        private readonly RaycastHit[] golpesRayo = new RaycastHit[16];
        private const float AltoRayoSuelo = 2f;
        private const float LargoRayoSuelo = 10f;
        private const float MinimoRaizABajo = 0.05f;
        private const float MaximoRaizABajo = 1.2f;

        /// <summary>T-707: qué tan abajo de la raíz quedan las ruedas (m).</summary>
        public float RaizABajo => raizABajo >= 0f ? raizABajo : alturaRaiz;

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody>();
            montable = GetComponent<IMontable>();
            carro = GetComponent<CarroViejo>();
            List<Renderer> mallas = new List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    mallas.Add(r);
                }
            }
            visuales = mallas.ToArray();
            raizABajo = MedirRaizABajo();
            if (montable != null)
            {
                montable.OcupadoCambio += AlCambiarOcupado;
            }
        }

        /// <summary>T-709: los policías que se bajaron de esta patrulla.</summary>
        public IReadOnlyList<GameObject> Policias => policias;

        /// <summary>T-709: True si ya bajó a sus policías (lo hace una sola vez, al llegar).</summary>
        public bool PoliciasBajaron { get; private set; }

        private readonly List<GameObject> policias = new List<GameObject>();
        private const float SeparacionPuerta = 1.9f;
        private const float SeparacionEntrePolicias = 0.9f;
        private const float AlturaSobreSuelo = 0.05f;

        /// <summary>T-709: al llegar junto al jugador, los policías abren la puerta y se bajan, uno por cada lado de la patrulla.</summary>
        public void BajarPolicias()
        {
            if (PoliciasBajaron || prefabPolicia == null || config == null || Robada)
            {
                return;
            }
            PoliciasBajaron = true;
            Vector3? jugador = posicionJugador?.Invoke();
            for (int i = 0; i < config.PoliciasPorPatrulla; i++)
            {
                float lado = i % 2 == 0 ? 1f : -1f;
                Vector3 punto = transform.position + transform.right * (lado * SeparacionPuerta) - transform.forward * (SeparacionEntrePolicias * (i / 2));
                punto.y = BuscarSuelo(punto, transform.position.y, transform.position.y - RaizABajo) + AlturaSobreSuelo;
                Vector3 mirada = (jugador ?? transform.position + transform.forward) - punto;
                mirada.y = 0f;
                Quaternion giro = mirada.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mirada) : transform.rotation;
                GameObject policia = Instantiate(prefabPolicia, punto, giro);
                policia.name = "Policia_" + (i + 1);
                policias.Add(policia);
            }
            Sonidos.ReproducirEn("VEH_PUERTA_ABRIR", transform.position);
        }

        private void OnDestroy()
        {
            if (!Robada)
            {
                foreach (GameObject policia in policias)
                {
                    if (policia != null)
                    {
                        Destroy(policia);
                    }
                }
            }
            ConJugadorLista.Remove(this);
            if (montable != null)
            {
                montable.OcupadoCambio -= AlCambiarOcupado;
            }
        }

        private float MedirRaizABajo()
        {
            bool hay = false;
            float bajo = float.MaxValue;
            foreach (Renderer r in visuales)
            {
                if (r.enabled)
                {
                    bajo = Mathf.Min(bajo, r.bounds.min.y);
                    hay = true;
                }
            }
            return hay ? Mathf.Clamp(transform.position.y - bajo, MinimoRaizABajo, MaximoRaizABajo) : alturaRaiz;
        }

        // T-707: el jugador subió (solo puede si la patrulla está detenida): pasa a ser un carro suyo, con física completa.
        private void AlCambiarOcupado(bool ocupado)
        {
            if (!ocupado)
            {
                ConJugadorLista.Remove(this);
                return;
            }
            if (!ConJugadorLista.Contains(this))
            {
                ConJugadorLista.Add(this);
            }
            if (Robada)
            {
                return;
            }
            Robada = true;
            VeAlJugador = false;
            VaDirecto = false;
            // Ya es un carro suelto del jugador: su suspensión y sus choques ignoran lo que comparta su raíz, y colgada del barrio compartiría la del suelo.
            transform.SetParent(null, true);
            carro?.SoltarIA(); // T-986: ahora la maneja el jugador
            PonerLuces(false, false);
            enabled = false; // OnDisable apaga la sirena y la quita de las activas
        }

        // Rayo hacia abajo desde un poco arriba de la patrulla: el suelo es lo primero sólido que no sea ella, otro carro ni gente.
        private float BuscarSuelo(Vector3 xz, float desdeY, float respaldo)
        {
            Vector3 origen = new Vector3(xz.x, desdeY + AltoRayoSuelo, xz.z);
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, golpesSuelo, LargoRayoSuelo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            float y = respaldo;
            for (int i = 0; i < n; i++)
            {
                Collider c = golpesSuelo[i].collider;
                if (c == null || c.transform.IsChildOf(transform) || c.attachedRigidbody != null || c.GetComponentInParent<Peatones.PeatonMundo>() != null)
                {
                    continue;
                }
                if (golpesSuelo[i].distance < menor)
                {
                    menor = golpesSuelo[i].distance;
                    y = golpesSuelo[i].point.y;
                }
            }
            return y;
        }

        // Nada sólido en el rayo (el de la propia patrulla no cuenta: su origen puede quedar dentro de su carrocería).
        private bool RayoLibre(Vector3 origen, Vector3 direccion, float largo)
        {
            int n = Physics.RaycastNonAlloc(origen, direccion, golpesRayo, largo, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = golpesRayo[i].collider;
                if (c != null && !c.transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>True si va directo al jugador (ya dejó la calle).</summary>
        public bool VaDirecto { get; private set; }

        /// <summary>True mientras ve al jugador (cerca y sin pared en medio).</summary>
        public bool VeAlJugador { get; private set; }

        /// <summary>Donde vio al jugador por última vez (a donde va si lo pierde).</summary>
        public Vector3 UltimaVista => ultimaVista;

        /// <summary>Nodo del grafo al que se dirige por la calle (-1 si va directo o regresa).</summary>
        public int NodoMeta => nodoMeta;

        /// <summary>True si esta patrulla se adelanta al cruce hacia donde corre el jugador (en vez de ir a donde está).</summary>
        public bool Intercepta { get; set; }

        /// <summary>True si ya se está yendo: ignora al jugador y sale de la vista por la calle.</summary>
        public bool Retirando { get; private set; }

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, GrafoCalles grafo, int nodoInicial, Func<Vector3?> dondeEstaElJugador, bool conSirena = true)
        {
            calculo = reglas;
            config = numeros;
            calles = grafo;
            posicionJugador = dondeEstaElJugador;
            posicion = grafo.Nodo(nodoInicial);
            camino.Clear();
            camino.Add(nodoInicial);
            indiceEnCamino = 0;
            nodoMeta = nodoInicial;
            rastro.Clear();
            Retirando = false;
            VeAlJugador = false;
            jugadorAnterior = null;
            velocidadJugador = Vector3.zero;
            // La policía sabe por la radio dónde fue el aviso: ahí va primero.
            ultimaVista = posicionJugador?.Invoke() ?? posicion;
            // T-986: la única vez que se coloca a mano es al aparecer, fuera de la vista; después maneja con la física del carro.
            float alturaSuelo = BuscarSuelo(posicion, posicion.y, posicion.y);
            Vector3 colocacion = new Vector3(posicion.x, alturaSuelo + RaizABajo, posicion.z);
            transform.position = colocacion;
            if (cuerpo != null)
            {
                cuerpo.isKinematic = false;
                cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
                cuerpo.position = colocacion;
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            parametrosConductor = config.ParametrosConductor;
            puntoDeMira = colocacion;
            velocidadDeseada = 0f;
            orientarAlSalir = true;
            segundosVolcada = 0f;
            segundosObstaculoTotal = 0f;
            desvioCarril = 0f;
            if (carro != null)
            {
                conductor = new ConductorCarroIA(carro.ParametrosManejo, parametrosConductor);
                carro.ConducirConIA(PedirEntrada);
            }
            if (conSirena && bucleSirena == Sonidos.SinBucle)
            {
                bucleSirena = Sonidos.ReproducirBucle("CAL_PATRULLA_SIRENA", transform);
            }
        }

        /// <summary>T-933: sale de ronda: circula por las calles sin sirena ni saber dónde está el jugador.</summary>
        public void PrepararRonda(CalculoPersecucion reglas, ConfigPersecucion numeros, GrafoCalles grafo, int nodoInicial, Func<Vector3?> dondeEstaElJugador)
        {
            Preparar(reglas, numeros, grafo, nodoInicial, dondeEstaElJugador, false);
            EnRonda = true;
            azarRonda = new System.Random(GetInstanceID());
            segundosSinCriminal = 0f;
            criminalVisto = null;
            PonerLuces(false, false);
        }

        /// <summary>T-933: empezó la búsqueda: esta patrulla deja la ronda y persigue al jugador.</summary>
        public void Alertar()
        {
            if (!EnRonda)
            {
                return;
            }
            EnRonda = false;
            criminalVisto = null;
            ultimaVista = posicionJugador?.Invoke() ?? posicion;
            if (bucleSirena == Sonidos.SinBucle)
            {
                bucleSirena = Sonidos.ReproducirBucle("CAL_PATRULLA_SIRENA", transform);
            }
        }

        /// <summary>T-587: deja de perseguir y se va por la calle al nodo más lejano del jugador; desaparece cuando ya no se ve.</summary>
        public void Retirar()
        {
            if (Retirando || calles == null || Robada)
            {
                return;
            }
            Retirando = true;
            VaDirecto = false;
            VeAlJugador = false;
            segundosRetirandose = 0f;
            Vector3 referencia = posicionJugador?.Invoke() ?? ultimaVista;
            nodoRetiro = calles.MasLejano(referencia, calles.MasCercano(transform.position)); // T-986: el más lejano al que de verdad puede llegar
            nodoMeta = -1;
            Sonidos.DetenerBucle(bucleSirena);
            bucleSirena = Sonidos.SinBucle;
            PonerLuces(false, false);
        }

        /// <summary>T-692: la patrulla explotó (lo avisa DanioCarro con un mensaje de Unity); deja de manejar, ver y sonar.</summary>
        public bool Destruida { get; private set; }

        private void AlExplotarVehiculo()
        {
            Destruida = true;
            carro?.SoltarIA();
            VeAlJugador = false;
            PonerLuces(false, false);
            enabled = false;
        }

        private void OnEnable()
        {
            ActivasLista.Add(this);
            Registro<Testigos.ITestigoPolicia>.Agregar(this);
        }

        private void OnDisable()
        {
            ActivasLista.Remove(this);
            Registro<Testigos.ITestigoPolicia>.Quitar(this);
            carro?.SoltarIA();
            Sonidos.DetenerBucle(bucleSirena);
            bucleSirena = Sonidos.SinBucle;
        }

        /// <summary>T-661, T-933: true si esta patrulla o alguno de sus policías a pie ve el punto: a menos de <see cref="ConfigPersecucion.RadioVista"/> m y sin pared en medio.</summary>
        public bool VeElPunto(Vector3 punto)
        {
            if (Retirando || config == null || Destruida)
            {
                return false;
            }
            if (DistanciaPlano(transform.position, punto) <= config.RadioVista && Testigos.LineaDeVista.Libre(transform.position + Vector3.up * AlturaOjos, punto))
            {
                return true;
            }
            foreach (GameObject policia in policias)
            {
                if (policia != null && policia.activeInHierarchy && DistanciaPlano(policia.transform.position, punto) <= config.RadioVista
                    && Testigos.LineaDeVista.Libre(policia.transform.position + Vector3.up * AlturaOjosPolicia, punto))
                {
                    return true;
                }
            }
            return false;
        }

        // T-986: el conductor IA pide su entrada al carro en cada paso de física.
        private EntradaCarro PedirEntrada()
        {
            return conductor.Calcular(cuerpo.position, cuerpo.rotation * Vector3.forward, cuerpo.linearVelocity, puntoDeMira, velocidadDeseada, Time.fixedDeltaTime);
        }

        /// <summary>T-986: rapidez actual de la patrulla (m/s).</summary>
        public float Rapidez => cuerpo != null ? cuerpo.linearVelocity.magnitude : 0f;

        /// <summary>T-986: el punto al que mira el conductor y los m/s que quiere llevar (para las pruebas).</summary>
        public Vector3 PuntoDeMira => puntoDeMira;
        public float VelocidadDeseada => velocidadDeseada;

        /// <summary>T-986: false hasta que quedó colocada y mirando hacia donde va (al aparecer, fuera de la vista).</summary>
        public bool Orientada => !orientarAlSalir;

        /// <summary>T-986: true mientras da reversa por haberse atorado.</summary>
        public bool EnReversa => conductor != null && conductor.EnReversa;

        /// <summary>T-986: cuántas veces se atoró y dio reversa.</summary>
        public int Reversas => conductor != null ? conductor.Reversas : 0;

        // T-666: frena si hay otra patrulla delante o un obstáculo cerrando los dos carriles. T-986: el cerebro corre en el paso de física,
        // decide a dónde mirar y qué velocidad llevar, y el conductor lo vuelve volante y pedales del carro; ya no se mueve el transform.
        private void FixedUpdate()
        {
            if (calculo == null || cuerpo == null)
            {
                return;
            }
            float dt = Time.fixedDeltaTime;
            posicion = cuerpo.position;
            if (QuitarSiVolcada(dt))
            {
                return;
            }
            factorVelocidad = Mathf.MoveTowards(factorVelocidad, FactorDeFreno(), RapidezDeFreno * dt);
            velocidadDeseada = 0f;
            puntoDeMira = posicion + cuerpo.rotation * Vector3.forward;
            Conducir(dt);
            AplicarCarril(dt);
            FrenarPorObstaculo(dt);
        }

        // Boca abajo o de lado por unos segundos y sin que nadie la vea: se quita como la que se retira; a la vista se queda.
        private bool QuitarSiVolcada(float dt)
        {
            bool volcada = Vector3.Dot(cuerpo.rotation * Vector3.up, Vector3.up) < MinimoArribaVolcada;
            segundosVolcada = volcada ? segundosVolcada + dt : 0f;
            if (segundosVolcada >= config.SegundosVolcada && !Robada && !ConJugador && !ReglaVistaPolicia.LoVe(Camera.main, posicion, AlcanceCamara))
            {
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        // Un obstáculo que cierra los dos carriles: frena y espera; si sigue ahí avanza muy despacio (si choca, el conductor da reversa).
        private void FrenarPorObstaculo(float dt)
        {
            if (estorboEnMiCarril && !ObstaculoTotal)
            {
                velocidadDeseada = Mathf.Min(velocidadDeseada, parametrosConductor.VelocidadMinimaCurva); // da tiempo a cambiar de carril antes de llegar
            }
            segundosObstaculoTotal = ObstaculoTotal ? segundosObstaculoTotal + dt : 0f;
            if (ObstaculoTotal)
            {
                velocidadDeseada = segundosObstaculoTotal >= parametrosConductor.SegundosParaAtoro ? velocidadDeseada * FactorObstaculoTotal : 0f;
            }
        }

        private void Conducir(float dtReal)
        {
            Vector3? jugador = posicionJugador?.Invoke();
            if (Retirando)
            {
                Retirada(jugador, dtReal, factorVelocidad);
                return;
            }
            if (EnRonda)
            {
                Rondar(dtReal, factorVelocidad * config.FactorVelocidadRonda);
                return;
            }
            Sirena();
            if (!jugador.HasValue)
            {
                return;
            }
            float distanciaReal = DistanciaPlano(posicion, jugador.Value);
            bool libre = CaminoLibre(jugador.Value, distanciaReal, AlturaVista);
            // T-707: con el jugador en una patrulla robada la policía lo ubica siempre (GPS), esté donde esté.
            VeAlJugador = JugadorEnPatrulla || (distanciaReal <= config.RadioVista && CaminoLibre(jugador.Value, distanciaReal, AlturaOjos));
            MedirVelocidad(jugador.Value, dtReal);
            if (VeAlJugador)
            {
                ultimaVista = jugador.Value;
            }
            Vector3 objetivo = ultimaVista;
            float distancia = DistanciaPlano(posicion, objetivo);
            if (VeAlJugador && distancia <= DistanciaMegafono && Time.time >= proximoMegafono)
            {
                proximoMegafono = Time.time + SegundosEntreMegafono;
                Sonidos.ReproducirEn("CAL_PATRULLA_MEGAFONO", transform.position);
            }
            VaDirecto = VeAlJugador && distancia <= DistanciaDirecta && libre;
            if (!PoliciasBajaron && VaDirecto && distancia <= config.DistanciaParaBajarPolicias)
            {
                BajarPolicias();
            }
            if (VaDirecto)
            {
                Directo(objetivo, factorVelocidad);
                return;
            }
            if (rastro.Count > 0 && Regresar(factorVelocidad))
            {
                return;
            }
            Vector3 destino = Intercepta && VeAlJugador
                ? ReglaVistaPolicia.PuntoDeCorte(objetivo, velocidadJugador, config.SegundosAdelantoCorte, config.VelocidadMinimaCorte)
                : objetivo;
            int nuevaMeta = calles.MasCercano(destino);
            if (nuevaMeta != nodoMeta || indiceEnCamino >= camino.Count)
            {
                Replanear(nuevaMeta);
            }
            Avanzar(distancia, factorVelocidad);
        }

        // T-933: la ronda. Va de cruce en cruce al azar; si ve a un criminal atacando va por él, baja a sus policías y, cuando ya no hay criminales a la vista, se va.
        private void Rondar(float dt, float factor)
        {
            if (Time.time >= proximoVistazo)
            {
                proximoVistazo = Time.time + SegundosEntreVistazos;
                criminalVisto = BuscarCriminal();
            }
            if (CriminalVivo())
            {
                segundosSinCriminal = 0f;
                Vector3 donde = criminalVisto.Posicion;
                if (DistanciaPlano(posicion, donde) <= config.DistanciaParaBajarPolicias)
                {
                    BajarPolicias();
                    return;
                }
                Directo(donde, factor);
                return;
            }
            if (PoliciasBajaron)
            {
                segundosSinCriminal += dt;
                if (segundosSinCriminal >= config.SegundosTrasCriminal)
                {
                    Retirar();
                }
                return;
            }
            if (rastro.Count > 0 && Regresar(factor))
            {
                return;
            }
            // Elige el siguiente destino al llegar al último cruce, sin detenerse.
            if (indiceEnCamino >= camino.Count - 1)
            {
                Replanear(azarRonda.Next(calles.Cantidad));
            }
            Avanzar(float.MaxValue, factor);
        }

        private bool CriminalVivo() => criminalVisto is UnityEngine.Object objeto && objeto != null && criminalVisto.EstaAtacando;

        // El criminal que está atacando más cercano que esta patrulla ve; null si no hay.
        private ICriminalCalle BuscarCriminal()
        {
            ICriminalCalle mejor = null;
            float menor = config.RadioVista;
            foreach (ICriminalCalle c in Registro<ICriminalCalle>.Todos)
            {
                if (!(c is UnityEngine.Object objeto) || objeto == null || !c.EstaAtacando)
                {
                    continue;
                }
                float d = DistanciaPlano(posicion, c.Posicion);
                if (d <= menor && Testigos.LineaDeVista.Libre(transform.position + Vector3.up * AlturaOjos, c.Posicion + Vector3.up * AlturaCriminal))
                {
                    menor = d;
                    mejor = c;
                }
            }
            return mejor;
        }

        // Velocidad del jugador suavizada (m/s): con ella las patrullas que cortan el paso saben hacia dónde corre.
        private void MedirVelocidad(Vector3 jugador, float dt)
        {
            if (jugadorAnterior.HasValue && dt > 0f)
            {
                Vector3 instantanea = (jugador - jugadorAnterior.Value) / dt;
                instantanea.y = 0f;
                if (instantanea.magnitude < VelocidadImposible)
                {
                    velocidadJugador = Vector3.Lerp(velocidadJugador, instantanea, 1f - Mathf.Exp(-SuavizadoVelocidad * dt));
                }
            }
            jugadorAnterior = jugador;
        }

        // T-587: de camino a la salida de la ciudad; desaparece solo fuera de la vista de la cámara y lejos del jugador.
        private void Retirada(Vector3? jugador, float dt, float factor)
        {
            segundosRetirandose += dt;
            if (rastro.Count == 0 || !Regresar(factor))
            {
                if (nodoMeta != nodoRetiro)
                {
                    Replanear(nodoRetiro);
                }
                Avanzar(float.MaxValue, factor);
            }
            bool lejos = !jugador.HasValue || DistanciaPlano(posicion, jugador.Value) >= config.DistanciaRetiro;
            bool sinVerse = lejos && !ReglaVistaPolicia.LoVe(Camera.main, posicion, AlcanceCamara);
            if ((sinVerse && segundosRetirandose >= SegundosMinimosDeRetiro) || segundosRetirandose >= config.SegundosMaximosRetiro)
            {
                Destroy(gameObject);
            }
        }

        // De cerca: directo hacia el jugador hasta la distancia de seguimiento. Al alejarse vuelve a la calle por el nodo más cercano.
        private void Directo(Vector3 jugador, float factor)
        {
            Vector3 hacia = jugador - posicion;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            float mira = Mathf.Max(DistanciaMinimaDeMira, ConductorCarroIA.DistanciaMira(parametrosConductor, Rapidez));
            if (distancia > 0.01f)
            {
                Vector3 recta = hacia / distancia;
                Vector3 direccion = Esquivar(recta); // T-666: rodea lo que le tapa el paso
                if (rastro.Count == 0 || Vector3.Distance(rastro[rastro.Count - 1], posicion) >= DistanciaEntreMigas)
                {
                    if (rastro.Count >= MaximoDeMigas)
                    {
                        rastro.RemoveAt(0);
                    }
                    rastro.Add(posicion);
                }
                bool rodea = Vector3.Dot(direccion, recta) < 0.999f;
                puntoDeMira = rodea || distancia > mira ? posicion + direccion * mira : jugador;
                velocidadDeseada = Mathf.Min(calculo.VelocidadDeseada(false, distancia, config.DesaceleracionPlaneada), config.VelocidadPatrulla * factor);
            }
            int cercano = calles.MasCercano(posicion);
            camino.Clear();
            camino.Add(cercano);
            indiceEnCamino = 0;
            nodoMeta = -1;
        }

        // T-581: nada sólido entre la patrulla y el jugador (se queda MargenAlJugador antes de él: si va en un carro, el carro no cuenta).
        private bool CaminoLibre(Vector3 jugador, float distancia, float altura)
        {
            float largo = distancia - MargenAlJugador;
            if (largo <= MargenDelante)
            {
                return true;
            }
            Vector3 origen = posicion + Vector3.up * altura;
            Vector3 destino = new Vector3(jugador.x, origen.y, jugador.z);
            Vector3 direccion = (destino - origen).normalized;
            return RayoLibre(origen, direccion, largo);
        }

        // T-581: de vuelta a la calle por las migas que dejó al ir directo (nunca en línea recta por donde no sabe qué hay).
        // T-986: va hacia la miga más reciente que quede a la distancia de mira; las que ya tiene cerca las da por recorridas. False si ya no quedan.
        private bool Regresar(float factor)
        {
            while (rastro.Count > 0 && DistanciaPlano(posicion, rastro[rastro.Count - 1]) <= config.RadioLlegadaMiga)
            {
                rastro.RemoveAt(rastro.Count - 1);
            }
            if (rastro.Count == 0)
            {
                return false;
            }
            float mira = Mathf.Max(DistanciaMinimaDeMira, ConductorCarroIA.DistanciaMira(parametrosConductor, Rapidez));
            Vector3 destino = rastro[0];
            for (int i = rastro.Count - 1; i >= 0; i--)
            {
                if (DistanciaPlano(posicion, rastro[i]) >= mira)
                {
                    destino = rastro[i];
                    break;
                }
            }
            puntoDeMira = destino;
            velocidadDeseada = config.VelocidadPatrulla * factor;
            int cercano = calles.MasCercano(posicion);
            camino.Clear();
            camino.Add(cercano);
            indiceEnCamino = 0;
            nodoMeta = -1;
            return true;
        }

        // La patrulla puede estar a medio tramo: el camino nuevo parte del nodo al que iba (o del último nodo si ya iba para atrás).
        private void Replanear(int meta)
        {
            int desde = camino[Mathf.Min(indiceEnCamino, camino.Count - 1)];
            List<int> nuevo = calles.Camino(desde, meta);
            if (nuevo.Count == 0)
            {
                return;
            }
            camino.Clear();
            camino.AddRange(nuevo);
            indiceEnCamino = 0;
            nodoMeta = meta;
        }

        // T-986: sigue la ruta del grafo con el carro real: da por pasado un cruce al llegar cerca o al rebasarlo, mira un punto de la ruta por delante
        // (más lejos cuanto más rápido va, corrido al carril) y pide la velocidad que le deja frenar a tiempo en la meta y en las curvas.
        private void Avanzar(float distanciaAlObjetivo, float factor)
        {
            while (indiceEnCamino < camino.Count && NodoPasado(indiceEnCamino))
            {
                indiceEnCamino++;
            }
            if (indiceEnCamino >= camino.Count)
            {
                puntoDeMira = calles.Nodo(camino[camino.Count - 1]);
                return; // llegó a la meta de la ruta: se queda parada
            }
            float aDesacelerar = config.DesaceleracionPlaneada;
            float tope = config.VelocidadPatrulla * factor;
            float mira = Mathf.Max(DistanciaMinimaDeMira, ConductorCarroIA.DistanciaMira(parametrosConductor, Rapidez));
            Vector3 desde = InicioDeTramo();
            puntoDeMira = PuntoSobreLaRuta(desde, mira);
            float velocidad = tope;
            if (distanciaAlObjetivo < float.MaxValue)
            {
                velocidad = Mathf.Min(velocidad, calculo.VelocidadDeseada(false, distanciaAlObjetivo, aDesacelerar));
            }
            // Frena para tomar cada curva a su velocidad y para quedar parada en el último cruce.
            float acumulado = DistanciaPlano(desde, calles.Nodo(camino[indiceEnCamino]));
            float alcanceFreno = tope * tope / (2f * aDesacelerar) + config.RadioLlegadaNodo;
            for (int k = indiceEnCamino; k < camino.Count; k++)
            {
                float antes = Mathf.Max(0f, acumulado - config.RadioLlegadaNodo);
                if (k == camino.Count - 1)
                {
                    velocidad = Mathf.Min(velocidad, ConductorCarroIA.VelocidadParaFrenar(antes, 0f, aDesacelerar));
                    break;
                }
                Vector3 origen = k > 0 ? calles.Nodo(camino[k - 1]) : desde;
                Vector3 entra = calles.Nodo(camino[k]) - (k > indiceEnCamino || indiceEnCamino > 0 ? origen : desde);
                Vector3 sale = calles.Nodo(camino[k + 1]) - calles.Nodo(camino[k]);
                entra.y = 0f;
                sale.y = 0f;
                float angulo = entra.sqrMagnitude > 0.01f && sale.sqrMagnitude > 0.01f ? Vector3.Angle(entra, sale) : 0f;
                float enCurva = ConductorCarroIA.VelocidadEnCurva(parametrosConductor, tope, angulo);
                velocidad = Mathf.Min(velocidad, ConductorCarroIA.VelocidadParaFrenar(antes, enCurva, aDesacelerar));
                acumulado += DistanciaPlano(calles.Nodo(camino[k]), calles.Nodo(camino[k + 1]));
                if (acumulado > alcanceFreno)
                {
                    break;
                }
            }
            velocidadDeseada = velocidad;
            if (orientarAlSalir)
            {
                OrientarAlSalir();
            }
        }

        // Al aparecer (fuera de la vista) queda mirando hacia donde va la ruta, para no tener que dar la vuelta con el volante.
        private void OrientarAlSalir()
        {
            orientarAlSalir = false;
            Vector3 hacia = puntoDeMira - posicion;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 1f && !ReglaVistaPolicia.LoVe(Camera.main, posicion, AlcanceCamara))
            {
                Quaternion giro = Quaternion.LookRotation(hacia, Vector3.up);
                cuerpo.rotation = giro;
                transform.rotation = giro;
            }
        }

        // Un cruce cuenta como pasado al llegar a RadioLlegadaNodo de él o al rebasarlo a lo largo del tramo que llegaba a él.
        private bool NodoPasado(int i)
        {
            Vector3 nodo = calles.Nodo(camino[i]);
            Vector3 aEl = nodo - posicion;
            aEl.y = 0f;
            if (aEl.magnitude <= config.RadioLlegadaNodo)
            {
                return true;
            }
            if (i == 0)
            {
                return false;
            }
            Vector3 tramo = nodo - calles.Nodo(camino[i - 1]);
            tramo.y = 0f;
            return Vector3.Dot(-aEl, tramo) > 0f;
        }

        // Donde la patrulla está sobre el eje de su tramo (la proyección de su posición); sin tramo anterior, donde está.
        private Vector3 InicioDeTramo()
        {
            if (indiceEnCamino <= 0)
            {
                return posicion;
            }
            Vector3 a = calles.Nodo(camino[indiceEnCamino - 1]);
            Vector3 b = calles.Nodo(camino[indiceEnCamino]);
            Vector3 ab = b - a;
            ab.y = 0f;
            float largo2 = ab.sqrMagnitude;
            if (largo2 < 0.01f)
            {
                return posicion;
            }
            Vector3 ap = posicion - a;
            ap.y = 0f;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / largo2);
            Vector3 punto = a + ab * t;
            punto.y = posicion.y;
            return punto;
        }

        // El punto a "mira" metros por la ruta desde "desde", con el desvío del carril sobre la derecha de ese tramo. Pasado el último cruce sigue recto.
        private Vector3 PuntoSobreLaRuta(Vector3 desde, float mira)
        {
            Vector3 actual = desde;
            Vector3 rumbo = cuerpo.rotation * Vector3.forward;
            rumbo.y = 0f;
            rumbo.Normalize();
            float restante = mira;
            for (int i = indiceEnCamino; i < camino.Count; i++)
            {
                Vector3 nodo = calles.Nodo(camino[i]);
                nodo.y = actual.y;
                Vector3 tramo = nodo - actual;
                float largo = tramo.magnitude;
                if (largo > 0.01f)
                {
                    rumbo = tramo / largo;
                }
                if (largo >= restante)
                {
                    break;
                }
                restante -= largo;
                actual = nodo;
            }
            return actual + rumbo * restante + Vector3.Cross(Vector3.up, rumbo) * desvioCarril;
        }

        // ---- T-666: conducción más natural ----

        // Va por su carril (a la derecha de la calle), lo cambia al otro lado si algo le tapa el carril y no se encima con otras patrullas.
        private void AplicarCarril(float dt)
        {
            bool enCalle = !VaDirecto && rastro.Count == 0;
            float meta = enCalle ? ElegirCarril() : 0f;
            if (!enCalle)
            {
                ObstaculoTotal = false;
                estorboEnMiCarril = false;
            }
            desvioCarril = Mathf.MoveTowards(desvioCarril, meta, VelocidadCarril * dt);
        }

        /// <summary>Metros que se separa de la línea central de la calle: positivo a la derecha, negativo a la izquierda.</summary>
        public float DesvioCarril => desvioCarril;

        /// <summary>True si algo tapa los dos carriles y la patrulla frena.</summary>
        public bool ObstaculoTotal { get; private set; }

        private float ElegirCarril()
        {
            Vector3 adelante = transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.01f)
            {
                return AnchoCarril;
            }
            adelante.Normalize();
            Vector3 derecha = transform.right;
            Vector3 eje = posicion - derecha * desvioCarril; // T-986: la patrulla ya va corrida; los dos carriles se miden desde el eje de la calle
            estorboEnMiCarril = HayObstaculo(eje + derecha * desvioCarril, adelante, DistanciaObstaculo);
            // El carril de la derecha, el de la izquierda y, si algo los tapa, un poco más hacia cada lado de la calle; si no hay paso, frena.
            foreach (float carril in CarrilesPosibles)
            {
                if (!HayObstaculo(eje + derecha * carril, adelante, DistanciaObstaculo))
                {
                    ObstaculoTotal = false;
                    return carril;
                }
            }
            ObstaculoTotal = true;
            return AnchoCarril;
        }

        // Un cilindro de RadioObstaculo m hacia delante; no cuentan los triggers, el jugador, los peatones ni las patrullas.
        private bool HayObstaculo(Vector3 desde, Vector3 direccion, float distancia)
        {
            Vector3 origen = desde + Vector3.up * AlturaObstaculo;
            int cuantos = Physics.SphereCastNonAlloc(origen, RadioObstaculo, direccion, impactos, distancia, ~0, QueryTriggerInteraction.Ignore);
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raizJugador = jugador != null ? jugador.Raiz : null;
            for (int i = 0; i < cuantos; i++)
            {
                Collider c = impactos[i].collider;
                if (c.GetComponentInParent<PatrullaMundo>() != null || c.GetComponentInParent<Peatones.PeatonMundo>() != null
                    || (raizJugador != null && c.transform.IsChildOf(raizJugador)))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        // De cerca y fuera de la calle: si algo le tapa la línea al jugador, prueba hacia los lados hasta encontrar paso.
        private Vector3 Esquivar(Vector3 direccion)
        {
            if (!HayObstaculo(posicion, direccion, DistanciaObstaculo))
            {
                return direccion;
            }
            foreach (float grados in GradosParaEsquivar)
            {
                Vector3 candidata = Quaternion.AngleAxis(grados, Vector3.up) * direccion;
                if (!HayObstaculo(posicion, candidata, DistanciaObstaculo))
                {
                    return candidata;
                }
            }
            return direccion;
        }

        // 1 si puede ir a toda velocidad; menos si hay otra patrulla delante (la de atrás frena y se queda a DistanciaMinimaEntrePatrullas)
        // o, en un cruce de caminos, la que tiene menos prioridad cede; 0.15 si un obstáculo le cierra los dos carriles.
        private float FactorDeFreno()
        {
            float factor = 1f;
            Vector3 adelante = transform.forward;
            adelante.y = 0f;
            for (int i = 0; i < ActivasLista.Count; i++)
            {
                PatrullaMundo otra = ActivasLista[i];
                if (otra == null || otra == this)
                {
                    continue;
                }
                Vector3 d = otra.transform.position - transform.position;
                d.y = 0f;
                float distancia = d.magnitude;
                if (distancia > DistanciaFrenoPatrullas)
                {
                    continue;
                }
                Vector3 haciaOtra = distancia > 0.001f ? d / distancia : adelante;
                bool delante = Vector3.Dot(haciaOtra, adelante) > 0.2f;
                float cruce = Vector3.Dot(adelante, otra.transform.forward);
                bool mismoSentido = cruce > 0.3f;
                bool tieneLaPrioridad = otra.GetInstanceID() < GetInstanceID();
                float lado = Mathf.Abs(Vector3.Dot(d, transform.right));
                bool enMiCamino = delante && lado < AnchoDePasoPatrullas;
                bool cruceDeCaminos = Mathf.Abs(cruce) <= 0.3f;
                bool cede = (enMiCamino && mismoSentido) || (cruceDeCaminos && distancia < DistanciaMinimaEntrePatrullas * 1.6f && tieneLaPrioridad);
                if (cede)
                {
                    float t = Mathf.Clamp01((distancia - DistanciaMinimaEntrePatrullas) / (DistanciaFrenoPatrullas - DistanciaMinimaEntrePatrullas));
                    factor = Mathf.Min(factor, t);
                }
            }
            return factor;
        }

        private void Sirena()
        {
            bool rojaPrendida = Mathf.FloorToInt(Time.time / config.PeriodoSirena) % 2 == 0;
            PonerLuces(rojaPrendida, !rojaPrendida);
        }

        private void PonerLuces(bool roja, bool azul)
        {
            if (luzRoja != null)
            {
                luzRoja.enabled = roja;
            }
            if (luzAzul != null)
            {
                luzAzul.enabled = azul;
            }
        }

        private static float DistanciaPlano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
