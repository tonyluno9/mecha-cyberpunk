using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1013: apaga una luz poco a poco (destello de una explosión).</summary>
    public class ApagarLuz : MonoBehaviour
    {
        private Light luz;
        private float intensidad;
        private float segundos = 0.3f;
        private float pasado;

        /// <summary>Empieza en <paramref name="desde"/> y llega a 0 en <paramref name="duracion"/> s.</summary>
        public void Iniciar(float desde, float duracion)
        {
            luz = GetComponent<Light>();
            intensidad = desde;
            segundos = Mathf.Max(0.01f, duracion);
            pasado = 0f;
        }

        private void Update()
        {
            if (luz == null)
            {
                return;
            }
            pasado += Time.deltaTime;
            luz.intensity = intensidad * Mathf.Clamp01(1f - pasado / segundos);
            if (pasado >= segundos)
            {
                luz.enabled = false;
                enabled = false;
            }
        }
    }
}
