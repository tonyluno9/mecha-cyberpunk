using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// Un carro del tráfico de la calle (T-451): anda por el grafo de calles del barrio por su carril derecho, elige al azar la calle que sigue en
    /// cada esquina, frena si hay otro carro o el jugador adelante, pita cuando lleva rato bloqueado y se detiene un rato (y pita) si oye un
    /// disparo (<see cref="EventosRuido"/>). Se mueve con el transform (cuerpo cinemático): el carro del jugador rebota contra él y se daña.
    /// </summary>
    public class CarroTrafico : MonoBehaviour
    {
        private const float AnchoCarril = 2.2f;
        private const float LargoCarro = 4.5f;
        private const float VelocidadCambioCarril = 6f;
        private const float VelocidadMinimaPasaCerca = 4f;
        private const float SegundosEntrePasaCerca = 4f;
        private const float AlturaDelCentro = 0.45f;
        // T-532: los cruces (nodos con 3 o más calles) se ceden: un carro espera afuera mientras otro que cruza en otra dirección está adentro.
        private const float RadioCruce = 8f;
        private const float DistanciaAvisoCruce = 16f;
        private const float CruceSeCruza = 0.6f;
        // T-532: lo que no es tráfico (carros estacionados o robados, patrullas, gente, puestos) se ve con un rayo en caja a la altura del cofre.
        private const float AlturaRayo = 0.9f;
        private const int MaxChoques = 8;
        // T-705: el conductor al que le roban el carro pierde de vista al jugador (y cancela su llamada) a esta distancia.
        public const string NombreConductorRobado = "ConductorRobado";
        public const float RadioAbandonoConductor = 25f;
        // T-532: aparece y desaparece lejos y fuera de la vista.
        private const float DistanciaOculta = 45f;
        // T-704: nace y se borra solo fuera del frustum de la cámara (con este margen en metros alrededor del carro) y lejos del jugador.
        private const float MargenVistaMetros = 3f;
        private const float MitadCarroAncho = 1.2f;
        private const float MitadCarroLargo = 2.5f;
        // T-704: se pega al suelo con un rayo: al nacer y cada tanto, para que no flote ni se hunda.
        private const float SegundosEntreRayosSuelo = 0.3f;
        private const float VelocidadAjusteSuelo = 6f;
        private const float AltoRayoSuelo = 2f;
        private const float LargoRayoSuelo = 10f;
        private const float MinimoRaizABajo = 0.05f;
        private const float MaximoRaizABajo = 1.2f;
        private const float SeparacionAlAparecer = 14f;
        private static readonly Plane[] PlanosVista = new Plane[6];
        private readonly RaycastHit[] golpesSuelo = new RaycastHit[8];
        private Renderer[] visuales;
        private float raizABajo = -1f;
        private float alturaSuelo;
        private float objetivoSuelo;
        private float proximoRayoSuelo;
        private static readonly Vector3 MitadRayo = new Vector3(0.95f, 0.35f, 0.1f);
        private readonly RaycastHit[] choques = new RaycastHit[MaxChoques];

        /// <summary>Los carros de tráfico que andan ahora (para que se vean entre sí).</summary>
        public static readonly List<CarroTrafico> Activos = new List<CarroTrafico>();

        private ConfigTraficoCalle config;
        private GrafoCalles calles;
        private CalculoTraficoCalle calculo;
        private System.Random azar;
        private Func<Vector3?> posicionJugador;
        private int nodoAnterior = -1;
        private int nodoDestino;
        private Vector3 eje;
        private Vector3 desvio;
        private Vector3 direccion = Vector3.forward;
        private float proximoPasaCerca;
        private int pitidos;
        private const float SegundosMinimosEntreClaxon = 1.5f;
        private const float SegundosMaximosEntreClaxon = 4f;
        private float proximoClaxon;
        private float ultimoChoque = -100f;
        private Renderer carroceria;

        private static readonly Color[] ColoresCarroceria =
        {
            new Color(0.75f, 0.1f, 0.1f), new Color(0.1f, 0.35f, 0.75f), new Color(0.9f, 0.75f, 0.1f), new Color(0.15f, 0.55f, 0.35f),
            new Color(0.85f, 0.85f, 0.88f), new Color(0.3f, 0.3f, 0.35f), new Color(0.75f, 0.35f, 0.7f),
        };

        [Tooltip("Altura (m) a la que va la raíz del carro sobre la calle: 0.45 el carro de cubos, 0.3 los carros manejables con modelo real.")]
        [Min(0f)] [SerializeField] private float alturaRaiz = 0.45f;
        [Tooltip("T-511: persona que se baja huyendo cuando le roban el carro (el prefab Peaton).")]
        [SerializeField] private GameObject prefabConductor;

        private IMontable montable;
        private Rigidbody cuerpo;

        /// <summary>True si el jugador ya se llevó este carro (T-511): deja de ser tráfico y es un carro suyo.</summary>
        public bool Robado { get; private set; }

        /// <summary>T-703: su velocidad normal de crucero (m/s), distinta en cada carro.</summary>
        public float VelocidadPropia { get; private set; }

        /// <summary>T-703: True mientras sigue detenido por un choque.</summary>
        public bool Chocado => calculo != null && calculo.Chocado;

        /// <summary>T-703: cuántos choques ha tenido que lo hicieron reaccionar.</summary>
        public int Choques { get; private set; }

        /// <summary>T-703: cómo fue el último choque.</summary>
        public NivelChoque UltimoChoque { get; private set; }

        /// <summary>T-703: el conductor enojado que se bajó tras un choque fuerte (null si no hay).</summary>
        public ConductorEnojado ConductorAfuera { get; private set; }

        public float Velocidad => calculo != null ? calculo.Velocidad : 0f;
        public bool EnPanico => calculo != null && calculo.EnPanico;
        public bool Bloqueado => calculo != null && calculo.Bloqueado;
        /// <summary>Cuántas veces ha pitado.</summary>
        public int Pitidos => pitidos;
        public int NodoDestino => nodoDestino;

        /// <summary>Qué lo detiene ahora ("libre", "carro X", "jugador", "cruce" o "fisico"): para depurar atorones (T-588).</summary>
        public string Causa { get; private set; } = "libre";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Activos.Clear();

        /// <summary>Lo prepara el administrador: números, calles, nodo de salida, azar y cómo saber dónde está el jugador.</summary>
        public void Preparar(ConfigTraficoCalle numeros, GrafoCalles grafo, int nodoInicial, System.Random azarCarro, Func<Vector3?> dondeEstaElJugador)
        {
            config = numeros;
            calles = grafo;
            azar = azarCarro;
            posicionJugador = dondeEstaElJugador;
            // T-703: cada carro anda a su propia velocidad normal, dentro del rango de la config (antes todos iban a 7 m/s, muy lento).
            VelocidadPropia = ReglaChoqueTrafico.VelocidadDeCarro(numeros.VelocidadMinima, numeros.VelocidadMaxima, azarCarro.NextDouble());
            calculo = numeros.Crear(VelocidadPropia);
            eje = grafo.Nodo(nodoInicial);
            nodoAnterior = nodoInicial;
            nodoDestino = ElegirSiguiente(nodoInicial, -1);
            direccion = DireccionA(nodoDestino);
            desvio = Derecha(direccion) * config.SeparacionCarril;
            PegarAlSuelo();
            transform.SetPositionAndRotation(Posicion(), Quaternion.LookRotation(direccion, Vector3.up));
            PintarCarroceria();
        }

        // Cada carro sale de un color al azar (la carrocería es el hijo "Carroceria").
        private void PintarCarroceria()
        {
            if (carroceria == null)
            {
                Transform hijo = transform.Find("Carroceria");
                carroceria = hijo != null ? hijo.GetComponent<Renderer>() : null;
            }
            if (carroceria == null || !carroceria.enabled)
            {
                return;
            }
            Color color = ColoresCarroceria[azar.Next(ColoresCarroceria.Length)];
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            carroceria.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", color);
            bloque.SetColor("_Color", color);
            carroceria.SetPropertyBlock(bloque);
            Transform cabina = transform.Find("Cabina");
            Renderer rc = cabina != null ? cabina.GetComponent<Renderer>() : null;
            if (rc != null)
            {
                MaterialPropertyBlock bloqueCabina = new MaterialPropertyBlock();
                Color oscuro = color * 0.8f;
                oscuro.a = 1f;
                bloqueCabina.SetColor("_BaseColor", oscuro);
                bloqueCabina.SetColor("_Color", oscuro);
                rc.SetPropertyBlock(bloqueCabina);
            }
        }

        private void Awake()
        {
            montable = GetComponent<IMontable>();
            cuerpo = GetComponent<Rigidbody>();
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
        }

        // T-704: qué tan abajo de la raíz quedan las ruedas (la parte más baja del modelo); así la raíz se pone justo sobre el suelo, flote o se hunda el prefab.
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
            if (!hay)
            {
                return alturaRaiz;
            }
            return Mathf.Clamp(transform.position.y - bajo, MinimoRaizABajo, MaximoRaizABajo);
        }

        /// <summary>T-704: qué tan abajo de la raíz quedan las ruedas (m). La raíz va a esta altura sobre el suelo.</summary>
        public float RaizABajo => raizABajo >= 0f ? raizABajo : alturaRaiz;

        /// <summary>T-704: la altura del suelo (m) bajo el carro, la última que le dio el rayo.</summary>
        public float AlturaSuelo => alturaSuelo;

        // Rayo hacia abajo desde un poco arriba del carro: el suelo es lo primero sólido que no sea él mismo, otro carro ni gente.
        private float BuscarSuelo(Vector3 xz, float desdeY, float respaldo)
        {
            Vector3 origen = new Vector3(xz.x, desdeY + AltoRayoSuelo, xz.z);
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, golpesSuelo, LargoRayoSuelo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            float y = respaldo;
            for (int i = 0; i < n; i++)
            {
                Collider c = golpesSuelo[i].collider;
                if (c == null || c.transform.IsChildOf(transform) || c.attachedRigidbody != null || c.GetComponentInParent<PeatonMundo>() != null)
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

        /// <summary>T-704: lo pega al suelo de inmediato con un rayo (al nacer, al reaparecer).</summary>
        public void PegarAlSuelo()
        {
            if (calles == null)
            {
                return;
            }
            alturaSuelo = BuscarSuelo(new Vector3(eje.x + desvio.x, 0f, eje.z + desvio.z), eje.y, eje.y);
            objetivoSuelo = alturaSuelo;
            proximoRayoSuelo = Time.time + SegundosEntreRayosSuelo;
        }

        private void SeguirElSuelo(float dt)
        {
            if (Time.time >= proximoRayoSuelo)
            {
                proximoRayoSuelo = Time.time + SegundosEntreRayosSuelo;
                objetivoSuelo = BuscarSuelo(new Vector3(eje.x + desvio.x, 0f, eje.z + desvio.z), alturaSuelo, objetivoSuelo);
            }
            alturaSuelo = Mathf.MoveTowards(alturaSuelo, objetivoSuelo, VelocidadAjusteSuelo * dt);
        }

        // T-704: los límites del carro (sus mallas) en el mundo, con un margen alrededor.
        private Bounds LimitesConMargen()
        {
            bool primero = true;
            Bounds b = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer r in visuales)
            {
                if (!r.enabled)
                {
                    continue;
                }
                if (primero)
                {
                    b = r.bounds;
                    primero = false;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            if (primero)
            {
                b = new Bounds(transform.position + Vector3.up * 0.8f, new Vector3(MitadCarroAncho * 2f, 1.6f, MitadCarroLargo * 2f));
            }
            b.Expand(MargenVistaMetros * 2f);
            return b;
        }

        /// <summary>T-704: True si el carro (con un margen) está dentro de lo que ve la cámara.</summary>
        public bool EnVista(Camera camara)
        {
            return camara != null && DentroDelFrustum(camara, LimitesConMargen());
        }

        /// <summary>T-704: True si un carro parado en <paramref name="centro"/> (en el piso) se vería (con un margen).</summary>
        public static bool PuntoEnVista(Camera camara, Vector3 centro)
        {
            if (camara == null)
            {
                return false;
            }
            Bounds b = new Bounds(centro + Vector3.up * 0.8f, new Vector3(MitadCarroLargo * 2f, 1.6f, MitadCarroLargo * 2f));
            b.Expand(MargenVistaMetros * 2f);
            return DentroDelFrustum(camara, b);
        }

        private static bool DentroDelFrustum(Camera camara, Bounds limites)
        {
            GeometryUtility.CalculateFrustumPlanes(camara, PlanosVista);
            return GeometryUtility.TestPlanesAABB(PlanosVista, limites);
        }

        private void OnEnable()
        {
            Activos.Add(this);
            EventosRuido.Disparo += AlOirDisparo;
            if (montable != null)
            {
                montable.OcupadoCambio += AlCambiarOcupado;
            }
        }

        private void OnDisable()
        {
            Activos.Remove(this);
            EventosRuido.Disparo -= AlOirDisparo;
            if (montable != null)
            {
                montable.OcupadoCambio -= AlCambiarOcupado;
            }
        }

        // T-511: el jugador subió (solo puede si el carro está detenido): el conductor se baja huyendo y el carro pasa a ser suyo (el calor lo suma CarroEstacionado).
        private void AlCambiarOcupado(bool ocupado)
        {
            if (!ocupado || Robado)
            {
                return;
            }
            Robado = true;
            if (cuerpo != null)
            {
                cuerpo.isKinematic = false;
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            Sonidos.ReproducirEn("VEH_CLAXON_TRAFICO", transform.position);
            SacarConductorRobado();
            enabled = false;
        }

        /// <summary>T-705: True mientras el jugador ya subió y el carro todavía no lo sabe (el conductor aún está adentro).</summary>
        public bool EstaSiendoRobado => !Robado && montable != null && montable.Ocupado;

        /// <summary>T-705: el conductor que sacaron del carro (null si no hay o aún no sale).</summary>
        public PeatonMundo ConductorSacado { get; private set; }

        /// <summary>
        /// T-705: el conductor sale del asiento (lo sacan), cae al suelo y se levanta asustado; es un testigo (<see cref="PeatonMundo"/>) que
        /// puede llamar a la policía. Se puede llamar antes que el aviso del robo (lo hace <c>TestigosDelito</c> para que cuente como testigo); la
        /// segunda vez no hace nada. Devuelve el conductor.
        /// </summary>
        public PeatonMundo SacarConductorRobado()
        {
            if (ConductorSacado != null || prefabConductor == null || montable == null)
            {
                return ConductorSacado;
            }
            Vector3 asiento = montable.Asiento != null ? montable.Asiento.position : transform.position;
            Vector3? jugador = posicionJugador?.Invoke();
            GameObject conductor = Instantiate(prefabConductor, asiento, Quaternion.identity);
            conductor.name = NombreConductorRobado;
            PeatonMundo peaton = conductor.GetComponent<PeatonMundo>();
            if (peaton == null)
            {
                peaton = conductor.AddComponent<PeatonMundo>();
            }
            peaton.RadioAbandonoPropio = RadioAbandonoConductor;
            conductor.AddComponent<ConductorRobado>().Iniciar(asiento, montable.PuntoSubida, jugador ?? transform.position);
            ConductorSacado = peaton;
            return peaton;
        }

        // T-703: lo chocó otro carro (el del jugador, una patrulla): reacciona según qué tan fuerte fue.
        private void OnCollisionEnter(Collision colision)
        {
            if (Robado || colision.collider == null || colision.collider.transform.root == transform.root)
            {
                return;
            }
            Vector3 donde = colision.contactCount > 0 ? colision.GetContact(0).point : colision.collider.transform.position;
            AlChocar(colision.relativeVelocity.magnitude, donde);
        }

        /// <summary>
        /// T-703: un choque de rapidez relativa <paramref name="rapidez"/> (m/s) contra algo en <paramref name="donde"/>. Si es más que un roce,
        /// el carro se detiene (leve o fuerte), pita y, en uno fuerte, el conductor se baja enojado a reclamar. Devuelve cómo fue.
        /// </summary>
        public NivelChoque AlChocar(float rapidez, Vector3 donde)
        {
            NivelChoque nivel = ReglaChoqueTrafico.Nivel(rapidez);
            if (nivel == NivelChoque.Ninguno || calculo == null || Robado || !ReglaChoqueTrafico.PuedeReaccionar(Time.time, ultimoChoque))
            {
                return NivelChoque.Ninguno;
            }
            ultimoChoque = Time.time;
            Choques++;
            UltimoChoque = nivel;
            calculo.Detener(ReglaChoqueTrafico.SegundosDetenido(nivel));
            proximoClaxon = 0f; // el claxon del choque suena de inmediato, sin esperar el descanso al azar
            if (ReglaChoqueTrafico.ConductorSeBaja(nivel) && ConductorAfuera == null)
            {
                ConductorAfuera = BajarConductorEnojado(donde);
            }
            return nivel;
        }

        private ConductorEnojado BajarConductorEnojado(Vector3 haciaQuien)
        {
            if (prefabConductor == null)
            {
                return null;
            }
            Vector3 puerta = montable != null ? montable.PuntoSubida : transform.position - transform.right * 1.5f;
            GameObject conductor = Instantiate(prefabConductor, puerta, Quaternion.identity);
            conductor.name = "ConductorEnojado";
            PeatonMundo peaton = conductor.GetComponent<PeatonMundo>();
            if (peaton != null)
            {
                Destroy(peaton);
            }
            ConductorEnojado enojado = conductor.AddComponent<ConductorEnojado>();
            enojado.Iniciar(haciaQuien, ReglaChoqueTrafico.SegundosConductorAfuera);
            return enojado;
        }

        /// <summary>Un disparo: si está cerca el carro se asusta (se detiene y pita).</summary>
        public void AlOirDisparo(Vector3 posicion)
        {
            if (calculo == null || config == null || !isActiveAndEnabled)
            {
                return;
            }
            Vector3 d = posicion - transform.position;
            d.y = 0f;
            if (d.magnitude <= config.RadioSusto)
            {
                calculo.Asustar(config.SegundosSusto);
            }
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            float dt = Time.deltaTime;
            Vector3? jugador = posicionJugador?.Invoke();
            SeguirElSuelo(dt);
            if (EnRefuerzo && RefuerzoLlego)
            {
                return; // T-714: ya llegó y está estacionado
            }
            if (EnRefuerzo && jugador.HasValue && Distancia(jugador.Value, transform.position) <= DistanciaLlegadaRefuerzo)
            {
                EstacionarRefuerzo(); // T-714: ya está junto al jugador (no sigue hasta el nodo si él está en medio de la calle)
                return;
            }
            float velocidad = calculo.Avanzar(dt, DistanciaAlObstaculo(jugador));
            if (EnRefuerzo)
            {
                // T-714: frena suave al acercarse al último nodo (no se clava de golpe).
                velocidad = Mathf.Min(velocidad, Mathf.Max(FrenadoMinimoRefuerzo, MetrosPorRecorrer * FactorFrenadoRefuerzo));
            }
            if (!EnRefuerzo && calculo.SegundosBloqueado > 0f && Causa != "jugador" && Destrabar())
            {
                return;
            }
            Mover(velocidad, dt);
            if (calculo.TomarPitido())
            {
                pitidos++;
                // T-595: cada carro deja pasar un tiempo al azar antes de volver a sonar el claxon (no todos pitan igual ni a la vez).
                if (Time.time >= proximoClaxon)
                {
                    proximoClaxon = Time.time + UnityEngine.Random.Range(SegundosMinimosEntreClaxon, SegundosMaximosEntreClaxon);
                    Sonidos.ReproducirEn("VEH_CLAXON_TRAFICO", transform.position);
                }
            }
            if (jugador.HasValue && velocidad >= VelocidadMinimaPasaCerca && Time.time >= proximoPasaCerca
                && Distancia(jugador.Value, transform.position) <= config.DistanciaPasaCerca)
            {
                proximoPasaCerca = Time.time + SegundosEntrePasaCerca;
                Sonidos.ReproducirEn("VEH_PASA_CERCA", transform.position);
            }
        }

        // T-588: atorado mucho rato (detrás de algo fijo, un carro parado o un cruce sin salida): vuelve a salir en otra calle, de preferencia fuera de la vista.
        // T-704: si se ve, no desaparece: tras el tiempo forzado da la vuelta en su carril para salir del atorón.
        private bool Destrabar()
        {
            bool seVe = EnVista(Camera.main);
            if (!CalculoTraficoCalle.DebeDestrabarse(calculo.SegundosBloqueado, seVe, config.SegundosParaDestrabar, config.SegundosDestrabarForzado))
            {
                return false;
            }
            if (Reaparecer(false))
            {
                Causa = "libre";
                return true;
            }
            if (seVe && calculo.SegundosBloqueado >= config.SegundosDestrabarForzado)
            {
                DarVuelta();
            }
            return false;
        }

        /// <summary>T-704: da media vuelta en su carril (sigue por donde venía). Cuenta como salir del atorón.</summary>
        public void DarVuelta()
        {
            if (calles == null || nodoAnterior < 0 || nodoAnterior == nodoDestino)
            {
                return;
            }
            int nodo = nodoDestino;
            nodoDestino = nodoAnterior;
            nodoAnterior = nodo;
            direccion = DireccionA(nodoDestino);
            calculo.ReiniciarBloqueo();
            Vueltas++;
        }

        /// <summary>T-704: cuántas veces dio la vuelta para salir de un atorón a la vista.</summary>
        public int Vueltas { get; private set; }

        private void Mover(float velocidad, float dt)
        {
            Vector3 haciaNodo = calles.Nodo(nodoDestino) - eje;
            haciaNodo.y = 0f;
            if (haciaNodo.magnitude <= config.MargenNodo)
            {
                int llegado = nodoDestino;
                if (EnRefuerzo)
                {
                    // T-714: va por su ruta; en el último nodo se estaciona y avisa que llegó.
                    if (rutaRefuerzo.Count == 0)
                    {
                        EstacionarRefuerzo();
                        return;
                    }
                    nodoDestino = rutaRefuerzo[0];
                    rutaRefuerzo.RemoveAt(0);
                    nodoAnterior = llegado;
                    haciaNodo = calles.Nodo(nodoDestino) - eje;
                    haciaNodo.y = 0f;
                }
                else
                {
                    if (calles.Vecinos(llegado).Count == 1 && Reaparecer(true))
                    {
                        return;
                    }
                    nodoDestino = ElegirSiguiente(llegado, nodoAnterior);
                    nodoAnterior = llegado;
                    haciaNodo = calles.Nodo(nodoDestino) - eje;
                    haciaNodo.y = 0f;
                }
            }
            if (haciaNodo.sqrMagnitude > 0.0001f)
            {
                direccion = haciaNodo.normalized;
            }
            eje += direccion * (velocidad * dt);
            desvio = Vector3.MoveTowards(desvio, Derecha(direccion) * config.SeparacionCarril, VelocidadCambioCarril * dt);
            Quaternion meta = Quaternion.LookRotation(direccion, Vector3.up);
            transform.SetPositionAndRotation(Posicion(), Quaternion.RotateTowards(transform.rotation, meta, config.GiroMaximo * dt));
        }

        // ---- Refuerzo de los SKULLS en carro (T-714) ----

        private readonly List<int> rutaRefuerzo = new List<int>();
        private const float DistanciaLlegadaRefuerzo = 12f;
        private const float FrenadoMinimoRefuerzo = 1.5f;
        private const float FactorFrenadoRefuerzo = 0.8f;

        /// <summary>T-714: True mientras este carro del pool va de refuerzo (o ya llegó): deja de ser tráfico normal, no lo apaga el pool ni sigue calles al azar.</summary>
        public bool EnRefuerzo { get; private set; }

        /// <summary>T-714: True si ya llegó al último nodo de su ruta y está estacionado.</summary>
        public bool RefuerzoLlego { get; private set; }

        /// <summary>T-714: avisa cuando el carro de refuerzo llega y se estaciona (de ahí se bajan los pasajeros).</summary>
        public event Action<CarroTrafico> AlLlegarRefuerzo;

        /// <summary>T-714: metros que le faltan por la ruta hasta el último nodo.</summary>
        public float MetrosPorRecorrer
        {
            get
            {
                if (!EnRefuerzo || calles == null || RefuerzoLlego)
                {
                    return 0f;
                }
                Vector3 actual = new Vector3(eje.x, 0f, eje.z);
                float metros = Vector3.Distance(actual, new Vector3(calles.Nodo(nodoDestino).x, 0f, calles.Nodo(nodoDestino).z));
                Vector3 anterior = calles.Nodo(nodoDestino);
                foreach (int nodo in rutaRefuerzo)
                {
                    metros += Vector3.Distance(new Vector3(anterior.x, 0f, anterior.z), new Vector3(calles.Nodo(nodo).x, 0f, calles.Nodo(nodo).z));
                    anterior = calles.Nodo(nodo);
                }
                return metros;
            }
        }

        /// <summary>
        /// T-714: saca este carro del pool y lo manda de refuerzo: nace en <paramref name="nodoSalida"/> (fuera de la vista, lo elige quien lo llama) y va por las calles al nodo
        /// <paramref name="nodoLlegada"/>, donde se estaciona. Devuelve false si ya es refuerzo, lo robaron o no tiene calles.
        /// </summary>
        public bool IniciarRefuerzo(int nodoSalida, int nodoLlegada, float velocidad = 0f)
        {
            if (EnRefuerzo || Robado || calles == null || azar == null || nodoSalida < 0 || nodoLlegada < 0)
            {
                return false;
            }
            List<int> camino = calles.Camino(nodoSalida, nodoLlegada);
            if (camino == null || camino.Count == 0)
            {
                return false;
            }
            EnRefuerzo = true;
            RefuerzoLlego = false;
            if (velocidad > 0f)
            {
                VelocidadPropia = velocidad; // T-714: la ruta se eligió para que llegue en un tiempo, a esta velocidad
                calculo = config.Crear(velocidad);
            }
            rutaRefuerzo.Clear();
            for (int i = 1; i < camino.Count; i++)
            {
                rutaRefuerzo.Add(camino[i]);
            }
            eje = calles.Nodo(nodoSalida);
            nodoAnterior = nodoSalida;
            if (rutaRefuerzo.Count > 0)
            {
                nodoDestino = rutaRefuerzo[0];
                rutaRefuerzo.RemoveAt(0);
            }
            else
            {
                nodoDestino = nodoSalida;
            }
            direccion = DireccionA(nodoDestino);
            desvio = Derecha(direccion) * config.SeparacionCarril;
            PegarAlSuelo();
            transform.SetPositionAndRotation(Posicion(), Quaternion.LookRotation(direccion, Vector3.up));
            gameObject.SetActive(true);
            calculo.ReiniciarBloqueo();
            return true;
        }

        private void EstacionarRefuerzo()
        {
            RefuerzoLlego = true;
            Sonidos.ReproducirEn("VEH_PUERTA_ABRIR", transform.position);
            AlLlegarRefuerzo?.Invoke(this);
        }

        private Vector3 Posicion() => new Vector3(eje.x + desvio.x, alturaSuelo + RaizABajo, eje.z + desvio.z);

        private Vector3 DireccionA(int nodo)
        {
            Vector3 d = calles.Nodo(nodo) - eje;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }

        private static Vector3 Derecha(Vector3 adelante) => Vector3.Cross(Vector3.up, adelante);

        // La calle que sigue: al azar entre las que salen del nodo, sin regresar por donde vino salvo que no haya otra.
        private int ElegirSiguiente(int nodo, int deDonde)
        {
            IReadOnlyList<int> vecinos = calles.Vecinos(nodo);
            List<int> opciones = new List<int>();
            foreach (int v in vecinos)
            {
                if (v != deDonde)
                {
                    opciones.Add(v);
                }
            }
            if (opciones.Count == 0)
            {
                return vecinos.Count > 0 ? vecinos[0] : nodo;
            }
            return opciones[azar.Next(opciones.Count)];
        }

        // Qué tan lejos está lo que le estorba adelante (otro carro de tráfico o el jugador, en su carril); float.MaxValue si nada.
        private float DistanciaAlObstaculo(Vector3? jugador)
        {
            float menor = float.MaxValue;
            Causa = "libre";
            Vector3 adelante = transform.forward;
            Vector3 derecha = transform.right;
            for (int i = 0; i < Activos.Count; i++)
            {
                CarroTrafico otro = Activos[i];
                if (otro != this)
                {
                    Anotar(ref menor, DistanciaDelante(otro.transform.position, adelante, derecha), "carro " + otro.name);
                }
            }
            if (jugador.HasValue)
            {
                Anotar(ref menor, DistanciaDelante(jugador.Value, adelante, derecha), "jugador");
            }
            Anotar(ref menor, DistanciaAlCruce(), "cruce");
            Anotar(ref menor, DistanciaFisica(adelante), "fisico");
            return menor;
        }

        // T-588: guarda también qué es lo que lo detiene (para depurar atorones).
        private void Anotar(ref float menor, float distancia, string causa)
        {
            if (distancia < menor)
            {
                menor = distancia;
                Causa = causa;
            }
        }

        // T-532: cede el cruce si otro carro que va en otra dirección ya está adentro o llega antes.
        private float DistanciaAlCruce()
        {
            if (calles.Vecinos(nodoDestino).Count < 3)
            {
                return float.MaxValue;
            }
            Vector3 cruce = calles.Nodo(nodoDestino);
            float mia = Distancia(transform.position, cruce);
            if (mia <= RadioCruce || mia > DistanciaAvisoCruce)
            {
                return float.MaxValue;
            }
            for (int i = 0; i < Activos.Count; i++)
            {
                CarroTrafico otro = Activos[i];
                if (otro == this || Mathf.Abs(Vector3.Dot(otro.transform.forward, transform.forward)) > CruceSeCruza)
                {
                    continue;
                }
                float suya = Distancia(otro.transform.position, cruce);
                bool adentro = suya <= RadioCruce;
                bool llegaAntes = otro.nodoDestino == nodoDestino && suya < mia;
                if (adentro || llegaAntes)
                {
                    return Mathf.Max(0f, mia - RadioCruce);
                }
            }
            return float.MaxValue;
        }

        // T-532: carros estacionados, robados, patrullas, gente o puestos en el carril (todo lo que tenga collider y no sea este carro).
        private float DistanciaFisica(Vector3 adelante)
        {
            Vector3 centro = transform.position + Vector3.up * (AlturaRayo - RaizABajo) + adelante * (LargoCarro * 0.5f);
            int n = Physics.BoxCastNonAlloc(centro, MitadRayo, adelante, choques, transform.rotation, config.DistanciaFrenado, ~0, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Collider c = choques[i].collider;
                if (c == null || c.transform.root == transform.root || choques[i].distance <= 0f)
                {
                    continue;
                }
                menor = Mathf.Min(menor, choques[i].distance);
            }
            return menor;
        }

        /// <summary>
        /// T-532: vuelve a salir en un nodo lejos del jugador y fuera de la vista de la cámara; T-704: y solo si el carro tampoco se ve ahora (con <paramref name="soloFinales"/>, solo en los finales
        /// de las calles de las afueras). Devuelve false si no encontró dónde.
        /// </summary>
        public bool Reaparecer(bool soloFinales, bool desdeInactivo = false)
        {
            if (calles == null || azar == null)
            {
                return false;
            }
            Vector3? jugador = posicionJugador?.Invoke();
            Camera camara = Camera.main;
            if (!desdeInactivo && EnVista(camara))
            {
                return false; // T-704: no desaparece a la vista de la cámara
            }
            int elegido = -1;
            int intentos = calles.Cantidad * 3;
            for (int i = 0; i < intentos && elegido < 0; i++)
            {
                int nodo = azar.Next(calles.Cantidad);
                if (soloFinales && calles.Vecinos(nodo).Count != 1)
                {
                    continue;
                }
                Vector3 p = calles.Nodo(nodo);
                if (jugador.HasValue && Distancia(p, jugador.Value) < DistanciaOculta)
                {
                    continue;
                }
                if (PuntoEnVista(camara, p))
                {
                    continue;
                }
                if (HayCarroCerca(p))
                {
                    continue; // T-588: no sale encimado a otro
                }
                elegido = nodo;
            }
            if (elegido < 0)
            {
                return false;
            }
            eje = calles.Nodo(elegido);
            nodoAnterior = elegido;
            nodoDestino = ElegirSiguiente(elegido, -1);
            direccion = DireccionA(nodoDestino);
            desvio = Derecha(direccion) * config.SeparacionCarril;
            PegarAlSuelo();
            transform.SetPositionAndRotation(Posicion(), Quaternion.LookRotation(direccion, Vector3.up));
            return true;
        }

        private bool HayCarroCerca(Vector3 punto)
        {
            for (int i = 0; i < Activos.Count; i++)
            {
                if (Activos[i] != this && Distancia(Activos[i].transform.position, punto) < SeparacionAlAparecer)
                {
                    return true;
                }
            }
            return false;
        }

        private float DistanciaDelante(Vector3 punto, Vector3 adelante, Vector3 derecha)
        {
            Vector3 hacia = punto - transform.position;
            hacia.y = 0f;
            float delante = Vector3.Dot(hacia, adelante);
            if (delante <= 0f || delante > config.DistanciaFrenado + LargoCarro || Mathf.Abs(Vector3.Dot(hacia, derecha)) > AnchoCarril)
            {
                return float.MaxValue;
            }
            return Mathf.Max(0f, delante - LargoCarro);
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
