using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Datos
{
    /// <summary>Los números del daño de los carros (T-444): vida, humo y fuego, choques y explosión.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config de daño de carro", fileName = "ConfigDanioCarro_Nueva")]
    public class ConfigDanioCarro : ScriptableObject
    {
        [Header("Vida")]
        [Min(1f)] [Tooltip("Vida del carro (puntos).")]
        [SerializeField] private float vidaMaxima = 250f;
        [Range(0f, 1f)] [Tooltip("Con esta fracción de vida o menos sale humo.")]
        [SerializeField] private float fraccionHumo = 0.55f;
        [Range(0f, 1f)] [Tooltip("Con esta fracción de vida o menos hay fuego.")]
        [SerializeField] private float fraccionFuego = 0.25f;
        [Min(0f)] [Tooltip("Daño por segundo que se hace un carro en llamas hasta explotar (puntos/s).")]
        [SerializeField] private float danioFuegoPorSegundo = 6f;

        [Header("Choques")]
        [Min(0f)] [Tooltip("Rapidez relativa hasta la que un golpe no hace daño (m/s).")]
        [SerializeField] private float umbralChoque = 6f;
        [Min(0f)] [Tooltip("Rapidez relativa desde la que el choque suena fuerte (m/s).")]
        [SerializeField] private float umbralChoqueFuerte = 14f;
        [Min(0f)] [Tooltip("Daño por cada m/s por encima del umbral (puntos).")]
        [SerializeField] private float danioPorChoque = 7f;
        [Min(0f)] [Tooltip("Tiempo mínimo entre dos choques que cuentan (s).")]
        [SerializeField] private float segundosEntreChoques = 0.4f;

        [Header("Explosión")]
        [Min(0.5f)] [Tooltip("Radio de la explosión (m).")]
        [SerializeField] private float radioExplosion = 7f;
        [Min(0f)] [Tooltip("Daño de la explosión en el centro; baja hasta 0 en el borde (puntos).")]
        [SerializeField] private float danioExplosion = 90f;
        [Min(0f)] [Tooltip("Fuerza con la que la explosión empuja lo que se mueve (N).")]
        [SerializeField] private float fuerzaExplosion = 1400f;
        [Min(0f)] [Tooltip("Calor policial si el jugador está cerca de la explosión.")]
        [SerializeField] private float calorExplosion = 25f;
        [Min(0f)] [Tooltip("A qué distancia del carro cuenta que el jugador lo hizo explotar para el calor (m).")]
        [SerializeField] private float distanciaCalor = 30f;
        [Min(0f)] [Tooltip("Cuánto sigue ardiendo el carro destruido (s).")]
        [SerializeField] private float segundosArdiendo = 25f;

        public float VidaMaxima => vidaMaxima;
        public float FraccionHumo => fraccionHumo;
        public float FraccionFuego => fraccionFuego;
        public float DanioFuegoPorSegundo => danioFuegoPorSegundo;
        public float UmbralChoque => umbralChoque;
        public float UmbralChoqueFuerte => umbralChoqueFuerte;
        public float DanioPorChoque => danioPorChoque;
        public float SegundosEntreChoques => segundosEntreChoques;
        public float RadioExplosion => radioExplosion;
        public float DanioExplosion => danioExplosion;
        public float FuerzaExplosion => fuerzaExplosion;
        public float CalorExplosion => calorExplosion;
        public float DistanciaCalor => distanciaCalor;
        public float SegundosArdiendo => segundosArdiendo;
    }
}
