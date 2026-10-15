using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Mecha.Danio;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Cabina
{
    /// <summary>
    /// Alarma roja de la cabina: una luz puntual y un LED. Con vida baja parpadea a la frecuencia de la config,
    /// en KO queda fija prendida y con la vida arriba del umbral está apagada.
    /// </summary>
    public class AlarmaCabina : MonoBehaviour
    {
        [SerializeField] private ConfigMechaDaniado config;
        [SerializeField] private Light luz;
        [SerializeField] private Renderer led;
        [Tooltip("Material del LED cuando la alarma está apagada.")]
        [SerializeField] private Material materialApagado;
        [Tooltip("Material del LED cuando la alarma está prendida.")]
        [SerializeField] private Material materialPrendido;

        private VidaMecha vida;
        private bool vidaBaja;
        private bool prendida;
        private bool aplicada;

        /// <summary>True si la luz y el LED están prendidos en este momento.</summary>
        public bool Prendida => prendida;

        private void Awake()
        {
            vida = GetComponentInParent<VidaMecha>();
            if (config == null || luz == null || led == null || materialApagado == null || materialPrendido == null || vida == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en AlarmaCabina de " + name, this);
                enabled = false;
                return;
            }
            luz.color = config.ColorAlarma;
            luz.range = config.AlcanceLuzAlarma;
            Aplicar(false);
        }

        private void OnEnable()
        {
            if (vida == null)
            {
                return;
            }
            vida.Cambio += Reevaluar;
            Reevaluar();
        }

        private void OnDisable()
        {
            if (vida != null)
            {
                vida.Cambio -= Reevaluar;
            }
            if (luz != null && led != null)
            {
                Aplicar(false);
            }
        }

        private void Reevaluar()
        {
            vidaBaja = ReglaMechaDaniado.VidaBaja(vida.Actual, vida.Maxima, config.UmbralVidaBaja) || vida.EstaKO;
            Aplicar(CalcularPrendida());
        }

        private void Update()
        {
            Aplicar(CalcularPrendida());
        }

        private bool CalcularPrendida()
        {
            if (vida.EstaKO)
            {
                return true;
            }
            return vidaBaja && ReglaMechaDaniado.AlarmaPrendida(Time.time, config.FrecuenciaAlarma);
        }

        private void Aplicar(bool encendida)
        {
            if (aplicada && prendida == encendida)
            {
                return;
            }
            aplicada = true;
            prendida = encendida;
            luz.enabled = encendida;
            luz.intensity = config.IntensidadLuzAlarma;
            led.sharedMaterial = encendida ? materialPrendido : materialApagado;
        }
    }
}
