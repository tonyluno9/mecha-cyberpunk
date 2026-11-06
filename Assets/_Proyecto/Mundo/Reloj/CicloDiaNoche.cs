using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// Cambia con la hora del reloj del mundo la luz direccional (sol o luna), el ambiente y la niebla de la escena,
    /// según <see cref="ConfigCicloDia"/>, y mezcla el peso de los dos Volumes de posproceso (noche y día). Solo toca RenderSettings de la escena en ejecución, nunca ProjectSettings.
    /// </summary>
    public class CicloDiaNoche : MonoBehaviour
    {
        [SerializeField] private ConfigCicloDia config;

        [Tooltip("Luz direccional que hace de sol de día y de luna de noche.")]
        [SerializeField] private Light luz;

        [Tooltip("Volume global del posproceso de noche (peso 1 de noche, 0 de día).")]
        [SerializeField] private Volume volumeNoche;

        [Tooltip("Volume global del posproceso de día (peso 1 de día, 0 de noche).")]
        [SerializeField] private Volume volumeDia;

        [Tooltip("Material del cielo (MechaCyberpunk/CieloGradiente desde T-806). Si falta, el fondo es de color sólido (el de la niebla).")]
        [SerializeField] private Material cielo;

        private static readonly int IdTinteCielo = Shader.PropertyToID("_SkyTint");
        private static readonly int IdColorSuelo = Shader.PropertyToID("_GroundColor");
        private static readonly int IdExposicion = Shader.PropertyToID("_Exposure");
        private static readonly int IdTamanoSol = Shader.PropertyToID("_SunSize");
        private static readonly int IdColorHorizonte = Shader.PropertyToID("_ColorHorizonte");
        private static readonly int IdColorBrillo = Shader.PropertyToID("_ColorBrillo");
        private static readonly int IdColorSol = Shader.PropertyToID("_ColorSol");

        private IRelojMundo reloj;
        private Camera camara;
        private Material cieloEnEjecucion;
        private CieloTexturizado cieloTextura;

        /// <summary>Las nubes y estrellas con textura (T-635); null si falta su shader.</summary>
        public CieloTexturizado CieloTextura => cieloTextura;

        public Volume VolumeNoche => volumeNoche;
        public Volume VolumeDia => volumeDia;

        private void Awake()
        {
            if (config == null || luz == null)
            {
                Debug.LogError("[Mundo] CicloDiaNoche en '" + name + "' necesita config y luz.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            cieloTextura = CieloTexturizado.Crear();
            reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                Aplicar(reloj.Hora);
            }
        }

        private void Update()
        {
            if (reloj == null)
            {
                reloj = Registro<IRelojMundo>.Actual;
            }
            if (reloj != null)
            {
                Aplicar(reloj.Hora);
            }
        }

        /// <summary>Pone luz, ambiente y niebla como corresponden a esa hora.</summary>
        public void Aplicar(float hora)
        {
            float pesoNoche = config.PesoNoche(hora);
            // T-635: de día el sol es cálido y proyecta sombras suaves (menos plano que un solo color de ambiente).
            luz.color = CalculoCieloTextura.LuzCalida(config.ColorLuz(hora), pesoNoche);
            luz.intensity = config.IntensidadLuz(hora);
            if (luz.shadows == LightShadows.None)
            {
                luz.shadows = LightShadows.Soft;
            }
            luz.transform.rotation = Quaternion.Euler(config.Elevacion(hora), config.Giro, 0f);

            if (volumeNoche != null)
            {
                volumeNoche.weight = pesoNoche;
            }
            if (volumeDia != null)
            {
                volumeDia.weight = 1f - pesoNoche;
            }

            CalculoCieloTextura.Ambiente(config.ColorAmbiente(hora), config.ColorCielo(hora), pesoNoche,
                out Color arriba, out Color horizonte, out Color suelo);
            RenderSettings.ambientLight = config.ColorAmbiente(hora);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = arriba;
            RenderSettings.ambientEquatorColor = horizonte;
            RenderSettings.ambientGroundColor = suelo;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = config.ColorNiebla(hora);
            RenderSettings.fogDensity = config.DensidadNiebla(hora);

            AplicarCielo(hora);
            if (camara == null)
            {
                camara = Camera.main;
            }
            cieloTextura?.Actualizar(camara, pesoNoche);

            // Sin skybox, el fondo de la cámara es del color de la niebla: así el cielo se funde con el horizonte.
            if (camara == null)
            {
                camara = Camera.main;
            }
            if (camara != null && cieloEnEjecucion != null)
            {
                camara.clearFlags = CameraClearFlags.Skybox;
            }
            else if (camara != null && camara.clearFlags == CameraClearFlags.SolidColor)
            {
                camara.backgroundColor = RenderSettings.fogColor;
            }
        }

        // El cielo usa una copia del material para no tocar el asset al cambiar la hora.
        private void AplicarCielo(float hora)
        {
            if (cielo == null)
            {
                return;
            }
            if (cieloEnEjecucion == null)
            {
                cieloEnEjecucion = new Material(cielo) { name = cielo.name + " (hora)" };
            }
            cieloEnEjecucion.SetColor(IdTinteCielo, config.ColorCielo(hora));
            cieloEnEjecucion.SetColor(IdColorSuelo, config.ColorNiebla(hora));
            cieloEnEjecucion.SetFloat(IdExposicion, config.ExposicionCielo(hora));
            cieloEnEjecucion.SetFloat(IdTamanoSol, config.DiscoSol(hora));
            // T-806 (CieloGradiente): el horizonte es el color de la niebla, para que lo lejano se funda con el cielo sin una raya.
            cieloEnEjecucion.SetColor(IdColorHorizonte, config.ColorNiebla(hora));
            cieloEnEjecucion.SetColor(IdColorBrillo, config.BrilloCiudad(hora));
            cieloEnEjecucion.SetColor(IdColorSol, luz.color);
            RenderSettings.skybox = cieloEnEjecucion;
        }

        private void LateUpdate()
        {
            if (cieloTextura != null && camara != null)
            {
                cieloTextura.Actualizar(camara, config.PesoNoche(reloj != null ? reloj.Hora : 0f));
            }
        }

        private void OnDestroy()
        {
            cieloTextura?.Destruir();
            if (cieloEnEjecucion != null)
            {
                Destroy(cieloEnEjecucion);
            }
        }
    }
}
