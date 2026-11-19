using UnityEngine;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1398: mueve este objeto en bucle por una ruta de puntos (x, z) a una altura, con velocidad en m/s, inclinándose al girar y oscilando suave.
    /// Los puntos y el movimiento van en el espacio local del padre. Solo mueve el visual: no tiene colisión ni IA. La lógica está en <see cref="CalculoRutaAerea"/>.
    /// </summary>
    public class RutaAerea : MonoBehaviour
    {
        private const float SuavizadoAlabeo = 4f;

        [SerializeField, Tooltip("Esquinas de la ruta (x, z) en m, en el espacio local del padre. El bucle se cierra solo. Al menos 3.")] private Vector2[] puntos;
        [SerializeField, Min(0f), Tooltip("Altura media sobre el origen del padre (m).")] private float altura = 20f;
        [SerializeField, Min(0.1f), Tooltip("Velocidad a lo largo de la ruta (m/s).")] private float velocidad = 8f;
        [SerializeField, Min(0f), Tooltip("Cuánto sube y baja (m).")] private float amplitudOscilacion = 0.6f;
        [SerializeField, Min(0.1f), Tooltip("Segundos de una oscilación completa.")] private float periodoOscilacion = 4f;
        [SerializeField, Range(0f, 45f), Tooltip("Tope de la inclinación al girar (°).")] private float alabeoMaximo = 12f;
        [SerializeField, Min(0f), Tooltip("Grados de inclinación por cada °/s que gira el rumbo.")] private float alabeoPorGiro = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Fracción de vuelta en que arranca, para que dos drones de la misma ruta no vayan juntos.")] private float fase;

        private CalculoRutaAerea calculo;
        private float tiempo;
        private float alabeoActual;

        public float Altura => altura;
        public float Velocidad => velocidad;
        public int CantidadPuntos => puntos != null ? puntos.Length : 0;

        private void Awake()
        {
            if (puntos == null || puntos.Length < 3)
            {
                Debug.LogError("[Trafico] RutaAerea en " + name + ": necesita al menos 3 puntos.", this);
                enabled = false;
                return;
            }
            calculo = new CalculoRutaAerea(puntos, altura, velocidad, amplitudOscilacion, periodoOscilacion, alabeoMaximo, alabeoPorGiro);
            Aplicar(0f, true);
        }

        private void Update()
        {
            tiempo += Time.deltaTime;
            Aplicar(Time.deltaTime, false);
        }

        private void Aplicar(float dt, bool saltar)
        {
            MuestraRutaAerea m = calculo.Muestra(tiempo, fase);
            alabeoActual = saltar ? m.Alabeo : Mathf.Lerp(alabeoActual, m.Alabeo, 1f - Mathf.Exp(-SuavizadoAlabeo * dt));
            transform.localPosition = m.Posicion;
            transform.localRotation = Quaternion.LookRotation(m.Rumbo, Vector3.up) * Quaternion.Euler(0f, 0f, alabeoActual);
        }
    }
}
