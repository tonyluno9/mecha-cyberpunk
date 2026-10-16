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

        private IRelojMundo reloj;
        private Camera camara;

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
            luz.color = config.ColorLuz(hora);
            luz.intensity = config.IntensidadLuz(hora);
            luz.transform.rotation = Quaternion.Euler(config.Elevacion(hora), config.Giro, 0f);

            float pesoNoche = config.PesoNoche(hora);
            if (volumeNoche != null)
            {
                volumeNoche.weight = pesoNoche;
            }
            if (volumeDia != null)
            {
                volumeDia.weight = 1f - pesoNoche;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = config.ColorAmbiente(hora);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = config.ColorNiebla(hora);
            RenderSettings.fogDensity = config.DensidadNiebla(hora);

            // Sin skybox, el fondo de la cámara es del color de la niebla: así el cielo se funde con el horizonte.
            if (camara == null)
            {
                camara = Camera.main;
            }
            if (camara != null && camara.clearFlags == CameraClearFlags.SolidColor)
            {
                camara.backgroundColor = RenderSettings.fogColor;
            }
        }
    }
}
