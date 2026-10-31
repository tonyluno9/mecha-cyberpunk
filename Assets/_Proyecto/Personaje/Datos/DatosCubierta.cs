using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Parámetros de cubrirse y rodar (T-421). El asset va en Resources/DatosCubierta; sin él se usan estos valores.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de cubierta y rodada", fileName = "DatosCubierta")]
    public class DatosCubierta : ScriptableObject
    {
        [Header("Cubierta")]
        [Tooltip("Hasta qué distancia del jugador se busca una cubierta al pulsar la tecla (m).")]
        [Min(0.2f)] [SerializeField] private float distanciaBuscar = 1.2f;
        [Tooltip("Altura mínima que debe tener algo para servir de cubierta (m).")]
        [Min(0.3f)] [SerializeField] private float alturaMinimaCubierta = 0.9f;
        [Tooltip("Altura de la cápsula agachado (m).")]
        [Min(0.5f)] [SerializeField] private float alturaAgachado = 1.0f;
        [Tooltip("Cuánto baja la cámara al agacharse (m).")]
        [Min(0f)] [SerializeField] private float bajadaCamara = 0.7f;
        [Tooltip("Escala vertical del muñeco agachado (1 = de pie).")]
        [Range(0.3f, 1f)] [SerializeField] private float escalaAgachado = 0.65f;
        [Tooltip("Velocidad al moverse a lo largo de la cubierta, como fracción de la normal.")]
        [Range(0.05f, 1f)] [SerializeField] private float factorVelocidad = 0.4f;
        [Tooltip("Coseno del ángulo con el que se sale: al caminar con más que esto hacia afuera de la cubierta, se suelta.")]
        [Range(0.1f, 1f)] [SerializeField] private float cosenoSalida = 0.6f;
        [Tooltip("Cuánto protege: una bala del otro lado se cubre si el coseno entre la dirección al tirador y la normal es menor que el negativo de esto.")]
        [Range(0f, 1f)] [SerializeField] private float cosenoProteccion = 0.3f;
        [Tooltip("Fracción del daño de bala que pasa a pesar de la cubierta (0 = ninguno).")]
        [Range(0f, 1f)] [SerializeField] private float danioCubierto = 0f;
        [Tooltip("Rapidez con la que se pega a la cubierta (m/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezPegarse = 3f;

        [Header("Rodada")]
        [Tooltip("Distancia que recorre la rodada (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaRodada = 4f;
        [Tooltip("Duración de la rodada (s).")]
        [Min(0.1f)] [SerializeField] private float duracionRodada = 0.45f;
        [Tooltip("Segundos de invulnerabilidad desde que empieza la rodada.")]
        [Min(0f)] [SerializeField] private float invulnerabilidad = 0.3f;
        [Tooltip("Espera para volver a rodar, contada desde que empieza la rodada (s).")]
        [Min(0.1f)] [SerializeField] private float enfriamientoRodada = 1.2f;
        [Tooltip("Altura de la cápsula al rodar (m).")]
        [Min(0.5f)] [SerializeField] private float alturaRodando = 0.9f;

        public float DistanciaBuscar => distanciaBuscar;
        public float AlturaMinimaCubierta => alturaMinimaCubierta;
        public float AlturaAgachado => alturaAgachado;
        public float BajadaCamara => bajadaCamara;
        public float EscalaAgachado => escalaAgachado;
        public float FactorVelocidad => factorVelocidad;
        public float CosenoSalida => cosenoSalida;
        public float CosenoProteccion => cosenoProteccion;
        public float DanioCubierto => danioCubierto;
        public float RapidezPegarse => rapidezPegarse;
        public float DistanciaRodada => distanciaRodada;
        public float DuracionRodada => duracionRodada;
        public float Invulnerabilidad => invulnerabilidad;
        public float EnfriamientoRodada => Mathf.Max(enfriamientoRodada, duracionRodada);
        public float AlturaRodando => alturaRodando;
        public float RapidezRodada => distanciaRodada / duracionRodada;
    }
}
