using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Sensación de las puertas de los locales: cuánto tardan en abrir, cuánto esperan para cerrarse solas y cuánto gira una hoja con bisagra.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config puertas", fileName = "ConfigPuertas_Nuevo")]
    public class ConfigPuertas : ScriptableObject
    {
        [Tooltip("Tiempo que tarda la hoja en abrir o cerrar (s).")]
        [SerializeField, Min(0.01f)] private float duracionApertura = 0.25f;

        [Tooltip("Tiempo que espera abierta, sin nadie en el umbral, antes de cerrarse sola (s).")]
        [SerializeField, Min(0f)] private float segundosParaCerrar = 3f;

        [Tooltip("Cuánto sube la hoja al abrir, hacia el dintel (m).")]
        [SerializeField, Min(0.1f)] private float desplazamiento = 3.3f;

        [Tooltip("T-804: puertas con bisagra. Cuánto gira la hoja al abrir (°).")]
        [SerializeField, Range(10f, 170f)] private float gradosBisagra = 95f;

        [Tooltip("T-804: tiempo que tarda una hoja con bisagra en abrir o cerrar (s).")]
        [SerializeField, Min(0.01f)] private float duracionBisagra = 0.7f;

        public float DuracionApertura => duracionApertura;
        public float SegundosParaCerrar => segundosParaCerrar;
        public float Desplazamiento => desplazamiento;
        public float GradosBisagra => gradosBisagra;
        public float DuracionBisagra => duracionBisagra;
    }
}
