using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Testigos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Crea y retira a los perseguidores según el calor (T-341). Escucha <c>ICalor.NivelCambio</c> (el calor aparece cuando el jugador ya está en escena, por eso
    /// se engancha al registro) y deja drones y patrullas con las cantidades de <see cref="CalculoPersecucion"/>. Al entrar el jugador a la casa borra el calor
    /// y por lo tanto retira a todos. Las calles salen de las manzanas (hijos "Manzana_N") del objeto padre, normalmente Barrio.
    /// </summary>
    public class GestorPersecucion : MonoBehaviour
    {
        /// <summary>Mensaje de Unity que recibe el jugador al ser arrestado (T-690).</summary>
        public const string MensajeArresto = "AlSerArrestado";
        private const string PrefijoManzana = "Manzana_";
        private const float LadoManzana = 80f;          // medida del barrio (CreadorBarrio, m)
        private const float AlturaCalles = 0.2f;        // sobre la banqueta (m)
        private const float GradosPorVuelta = 360f;
        public const float DistanciaSalidaPatrulla = 80f;
        private const int IntentosDeSalidaDron = 8;
        private const float GradosReintentoDron = 47f;
        private const float SeparacionDeSalida = 20f;
        private const float AlcanceCamara = 400f;
        private const float SegundosReintentoSalida = 0.5f;
        /// <summary>T-1660: al dispararle a una patrulla (o con la búsqueda en marcha), las patrullas a esta distancia del jugador (m) prenden sirena y van por él.</summary>
        public const float RadioAlertaPatrullas = 120f;
        /// <summary>T-1660: una camioneta cuenta como llegada al estar a esta distancia (m) del jugador, ir directo o haber bajado a sus policías.</summary>
        public const float DistanciaLlegadaCamioneta = 35f;

        [SerializeField] private ConfigPersecucion config;
        [SerializeField] private DronMundo prefabDron;
        [SerializeField] private PatrullaMundo prefabPatrulla;
        [Tooltip("T-1322: la camioneta de los 5 estrellas. Vacío: no hay modelo todavía y salen patrullas normales.")]
        [SerializeField] private PatrullaMundo prefabCamioneta;
        private int estrellasPublicadas;
        private int bucleSirenaMuro = Sonidos.SinBucle; // T-1533

        private void PararSirenaMuro()
        {
            SonidosPolicia.DetenerSirena(bucleSirenaMuro);
            bucleSirenaMuro = Sonidos.SinBucle;
        }
        [Tooltip("T-1498: variantes de dron de combate por estrellas. Vacío: se busca en Resources; sin él todos los drones son el DR01.")]
        [SerializeField] private ConfigDronesCombate variantesDron;
        private bool variantesBuscadas;

        /// <summary>T-1498: para pruebas y el editor.</summary>
        public void ConfigurarVariantes(ConfigDronesCombate variantes)
        {
            variantesDron = variantes;
            variantesBuscadas = true;
        }

        private readonly List<DronMundo> drones = new List<DronMundo>();
        private readonly List<PatrullaMundo> patrullas = new List<PatrullaMundo>();
        // T-933: las patrullas que circulan sin búsqueda; son las únicas que pueden verte cometer un delito.
        private readonly List<PatrullaMundo> rondas = new List<PatrullaMundo>();
        // T-1660: las rondas que se alertaron por la búsqueda (más de las que pide el nivel): persiguen y se retiran al acabar la búsqueda o en modo Nova.
        private readonly List<PatrullaMundo> alertadas = new List<PatrullaMundo>();
        private readonly List<PatrullaMundo> camionetas = new List<PatrullaMundo>();
        private readonly HashSet<PatrullaMundo> camionetasLlegaron = new HashSet<PatrullaMundo>();

        /// <summary>T-1660: el gestor vivo (para <see cref="DepurarEscalada"/>).</summary>
        public static GestorPersecucion Actual { get; private set; }

        /// <summary>T-1660: policías a pie muertos en esta búsqueda; vuelve a 0 al perder la persecución.</summary>
        public int PoliciasMuertos => DirectorRespuesta.PoliciasMuertos;

        /// <summary>T-1660: camionetas que ya llegaron junto al jugador en esta búsqueda; vuelve a 0 al perder la persecución.</summary>
        public int CamionetasLlegadas => camionetasLlegaron.Count;

        /// <summary>T-1660: las rondas alertadas que persiguen ahora.</summary>
        public IReadOnlyList<PatrullaMundo> Alertadas => alertadas;
        private const float SegundosEntreRondas = 5f;
        private float reintentoRonda;

        /// <summary>T-933: las patrullas de ronda de ahora.</summary>
        public IReadOnlyList<PatrullaMundo> Rondas => rondas;
        private readonly List<Component> retirandose = new List<Component>();
        private bool huboContacto;
        private float reintentoSalida;
        private float valorAnterior;
        private CalculoPersecucion calculo;
        private GrafoCalles calles;
        private ICalor calor;
        private System.Random azar;
        private TestigosDelito testigos;
        private ReglaBusquedaPolicial busqueda;
        private ReglaArresto arresto;
        private ZonaBusqueda zona;
        private RutaCallesBarrio rutaPropia;
        private Vector3? jugadorAnterior;

        public int Drones => drones.Count;
        public int Patrullas => patrullas.Count;

        /// <summary>Cuántos perseguidores se están yendo (T-587): ya no cuentan en <see cref="Drones"/> ni <see cref="Patrullas"/>.</summary>
        public int Retirandose => retirandose.Count;

        /// <summary>Segundos que lleva la policía sin verte desde la última vez que te vio (T-587).</summary>
        public float SegundosSinVer { get; private set; }

        /// <summary>Segundos seguidos que llevas fuera de su vista y del círculo de búsqueda (T-665).</summary>
        public float SegundosFueraDeLaZona => busqueda != null ? busqueda.SegundosFuera : 0f;

        /// <summary>True si ya te dieron por perdido (T-587).</summary>
        public bool Perdido => config != null && ReglaVistaPolicia.Perdido(SegundosSinVer, config.SegundosParaPerder) && huboContacto;

        /// <summary>T-1322: el prefab de la camioneta (opcional). Con 5 estrellas las patrullas extra salen con él; si es null, con el de la patrulla.</summary>
        public void PonerCamioneta(PatrullaMundo camioneta) => prefabCamioneta = camioneta;

        /// <summary>T-1322: true si ahora mismo hay 5 estrellas (llegan los refuerzos extra).</summary>
        public bool CincoEstrellas => calor != null && calor.Nivel > 0 && EstrellasBusqueda.EnMaximo(EstrellasBusqueda.De(calor));

        private int PatrullasPedidas(int nivel) => calculo == null ? 0 : calculo.Patrullas(nivel) + (CincoEstrellas && config != null ? config.PatrullasExtraCincoEstrellas : 0);

        private int DronesPedidos(int nivel) => calculo == null ? 0 : calculo.Drones(nivel) + (CincoEstrellas && config != null ? config.DronesExtraCincoEstrellas : 0);

        // T-1322: avisa cuando cambian las estrellas; al llegar o salir de las 5 reajusta los perseguidores (el nivel de calor no cambia entre 4 y 5 estrellas).
        private void AlCambiarValor(float valor)
        {
            int estrellas = EstrellasBusqueda.De(calor);
            if (estrellas == estrellasPublicadas)
            {
                return;
            }
            bool eraMaximo = EstrellasBusqueda.EnMaximo(estrellasPublicadas);
            bool sera = EstrellasBusqueda.EnMaximo(estrellas);
            if (eraMaximo && !sera)
            {
                PararSirenaMuro(); // T-1533
            }
            bool sube = estrellas > estrellasPublicadas;
            estrellasPublicadas = estrellas;
            EstrellasBusqueda.PublicarCambio(estrellas);
            if (eraMaximo != sera)
            {
                Reconciliar(calor.Nivel);
                if (sera && sube)
                {
                    RadioPolicia.Sonar(PosicionDelJugador() ?? transform.position, config); // llegan los refuerzos
                    PararSirenaMuro();
                    bucleSirenaMuro = SonidosPolicia.IniciarSirenaMuro(PosicionDelJugador() ?? transform.position); // T-1533: la torre del muro
                }
            }
        }

        /// <summary>Lo usan el creador de editor y las pruebas, antes de que el gestor se active.</summary>
        public void Configurar(ConfigPersecucion numeros, DronMundo dron, PatrullaMundo patrulla)
        {
            config = numeros;
            prefabDron = dron;
            prefabPatrulla = patrulla;
        }

        private void Awake()
        {
            if (config == null || prefabDron == null || prefabPatrulla == null)
            {
                Debug.LogError("[Trafico] GestorPersecucion necesita config y los prefabs de dron y patrulla.", this);
                enabled = false;
                return;
            }
            calculo = config.Crear();
            azar = new System.Random(341);
            testigos = gameObject.AddComponent<TestigosDelito>(); // T-661: sin testigos no hay calor
            testigos.Configurar(config);
            gameObject.AddComponent<VigilanteDisparoACarros>().Configurar(testigos); // T-1322: dispararle a un carro frente a la policía
            gameObject.AddComponent<GestorRefuerzos>().Configurar(config); // T-714: los SKULLS llaman refuerzos en carro
            gameObject.AddComponent<AsaltoMundo>(); // T-717: el carro del asaltante de la historia del dinero
            busqueda = new ReglaBusquedaPolicial(config.RadioBusqueda, config.SegundosParaEscapar);
            // T-1303: el arresto es por contacto: a lo más DistanciaToque m y unos instantes (la configuración puede pedir menos, no más).
            arresto = new ReglaArresto(Mathf.Min(config.DistanciaArresto, DistanciaToque), Mathf.Min(config.SegundosArresto, SegundosToque), float.MaxValue);
            zona = new ZonaBusqueda(busqueda);
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.Dron, prefabDron != null); // T-1620
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.Patrulla, prefabPatrulla != null);
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.Camioneta, prefabCamioneta != null);
        }

        private void OnEnable()
        {
            Actual = this;
            Registro<ICalor>.Agregado += Enlazar;
            Registro<ICalor>.Quitado += Desenlazar;
            EventosLugares.JugadorEntro += AlEntrarALugar;
            DronMundo.Derribado += AlDerribarDron;
            if (zona != null)
            {
                Registro<IZonaBusqueda>.Agregar(zona);
            }
            RegistrarRutaDeCalles();
            if (Registro<ICalor>.Actual != null)
            {
                Enlazar(Registro<ICalor>.Actual);
            }
        }

        // T-669: las calles de este barrio (sus manzanas) sirven también para la ruta GPS del mapa; solo si nadie más la trae ya registrada.
        private void RegistrarRutaDeCalles()
        {
            if (Registro<IRutaCalles>.Actual != null)
            {
                return;
            }
            GrafoCalles grafo = ArmarCallesDe(transform.parent != null ? transform.parent : transform);
            if (grafo != null)
            {
                rutaPropia = new RutaCallesBarrio(grafo);
                Registro<IRutaCalles>.Agregar(rutaPropia);
            }
        }

        private void OnDisable()
        {
            if (Actual == this)
            {
                Actual = null;
            }
            Registro<ICalor>.Agregado -= Enlazar;
            Registro<ICalor>.Quitado -= Desenlazar;
            PararSirenaMuro(); // T-1533
            EventosLugares.JugadorEntro -= AlEntrarALugar;
            DronMundo.Derribado -= AlDerribarDron;
            if (zona != null)
            {
                Registro<IZonaBusqueda>.Quitar(zona);
            }
            if (rutaPropia != null)
            {
                Registro<IRutaCalles>.Quitar(rutaPropia);
                rutaPropia = null;
            }
            busqueda?.Detener();
            Desenlazar(calor);
            Reconciliar(0);
            foreach (PatrullaMundo ronda in rondas)
            {
                if (ronda != null)
                {
                    Destroy(ronda.gameObject);
                }
            }
            rondas.Clear();
            // Apagado el gestor ya no hay quien vea irse a los que se retiraban: se quitan de golpe.
            foreach (Component c in retirandose)
            {
                if (c != null)
                {
                    Destroy(c.gameObject);
                }
            }
            retirandose.Clear();
        }

        // T-665: mientras te buscan la policía no se va sola. Te persigue hasta arrestarte o hasta que salgas de su vista Y del círculo de búsqueda
        // el tiempo suficiente; entonces escapas y se retira. El calor no baja solo mientras tanto (CalorConTestigos).
        private void Update()
        {
            retirandose.RemoveAll(c => c == null || (c is PatrullaMundo robada && robada.Robada));
            patrullas.RemoveAll(p => p == null || p.Robada || p.Destruida); // T-707: una patrulla que el jugador se robó ya no es de la policía: llega otra
            alertadas.RemoveAll(p => p == null || p.Robada || p.Destruida || p.Retirando);
            camionetas.RemoveAll(p => p == null);
            drones.RemoveAll(d => d == null); // T-730: un dron derribado ya no cuenta
            DirectorRespuesta.DeclararDisponible(TipoRespuesta.Camioneta, prefabCamioneta != null);
            DirectorRespuesta.Avanzar(Time.time, EstrellasBusqueda.De(calor)); // T-1620
            if (MisionProtegida.Activa)
            {
                MantenerMisionProtegida();
            }
            for (int i = 0; i < drones.Count; i++)
            {
                drones[i].AsignarOrbita(i, drones.Count); // T-938: cada dron su punto
            }
            MantenerRondas(Time.deltaTime);
            if (calor == null || calor.Nivel <= 0 || config == null)
            {
                SegundosSinVer = 0f;
                huboContacto = false;
                valorAnterior = calor != null ? calor.Valor : 0f;
                jugadorAnterior = null;
                arresto.Reiniciar();
                if (camionetasLlegaron.Count > 0)
                {
                    camionetasLlegaron.Clear(); // T-1660: perdiste la persecución: la cuenta para Nova empieza de cero
                    DirectorRespuesta.PonerCamionetasLlegadas(0);
                }
                return;
            }
            AlertarRondasCercanas();
            ContarCamionetasLlegadas();
            float dt = Time.deltaTime;
            reintentoSalida -= dt;
            if (reintentoSalida <= 0f)
            {
                reintentoSalida = SegundosReintentoSalida;
                if (patrullas.Count < PatrullasPedidas(calor.Nivel) || drones.Count < DronesPedidos(calor.Nivel)
                    || (DirectorRespuesta.ModoNova && (patrullas.Count > 0 || alertadas.Count > 0))) // T-1631: NOVA INTERVIENE: las patrullas que queden se retiran
                {
                    Reconciliar(calor.Nivel); // T-664: las patrullas que no pudieron salir sin verse lo intentan de nuevo
                }
            }
            Vector3? jugador = PosicionDelJugador();
            bool patrullaVe = false;
            float cercana = float.MaxValue;
            foreach (PatrullaMundo p in patrullas)
            {
                if (p != null && p.VeAlJugador)
                {
                    patrullaVe = true;
                    if (jugador.HasValue)
                    {
                        cercana = Mathf.Min(cercana, DistanciaPlano(p.transform.position, jugador.Value));
                    }
                }
            }
            bool teVen = patrullaVe || PatrullaMundo.JugadorEnPatrulla || (jugador.HasValue && LoVeUnDron(jugador.Value));
            if (teVen)
            {
                huboContacto = true;
                SegundosSinVer = 0f;
            }
            else if (huboContacto)
            {
                SegundosSinVer += dt;
            }
            if (calor.Valor > valorAnterior + 0.01f)
            {
                SegundosSinVer = 0f; // un delito nuevo: la policía vuelve a estar encima
                if (jugador.HasValue)
                {
                    busqueda.Recentrar(jugador.Value);
                }
            }
            valorAnterior = calor.Valor;
            if (!jugador.HasValue)
            {
                return;
            }
            float velocidad = jugadorAnterior.HasValue && dt > 0f ? DistanciaPlano(jugador.Value, jugadorAnterior.Value) / dt : 0f;
            jugadorAnterior = jugador;
            // T-1303: solo te arresta un policía a pie que te alcanza y te toca (no una patrulla cerca un rato).
            float toque = PoliciaMasCercano(jugador.Value);
            if (ArrestoActivo && arresto.Avanzar(toque <= DistanciaToque, toque, 0f, dt))
            {
                Arrestar();
                return;
            }
            if (busqueda.Avanzar(jugador.Value, teVen, dt))
            {
                Escapar();
            }
        }

        /// <summary>T25: la policía ya no arresta (solo persigue y dispara). Ponlo en true solo para pruebas del arresto.</summary>
        public static bool ArrestoActivo = false;

        /// <summary>T-1303: a esta distancia (m, en el plano) un policía a pie ya te tocó.</summary>
        public const float DistanciaToque = 1.3f;
        /// <summary>T-1303: segundos seguidos de contacto para el arresto (un roce no basta, pero es casi inmediato).</summary>
        public const float SegundosToque = 0.4f;
        private const float DiferenciaAlturaToque = 1.4f;

        /// <summary>T-1303: distancia en el plano al policía a pie vivo más cercano que está a tu altura (MaxValue si no hay).</summary>
        public static float PoliciaMasCercano(Vector3 jugador)
        {
            float menor = float.MaxValue;
            List<GameObject> lista = PatrullaMundo.PoliciasAPieLista;
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                GameObject policia = lista[i];
                if (policia == null)
                {
                    lista.RemoveAt(i);
                    continue;
                }
                if (!policia.activeInHierarchy || Mathf.Abs(policia.transform.position.y - jugador.y) > DiferenciaAlturaToque)
                {
                    continue;
                }
                IVida vida = policia.GetComponentInChildren<IVida>();
                if (vida != null && vida.EstaKO)
                {
                    continue;
                }
                menor = Mathf.Min(menor, DistanciaPlano(policia.transform.position, jugador));
            }
            return menor;
        }

        /// <summary>Un policía te alcanzó y te tocó; antes: una patrulla cerca te arrestó (T-665): el calor se borra, la policía se retira y se avisa en <see cref="EventosPolicia"/>.</summary>
        private void Arrestar()
        {
            Debug.Log("[Trafico] El jugador fue arrestado.");
            EventosPolicia.PublicarArresto();
            calor.Borrar();
            // T-690: las consecuencias (pantalla ARRESTADO, multa, casa) las hace el jugador (Progresion): Trafico no conoce esas áreas.
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null)
            {
                jugador.Raiz.gameObject.SendMessage(MensajeArresto, SendMessageOptions.DontRequireReceiver);
            }
        }

        /// <summary>Saliste de la vista y del círculo de búsqueda el tiempo suficiente (T-665): ya no te buscan.</summary>
        private void Escapar()
        {
            Debug.Log("[Trafico] El jugador escapó de la policía.");
            EventosPolicia.PublicarEscape();
            calor.Borrar();
        }

        // Un dron te ve si está cerca (vuela por encima de las paredes).
        private bool LoVeUnDron(Vector3 jugador)
        {
            foreach (DronMundo d in drones)
            {
                if (d != null && DistanciaPlano(d.transform.position, jugador) <= config.RadioVistaDron)
                {
                    return true;
                }
            }
            return false;
        }

        private static float DistanciaPlano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        private void Enlazar(ICalor nuevo)
        {
            if (!ReferenceEquals(nuevo, Registro<ICalor>.Actual))
            {
                return; // T-661: el calor real queda detrás de CalorConTestigos, que es el último del registro
            }
            Desenlazar(calor);
            calor = nuevo;
            calor.NivelCambio += Reconciliar;
            calor.ValorCambio += AlCambiarValor;
            estrellasPublicadas = EstrellasBusqueda.De(calor);
            Reconciliar(calor.Nivel);
        }

        private void Desenlazar(ICalor viejo)
        {
            if (viejo != null && ReferenceEquals(viejo, calor))
            {
                calor.NivelCambio -= Reconciliar;
                calor.ValorCambio -= AlCambiarValor;
                if (estrellasPublicadas != 0)
                {
                    estrellasPublicadas = 0;
                    EstrellasBusqueda.PublicarCambio(0);
                }
                calor = null;
                Reconciliar(0);
            }
        }

        // T-730: derribar un dron baja la búsqueda (CalorAlDerribarDron puntos; con CalorConTestigos también baja el piso que se sostiene).
        private void AlDerribarDron(DronMundo dron)
        {
            if (calor != null && config != null && drones.Contains(dron))
            {
                calor.Enfriar(config.CalorAlDerribarDron);
            }
        }

        private void AlEntrarALugar(IdLugar lugar)
        {
            if (lugar == IdLugar.Casa)
            {
                Registro<ICalor>.Actual?.Borrar();
            }
        }

        // T-665: al empezar el calor nace el círculo de búsqueda donde estás; al volver a 0 se apaga.
        private void ActualizarBusqueda(int nivel)
        {
            if (busqueda == null)
            {
                return;
            }
            if (nivel <= 0)
            {
                busqueda.Detener();
            }
            else if (!busqueda.Activa)
            {
                busqueda.Iniciar(PosicionDelJugador() ?? transform.position);
            }
        }

        // Deja exactamente las cantidades que pide el nivel: crea las que faltan y retira las que sobran.
        private int nivelDespachado;

        private void Reconciliar(int nivel)
        {
            if (nivel > nivelDespachado && nivel > 0)
            {
                RadioPolicia.SonarDespacho(PosicionDelJugador() ?? transform.position, config); // T-1476: la central manda más unidades
            }
            nivelDespachado = nivel;
            patrullas.RemoveAll(p => p == null || p.Robada || p.Destruida); // T-707
            ActualizarBusqueda(nivel);
            DirectorRespuesta.Avanzar(Time.time, EstrellasBusqueda.De(calor)); // T-1620: la jerarquía y los topes salen del director
            int dronesPedidos = DronesPedidos(nivel);
            int patrullasPedidas = PatrullasPedidas(nivel);
            // Lo que ya no cabe con estas estrellas (o por una misión protegida) se retira: primero los drones, que son de un nivel más alto.
            Retirar(drones, DirectorRespuesta.PermiteMantener(TipoRespuesta.Dron) ? dronesPedidos : 0);
            while (drones.Count < dronesPedidos && DirectorRespuesta.PuedeGenerar(TipoRespuesta.Dron))
            {
                CrearDron(dronesPedidos);
            }
            Retirar(patrullas, DirectorRespuesta.PermiteMantener(TipoRespuesta.Patrulla) ? patrullasPedidas : 0);
            Retirar(alertadas, nivel > 0 && DirectorRespuesta.PermiteMantener(TipoRespuesta.Patrulla) ? alertadas.Count : 0); // T-1660: las alertadas se van al acabar la búsqueda o al entrar Nova
            while (patrullas.Count < patrullasPedidas)
            {
                bool camioneta = prefabCamioneta != null && calor != null && CalculoPersecucion.UsaCamioneta(EstrellasBusqueda.De(calor), patrullas.Count, calculo.Patrullas(calor.Nivel))
                    && DirectorRespuesta.PuedeGenerar(TipoRespuesta.Camioneta);
                if (!camioneta && !DirectorRespuesta.PuedeGenerar(TipoRespuesta.Patrulla))
                {
                    break; // T-1620: en cooldown o en el tope: la reposición llega después, no de golpe
                }
                PatrullaMundo adoptada = camioneta ? null : SacarRonda();
                if (adoptada != null)
                {
                    adoptada.Alertar();
                    adoptada.Intercepta = patrullas.Count % 2 == 1;
                    patrullas.Add(adoptada);
                    RegistrarPatrulla(TipoRespuesta.Patrulla, adoptada);
                    continue;
                }
                if (!CrearPatrulla(camioneta))
                {
                    break; // T-598: sin calles no hay patrulla; sin este corte el ciclo no terminaba
                }
            }
        }

        private static void RegistrarPatrulla(TipoRespuesta tipo, PatrullaMundo patrulla)
        {
            DirectorRespuesta.Registrar(tipo, patrulla.gameObject, () =>
                patrulla == null || patrulla.Retirando ? EstadoUnidad.Retirada : patrulla.Destruida || patrulla.Robada ? EstadoUnidad.Muerta : EstadoUnidad.Viva);
        }

        // T-1620: misión protegida: el calor se congela en 0 y lo que ya estaba se va sin desaparecer a la vista (se borra fuera de la vista o en una cinemática).
        private void MantenerMisionProtegida()
        {
            if (calor != null && calor.Valor > 0f)
            {
                calor.Borrar(); // baja el nivel a 0: Reconciliar retira patrullas y drones manejando o volando
            }
            for (int i = rondas.Count - 1; i >= 0; i--)
            {
                PatrullaMundo ronda = rondas[i];
                if (ronda != null)
                {
                    ronda.Retirar();
                    retirandose.Add(ronda);
                }
                rondas.RemoveAt(i);
            }
            bool cinematica = CinematicaEnCurso.Activa;
            Camera camara = Camera.main;
            if (cinematica)
            {
                for (int i = retirandose.Count - 1; i >= 0; i--)
                {
                    if (retirandose[i] != null)
                    {
                        Destroy(retirandose[i].gameObject);
                    }
                    retirandose.RemoveAt(i);
                }
            }
            List<GameObject> policias = PatrullaMundo.PoliciasAPieLista;
            for (int i = policias.Count - 1; i >= 0; i--)
            {
                GameObject policia = policias[i];
                if (policia == null)
                {
                    policias.RemoveAt(i);
                }
                else if (cinematica || !ReglaVistaPolicia.LoVe(camara, policia.transform.position, AlcanceCamara))
                {
                    DirectorRespuesta.MarcarRetirado(policia);
                    Destroy(policia);
                    policias.RemoveAt(i);
                }
            }
        }

        // T-587: los que sobran no desaparecen a la vista: se van por la calle (o por el aire) y se borran solos al salir de la vista.
        private void Retirar<T>(List<T> lista, int cantidad) where T : Component
        {
            while (lista.Count > cantidad)
            {
                int ultimo = lista.Count - 1;
                T quien = lista[ultimo];
                lista.RemoveAt(ultimo);
                DirectorRespuesta.MarcarRetirado(quien.gameObject); // T-1620: retirar no es una baja
                if (quien is PatrullaMundo patrulla)
                {
                    patrulla.Retirar();
                }
                else if (quien is DronMundo dron)
                {
                    dron.Retirar();
                }
                retirandose.Add(quien);
            }
        }

        private void CrearDron(int total)
        {
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            Camera camara = Camera.main;
            Vector3 lejos = centro;
            Vector3 sueloCapsula = centro;
            int indice = drones.Count;
            float anguloBase = (float)azar.NextDouble() * GradosPorVuelta;
            for (int intento = 0; intento < IntentosDeSalidaDron; intento++)
            {
                // T-938: cada dron sale por su lado (en círculo, por índice); si ese lado se ve, se gira el círculo y se prueba otra vez.
                lejos = CalculoSeparacionDrones.PuntoEnCirculo(centro, indice, total, config.DistanciaAparicionDron, config.AlturaDron, anguloBase + intento * GradosReintentoDron);
                if (!ReglaVistaPolicia.LoVe(camara, lejos, AlcanceCamara))
                {
                    break; // T-587: sale fuera de la vista
                }
            }
            // T-1631: con NOVA INTERVIENE los drones son de Nova y llegan en cápsula: cae en un punto libre del piso cerca del jugador (si no hay punto o cápsula, llega volando como antes).
            bool enCapsula = DirectorRespuesta.ModoNova && EventosRespuesta.CapsulaDron != null && BuscarPuntoCapsula(centro, out sueloCapsula);
            DronMundo dron = Instantiate(prefabDron, enCapsula ? sueloCapsula : lejos, Quaternion.identity, transform);
            dron.Preparar(calculo, config, PosicionDelJugador);
            if (!variantesBuscadas)
            {
                variantesBuscadas = true;
                if (variantesDron == null)
                {
                    variantesDron = Resources.Load<ConfigDronesCombate>(ConfigDronesCombate.Recurso);
                }
            }
            VarianteDronCombate variante = variantesDron != null ? variantesDron.Elegir(EstrellasBusqueda.De(calor), indice) : null;
            if (variantesDron != null)
            {
                dron.AplicarVariante(variante); // T-1498
            }
            drones.Add(dron);
            dron.AsignarOrbita(indice, total);
            DirectorRespuesta.Registrar(TipoRespuesta.Dron, dron.gameObject, () =>
                dron == null || dron.EstaCayendo ? EstadoUnidad.Muerta : dron.Retirando ? EstadoUnidad.Retirada : EstadoUnidad.Viva);
            if (enCapsula)
            {
                string nombre = variante != null ? (variante.Modelo != null ? variante.Modelo.name : variante.Nombre) : dron.name;
                Vector3 mira = centro - sueloCapsula;
                mira.y = 0f;
                Quaternion giro = mira.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mira) : Quaternion.identity;
                if (!EventosRespuesta.CapsulaDron(dron.gameObject, nombre, sueloCapsula, giro, null))
                {
                    dron.transform.position = lejos; // sin cápsula (falta su config): llega volando
                }
            }
        }

        private const int IntentosPuntoCapsula = 12;
        private const float DistanciaCapsulaMin = 14f;
        private const float DistanciaCapsulaMax = 24f;
        private const float AlturaSondaCapsula = 40f;
        private const float MismoPisoCapsula = 2f;
        private const float AireLibreCapsula = 14f;
        private const float RadioLibreCapsula = 0.6f;

        /// <summary>T-1631: un punto del piso a 14-24 m del jugador, a su nivel, plano y con aire libre arriba (donde cae la cápsula).</summary>
        private bool BuscarPuntoCapsula(Vector3 centro, out Vector3 punto)
        {
            for (int i = 0; i < IntentosPuntoCapsula; i++)
            {
                float angulo = (float)azar.NextDouble() * Mathf.PI * 2f;
                float d = DistanciaCapsulaMin + (float)azar.NextDouble() * (DistanciaCapsulaMax - DistanciaCapsulaMin);
                Vector3 arriba = new Vector3(centro.x + Mathf.Cos(angulo) * d, centro.y + AlturaSondaCapsula, centro.z + Mathf.Sin(angulo) * d);
                if (!Physics.Raycast(arriba, Vector3.down, out RaycastHit golpe, AlturaSondaCapsula * 2f, ~0, QueryTriggerInteraction.Ignore)
                    || golpe.normal.y < 0.85f || Mathf.Abs(golpe.point.y - centro.y) > MismoPisoCapsula)
                {
                    continue;
                }
                Vector3 pie = golpe.point + Vector3.up * (RadioLibreCapsula + 0.1f);
                if (Physics.CheckSphere(pie, RadioLibreCapsula, ~0, QueryTriggerInteraction.Ignore)
                    || Physics.Raycast(pie, Vector3.up, AireLibreCapsula, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                punto = golpe.point;
                return true;
            }
            punto = centro;
            return false;
        }

        // T-933: mantiene las patrullas de ronda: si falta alguna sale otra, siempre lejos y fuera de la vista.
        private void MantenerRondas(float dt)
        {
            rondas.RemoveAll(p => p == null || p.Robada || p.Retirando || !p.EnRonda);
            if (MisionProtegida.Activa || DirectorRespuesta.ModoNova || (calor != null && calor.Nivel > 0))
            {
                return; // T-1660: con la búsqueda en marcha tampoco salen rondas nuevas (los refuerzos vienen de Reconciliar). T-1620: misión protegida (T-1631: y NOVA INTERVIENE): no salen rondas nuevas
            }
            reintentoRonda -= dt;
            if (reintentoRonda > 0f || config == null || calculo == null || prefabPatrulla == null || rondas.Count >= config.PatrullasDeRonda)
            {
                return;
            }
            reintentoRonda = SegundosEntreRondas;
            if (calles == null)
            {
                calles = ArmarCalles();
            }
            int salida = calles != null ? NodoDeSalida(PosicionDelJugador() ?? transform.position) : -1;
            if (salida < 0)
            {
                return;
            }
            PatrullaMundo ronda = Instantiate(prefabPatrulla, transform);
            ronda.PrepararRonda(calculo, config, calles, salida, PosicionDelJugador);
            rondas.Add(ronda);
        }

        // T-1660: con la búsqueda en marcha (por ejemplo al dispararle a una patrulla) TODAS las rondas a menos de RadioAlertaPatrullas m del jugador prenden luces y sirena y van por él;
        // las que ya cuentan como perseguidoras del nivel quedan en "patrullas", el resto en "alertadas". Ninguna se queda dando la vuelta de ronda.
        private void AlertarRondasCercanas()
        {
            if (DirectorRespuesta.ModoNova || MisionProtegida.Activa)
            {
                return; // en modo Nova la policía se retira
            }
            Vector3? jugador = PosicionDelJugador();
            if (!jugador.HasValue)
            {
                return;
            }
            IReadOnlyList<PatrullaMundo> activas = PatrullaMundo.Activas;
            for (int i = activas.Count - 1; i >= 0; i--)
            {
                PatrullaMundo p = activas[i];
                if (p == null || !p.EnRonda || !p.PuedeAlertarse || DistanciaPlano(p.transform.position, jugador.Value) > RadioAlertaPatrullas)
                {
                    continue;
                }
                p.Alertar();
                rondas.Remove(p);
                if (!patrullas.Contains(p))
                {
                    alertadas.Add(p);
                }
            }
        }

        // T-1660: una camioneta "llegó" al estar cerca del jugador, ir directo o haber bajado a sus policías; la cuenta no baja si después la matan.
        private void ContarCamionetasLlegadas()
        {
            Vector3? jugador = PosicionDelJugador();
            if (jugador.HasValue)
            {
                foreach (PatrullaMundo c in camionetas)
                {
                    if (c != null && !c.Robada && !camionetasLlegaron.Contains(c)
                        && (c.PoliciasBajaron || c.VaDirecto || DistanciaPlano(c.transform.position, jugador.Value) <= DistanciaLlegadaCamioneta))
                    {
                        camionetasLlegaron.Add(c);
                    }
                }
            }
            DirectorRespuesta.PonerCamionetasLlegadas(camionetasLlegaron.Count);
        }

        /// <summary>
        /// T-1660: estado de la escalada para comprobar en Play (invocable por reflexión): estrellas, policías muertos, camionetas llegadas, fase de Nova y cada patrulla con su estado.
        /// También lo escribe en consola.
        /// </summary>
        public static string DepurarEscalada()
        {
            StringBuilder texto = new StringBuilder();
            GestorPersecucion g = Actual;
            int nivel = DirectorRespuesta.NivelDesbloqueado;
            bool abierta = DirectorRespuesta.PuertaNovaAbierta;
            ParametrosRespuesta numeros = DirectorRespuesta.Parametros;
            texto.Append("[Escalada] estrellas ").Append(g != null && g.calor != null ? EstrellasBusqueda.De(g.calor) : 0)
                .Append(" | policias muertos ").Append(DirectorRespuesta.PoliciasMuertos).Append('/').Append(numeros.PoliciasMuertosParaNova)
                .Append(" | camionetas llegadas ").Append(g != null ? g.CamionetasLlegadas : 0).Append('/').Append(DirectorRespuesta.Regla.CamionetasRequeridas)
                .Append(" | nova ").Append(abierta ? "ABIERTA" : "cerrada").Append(" fase: ").Append(ReglaPuertaNova.NombreFase(nivel, abierta))
                .Append(" (nivel ").Append(nivel).Append(", modoNova ").Append(DirectorRespuesta.ModoNova).Append(')');
            int n = 0;
            foreach (PatrullaMundo p in PatrullaMundo.Activas)
            {
                if (p != null)
                {
                    n++;
                    texto.Append("\n  patrulla ").Append(n).Append(": ").Append(p.Estado());
                }
            }
            string resultado = texto.ToString();
            Debug.Log(resultado);
            return resultado;
        }

        // T-933: la patrulla de ronda más cercana al jugador deja la ronda; null si no hay.
        private PatrullaMundo SacarRonda()
        {
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            PatrullaMundo mejor = null;
            float menor = float.MaxValue;
            foreach (PatrullaMundo p in rondas)
            {
                if (p != null && !p.Robada && !p.Retirando && p.EnRonda)
                {
                    float d = DistanciaPlano(p.transform.position, centro);
                    if (d < menor)
                    {
                        menor = d;
                        mejor = p;
                    }
                }
            }
            rondas.Remove(mejor);
            return mejor;
        }

        private bool CrearPatrulla(bool camioneta)
        {
            if (calles == null)
            {
                calles = ArmarCalles();
            }
            if (calles == null)
            {
                return false; // T-598: sin manzanas (escena de prueba sin barrio) no hay calles por donde salga una patrulla; los drones sí salen
            }
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            int salida = NodoDeSalida(centro);
            if (salida < 0)
            {
                return false; // T-664: ahora no hay por dónde salir sin que se vea; se reintenta en Update
            }
            // T-1322: con 5 estrellas las que pasan de las del nivel son camionetas (si hay prefab). T-1499: desde 3 estrellas, una de cada dos.
            PatrullaMundo molde = camioneta && prefabCamioneta != null ? prefabCamioneta : prefabPatrulla; // T-1620: lo decide Reconciliar con el director
            PatrullaMundo patrulla = Instantiate(molde, transform);
            patrulla.Preparar(calculo, config, calles, salida, PosicionDelJugador);
            patrulla.Intercepta = patrullas.Count % 2 == 1; // T-587: cada segunda patrulla corta el paso
            patrullas.Add(patrulla);
            if (camioneta)
            {
                camionetas.Add(patrulla);
            }
            RegistrarPatrulla(camioneta ? TipoRespuesta.Camioneta : TipoRespuesta.Patrulla, patrulla);
            return true;
        }

        /// <summary>Arma el grafo de calles con las manzanas (hijos "Manzana_") de un barrio (T-451: el tráfico de calle lo comparte).</summary>
        /// <summary>T-533: la patrulla sale en el nodo más cercano a <see cref="DistanciaSalidaPatrulla"/> m del jugador (no al otro lado del barrio).</summary>
        private int NodoDeSalida(Vector3 centro)
        {
            // T-664: la patrulla solo sale en un nodo a más de DistanciaMinimaSalida m del jugador, fuera de la vista de la cámara y sin otra patrulla encima;
            // de esos, el más cercano a la distancia de salida. Si no hay ninguno devuelve -1 y la patrulla espera (nunca aparece a la vista).
            Camera camara = Camera.main;
            // T-708: la salida se elige por el tiempo real que tarda en llegar por las calles; y fuera de la vista con margen, como el pool del tráfico (T-704).
            int destino = calles.MasCercano(centro);
            List<float> metros = new List<float>(calles.Cantidad);
            List<bool> permitido = new List<bool>(calles.Cantidad);
            for (int i = 0; i < calles.Cantidad; i++)
            {
                Vector3 d = calles.Nodo(i) - centro;
                d.y = 0f;
                bool vale = calles.Dentro(i) && d.magnitude >= config.DistanciaMinimaSalida && !ReglaVistaPolicia.LoVe(camara, calles.Nodo(i), AlcanceCamara)
                    && !CarroTrafico.PuntoEnVista(camara, calles.Nodo(i)) && !HayPatrullaCerca(calles.Nodo(i))
                    && EspacioLibre.Libre(calles.Nodo(i)); // T-728: nunca encima de otro vehículo
                permitido.Add(vale);
                metros.Add(vale ? CalculoLlegadaPatrulla.MetrosDeRuta(calles, i, destino) : float.MaxValue);
            }
            int mejor = CalculoLlegadaPatrulla.Elegir(metros, permitido, config.VelocidadPatrulla, config.SegundosLlegadaPatrulla, config.SegundosMinimosLlegadaPatrulla);
            return mejor;
        }

        private bool HayPatrullaCerca(Vector3 punto)
        {
            foreach (PatrullaMundo p in patrullas)
            {
                Vector3 d = p.transform.position - punto;
                d.y = 0f;
                if (d.magnitude < SeparacionDeSalida)
                {
                    return true;
                }
            }
            return false;
        }

        public static GrafoCalles ArmarCallesDe(Transform barrio)
        {
            List<Vector2> centros = new List<Vector2>();
            foreach (Transform hijo in barrio)
            {
                if (hijo.name.StartsWith(PrefijoManzana))
                {
                    centros.Add(new Vector2(hijo.position.x, hijo.position.z));
                }
            }
            if (centros.Count == 0)
            {
                return null;
            }
            GrafoCalles grafo = GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);
            CallesDeZona.Agregar(grafo); // T-1350: las calles de las zonas nuevas (antes del alargue de las afueras)
            return grafo;
        }

        private GrafoCalles ArmarCalles()
        {
            List<Vector2> centros = new List<Vector2>();
            foreach (Transform hijo in transform.parent != null ? transform.parent : transform)
            {
                if (hijo.name.StartsWith(PrefijoManzana))
                {
                    centros.Add(new Vector2(hijo.position.x, hijo.position.z));
                }
            }
            if (centros.Count == 0)
            {
                return null;
            }
            GrafoCalles grafo = GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);
            CallesDeZona.Agregar(grafo); // T-1350
            TraficoCalle.AlargarHaciaLasAfueras(grafo); // T-587: las patrullas llegan y se van por las calles de las afueras, lejos de la vista
            AreaMuro.Recortar(grafo); // T-1526: pero solo dentro del muro: las calles de salida terminan ahí y las patrullas dan vuelta
            BloqueosDeCalles.Marcar(grafo); // T-986: manejan con física: un cierre en la calle las detiene, así que ninguna sale ni va por ahí
            return grafo;
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}
