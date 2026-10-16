using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Cabina
{
    /// <summary>
    /// Sacude el objeto intermedio "Sacudida" (que contiene la cabina y la cámara) cuando el peleador propio recibe un impacto.
    /// Va en el objeto Sacudida; no toca la cámara directamente porque ControladorMecha la usa para mirar y mecerse.
    /// </summary>
    public class SacudidaCabina : MonoBehaviour
    {
        private const float VueltaCompleta = Mathf.PI * 2f;
        private const float VueltasAntesDeReiniciar = 100f;
        private const float RazonEjeY = 1.7f;
        private const float RazonEjeZ = 3.1f;

        [SerializeField] private ConfigSacudidaCabina config;

        private IPeleador peleador;
        private float intensidad;
        private float fase;
        private Vector3 desplazamientoBase;

        /// <summary>Desplazamiento local fijo de la cabina (m): el encendido la baja con él. La sacudida se suma encima.</summary>
        public Vector3 DesplazamientoBase => desplazamientoBase;

        /// <summary>Intensidad actual de 0 (quieta) a 1.</summary>
        public float Intensidad => intensidad;

        private void Awake()
        {
            peleador = GetComponentInParent<IPeleador>();
            if (config == null || peleador == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en SacudidaCabina de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventosPelea.ImpactoRecibido += AlRecibirImpacto;
        }

        private void OnDisable()
        {
            EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
            intensidad = 0f;
            Reposo();
        }

        /// <summary>Mueve la cabina a su posición de reposo más este desplazamiento (m).</summary>
        public void FijarDesplazamientoBase(Vector3 desplazamiento)
        {
            desplazamientoBase = desplazamiento;
            if (intensidad <= 0f)
            {
                Reposo();
            }
        }

        private void AlRecibirImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (objetivo != peleador)
            {
                return;
            }
            float nueva = CalculoSacudida.IntensidadInicial(resultado.DanioAplicado, config.DanioReferencia,
                config.IntensidadMinima, config.IntensidadMaxima);
            intensidad = Mathf.Max(intensidad, nueva);
        }

        private void LateUpdate()
        {
            if (intensidad <= 0f)
            {
                return;
            }

            float dt = Time.deltaTime;
            fase = Mathf.Repeat(fase + config.Frecuencia * VueltaCompleta * dt, VueltasAntesDeReiniciar * VueltaCompleta);
            Vector3 direccion = new Vector3(Mathf.Sin(fase), Mathf.Sin(fase * RazonEjeY), Mathf.Sin(fase * RazonEjeZ));
            transform.localPosition = desplazamientoBase + direccion * (config.AmplitudPosicion * intensidad);
            transform.localRotation = Quaternion.Euler(0f, 0f, direccion.z * config.AmplitudRotacion * intensidad);

            intensidad = CalculoSacudida.Decaer(intensidad, dt, config.Duracion, config.IntensidadMaxima);
            if (intensidad <= 0f)
            {
                Reposo();
            }
        }

        private void Reposo()
        {
            transform.localPosition = desplazamientoBase;
            transform.localRotation = Quaternion.identity;
        }
    }
}
