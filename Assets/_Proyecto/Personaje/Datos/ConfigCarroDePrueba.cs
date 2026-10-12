using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Manejo simple del carro de prueba de Tony (doble del carro de Alan): velocidades, giro y cuándo se puede bajar.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config carro de prueba", fileName = "ConfigCarroDePrueba_Nuevo")]
    public class ConfigCarroDePrueba : ScriptableObject
    {
        [Tooltip("Velocidad máxima hacia adelante (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadMaxima = 12f;

        [Tooltip("Velocidad máxima en reversa (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadReversa = 4f;

        [Tooltip("Qué tan rápido acelera con la flecha pulsada (m/s²).")]
        [Min(0.1f)] [SerializeField] private float aceleracion = 6f;

        [Tooltip("Qué tan rápido frena al soltar la flecha (m/s²).")]
        [Min(0.1f)] [SerializeField] private float frenado = 10f;

        [Tooltip("Giro máximo a velocidad plena (°/s).")]
        [Min(1f)] [SerializeField] private float giroMaximo = 80f;

        [Tooltip("Velocidad a partir de la cual el giro es completo (m/s); por debajo gira menos.")]
        [Min(0.1f)] [SerializeField] private float velocidadGiroCompleto = 4f;

        [Tooltip("Solo se puede bajar con una velocidad menor a esta (m/s).")]
        [Min(0f)] [SerializeField] private float velocidadMaximaParaBajar = 0.5f;

        public float VelocidadMaxima => velocidadMaxima;
        public float VelocidadReversa => velocidadReversa;
        public float Aceleracion => aceleracion;
        public float Frenado => frenado;
        public float GiroMaximo => giroMaximo;
        public float VelocidadGiroCompleto => velocidadGiroCompleto;
        public float VelocidadMaximaParaBajar => velocidadMaximaParaBajar;
    }
}
