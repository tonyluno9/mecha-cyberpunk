using UnityEngine;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>Valores de la cámara del carro: primera persona (por defecto) y tercera persona con la tecla C (T-075).</summary>
    [CreateAssetMenu(fileName = "ConfigCamaraCarro", menuName = "Mecha Cyberpunk/Config cámara carro")]
    public class ConfigCamaraCarro : ScriptableObject
    {
        [Header("Primera persona")]
        [Tooltip("Altura de los ojos del conductor sobre el asiento (m).")]
        [Min(0f)] [SerializeField] private float alturaOjos = 1.15f;
        [Tooltip("Cuánto puede mirar a los lados (°).")]
        [Range(10f, 180f)] [SerializeField] private float limiteLados = 110f;
        [Tooltip("Cuánto puede mirar arriba y abajo (°).")]
        [Range(10f, 89f)] [SerializeField] private float limiteVertical = 60f;
        [Tooltip("Grados que gira la vista por cada pixel que se mueve el ratón (°/px).")]
        [Min(0f)] [SerializeField] private float sensibilidad = 0.1f;
        [Tooltip("Segundos sin mover el ratón antes de que la vista regrese al frente (s).")]
        [Min(0f)] [SerializeField] private float esperaRegreso = 1.5f;
        [Tooltip("Qué tan rápido regresa la vista al frente (°/s).")]
        [Min(1f)] [SerializeField] private float velocidadRegreso = 150f;

        [Header("Tercera persona")]
        [Tooltip("Distancia detrás del carro (m).")]
        [Min(1f)] [SerializeField] private float distanciaAtras = 6f;
        [Tooltip("Altura sobre el foco del carro (m).")]
        [Min(0f)] [SerializeField] private float alturaTercera = 2.5f;
        [Tooltip("Altura del punto al que mira la cámara, sobre el origen del carro (m).")]
        [Min(0f)] [SerializeField] private float alturaFoco = 1.2f;
        [Tooltip("Qué tan rápido la cámara alcanza su lugar ideal (1/s); más alto es más pegado.")]
        [Min(0.5f)] [SerializeField] private float suavidad = 8f;
        [Tooltip("Cuánto se separa la cámara de un muro cuando lo tiene detrás (m).")]
        [Min(0f)] [SerializeField] private float margenMuro = 0.3f;
        [Tooltip("Distancia mínima al foco cuando hay un muro pegado (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaMinima = 1.2f;

        [Tooltip("T-723: cuánto sube (m) el punto al que mira la cámara sobre el foco: el vehículo queda más abajo en la pantalla, como CAM03_Carro.png. 0 mira justo al foco.")]
        [Min(0f)] [SerializeField] private float alturaMira = 0f;

        [Header("Órbita en tercera persona (T-401)")]
        [Tooltip("Inclinación mínima de la órbita (°); negativo es por debajo del horizonte del foco.")]
        [Range(-45f, 0f)] [SerializeField] private float inclinacionMinOrbita = -10f;
        [Tooltip("Inclinación máxima de la órbita (°).")]
        [Range(10f, 80f)] [SerializeField] private float inclinacionMaxOrbita = 40f;
        [Tooltip("Inclinación de reposo detrás del carro (°).")]
        [Range(-10f, 40f)] [SerializeField] private float inclinacionReposo = 15f;
        [Tooltip("Metros extra de distancia al ir a la rapidez de referencia.")]
        [Min(0f)] [SerializeField] private float distanciaExtraVelocidad = 2f;
        [Tooltip("Rapidez a la que la distancia extra llega a su máximo (km/h).")]
        [Min(1f)] [SerializeField] private float kmhDistanciaExtra = 80f;
        [Tooltip("T-658: qué tanto se atrasa la cámara en seguir hacia dónde apunta el carro al girar (s); evita que la vista se arremoline al maniobrar o dar reversa.")]
        [Min(0f)] [SerializeField] private float tiempoRumbo = 0.2f;

        public float AlturaMira => alturaMira;
        public float InclinacionMinOrbita => inclinacionMinOrbita;
        public float InclinacionMaxOrbita => inclinacionMaxOrbita;
        public float InclinacionReposo => inclinacionReposo;
        public float DistanciaExtraVelocidad => distanciaExtraVelocidad;
        public float KmhDistanciaExtra => kmhDistanciaExtra;
        public float TiempoRumbo => tiempoRumbo;
        public float AlturaOjos => alturaOjos;
        public float LimiteLados => limiteLados;
        public float LimiteVertical => limiteVertical;
        public float Sensibilidad => sensibilidad;
        public float EsperaRegreso => esperaRegreso;
        public float VelocidadRegreso => velocidadRegreso;
        public float DistanciaAtras => distanciaAtras;
        public float AlturaTercera => alturaTercera;
        public float AlturaFoco => alturaFoco;
        public float Suavidad => suavidad;
        public float MargenMuro => margenMuro;
        public float DistanciaMinima => distanciaMinima;
    }
}
