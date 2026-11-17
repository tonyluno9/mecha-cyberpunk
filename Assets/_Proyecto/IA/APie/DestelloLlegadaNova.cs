using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1323: el destello con el que llega un agente de Nova: una luz azul que se apaga sola y, si la configuración trae el material de chispas,
    /// una ráfaga de chispas. Se destruye al terminar.
    /// </summary>
    public class DestelloLlegadaNova : MonoBehaviour
    {
        private const int Chispas = 40;
        private const float RadioChispas = 0.2f;
        private const float SegundosPorOmision = 0.6f;
        private const float IntensidadPorOmision = 25f;
        private const float AlcancePorOmision = 12f;

        private Light luz;
        private float inicio;
        private float duracion;
        private float intensidad;
        private float retraso;
        private ParticleSystem particulas;
        private bool disparado;

        /// <summary>True cuando ya se encendió (pasó su retraso).</summary>
        public bool Disparado => disparado;

        /// <summary>Crea un destello en <paramref name="posicion"/>; <paramref name="segundosRetraso"/> lo hace esperar (por ejemplo, hasta que el agente aterriza).</summary>
        public static DestelloLlegadaNova Lanzar(Vector3 posicion, ConfigRespuestaNova config, float segundosRetraso)
        {
            GameObject objeto = new GameObject("DestelloLlegadaNova");
            objeto.transform.position = posicion;
            DestelloLlegadaNova d = objeto.AddComponent<DestelloLlegadaNova>();
            d.duracion = config != null ? config.SegundosDestello : SegundosPorOmision;
            d.intensidad = config != null ? config.IntensidadDestello : IntensidadPorOmision;
            d.retraso = Mathf.Max(0f, segundosRetraso);
            d.inicio = Time.time;
            Color color = config != null ? config.ColorDestello : Color.cyan;
            d.luz = objeto.AddComponent<Light>();
            d.luz.type = LightType.Point;
            d.luz.color = color;
            d.luz.range = config != null ? config.AlcanceDestello : AlcancePorOmision;
            d.luz.shadows = LightShadows.None;
            d.luz.enabled = false;
            Material material = config != null ? config.MaterialDestello : null;
            if (material != null)
            {
                d.particulas = objeto.AddComponent<ParticleSystem>();
                d.particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = d.particulas.main;
                main.loop = false;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = d.duracion;
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
                main.startColor = color;
                main.maxParticles = Chispas * 2;
                ParticleSystem.EmissionModule emision = d.particulas.emission;
                emision.enabled = false;
                ParticleSystem.ShapeModule forma = d.particulas.shape;
                forma.shapeType = ParticleSystemShapeType.Sphere;
                forma.radius = RadioChispas;
                d.particulas.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            }
            return d;
        }

        private void Update()
        {
            float t = Time.time - inicio - retraso;
            if (t < 0f)
            {
                return;
            }
            if (!disparado)
            {
                disparado = true;
                luz.enabled = true;
                if (particulas != null)
                {
                    particulas.Emit(Chispas);
                }
            }
            luz.intensity = intensidad * Mathf.Clamp01(1f - t / duracion);
            if (t >= duracion)
            {
                Destroy(gameObject);
            }
        }
    }
}
