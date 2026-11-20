using Unity.Cinemachine;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-1488: temblor de cámara de los choques. Vive en el carro: crea su <see cref="CinemachineImpulseSource"/> y, ante un choque, genera un impulso con
    /// fuerza proporcional al impacto (<see cref="CalculoTemblor"/>). La cámara del carro (<see cref="CamaraCarro"/>) lee esos impulsos del
    /// CinemachineImpulseManager y los aplica a su pose, igual que lo haría un CinemachineImpulseListener (que solo funciona sobre una cámara virtual).
    /// </summary>
    [RequireComponent(typeof(CarroViejo))]
    public class TemblorCarro : MonoBehaviour
    {
        public const int CanalImpulso = 1;
        private const float SegundosImpulso = 0.35f;
        private const float DisipacionDistancia = 100f;

        private CinemachineImpulseSource fuente;

        /// <summary>Fuerza del último impulso generado (para las pruebas).</summary>
        public float UltimaFuerza { get; private set; }

        private void Awake()
        {
            fuente = GetComponent<CinemachineImpulseSource>();
            if (fuente == null)
            {
                fuente = gameObject.AddComponent<CinemachineImpulseSource>();
            }
            fuente.ImpulseDefinition.ImpulseChannel = CanalImpulso;
            fuente.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            fuente.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            fuente.ImpulseDefinition.ImpulseDuration = SegundosImpulso;
            fuente.ImpulseDefinition.DissipationDistance = DisipacionDistancia;
            fuente.DefaultVelocity = Vector3.down;
        }

        /// <summary>Un choque con esa rapidez relativa (m/s): genera el impulso si pasa el umbral de <paramref name="config"/>.</summary>
        public void Sacudir(float rapidez, ConfigCamaraCarro config)
        {
            if (fuente == null || config == null)
            {
                return;
            }
            float fuerza = CalculoTemblor.Fuerza(rapidez, config.UmbralTemblor, config.RapidezTemblorMaximo, config.FuerzaTemblorMaxima);
            if (fuerza <= 0f)
            {
                return;
            }
            UltimaFuerza = fuerza;
            fuente.GenerateImpulse(fuerza);
        }
    }
}
