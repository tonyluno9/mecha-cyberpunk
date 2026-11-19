using UnityEngine;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// T-1462: mide la altura de la cámara y publica el factor de niebla que <see cref="CicloDiaNoche"/> multiplica a la densidad que fija cada frame.
    /// Así desde arriba se lee la zona y a nivel de calle la niebla no cambia.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class NieblaPorAltura : MonoBehaviour
    {
        [Tooltip("Altura del piso del mundo (m) desde la que se mide la altura de la cámara.")]
        [SerializeField] private float alturaPiso;

        private Camera camara;

        /// <summary>Factor vigente (0 a 1) sobre la densidad de niebla. 1 si no hay componente activo.</summary>
        public static float FactorActual { get; private set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            FactorActual = 1f;
        }

        /// <summary>Fija el factor a mano (las fotos del editor, donde Update no corre).</summary>
        public static void Fijar(float factor)
        {
            FactorActual = Mathf.Clamp01(factor);
        }

        private void Update()
        {
            if (camara == null)
            {
                camara = Camera.main;
            }
            if (camara != null)
            {
                FactorActual = CalculoNieblaAltura.Factor(camara.transform.position.y - alturaPiso);
            }
        }

        private void OnDisable()
        {
            FactorActual = 1f;
        }
    }
}
