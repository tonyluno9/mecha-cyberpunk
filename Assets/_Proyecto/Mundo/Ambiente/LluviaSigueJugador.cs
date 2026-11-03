using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Lluvia fina y niebla baja que siguen al jugador (T-096). Crea dos ParticleSystem con los números de ConfigLluvia. Se apagan
    /// dentro de los interiores (ZonaInterior.JugadorDentro), de día (salvo que la config lo pida) y si <see cref="Activa"/> es false.
    /// <see cref="Activa"/> es el interruptor para que el director del prólogo apague o prenda la lluvia sin contratos nuevos.
    /// </summary>
    public class LluviaSigueJugador : MonoBehaviour
    {
        [SerializeField] private ConfigLluvia config;
        [SerializeField] private Material materialGota;
        [SerializeField] private Material materialNiebla;

        private const float VelocidadDeriva = 0.3f;     // m/s a la que se mueven las nubes de niebla
        // T-535: las gotas salen en todo el alto de la caja (también cerca del piso, donde se ven) y mueren al tocar el piso con una salpicadura.
        private const float MargenVida = 1.15f;
        private const float AlturaTecho = 2.2f;
        private const float LargoRayoTecho = 8f;
        private const float AlturaRayoPiso = 2f;
        private const float LargoRayoPiso = 6f;
        private const float ProbabilidadSalpicadura = 0.35f;
        private const int MaximoSalpicaduras = 600;

        private Transform suelo;
        private ParticleSystem salpicaduras;

        public ParticleSystem Salpicaduras => salpicaduras;
        public Transform Suelo => suelo;

        private ParticleSystem gotas;
        private ParticleSystem niebla;
        private ZonaInterior[] zonas;
        private bool llueve;
        private float intensidad = -1f;

        /// <summary>Interruptor global de la lluvia.</summary>
        public static bool Activa { get; set; } = true;

        /// <summary>Qué tanto llueve ahora (0 a 1; de día, la fracción de la config).</summary>
        public float Intensidad => intensidad;

        /// <summary>True si la lluvia está cayendo ahora.</summary>
        public bool EstaLloviendo => llueve;

        public ParticleSystem Gotas => gotas;
        public ParticleSystem Niebla => niebla;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Activa = true;
        }

        private void Awake()
        {
            if (config == null || materialGota == null || materialNiebla == null)
            {
                Debug.LogError("[Mundo] LluviaSigueJugador en '" + name + "' necesita config y materiales.", this);
                enabled = false;
                return;
            }
            gotas = CrearGotas();
            niebla = CrearNiebla();
        }

        private void Start()
        {
            zonas = FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None);
        }

        private void LateUpdate()
        {
            Transform objetivo = BuscarObjetivo();
            if (objetivo != null)
            {
                gotas.transform.position = objetivo.position + Vector3.up * (config.AltoCaja * 0.5f);
                Vector3 desde = objetivo.position + Vector3.up * AlturaRayoPiso;
                float piso = Physics.Raycast(desde, Vector3.down, out RaycastHit golpe, LargoRayoPiso, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    ? golpe.point.y : objetivo.position.y;
                suelo.position = new Vector3(objetivo.position.x, piso, objetivo.position.z);
                niebla.transform.position = objetivo.position + Vector3.up * (config.AltoNiebla / 2f);
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            bool deNoche = reloj == null || reloj.EsDeNoche;
            // T-537: bajo techo (los locales nuevos sin ZonaInterior, toldos, puentes) tampoco llueve.
            bool techo = objetivo != null && Physics.Raycast(objetivo.position + Vector3.up * AlturaTecho, Vector3.up, LargoRayoTecho, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            bool debe = CalculoLluvia.DebeLlover(Activa, EnInterior() || techo, deNoche, config.LluviaDeDia) && objetivo != null;
            float nueva = CalculoLluvia.Intensidad(deNoche, config.FraccionDeDia);
            if (!Mathf.Approximately(nueva, intensidad))
            {
                // De día cae menos (lluvia ligera); de noche, toda.
                intensidad = nueva;
                ParticleSystem.EmissionModule emisionGotas = gotas.emission;
                emisionGotas.rateOverTime = config.GotasPorSegundo * intensidad;
                ParticleSystem.EmissionModule emisionNiebla = niebla.emission;
                emisionNiebla.rateOverTime = config.NubesPorSegundo * intensidad;
            }
            if (debe != llueve)
            {
                llueve = debe;
                Aplicar(gotas, debe);
                Aplicar(niebla, debe);
            }
        }

        private static void Aplicar(ParticleSystem sistema, bool prender)
        {
            if (prender)
            {
                sistema.Play();
            }
            else
            {
                sistema.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private static Transform BuscarObjetivo()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Raiz != null)
            {
                return jugador.Raiz;
            }
            Camera camara = Camera.main;
            return camara != null ? camara.transform : null;
        }

        private bool EnInterior()
        {
            if (zonas == null)
            {
                return false;
            }
            foreach (ZonaInterior zona in zonas)
            {
                if (zona != null && zona.JugadorDentro)
                {
                    return true;
                }
            }
            return false;
        }

        private ParticleSystem CrearGotas()
        {
            ParticleSystem sistema = Crear("Gotas", materialGota);
            ParticleSystem.MainModule main = sistema.main;
            main.loop = true;
            main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;     // si no, se pausa cuando sus límites (vacíos al empezar) no se ven
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = config.AltoCaja / config.VelocidadCaida * MargenVida;
            main.startSpeed = 0f;
            main.startSize = config.AnchoGota;
            main.startColor = config.ColorGota;
            main.maxParticles = config.MaximoGotas;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = config.GotasPorSegundo;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(config.LadoCaja, config.AltoCaja, config.LadoCaja);
            // T-535: chocan con el plano del piso (sigue al jugador): ahí mueren y salpican.
            suelo = new GameObject("PisoLluvia").transform;
            suelo.SetParent(transform, false);
            suelo.rotation = Quaternion.identity;
            ParticleSystem.CollisionModule choque = sistema.collision;
            choque.enabled = true;
            choque.type = ParticleSystemCollisionType.Planes;
            choque.SetPlane(0, suelo);
            choque.lifetimeLoss = 1f;
            choque.bounce = 0f;
            choque.dampen = 1f;
            choque.radiusScale = 0.1f;
            salpicaduras = CrearSalpicaduras(sistema);
            ParticleSystem.SubEmittersModule sub = sistema.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(salpicaduras, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing, ProbabilidadSalpicadura);
            ParticleSystem.VelocityOverLifetimeModule velocidad = sistema.velocityOverLifetime;
            velocidad.enabled = true;
            velocidad.space = ParticleSystemSimulationSpace.World;
            velocidad.x = 0f;
            velocidad.y = -config.VelocidadCaida;
            velocidad.z = 0f;
            ParticleSystemRenderer render = sistema.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Stretch;
            render.lengthScale = config.LargoGota;
            render.velocityScale = 0f;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            return sistema;
        }

        // Salpicadura chiquita donde cae cada gota (2 gotitas que saltan y caen).
        private ParticleSystem CrearSalpicaduras(ParticleSystem gotasPadre)
        {
            GameObject go = new GameObject("SalpicadurasGotas");
            go.transform.SetParent(gotasPadre.transform, false);
            ParticleSystem sistema = go.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = sistema.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = config.ColorGota;
            main.gravityModifier = 1.2f;
            main.maxParticles = MaximoSalpicaduras;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = 0f;
            emision.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Hemisphere;
            forma.radius = 0.05f;
            ParticleSystemRenderer render = go.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = materialGota;
            render.renderMode = ParticleSystemRenderMode.Billboard;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            return sistema;
        }

        private ParticleSystem CrearNiebla()
        {
            ParticleSystem sistema = Crear("NieblaBaja", materialNiebla);
            ParticleSystem.MainModule main = sistema.main;
            main.loop = true;
            main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;     // si no, se pausa cuando sus límites (vacíos al empezar) no se ven
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = config.VidaNube;
            main.startSpeed = VelocidadDeriva;
            main.startSize = config.TamanoNube;
            main.startColor = config.ColorNiebla;
            main.maxParticles = config.MaximoNubes;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = config.NubesPorSegundo;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(config.LadoNiebla, config.AltoNiebla, config.LadoNiebla);
            ParticleSystem.ColorOverLifetimeModule color = sistema.colorOverLifetime;
            color.enabled = true;
            Gradient desvanece = new Gradient();
            desvanece.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = desvanece;
            ParticleSystemRenderer render = sistema.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Billboard;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            return sistema;
        }

        private ParticleSystem Crear(string nombre, Material material)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            ParticleSystem sistema = go.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return sistema;
        }
    }
}
