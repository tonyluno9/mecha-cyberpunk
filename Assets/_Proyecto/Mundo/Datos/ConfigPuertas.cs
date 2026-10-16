using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Sensación de las puertas de los locales: cuánto tardan en abrir y cuánto esperan para cerrarse solas.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config puertas", fileName = "ConfigPuertas_Nuevo")]
    public class ConfigPuertas : ScriptableObject
    {
        [Tooltip("Tiempo que tarda la hoja en abrir o cerrar (s).")]
        [SerializeField, Min(0.01f)] private float duracionApertura = 0.25f;

        [Tooltip("Tiempo que espera abierta, sin nadie en el umbral, antes de cerrarse sola (s).")]
        [SerializeField, Min(0f)] private float segundosParaCerrar = 3f;

        [Tooltip("Cuánto sube la hoja al abrir, hacia el dintel (m).")]
        [SerializeField, Min(0.1f)] private float desplazamiento = 3.3f;

        public float DuracionApertura => duracionApertura;
        public float SegundosParaCerrar => segundosParaCerrar;
        public float Desplazamiento => desplazamiento;
    }
}
