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

        private ParticleSystem gotas;
        private ParticleSystem niebla;
        private ZonaInterior[] zonas;
        private bool llueve;

        /// <summary>Interruptor global de la lluvia.</summary>
        public static bool Activa { get; set; } = true;

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
                gotas.transform.position = objetivo.position + Vector3.up * config.AltoCaja;
                niebla.transform.position = objetivo.position + Vector3.up * (config.AltoNiebla / 2f);
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            bool deNoche = reloj == null || reloj.EsDeNoche;
            bool debe = CalculoLluvia.DebeLlover(Activa, EnInterior(), deNoche, config.LluviaDeDia) && objetivo != null;
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
            main.startLifetime = config.AltoCaja / config.VelocidadCaida;
            main.startSpeed = 0f;
            main.startSize = config.AnchoGota;
            main.startColor = config.ColorGota;
            main.maxParticles = config.MaximoGotas;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = config.GotasPorSegundo;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(config.LadoCaja, 0.1f, config.LadoCaja);
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
