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
        // T-532: aparece y desaparece lejos y fuera de la vista.
        private const float DistanciaOculta = 45f;
        private const float MargenVista = 0.1f;
        private const float AlcanceVista = 160f;
        private const float SeparacionAlAparecer = 14f;
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
            calculo = numeros.Crear();
            eje = grafo.Nodo(nodoInicial);
            nodoAnterior = nodoInicial;
            nodoDestino = ElegirSiguiente(nodoInicial, -1);
            direccion = DireccionA(nodoDestino);
            desvio = Derecha(direccion) * config.SeparacionCarril;
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
            Vector3 puerta = montable.PuntoSubida;
            Vector3? jugador = posicionJugador?.Invoke();
            if (cuerpo != null)
            {
                cuerpo.isKinematic = false;
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            Sonidos.ReproducirEn("VEH_CLAXON_TRAFICO", transform.position);
            SacarConductor(puerta, jugador ?? transform.position);
            enabled = false;
        }

        private void SacarConductor(Vector3 puerta, Vector3 amenaza)
        {
            if (prefabConductor == null)
            {
                return;
            }
            GameObject conductor = Instantiate(prefabConductor, puerta, Quaternion.identity);
            conductor.name = "ConductorHuyendo";
            PeatonMundo peaton = conductor.GetComponent<PeatonMundo>();
            if (peaton != null)
            {
                Destroy(peaton);
            }
            conductor.AddComponent<ConductorHuye>().Iniciar(amenaza);
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
            float velocidad = calculo.Avanzar(dt, DistanciaAlObstaculo(jugador));
            if (calculo.SegundosBloqueado > 0f && Causa != "jugador" && Destrabar())
            {
                return;
            }
            Mover(velocidad, dt);
            if (calculo.TomarPitido())
            {
                pitidos++;
                Sonidos.ReproducirEn("VEH_CLAXON_TRAFICO", transform.position);
            }
            if (jugador.HasValue && velocidad >= VelocidadMinimaPasaCerca && Time.time >= proximoPasaCerca
                && Distancia(jugador.Value, transform.position) <= config.DistanciaPasaCerca)
            {
                proximoPasaCerca = Time.time + SegundosEntrePasaCerca;
                Sonidos.ReproducirEn("VEH_PASA_CERCA", transform.position);
            }
        }

        // T-588: atorado mucho rato (detrás de algo fijo, un carro parado o un cruce sin salida): vuelve a salir en otra calle, de preferencia fuera de la vista.
        private bool Destrabar()
        {
            bool seVe = SeVe(Camera.main, transform.position);
            bool sale = CalculoTraficoCalle.DebeDestrabarse(calculo.SegundosBloqueado, seVe, config.SegundosParaDestrabar, config.SegundosDestrabarForzado)
                && Reaparecer(false);
            if (sale)
            {
                Causa = "libre";
            }
            return sale;
        }

        private void Mover(float velocidad, float dt)
        {
            Vector3 haciaNodo = calles.Nodo(nodoDestino) - eje;
            haciaNodo.y = 0f;
            if (haciaNodo.magnitude <= config.MargenNodo)
            {
                int llegado = nodoDestino;
                if (calles.Vecinos(llegado).Count == 1 && Reaparecer(true))
                {
                    return;
                }
                nodoDestino = ElegirSiguiente(llegado, nodoAnterior);
                nodoAnterior = llegado;
                haciaNodo = calles.Nodo(nodoDestino) - eje;
                haciaNodo.y = 0f;
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

        private Vector3 Posicion() => new Vector3(eje.x + desvio.x, alturaRaiz, eje.z + desvio.z);

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
            Vector3 centro = transform.position + Vector3.up * (AlturaRayo - alturaRaiz) + adelante * (LargoCarro * 0.5f);
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
        /// T-532: vuelve a salir en un nodo lejos del jugador y fuera de la vista de la cámara (con <paramref name="soloFinales"/>, solo en los finales
        /// de las calles de las afueras). Devuelve false si no encontró dónde.
        /// </summary>
        public bool Reaparecer(bool soloFinales)
        {
            if (calles == null || azar == null)
            {
                return false;
            }
            Vector3? jugador = posicionJugador?.Invoke();
            Camera camara = Camera.main;
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
                if (camara != null && SeVe(camara, p))
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

        private static bool SeVe(Camera camara, Vector3 punto)
        {
            if (camara == null)
            {
                return false;
            }
            Vector3 v = camara.WorldToViewportPoint(punto + Vector3.up);
            return v.z > 0f && v.z < AlcanceVista && v.x > -MargenVista && v.x < 1f + MargenVista && v.y > -MargenVista && v.y < 1f + MargenVista;
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
