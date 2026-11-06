using System.Collections.Generic;
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
        private const string PrefijoManzana = "Manzana_";
        private const string NombreCamara = "CamaraSeguridad";
        private const float LadoManzana = 80f;          // medida del barrio (CreadorBarrio, m)
        private const float AlturaCalles = 0.2f;        // sobre la banqueta (m)
        private const float GradosPorVuelta = 360f;
        public const float DistanciaSalidaPatrulla = 80f;
        private const int IntentosDeSalidaDron = 8;
        private const float SeparacionDeSalida = 20f;
        private const float AlcanceCamara = 400f;
        private const float SegundosReintentoSalida = 0.5f;

        [SerializeField] private ConfigPersecucion config;
        [SerializeField] private DronMundo prefabDron;
        [SerializeField] private PatrullaMundo prefabPatrulla;

        private readonly List<DronMundo> drones = new List<DronMundo>();
        private readonly List<PatrullaMundo> patrullas = new List<PatrullaMundo>();
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
            busqueda = new ReglaBusquedaPolicial(config.RadioBusqueda, config.SegundosParaEscapar);
            arresto = new ReglaArresto(config.DistanciaArresto, config.SegundosArresto, config.VelocidadArresto);
            zona = new ZonaBusqueda(busqueda);
        }

        private void Start() => ColocarCamaras();

        /// <summary>Cuántas cámaras de seguridad puso el gestor en las esquinas (T-662).</summary>
        public int Camaras { get; private set; }

        // T-662: cámaras de seguridad en postes, en las esquinas de los cruces de las calles del barrio. Son testigos del delito (TestigosDelito).
        private void ColocarCamaras()
        {
            if (config.CamarasPorCruce <= 0)
            {
                return;
            }
            if (calles == null)
            {
                calles = ArmarCalles();
            }
            GameObject molde = Resources.Load<GameObject>(NombreCamara);
            if (calles == null || molde == null)
            {
                return;
            }
            Transform contenedor = new GameObject("CamarasSeguridad").transform;
            contenedor.SetParent(transform, false);
            foreach (CalculoCamaras.Colocacion c in CalculoCamaras.Colocaciones(calles, config.CamarasPorCruce, config.DesfaseEsquinaCamara))
            {
                GameObject poste = Instantiate(molde, c.Posicion, Quaternion.Euler(0f, c.Giro, 0f), contenedor);
                poste.name = "CamaraSeguridad_" + (Camaras + 1);
                poste.GetComponent<CamaraSeguridad>().Configurar(config.AlcanceCamara, config.SemiAnguloCamara);
                Camaras++;
            }
        }

        private void OnEnable()
        {
            Registro<ICalor>.Agregado += Enlazar;
            Registro<ICalor>.Quitado += Desenlazar;
            EventosLugares.JugadorEntro += AlEntrarALugar;
            if (zona != null)
            {
                Registro<IZonaBusqueda>.Agregar(zona);
            }
            if (Registro<ICalor>.Actual != null)
            {
                Enlazar(Registro<ICalor>.Actual);
            }
        }

        private void OnDisable()
        {
            Registro<ICalor>.Agregado -= Enlazar;
            Registro<ICalor>.Quitado -= Desenlazar;
            EventosLugares.JugadorEntro -= AlEntrarALugar;
            if (zona != null)
            {
                Registro<IZonaBusqueda>.Quitar(zona);
            }
            busqueda?.Detener();
            Desenlazar(calor);
            Reconciliar(0);
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
            retirandose.RemoveAll(c => c == null);
            if (calor == null || calor.Nivel <= 0 || config == null)
            {
                SegundosSinVer = 0f;
                huboContacto = false;
                valorAnterior = calor != null ? calor.Valor : 0f;
                jugadorAnterior = null;
                arresto.Reiniciar();
                return;
            }
            float dt = Time.deltaTime;
            reintentoSalida -= dt;
            if (reintentoSalida <= 0f)
            {
                reintentoSalida = SegundosReintentoSalida;
                if (patrullas.Count < calculo.Patrullas(calor.Nivel))
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
            bool teVen = patrullaVe || (jugador.HasValue && (LoVeUnDron(jugador.Value) || testigos.CamaraQueVe(jugador.Value) != null));
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
            if (arresto.Avanzar(patrullaVe, cercana, velocidad, dt))
            {
                Arrestar();
                return;
            }
            if (busqueda.Avanzar(jugador.Value, teVen, dt))
            {
                Escapar();
            }
        }

        /// <summary>Una patrulla cerca te arrestó (T-665): el calor se borra, la policía se retira y se avisa en <see cref="EventosPolicia"/>.</summary>
        private void Arrestar()
        {
            Debug.Log("[Trafico] El jugador fue arrestado.");
            EventosPolicia.PublicarArresto();
            calor.Borrar();
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
            Reconciliar(calor.Nivel);
        }

        private void Desenlazar(ICalor viejo)
        {
            if (viejo != null && ReferenceEquals(viejo, calor))
            {
                calor.NivelCambio -= Reconciliar;
                calor = null;
                Reconciliar(0);
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
        private void Reconciliar(int nivel)
        {
            ActualizarBusqueda(nivel);
            int dronesPedidos = calculo == null ? 0 : calculo.Drones(nivel);
            int patrullasPedidas = calculo == null ? 0 : calculo.Patrullas(nivel);
            while (drones.Count < dronesPedidos)
            {
                CrearDron();
            }
            Retirar(drones, dronesPedidos);
            while (patrullas.Count < patrullasPedidas)
            {
                if (!CrearPatrulla())
                {
                    break; // T-598: sin calles no hay patrulla; sin este corte el ciclo no terminaba
                }
            }
            Retirar(patrullas, patrullasPedidas);
        }

        // T-587: los que sobran no desaparecen a la vista: se van por la calle (o por el aire) y se borran solos al salir de la vista.
        private void Retirar<T>(List<T> lista, int cantidad) where T : Component
        {
            while (lista.Count > cantidad)
            {
                int ultimo = lista.Count - 1;
                T quien = lista[ultimo];
                lista.RemoveAt(ultimo);
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

        private void CrearDron()
        {
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            Camera camara = Camera.main;
            Vector3 lejos = centro;
            for (int intento = 0; intento < IntentosDeSalidaDron; intento++)
            {
                float angulo = (float)azar.NextDouble() * GradosPorVuelta * Mathf.Deg2Rad;
                lejos = centro + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * config.DistanciaAparicionDron + Vector3.up * config.AlturaDron;
                if (!ReglaVistaPolicia.LoVe(camara, lejos, AlcanceCamara))
                {
                    break; // T-587: sale fuera de la vista
                }
            }
            DronMundo dron = Instantiate(prefabDron, lejos, Quaternion.identity, transform);
            dron.Preparar(calculo, config, PosicionDelJugador);
            drones.Add(dron);
        }

        private bool CrearPatrulla()
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
            PatrullaMundo patrulla = Instantiate(prefabPatrulla, transform);
            patrulla.Preparar(calculo, config, calles, salida, PosicionDelJugador);
            patrulla.Intercepta = patrullas.Count % 2 == 1; // T-587: cada segunda patrulla corta el paso
            patrullas.Add(patrulla);
            return true;
        }

        /// <summary>Arma el grafo de calles con las manzanas (hijos "Manzana_") de un barrio (T-451: el tráfico de calle lo comparte).</summary>
        /// <summary>T-533: la patrulla sale en el nodo más cercano a <see cref="DistanciaSalidaPatrulla"/> m del jugador (no al otro lado del barrio).</summary>
        private int NodoDeSalida(Vector3 centro)
        {
            // T-664: la patrulla solo sale en un nodo a más de DistanciaMinimaSalida m del jugador, fuera de la vista de la cámara y sin otra patrulla encima;
            // de esos, el más cercano a la distancia de salida. Si no hay ninguno devuelve -1 y la patrulla espera (nunca aparece a la vista).
            Camera camara = Camera.main;
            int mejor = -1;
            float error = float.MaxValue;
            for (int i = 0; i < calles.Cantidad; i++)
            {
                Vector3 d = calles.Nodo(i) - centro;
                d.y = 0f;
                if (d.magnitude < config.DistanciaMinimaSalida || ReglaVistaPolicia.LoVe(camara, calles.Nodo(i), AlcanceCamara) || HayPatrullaCerca(calles.Nodo(i)))
                {
                    continue;
                }
                float e = Mathf.Abs(d.magnitude - DistanciaSalidaPatrulla);
                if (e < error)
                {
                    error = e;
                    mejor = i;
                }
            }
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
            return centros.Count > 0 ? GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles) : null;
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
            TraficoCalle.AlargarHaciaLasAfueras(grafo); // T-587: las patrullas llegan y se van por las calles de las afueras, lejos de la vista
            return grafo;
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}
