using MechaCyberpunk.Mundo.Datos;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T14 (T-1605): partículas finas de polvo en el aire alrededor de la cámara principal, que se iluminan con las luces cercanas (material AMB_HaloNiebla de
    /// ConfigRealismo). Un solo sistema de partículas en espacio de mundo que sigue a la cámara. Se llama "Polvo_Aire" para que CalidadRealismo lo apague en Bajo.
    /// </summary>
    public class PolvoSigueJugador : MonoBehaviour
    {
        private const string NombreSistema = "Polvo_Aire";
        private const float VidaMinima = 6f;
        private const float VidaMaxima = 10f;
        private const float TamanoMinimo = 0.03f;
        private const float TamanoMaximo = 0.09f;
        private const float AlfaPolvo = 0.35f;
        private const float AlturaCaja = 6f;
        private const float DerivaViento = 0.12f;

        [SerializeField] private ConfigRealismo config;

        private ParticleSystem sistema;
        private Camera camara;

        private void Awake()
        {
            if (config == null || config.MaterialPolvo == null)
            {
                Debug.LogError("[Mundo] PolvoSigueJugador en '" + name + "' necesita ConfigRealismo con material de polvo.", this);
                enabled = false;
                return;
            }
            sistema = Crear();
        }

        private void LateUpdate()
        {
            if (camara == null || !camara.isActiveAndEnabled)
            {
                camara = Camera.main;
            }
            if (camara != null)
            {
                sistema.transform.position = camara.transform.position + Vector3.up * (AlturaCaja * 0.25f);
            }
        }

        private ParticleSystem Crear()
        {
            GameObject objeto = new GameObject(NombreSistema);
            objeto.transform.SetParent(transform, false);
            ParticleSystem ps = objeto.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(VidaMinima, VidaMaxima);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(TamanoMinimo, TamanoMaximo);
            main.startColor = new Color(1f, 0.92f, 0.8f, AlfaPolvo);
            main.maxParticles = config.MaximoPolvo;
            ParticleSystem.EmissionModule emision = ps.emission;
            emision.rateOverTime = config.MaximoPolvo / VidaMaxima;
            ParticleSystem.ShapeModule forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(config.LadoPolvo, AlturaCaja, config.LadoPolvo);
            ParticleSystem.VelocityOverLifetimeModule deriva = ps.velocityOverLifetime;
            deriva.enabled = true;
            deriva.space = ParticleSystemSimulationSpace.World;
            deriva.x = new ParticleSystem.MinMaxCurve(-DerivaViento, DerivaViento);
            deriva.y = new ParticleSystem.MinMaxCurve(-DerivaViento * 0.5f, DerivaViento * 0.5f);
            deriva.z = new ParticleSystem.MinMaxCurve(-DerivaViento, DerivaViento);
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            color.color = gradiente;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = config.MaterialPolvo;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            // Si la calidad guardada apaga las particulas, nace apagado (CalidadRealismo lo prende si cambia).
            bool prendido = CalculoCalidadRealismo.Ajustes(CalculoCalidadRealismo.Nivel(PlayerPrefs.GetInt(CalculoCalidadRealismo.LlaveNivel, (int)NivelCalidadRealismo.Medio))).Particulas;
            emision.enabled = prendido;
            render.enabled = prendido;
            ps.Play();
            return ps;
        }
    }
}
