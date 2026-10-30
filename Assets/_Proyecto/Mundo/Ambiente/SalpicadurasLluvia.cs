using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Presentación: anillos de salpicadura en el piso alrededor del jugador mientras cae la lluvia de
    /// <see cref="LluviaSigueJugador"/> (no la cambia; solo mira si está lloviendo). Se apaga con ella dentro de los locales y de día.
    /// </summary>
    public class SalpicadurasLluvia : MonoBehaviour
    {
        private const float AlturaRayo = 1.5f;
        private const float LargoRayo = 4f;

        [SerializeField] private ConfigAmbienteNoche config;
        [SerializeField] private LluviaSigueJugador lluvia;
        [SerializeField] private Material material;

        private ParticleSystem sistema;
        private bool emitiendo;

        public ParticleSystem Sistema => sistema;

        private void Awake()
        {
            if (config == null || lluvia == null || material == null)
            {
                Debug.LogError("[Mundo] SalpicadurasLluvia en '" + name + "' necesita config, lluvia y material.", this);
                enabled = false;
                return;
            }
            sistema = Crear();
        }

        private void LateUpdate()
        {
            Transform objetivo = BuscarObjetivo();
            if (objetivo != null)
            {
                Vector3 desde = objetivo.position + Vector3.up * AlturaRayo;
                float piso = Physics.Raycast(desde, Vector3.down, out RaycastHit golpe, LargoRayo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    ? golpe.point.y
                    : objetivo.position.y;
                sistema.transform.position = new Vector3(objetivo.position.x, piso + config.AlturaSalpicadura, objetivo.position.z);
            }
            bool debe = lluvia.EstaLloviendo && objetivo != null;
            if (debe != emitiendo)
            {
                emitiendo = debe;
                if (debe)
                {
                    sistema.Play();
                }
                else
                {
                    sistema.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
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

        private ParticleSystem Crear()
        {
            GameObject objeto = new GameObject("Salpicaduras");
            objeto.transform.SetParent(transform, false);
            ParticleSystem nuevo = objeto.AddComponent<ParticleSystem>();
            nuevo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = nuevo.main;
            main.loop = true;
            main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = config.VidaSalpicadura;
            main.startSpeed = 0f;
            main.startSize = config.TamanoInicialSalpicadura;
            main.startColor = config.ColorSalpicadura;
            main.maxParticles = config.MaximoSalpicaduras;

            ParticleSystem.EmissionModule emision = nuevo.emission;
            emision.rateOverTime = config.SalpicadurasPorSegundo;

            ParticleSystem.ShapeModule forma = nuevo.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(config.LadoSalpicaduras, 0f, config.LadoSalpicaduras);

            ParticleSystem.SizeOverLifetimeModule tamano = nuevo.sizeOverLifetime;
            tamano.enabled = true;
            float razon = config.TamanoFinalSalpicadura / config.TamanoInicialSalpicadura;
            tamano.size = new ParticleSystem.MinMaxCurve(razon, AnimationCurve.Linear(0f, 1f / razon, 1f, 1f));

            ParticleSystem.ColorOverLifetimeModule color = nuevo.colorOverLifetime;
            color.enabled = true;
            Gradient desvanece = new Gradient();
            desvanece.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = desvanece;

            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            return nuevo;
        }
    }
}
