using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Tiempos y distancias del llamado del mecha desde el taller (T-066).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config llamado mecha", fileName = "ConfigLlamadoMecha")]
    public class ConfigLlamadoMecha : ScriptableObject
    {
        [Tooltip("Lo que tarda el mecha en llegar desde que se llama (s).")]
        [Min(0.5f)] [SerializeField] private float segundosLlegada = 6f;
        [Tooltip("Los últimos segundos de la llegada en los que se ve bajar del cielo (s). No puede pasar de la llegada.")]
        [Min(0.1f)] [SerializeField] private float segundosDescenso = 1.5f;
        [Tooltip("Altura desde la que baja, o hasta la que sube al regresar (m).")]
        [Min(1f)] [SerializeField] private float alturaDescenso = 25f;
        [Tooltip("Lo que tarda en subir y desaparecer al regresar al taller (s).")]
        [Min(0.1f)] [SerializeField] private float segundosRegreso = 2f;
        [Tooltip("Si el jugador se aleja más de esto (m, horizontal) de un mecha vacío en la calle, el mecha regresa solo.")]
        [Min(1f)] [SerializeField] private float distanciaRegresoAutomatico = 60f;
        [Tooltip("Separación entre el piso y la base del mecha al aterrizar y al estacionarse (m).")]
        [Min(0f)] [SerializeField] private float separacionPiso = 0.05f;
        [Tooltip("Radio de la marca de neón que se ve en el piso mientras el mecha viene (m).")]
        [Min(0.5f)] [SerializeField] private float radioMarca = 3f;

        public float SegundosLlegada => segundosLlegada;
        public float SegundosDescenso => Mathf.Min(segundosDescenso, segundosLlegada);
        public float AlturaDescenso => alturaDescenso;
        public float SegundosRegreso => segundosRegreso;
        public float DistanciaRegresoAutomatico => distanciaRegresoAutomatico;
        public float SeparacionPiso => separacionPiso;
        public float RadioMarca => radioMarca;
    }
}
