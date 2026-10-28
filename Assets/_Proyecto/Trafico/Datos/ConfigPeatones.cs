using MechaCyberpunk.Trafico.Peatones;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>Los números de los peatones del barrio (T-330): velocidad, esperas en las esquinas y cuántos hay de día y de noche.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config de peatones", fileName = "ConfigPeatones_Nueva")]
    public class ConfigPeatones : ScriptableObject
    {
        [Min(0.1f)] [Tooltip("Velocidad al caminar (m/s).")]
        [SerializeField] private float velocidad = 1.4f;
        [Min(0f)] [Tooltip("Espera mínima en una esquina (s).")]
        [SerializeField] private float esperaMinima = 1f;
        [Min(0f)] [Tooltip("Espera máxima en una esquina (s).")]
        [SerializeField] private float esperaMaxima = 5f;
        [Min(0)] [Tooltip("Cuántos peatones hay de día (personas).")]
        [SerializeField] private int peatonesDeDia = 12;
        [Min(0)] [Tooltip("Cuántos peatones hay de noche (personas).")]
        [SerializeField] private int peatonesDeNoche = 24;

        public float Velocidad => velocidad;
        public float EsperaMinima => esperaMinima;
        public float EsperaMaxima => Mathf.Max(esperaMinima, esperaMaxima);
        public int PeatonesDeDia => peatonesDeDia;
        public int PeatonesDeNoche => peatonesDeNoche;

        /// <summary>Cuántos peatones debe haber ahora (personas).</summary>
        public int Cantidad(bool esDeNoche) => esDeNoche ? peatonesDeNoche : peatonesDeDia;

        /// <summary>Arma las reglas de un peatón con estos números y su ruta.</summary>
        public CalculoPeaton Crear(System.Collections.Generic.IReadOnlyList<float> largosSegmentos, System.Random azar)
        {
            return new CalculoPeaton(largosSegmentos, velocidad, esperaMinima, EsperaMaxima, azar);
        }
    }
}
