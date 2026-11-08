using System;
using MechaCyberpunk.Vehiculos.Efectos;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Un dron de la policía (T-341): vuela sobre el jugador y se le acerca hasta la distancia de seguimiento, con su luz apuntando hacia abajo.
    /// T-710: dispara de verdad: suena el disparo, deja un trazo de la bala (T-745: termina donde pega o se desvanece al máximo alcance), si falla y la bala
    /// pasa cerca del jugador suena el silbido de la bala rozando, y si acierta hace daño real por <see cref="IRecibeDanio"/>.
    /// T-730: se puede derribar a balazos (tiene una esfera de impacto chica en el aire, donde no estorba): al quedarse sin vida deja de disparar, cae girando y explota
    /// en chico (EfectosExplosion, con un radio chico); al derribarlo baja la búsqueda (lo hace el gestor con <see cref="Derribado"/>).
    /// </summary>
    public class DronMundo : MonoBehaviour, IRecibeDanio, IObjetoInerte
    {
        private const float GradosPorSegundoGiro = 180f;

        /// <summary>Fuente del azar (0 a 1) de los disparos. Las pruebas la cambian.</summary>
        public static Func<float> Azar = () => UnityEngine.Random.value;

        private const float AlturaPecho = 1.2f;
        // T-710: trazo de la bala (hilo finísimo y cortísimo, como el del pandillero) y silbido de la bala que pasa cerca.
        public const string IdDisparo = "ARM_PIS_DISPARO";
        public const string IdRoce = "ARM_BALA_ROZA";
        private const float BajoElDron = 0.2f;

        private Light[] luces;
        private Color[] coloresLuces;
        private float proximoTiro;
        private float finAviso = -1f;
        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private Func<Vector3?> posicionJugador;

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, Func<Vector3?> dondeEstaElJugador)
        {
            calculo = reglas;
            config = numeros;
            posicionJugador = dondeEstaElJugador;
            vida = numeros != null ? numeros.VidaDron : -1f;
        }

        // ---- T-730: derribar al dron ----

        /// <summary>Radio (m) de la esfera donde le pegan las balas.</summary>
        public const float RadioImpacto = 0.6f;

        /// <summary>Radio (m) de la explosión chica al estrellarse.</summary>
        public const float RadioExplosion = 2.5f;

        public const string IdExplosion = "VEH_EXPLOSION";
        private const float GravedadCaida = 14f;
        private const float GiroCaida = 540f;
        private const float SegundosMaximosCayendo = 4f;
        private const float AlturaDeSuelo = 0.4f;

        /// <summary>Se avisa una vez por cada dron que cae (el gestor de la persecución baja la búsqueda). Se vacía al entrar a Play.</summary>
        public static event Action<DronMundo> Derribado;

        private float vida = -1f;
        private float velocidadCaida;
        private float segundosCayendo;

        /// <summary>Vida que le queda (puntos de daño). Se llena al prepararlo con <see cref="ConfigPersecucion.VidaDron"/>.</summary>
        public float Vida => vida;

        /// <summary>True desde que se queda sin vida: ya no dispara ni persigue, solo cae.</summary>
        public bool EstaCayendo { get; private set; }

        /// <summary>True una vez que se estrelló y explotó (el objeto se destruye en ese cuadro).</summary>
        public bool Exploto { get; private set; }

        /// <summary>La explosión chica de la caída (para las pruebas; null hasta que explota).</summary>
        public InstanciaExplosion Explosion { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEventos() => Derribado = null;

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (EstaCayendo || info.Cantidad <= 0f || config == null)
            {
                return ResultadoDanio.Ignorado;
            }
            if (vida < 0f)
            {
                vida = config.VidaDron;
            }
            float aplicado = Mathf.Min(info.Cantidad, vida);
            vida -= aplicado;
            if (vida <= 0f)
            {
                Derribar();
            }
            return new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, vida <= 0f);
        }

        private void Derribar()
        {
            EstaCayendo = true;
            Retirando = false;
            finAviso = -1f;
            PonerLuces(false);
            velocidadCaida = 0f;
            segundosCayendo = 0f;
            Derribado?.Invoke(this);
        }

        private void Caer(float dt)
        {
            segundosCayendo += dt;
            velocidadCaida += GravedadCaida * dt;
            Vector3 paso = Vector3.down * (velocidadCaida * dt);
            bool toca = Physics.Raycast(transform.position, Vector3.down, out RaycastHit golpe, paso.magnitude + AlturaDeSuelo, ~0, QueryTriggerInteraction.Ignore)
                && !golpe.collider.transform.IsChildOf(transform);
            transform.Rotate(Vector3.up, GiroCaida * dt, Space.World);
            transform.Rotate(Vector3.forward, GiroCaida * 0.4f * dt, Space.Self);
            if (toca || segundosCayendo >= SegundosMaximosCayendo)
            {
                Vector3 donde = toca ? golpe.point + Vector3.up * AlturaDeSuelo : transform.position;
                Estrellar(donde);
                return;
            }
            transform.position += paso;
        }

        private void Estrellar(Vector3 donde)
        {
            Exploto = true;
            Explosion = EfectosExplosion.Explosion(donde, RadioExplosion);
            Sonidos.ReproducirEn(IdExplosion, donde);
            Destroy(gameObject);
        }

        private void Awake()
        {
            if (GetComponentInChildren<Collider>() == null)
            {
                SphereCollider esfera = gameObject.AddComponent<SphereCollider>();
                esfera.radius = RadioImpacto;
                esfera.isTrigger = false;
            }
            luces = GetComponentsInChildren<Light>(true);
            coloresLuces = new Color[luces.Length];
            for (int i = 0; i < luces.Length; i++)
            {
                coloresLuces[i] = luces[i].color;
            }
        }

        // Con calor suficiente avisa 0.3 s con su luz en rojo y dispara al jugador a pie; a quien va en carro no le dispara.
        private void Disparar()
        {
            ICalor calor = Registro<ICalor>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            bool puede = calor != null && calor.Nivel >= config.NivelDisparoDron && jugador != null && jugador.Raiz != null
                && jugador.Modo == ModoJugador.APie;
            IRecibeDanio objetivo = puede ? jugador.Raiz.GetComponentInChildren<IRecibeDanio>() : null;
            IVida vida = puede ? jugador.Raiz.GetComponentInChildren<IVida>() : null;
            if (objetivo == null || (vida != null && vida.EstaKO))
            {
                finAviso = -1f;
                PonerLuces(false);
                return;
            }
            if (!HayLineaDeVista(jugador.Raiz))
            {
                // T-676: sin línea de vista (muro, barricada, techo) no avisa ni dispara.
                finAviso = -1f;
                PonerLuces(false);
                return;
            }
            if (finAviso < 0f)
            {
                if (Time.time >= proximoTiro)
                {
                    finAviso = Time.time + config.AvisoDron;
                    PonerLuces(true);
                }
                return;
            }
            if (Time.time < finAviso)
            {
                return;
            }
            finAviso = -1f;
            PonerLuces(false);
            proximoTiro = Time.time + config.SegundosEntreTirosDron;
            Disparos++;
            Vector3 origen = transform.position + Vector3.down * BajoElDron;
            Vector3 pecho = jugador.Raiz.position + Vector3.up * AlturaPecho;
            Vector3 direccion = (pecho - origen).normalized;
            EventosRuido.PublicarDisparo(origen);
            Sonidos.ReproducirEn(IdDisparo, origen);
            bool acierta = Azar() <= config.AciertoDron;
            // T-421: si el jugador está agachado tras una cubierta, la bala pega en ella y no le llega.
            if (acierta && objetivo is IProtegeDeBalas protegido && protegido.ProtegeDe(origen, out Vector3 puntoCubierta))
            {
                acierta = false;
                direccion = (puntoCubierta - origen).normalized;
            }
            else if (!acierta)
            {
                // Falla: la bala pasa a un metro o menos del jugador (de ahí el silbido).
                Vector3 lado = Vector3.Cross(direccion, Vector3.up).normalized;
                float desvio = (Azar() < 0.5f ? -1f : 1f) * (0.4f + 0.8f * Azar());
                direccion = (pecho + lado * desvio - origen).normalized;
            }
            DibujarTrazo(origen, direccion, Vector3.Distance(origen, pecho), acierta);
            if (acierta)
            {
                Aciertos++;
                objetivo.RecibirDanio(new InfoDanio(config.DanioDron, TipoDanio.Disparo, pecho, direccion, null));
                return;
            }
            float cerca = CalculoTiroDron.DistanciaALaBala(origen, direccion, pecho, out Vector3 masCercano);
            if (CalculoTiroDron.Roza(cerca, CalculoTiroDron.RadioRoce))
            {
                Rozaduras++;
                Sonidos.ReproducirEn(IdRoce, masCercano);
            }
        }

        /// <summary>T-710: cuántos disparos ha hecho.</summary>
        public int Disparos { get; private set; }

        /// <summary>T-710: cuántos le pegaron al jugador.</summary>
        public int Aciertos { get; private set; }

        /// <summary>T-710: cuántas balas pasaron lo bastante cerca del jugador como para silbar.</summary>
        public int Rozaduras { get; private set; }

        /// <summary>T-745: dónde terminó el trazo del último disparo del dron (el impacto o el final del alcance).</summary>
        public Vector3 DestinoUltimoTrazo { get; private set; }

        private void DibujarTrazo(Vector3 origen, Vector3 direccion, float distancia, bool acierta)
        {
            // T-745: si acertó, el trazo llega al pecho; si no, sigue hasta lo primero que pegue o se desvanece en el alcance máximo.
            if (acierta)
            {
                DestinoUltimoTrazo = origen + direccion * distancia;
                TrazadorBala.Trazar(origen, DestinoUltimoTrazo, true);
                return;
            }
            DestinoUltimoTrazo = TrazadorBala.TrazarDireccion(origen, direccion, transform);
        }

        private readonly RaycastHit[] impactosLinea = new RaycastHit[16];

        // True si el primer obstáculo entre el dron y el pecho del jugador es el propio jugador (o nada).
        private bool HayLineaDeVista(Transform raizJugador)
        {
            Vector3 pecho = raizJugador.position + Vector3.up * AlturaPecho;
            Vector3 hacia = pecho - transform.position;
            float distancia = hacia.magnitude;
            if (distancia < 0.01f)
            {
                return true;
            }
            int cantidad = Physics.RaycastNonAlloc(transform.position, hacia / distancia, impactosLinea, distancia, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                Collider c = impactosLinea[i].collider;
                if (c.transform.IsChildOf(raizJugador) || c.transform.IsChildOf(transform) || c.transform.root == raizJugador.root)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private void PonerLuces(bool rojo)
        {
            for (int i = 0; i < luces.Length; i++)
            {
                luces[i].color = rojo ? Color.red : coloresLuces[i];
            }
        }

        /// <summary>True si ya se va: sube y se aleja del jugador hasta salir de la vista (T-587).</summary>
        public bool Retirando { get; private set; }

        /// <summary>T-587: deja de perseguir; desaparece cuando ya no se ve y está lejos (o tras <see cref="ConfigPersecucion.SegundosMaximosRetiro"/> s).</summary>
        public void Retirar()
        {
            Retirando = true;
            segundosRetirandose = 0f;
            finAviso = -1f;
            PonerLuces(false);
        }

        private float segundosRetirandose;

        private void Retirada(Vector3? jugador, float dt)
        {
            segundosRetirandose += dt;
            Vector3 lejos = jugador.HasValue ? transform.position - jugador.Value : transform.forward;
            lejos.y = 0f;
            Vector3 direccion = (lejos.sqrMagnitude > 0.001f ? lejos.normalized : transform.forward) + Vector3.up * 0.25f;
            transform.position += direccion.normalized * (calculo.Paso(true, float.MaxValue, dt));
            bool distante = !jugador.HasValue || Vector3.Distance(transform.position, jugador.Value) >= config.DistanciaRetiro;
            if ((distante && !ReglaVistaPolicia.LoVe(Camera.main, transform.position, RetiroAlcanceCamara)) || segundosRetirandose >= config.SegundosMaximosRetiro)
            {
                Destroy(gameObject);
            }
        }

        private const float RetiroAlcanceCamara = 400f;

        private void Update()
        {
            Vector3? jugador = posicionJugador?.Invoke();
            if (calculo == null)
            {
                return;
            }
            if (EstaCayendo)
            {
                Caer(Time.deltaTime);
                return;
            }
            if (Retirando)
            {
                Retirada(jugador, Time.deltaTime);
                return;
            }
            if (!jugador.HasValue)
            {
                return;
            }
            Vector3 meta = jugador.Value + Vector3.up * config.AlturaDron;
            Vector3 haciaMeta = meta - transform.position;
            float paso = calculo.Paso(true, haciaMeta.magnitude, Time.deltaTime);
            if (paso > 0f)
            {
                transform.position += haciaMeta.normalized * paso;
            }
            Disparar();
            Vector3 haciaJugador = jugador.Value - transform.position;
            haciaJugador.y = 0f;
            if (haciaJugador.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(haciaJugador, Vector3.up), GradosPorSegundoGiro * Time.deltaTime);
            }
        }
    }
}
