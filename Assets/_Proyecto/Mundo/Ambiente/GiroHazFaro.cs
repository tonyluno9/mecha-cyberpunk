using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>T-1354: hace girar el haz del faro del Malecón (cono y luz hijos de este objeto) alrededor del eje vertical.</summary>
    public class GiroHazFaro : MonoBehaviour
    {
        [Tooltip("Velocidad del giro (°/s).")]
        [Min(0f)] [SerializeField] private float gradosPorSegundo = 24f;

        public float GradosPorSegundo => gradosPorSegundo;

        /// <summary>Ángulo nuevo tras <paramref name="dt"/> segundos (lógica pura, sin tiempo propio).</summary>
        public static float Avanzar(float angulo, float gradosPorSegundo, float dt)
        {
            return Mathf.Repeat(angulo + gradosPorSegundo * dt, 360f);
        }

        private void Update()
        {
            transform.Rotate(0f, gradosPorSegundo * Time.deltaTime, 0f, Space.World);
        }
    }
}
