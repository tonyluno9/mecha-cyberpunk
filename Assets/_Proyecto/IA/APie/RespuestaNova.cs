using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1304: con 4 estrellas de búsqueda llega Nova: guardias robot armados y un guardia pesado aparecen a 22-34 m del jugador (en el piso), van por él y lo
    /// atacan; se reponen por oleadas. Con 5 estrellas son más y llega también el jefe final (una vez por búsqueda). Bajo 4 estrellas unos segundos, Nova se
    /// retira. Arranca sola en cada partida (si existe IA/Resources/ConfigRespuestaNova) y solo actúa donde hay policía (IZonaBusqueda registrada).
    /// </summary>
    public class RespuestaNova : MonoBehaviour
    {
        private const float SegundosRevisar = 0.5f;
        private const int Intentos = 24;
        private const float AlturaSonda = 1.6f;
        private const float MismoPiso = 1.5f;
        private const float CaidaMaxima = 40f;
        private const float RadioCapsula = 0.6f; // T-1497: el robot pesado (3.14 m desde T-1552; el radio no baja por el margen de los pasillos)
        private const float GravedadAgente = 20f; // la misma de PandilleroAPie (m/s²)
        /// <summary>T-1552: lo que miden el robot pesado E16 y el jefe final E14 (3.14 m, EscalasNova) más un margen (m).</summary>
        public const float AlturaAgente = MechaCyberpunk.Personaje.Visual.EscalasNova.AlturaRobotPesadoE16 + 0.3f;
        private const float MargenCaida = 0.5f;
        private const float AlturaMinimaCaida = 3f; // con menos de esto encima no vale la pena caer: aparecen en el piso
        private const float AlturaDestelloSalida = 1f;

        /// <summary>Las pruebas que no son de Nova pueden apagarla para que no aparezca nadie en sus escenas.</summary>
        public static bool Apagada { get; set; }
        public static RespuestaNova Actual { get; private set; }

        private ConfigRespuestaNova config;
        private readonly List<PandilleroAPie> guardias = new List<PandilleroAPie>();
        private readonly List<PandilleroAPie> pesados = new List<PandilleroAPie>();
        private readonly List<PandilleroAPie> perros = new List<PandilleroAPie>();
        private readonly List<PandilleroAPie> deNave = new List<PandilleroAPie>();
        private NaveNova nave;
        private bool naveLlamada;
        private bool enNova;
        private float proximaNave;
        private int navesLanzadas;
        private PandilleroAPie jefe;
        private bool jefeSalio;
        private float proximaRevision;
        private float proximaOleada;
        private float segundosSinNova;
        private bool anunciada;
        private readonly HashSet<PandilleroAPie> vistos = new HashSet<PandilleroAPie>();

        /// <summary>T-1323: la búsqueda llegó al tope (5 estrellas) y suena el aviso especial; Nova baja del cielo.</summary>
        public event System.Action LlegadaEspecial;

        public int Estrellas { get; private set; }
        public int GuardiasVivos => Vivos(guardias);
        public int PesadosVivos => Vivos(pesados);
        public bool HayJefe => jefe != null && !jefe.EstaKO;
        public PandilleroAPie Jefe => jefe;
        public IReadOnlyList<PandilleroAPie> Guardias => guardias;
        public IReadOnlyList<PandilleroAPie> Pesados => pesados;
        /// <summary>T-1389b: perros robot de Nova (E17) en juego.</summary>
        public IReadOnlyList<PandilleroAPie> Perros => perros;
        public int PerrosVivos => Vivos(perros);
        /// <summary>T-1498: soldados que bajaron de una nave de Nova (aparte de los guardias de las oleadas).</summary>
        public IReadOnlyList<PandilleroAPie> DeNave => deNave;
        public int DeNaveVivos => Vivos(deNave);
        /// <summary>T-1498: la nave que está en escena ahora (null si ninguna).</summary>
        public NaveNova Nave => nave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico()
        {
            Apagada = false;
            Actual = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            if (Actual != null)
            {
                return;
            }
            ConfigRespuestaNova cfg = Resources.Load<ConfigRespuestaNova>(ConfigRespuestaNova.Recurso);
            if (cfg == null)
            {
                return;
            }
            GameObject objeto = new GameObject("RespuestaNova");
            DontDestroyOnLoad(objeto);
            objeto.AddComponent<RespuestaNova>().Configurar(cfg);
        }

        private readonly List<GameObject> humanoides = new List<GameObject>();
        private readonly List<GameObject> medianos = new List<GameObject>();

        public void Configurar(ConfigRespuestaNova cfg)
        {
            config = cfg;
            humanoides.Clear();
            medianos.Clear();
            if (cfg == null)
            {
                return;
            }
            // T-1620: los guardias se reparten por la jerarquía: centinelas E15 son humanoides (nivel 4), la élite E18 es un robot mediano (nivel 5).
            foreach (GameObject g in cfg.TodosLosGuardias())
            {
                (EsRobotMediano(g) ? medianos : humanoides).Add(g);
            }
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.NovaHumanoide, humanoides.Count > 0);
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.RobotMediano, medianos.Count > 0);
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.Perro, cfg.Perros.Count > 0);
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.RobotGrande, cfg.TodosLosPesados().Count > 0);
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.Jefe, cfg.JefeFinal != null);
            EventosRespuesta.CapsulaDron = LanzarCapsulaDron; // T-1631: los drones de Nova de la persecución (Trafico) llegan en cápsula
        }

        private static bool EsRobotMediano(GameObject prefab) => prefab != null && (prefab.name.Contains("E18") || prefab.name.Contains("Elite"));

        private void OnEnable()
        {
            Actual = this;
            EventosJugador.Desperto += RetirarTodos; // T-1328: al despertar con el Doc, Nova se va
        }

        private void OnDisable()
        {
            EventosJugador.Desperto -= RetirarTodos;
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void OnDestroy()
        {
            if (EventosRespuesta.CapsulaDron == (EventosRespuesta.LanzadorCapsulaDron)LanzarCapsulaDron)
            {
                EventosRespuesta.CapsulaDron = null;
            }
            RetirarTodos();
        }

        // T-1631: cápsula CP01 con un dron dentro; la escala sale de la tabla por el nombre del modelo (con 0.3 si no se reconoce).
        private static bool LanzarCapsulaDron(GameObject dron, string nombre, Vector3 punto, Quaternion rotacion, System.Action<GameObject> alTerminar)
        {
            float escala = CalculoCapsulaCaida.TipoDronDeNombre(nombre, out TipoCapsulaCaida tipo) || CalculoCapsulaCaida.TipoDronDeNombre("Dron_" + nombre, out tipo)
                ? CalculoCapsulaCaida.EscalaDe(tipo) : DronSinTipoEscala;
            return CapsulaCaida.LanzarDron(dron, punto, rotacion, escala, alTerminar) != null;
        }

        private const float DronSinTipoEscala = 0.3f;

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + SegundosRevisar;
            IJugador jugador = Registro<IJugador>.Actual;
            bool novaEnMision = MisionProtegida.Activa && MisionProtegida.NovaPermitida; // T-1620: en la misión de NOVA, NOVA sí responde
            bool hayPolicia = Registro<IZonaBusqueda>.Actual != null || novaEnMision;
            int estrellasCalor = EstrellasBusqueda.De(Registro<ICalor>.Actual);
            DirectorRespuesta.Avanzar(Time.time, estrellasCalor); // T-1620: el director cuenta bajas y desbloquea niveles
            if (DirectorRespuesta.ModoNova)
            {
                RetirarPoliciaAPie(); // T-1631: NOVA INTERVIENE: la policía a pie se aleja y desaparece solo fuera de la vista
            }
            if (novaEnMision)
            {
                estrellasCalor = Mathf.Max(estrellasCalor, DirectorRespuesta.Parametros.EstrellasNovaEnMision);
            }
            else if (MisionProtegida.Activa)
            {
                estrellasCalor = 0;
            }
            Estrellas = config == null || Apagada || !hayPolicia ? 0 : estrellasCalor;
            guardias.RemoveAll(p => p == null);
            pesados.RemoveAll(p => p == null);
            perros.RemoveAll(p => p == null);
            deNave.RemoveAll(p => p == null);
            if (jugador == null || jugador.Raiz == null || !EstrellasBusqueda.EsNova(Estrellas))
            {
                anunciada = false;
                enNova = false;
                segundosSinNova += SegundosRevisar;
                if (Apagada || config == null || Estrellas <= 0 || jugador == null || jugador.Raiz == null)
                {
                    // Sin búsqueda (el Doc, un arresto, otra escena): se van todos, hayan visto al jugador o no.
                    if (Apagada || config == null || segundosSinNova >= (config != null ? config.SegundosParaRetirarse : 0f))
                    {
                        RetirarTodos();
                    }
                    return;
                }
                // T-1323: con 1 a 3 estrellas se retiran los que nunca vieron al jugador; los que ya lo vieron lo siguen persiguiendo.
                if (segundosSinNova >= config.SegundosParaRetirarse)
                {
                    RetirarNoVistos();
                }
                Vector3 aqui = jugador.Raiz.position;
                MarcarVistos(aqui);
                Azuzar(aqui);
                return;
            }
            segundosSinNova = 0f;
            Vector3 donde = jugador.Raiz.position;
            if (Estrellas >= EstrellasBusqueda.Total && !anunciada)
            {
                // T-1323: al llegar al tope suena el aviso y Nova baja del cielo (la oleada de abajo).
                anunciada = true;
                Sonidos.Reproducir(config.SonidoLlegada);
                LlegadaEspecial?.Invoke();
                proximaOleada = Time.time; // la oleada baja en ese momento, no a la siguiente vuelta
            }
            GenerarGradual(donde); // T-1620: una unidad a la vez, en orden de jerarquía, con los cooldowns y topes del director
            if (Estrellas >= EstrellasBusqueda.Total && !jefeSalio && config.JefeFinal != null && DirectorRespuesta.PuedeGenerar(TipoRespuesta.Jefe))
            {
                jefe = Aparecer(config.JefeFinal, donde, "Nova_JefeFinal");
                jefeSalio = jefe != null;
                if (jefe != null)
                {
                    RegistrarNova(TipoRespuesta.Jefe, jefe);
                }
            }
            LlevarNave(donde);
            MarcarVistos(donde);
            Azuzar(donde);
        }

        // T-1631: NOVA INTERVIENE: cada policía a pie vivo recibe la orden de retirarse (una vez); se aleja y solo desaparece fuera de la vista (ConductaRetiradaPolicia).
        private static readonly List<PandilleroAPie> sinOrden = new List<PandilleroAPie>();

        private void RetirarPoliciaAPie()
        {
            sinOrden.Clear();
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p != null && p.Datos != null && p.Datos.EsPolicia && !p.EstaKO && p.GetComponent<ConductaRetiradaPolicia>() == null)
                {
                    sinOrden.Add(p);
                }
            }
            foreach (PandilleroAPie p in sinOrden)
            {
                ConductaRetiradaPolicia.Ordenar(p);
            }
            sinOrden.Clear();
        }

        // T-1498: con Nova en la ciudad llega una nave a la vez, a intervalos; cuando se va pasa el tiempo de espera y llega otra.
        private void LlevarNave(Vector3 donde)
        {
            if (config.Naves.Count == 0)
            {
                return;
            }
            if (!enNova)
            {
                enNova = true;
                proximaNave = Time.time + config.SegundosPrimeraNave;
            }
            if (naveLlamada && nave == null)
            {
                naveLlamada = false;
                proximaNave = Time.time + config.SegundosEntreNaves;
            }
            if (nave != null || Time.time < proximaNave || !DirectorRespuesta.PuedeGenerar(TipoRespuesta.NovaHumanoide))
            {
                return;
            }
            naveLlamada = LlamarNave(donde, Estrellas) != null;
            if (!naveLlamada)
            {
                proximaNave = Time.time + SegundosReintentoNave;
            }
        }

        private const float SegundosReintentoNave = 3f;
        private const float AltoDeBusquedaNave = 60f;
        private const float DistanciaNaveMin = 14f;
        private const float DistanciaNaveMax = 26f;
        private const float MismoPisoNave = 2f;
        private const float RadioFilaSoldados = 2f;
        private const float SeparacionRoce = 0.3f;
        private const float AlturaLlegadaNave = 15f;
        private const float AlturaSalidaNave = 25f;
        private const float GradosVariosSalida = 60f;
        private const float DescensoMaximo = 80f;
        private const float SobrePiso = 0.05f;
        private const float GradosEntreSoldados = 90f;

        /// <summary>
        /// T-1498: busca un punto libre en el piso (sin edificios ni techos encima) cerca del jugador y hace llegar una nave a flotar ahí con soldados de Nova.
        /// Devuelve la nave, o null si no hay naves configuradas, ya hay una, no toca nave con estas estrellas o no hay punto libre.
        /// </summary>
        public NaveNova LlamarNave(Vector3 jugador, int estrellas)
        {
            if (config == null || nave != null || config.Naves.Count == 0)
            {
                return null;
            }
            int cantidad = Mathf.Min(config.SoldadosNave(estrellas), DirectorRespuesta.Espacio(TipoRespuesta.NovaHumanoide)); // T-1620: nunca sobre el tope
            if (cantidad <= 0 || config.TodosLosGuardias().Count == 0 || !BuscarPuntoNave(jugador, out Vector3 suelo, out float altura))
            {
                return null;
            }
            GameObject modelo = config.Naves[navesLanzadas % config.Naves.Count];
            navesLanzadas++;
            Vector3 flote = suelo + Vector3.up * altura;
            Vector3 rumbo = flote - jugador;
            Vector3 rumboSalida = Quaternion.Euler(0f, Random.Range(-GradosVariosSalida, GradosVariosSalida), 0f) * rumbo;
            Vector3 desde = ReglaNaveNova.PuntoLejano(flote, rumbo, config.DistanciaLlegadaNave, AlturaLlegadaNave);
            Vector3 salida = ReglaNaveNova.PuntoLejano(flote, -rumboSalida, config.DistanciaLlegadaNave, AlturaSalidaNave);
            nave = NaveNova.Crear(modelo, desde, flote, salida, config.VelocidadNave, cantidad, config.SegundosEntreSoldados,
                ReglaNaveNova.SegundosDescenso(altura, config.VelocidadDescenso));
            nave.Soltar += AlSoltarSoldado;
            return nave;
        }

        /// <summary>T-1498: un punto del piso a 14-26 m del jugador, a su nivel, con la columna de aire libre que necesita la nave (raycast al piso desde arriba, esfera hacia arriba).</summary>
        public bool BuscarPuntoNave(Vector3 jugador, out Vector3 suelo, out float altura)
        {
            float radio = config.RadioLibreNave;
            for (int i = 0; i < Intentos; i++)
            {
                float angulo = Random.Range(0f, Mathf.PI * 2f);
                float d = Random.Range(DistanciaNaveMin, DistanciaNaveMax);
                Vector3 arriba = new Vector3(jugador.x + Mathf.Cos(angulo) * d, jugador.y + AltoDeBusquedaNave, jugador.z + Mathf.Sin(angulo) * d);
                if (!Physics.Raycast(arriba, Vector3.down, out RaycastHit golpe, AltoDeBusquedaNave * 2f, ~0, QueryTriggerInteraction.Ignore)
                    || golpe.normal.y < 0.7f || Mathf.Abs(golpe.point.y - jugador.y) > MismoPisoNave)
                {
                    continue;
                }
                Vector3 centro = golpe.point + Vector3.up * (radio + SeparacionRoce);
                if (Physics.CheckSphere(centro, radio, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                float libre = Physics.SphereCast(centro, radio, Vector3.up, out RaycastHit techo, AltoDeBusquedaNave, ~0, QueryTriggerInteraction.Ignore)
                    ? techo.distance + radio + SeparacionRoce : AltoDeBusquedaNave;
                float flote = ReglaNaveNova.AlturaDeFlote(libre, config.AlturaFlote, config.AlturaMinimaFlote, config.MargenAireNave);
                if (flote < 0f)
                {
                    continue;
                }
                suelo = golpe.point;
                altura = flote;
                return true;
            }
            suelo = jugador;
            altura = 0f;
            return false;
        }

        // T-1498: baja un soldado por la cuerda, a un costado del anterior; ataca al jugador al tocar el piso.
        private void AlSoltarSoldado(NaveNova quien, int numero)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            List<GameObject> prefabs = config.TodosLosGuardias();
            if (jugador == null || jugador.Raiz == null || prefabs.Count == 0)
            {
                return;
            }
            float angulo = numero * GradosEntreSoldados * Mathf.Deg2Rad;
            Vector3 lado = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * (numero == 0 ? 0f : RadioFilaSoldados);
            Vector3 anclaLocal = quien.AnclaLocal + quien.transform.InverseTransformVector(lado);
            Vector3 anclaMundo = quien.transform.TransformPoint(anclaLocal);
            bool hayPiso = Physics.Raycast(anclaMundo, Vector3.down, out RaycastHit golpe, DescensoMaximo, ~0, QueryTriggerInteraction.Ignore)
                && !golpe.collider.transform.IsChildOf(quien.transform) && golpe.normal.y >= 0.7f;
            Vector3 suelo = hayPiso ? golpe.point + Vector3.up * SobrePiso : new Vector3(anclaMundo.x, quien.PuntoFlote.y - config.AlturaFlote + SobrePiso, anclaMundo.z);
            suelo = HuecoLibreNpc.Buscar(suelo - Vector3.up * SobrePiso) + Vector3.up * SobrePiso; // T40: no baja encima de un cuerpo
            Vector3 mira = jugador.Raiz.position - suelo;
            mira.y = 0f;
            Quaternion giro = mira.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mira) : Quaternion.identity;
            GameObject objeto = Instantiate(prefabs[deNave.Count % prefabs.Count], anclaMundo, giro);
            objeto.name = "Nova_Soldado_" + (deNave.Count + 1);
            PandilleroAPie p = objeto.GetComponent<PandilleroAPie>();
            if (p == null)
            {
                Destroy(objeto);
                return;
            }
            objeto.AddComponent<ZonaCabezaNova>();
            Azuzar(p, jugador.Raiz.position);
            deNave.Add(p);
            RegistrarNova(TipoRespuesta.NovaHumanoide, p);
            DescensoNova.Iniciar(p, quien.transform, anclaLocal, suelo, config.VelocidadDescenso, config.MaterialCuerda);
        }

        /// <summary>T-1323: marca como "ya te vio" a los agentes de Nova en pie que están a <see cref="ConfigRespuestaNova.DistanciaVista"/> o menos del jugador.</summary>
        private void MarcarVistos(Vector3 donde)
        {
            float vista = config.DistanciaVista;
            MarcarVistos(guardias, donde, vista);
            MarcarVistos(pesados, donde, vista);
            MarcarVistos(perros, donde, vista);
            MarcarVistos(deNave, donde, vista);
            if (jefe != null && !jefe.EstaKO)
            {
                MarcarVisto(jefe, donde, vista);
            }
        }

        private void MarcarVistos(List<PandilleroAPie> lista, Vector3 donde, float vista)
        {
            foreach (PandilleroAPie p in lista)
            {
                MarcarVisto(p, donde, vista);
            }
        }

        private void MarcarVisto(PandilleroAPie p, Vector3 donde, float vista)
        {
            if (p == null || p.EstaKO || vistos.Contains(p))
            {
                return;
            }
            Vector3 d = p.transform.position - donde;
            d.y = 0f;
            if (d.sqrMagnitude <= vista * vista)
            {
                vistos.Add(p);
            }
        }

        /// <summary>True si este agente de Nova ya vio al jugador (y lo sigue persiguiendo aunque baje la búsqueda).</summary>
        public bool YaVioAlJugador(PandilleroAPie p) => vistos.Contains(p);

        // Todos van por el jugador: su punto (la correa) es donde está el jugador y están en alerta; el pesado y el jefe, además, en modo pelea.
        private void Azuzar(Vector3 donde)
        {
            foreach (PandilleroAPie p in guardias)
            {
                Azuzar(p, donde);
            }
            foreach (PandilleroAPie p in perros)
            {
                Azuzar(p, donde);
            }
            foreach (PandilleroAPie p in deNave)
            {
                Azuzar(p, donde);
            }
            foreach (PandilleroAPie p in pesados)
            {
                if (p == null || p.EstaKO)
                {
                    continue;
                }
                Azuzar(p, donde);
                ConductaGuardiaPesado pesado = p.GetComponent<ConductaGuardiaPesado>();
                if (pesado != null)
                {
                    pesado.FijarPuesto(donde, donde - p.transform.position);
                    pesado.Provocar();
                }
            }
            if (jefe != null && !jefe.EstaKO)
            {
                Azuzar(jefe, donde);
                ConductaJefeFinalNova conducta = jefe.GetComponent<ConductaJefeFinalNova>();
                if (conducta != null)
                {
                    conducta.Activar();
                }
            }
        }

        private static void Azuzar(PandilleroAPie p, Vector3 donde)
        {
            if (p == null || p.EstaKO)
            {
                return;
            }
            p.Configurar(donde, null);
            p.Alerta = true;
        }

        // T-1620: un solo agente por vuelta; el director decide si toca (nivel desbloqueado, cooldown de reposición, topes por tipo y totales).
        private void GenerarGradual(Vector3 donde)
        {
            if (Intentar(TipoRespuesta.NovaHumanoide, guardias, humanoides, donde, "Nova_Guardia_")
                || Intentar(TipoRespuesta.RobotMediano, guardias, medianos, donde, "Nova_Elite_")
                || Intentar(TipoRespuesta.Perro, perros, config.Perros, donde, "Nova_Perro_"))
            {
                return;
            }
            Intentar(TipoRespuesta.RobotGrande, pesados, config.TodosLosPesados(), donde, "Nova_GuardiaPesado_");
        }

        private bool Intentar(TipoRespuesta tipo, List<PandilleroAPie> lista, IReadOnlyList<GameObject> prefabs, Vector3 donde, string nombre)
        {
            if (prefabs == null || prefabs.Count == 0 || !DirectorRespuesta.PuedeGenerar(tipo))
            {
                return false;
            }
            GameObject prefab = prefabs[lista.Count % prefabs.Count];
            PandilleroAPie nuevo = Aparecer(prefab, donde, nombre + (lista.Count + 1));
            if (nuevo == null)
            {
                return false;
            }
            lista.Add(nuevo);
            RegistrarNova(tipo, nuevo);
            return true;
        }

        private static void RegistrarNova(TipoRespuesta tipo, PandilleroAPie agente)
        {
            DirectorRespuesta.Registrar(tipo, agente.gameObject, () =>
                agente == null ? EstadoUnidad.Retirada : agente.EstaKO ? EstadoUnidad.Muerta : EstadoUnidad.Viva);
        }

        // T-1389b: con varias variantes salen por turnos (la variante la da cuántos han salido ya de esa lista).
        private void Reponer(List<PandilleroAPie> lista, IReadOnlyList<GameObject> prefabs, int cantidad, Vector3 donde, string nombre)
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                return;
            }
            for (int i = Vivos(lista); i < cantidad; i++)
            {
                GameObject prefab = prefabs[lista.Count % prefabs.Count];
                PandilleroAPie nuevo = Aparecer(prefab, donde, nombre + (lista.Count + 1));
                if (nuevo == null)
                {
                    return;
                }
                lista.Add(nuevo);
            }
        }

        private PandilleroAPie Aparecer(GameObject prefab, Vector3 jugador, string nombre)
        {
            if (!BuscarPunto(jugador, out Vector3 punto))
            {
                return null;
            }
            Vector3 mira = jugador - punto;
            mira.y = 0f;
            Quaternion giro = mira.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mira) : Quaternion.identity;
            float altura = AlturaLibreArriba(punto);
            Vector3 inicio = punto + Vector3.up * altura;
            GameObject objeto = Instantiate(prefab, inicio, giro);
            objeto.name = nombre;
            PandilleroAPie p = objeto.GetComponent<PandilleroAPie>();
            if (p == null)
            {
                Destroy(objeto);
                return null;
            }
            if (prefab == config.GuardiaPesado && !Mathf.Approximately(config.EscalaPesado, 1f))
            {
                // T-1323: el pesado se veía más chico que los otros agentes de Nova.
                Transform modelo = objeto.transform.Find("Modelo");
                if (modelo != null)
                {
                    modelo.localScale *= config.EscalaPesado;
                }
            }
            objeto.AddComponent<ZonaCabezaNova>();
            Azuzar(p, jugador);
            // T-1323: llegan cayendo (la gravedad del agente y su animación de caída hacen el resto) con un destello al salir y otro al aterrizar.
            if (altura > 0f)
            {
                // T-1506: la caída especial (aire, aterrizaje, levantarse); aterriza sola cuando el agente toca el piso.
                // T-1621: E15, E16, E17 y E18 llegan en la cápsula CP01 (devuelve null): la cápsula pone su propio impacto, sin los destellos de la caída vieja.
                if (CaidaNova.Iniciar(objeto, true) != null)
                {
                    DestelloLlegadaNova.Lanzar(inicio + Vector3.up * AlturaDestelloSalida, config, 0f);
                    DestelloLlegadaNova.Lanzar(punto, config, SegundosDeCaida(altura));
                }
            }
            else
            {
                DestelloLlegadaNova.Lanzar(inicio + Vector3.up * AlturaDestelloSalida, config, 0f);
            }
            return p;
        }

        /// <summary>Cuánto se puede subir desde <paramref name="punto"/> (hasta <see cref="ConfigRespuestaNova.AlturaCaida"/>) sin topar con un techo: desde ahí caen.</summary>
        private float AlturaLibreArriba(Vector3 punto)
        {
            float maxima = config.AlturaCaida;
            if (maxima <= 0f)
            {
                return 0f;
            }
            float libre = maxima;
            if (Physics.Raycast(punto + Vector3.up * AlturaSonda, Vector3.up, out RaycastHit techo, maxima + MargenCaida, ~0, QueryTriggerInteraction.Ignore))
            {
                libre = Mathf.Max(0f, techo.distance + AlturaSonda - AlturaAgente);
            }
            return Mathf.Min(maxima, libre) >= AlturaMinimaCaida ? Mathf.Min(maxima, libre) : 0f;
        }

        /// <summary>Segundos que tarda en caer <paramref name="altura"/> metros con la gravedad de <see cref="PandilleroAPie"/>.</summary>
        public static float SegundosDeCaida(float altura) => Mathf.Sqrt(2f * altura / GravedadAgente);

        /// <summary>Un punto libre en el piso a 22-34 m del jugador: primero a su misma altura (su calle o su techo); si no hay, en el piso de abajo.</summary>
        public bool BuscarPunto(Vector3 jugador, out Vector3 punto)
        {
            float min = config != null ? config.DistanciaMinima : 22f;
            float max = config != null ? config.DistanciaMaxima : 34f;
            for (int i = 0; i < Intentos; i++)
            {
                bool estricto = i < Intentos / 2;
                float angulo = Random.Range(0f, Mathf.PI * 2f);
                float d = Random.Range(min, max);
                Vector3 arriba = jugador + new Vector3(Mathf.Cos(angulo) * d, AlturaSonda, Mathf.Sin(angulo) * d);
                if (!Physics.Raycast(arriba, Vector3.down, out RaycastHit golpe, CaidaMaxima, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if ((estricto && Mathf.Abs(golpe.point.y - jugador.y) > MismoPiso) || golpe.normal.y < 0.7f)
                {
                    continue;
                }
                Vector3 pie = golpe.point + Vector3.up * (RadioCapsula + 0.05f);
                if (Physics.CheckCapsule(pie, pie + Vector3.up * 1.2f, RadioCapsula, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                punto = HuecoLibreNpc.Buscar(golpe.point) + Vector3.up * 0.05f; // T40: no aparece encimado de otro cuerpo
                return true;
            }
            punto = jugador;
            return false;
        }

        /// <summary>Nova se va: quita a los suyos que siguen de pie (los caídos se quedan tirados) y la próxima búsqueda alta vuelve a traer al jefe.</summary>
        public void RetirarTodos()
        {
            Quitar(guardias);
            Quitar(pesados);
            Quitar(perros);
            Quitar(deNave);
            if (nave != null)
            {
                Destroy(nave.gameObject);
            }
            nave = null;
            naveLlamada = false;
            enNova = false;
            if (jefe != null && !jefe.EstaKO)
            {
                Destroy(jefe.gameObject);
            }
            jefe = null;
            jefeSalio = false;
            proximaOleada = 0f;
            anunciada = false;
            vistos.Clear();
        }

        /// <summary>T-1323: quita a los que nunca vieron al jugador; los que ya lo vieron se quedan persiguiéndolo.</summary>
        private void RetirarNoVistos()
        {
            QuitarNoVistos(guardias);
            QuitarNoVistos(pesados);
            QuitarNoVistos(perros);
            QuitarNoVistos(deNave);
            if (jefe != null && !jefe.EstaKO && !vistos.Contains(jefe))
            {
                Destroy(jefe.gameObject);
                jefe = null;
                jefeSalio = false;
            }
        }

        private void QuitarNoVistos(List<PandilleroAPie> lista)
        {
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                PandilleroAPie p = lista[i];
                if (p != null && !p.EstaKO && !vistos.Contains(p))
                {
                    Destroy(p.gameObject);
                    lista.RemoveAt(i);
                }
            }
        }

        private static void Quitar(List<PandilleroAPie> lista)
        {
            foreach (PandilleroAPie p in lista)
            {
                if (p != null && !p.EstaKO)
                {
                    Destroy(p.gameObject);
                }
            }
            lista.Clear();
        }

        private static int Vivos(List<PandilleroAPie> lista)
        {
            int n = 0;
            foreach (PandilleroAPie p in lista)
            {
                if (p != null && !p.EstaKO)
                {
                    n++;
                }
            }
            return n;
        }
    }
}
