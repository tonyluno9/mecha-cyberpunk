using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-501: un Volume local (p. ej. el de Suburbios) que solo se nota de noche: su peso copia al del volumen global de noche,
    /// que CicloDiaNoche sube y baja con la hora.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class VolumenSigueNoche : MonoBehaviour
    {
        [SerializeField] private Volume origen;
        [SerializeField, Range(0f, 1f)] private float pesoMaximo = 1f;

        private static readonly int IdNoche = Shader.PropertyToID("_T501Noche");
        private Volume propio;

        private void Awake()
        {
            propio = GetComponent<Volume>();
            if (origen == null)
            {
                Debug.LogError("[Mundo] VolumenSigueNoche en '" + name + "' necesita el volumen de noche.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            propio.weight = origen.weight * pesoMaximo;
            Shader.SetGlobalFloat(IdNoche, origen.weight);
        }
    }
}
