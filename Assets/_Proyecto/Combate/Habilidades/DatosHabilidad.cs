using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>Una habilidad especial del mecha (T-174, D-tony-136). Solo datos: lanzarla es T-175.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de habilidad", fileName = "DatosHabilidad_Nueva")]
    public class DatosHabilidad : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Identificador único (texto).")]
        [SerializeField] private string id = "habilidad";

        [Tooltip("Nombre que ve el jugador, en mayúsculas (texto).")]
        [SerializeField] private string nombre = "HABILIDAD";

        [Tooltip("Cómo actúa.")]
        [SerializeField] private TipoEfectoHabilidad efecto;

        [Tooltip("Daño a nivel I (puntos). En la salva es por proyectil.")]
        [Min(0f)] [SerializeField] private float danio;

        [Tooltip("Enfriamiento base (s).")]
        [Min(0f)] [SerializeField] private float enfriamiento = 5f;

        [Tooltip("Color de la rama para efectos y pantallas.")]
        [SerializeField] private Color colorRama = Color.white;

        [Header("Proyectil")]
        [Tooltip("Velocidad del proyectil (m/s).")]
        [Min(0f)] [SerializeField] private float velocidad;

        [Tooltip("Distancia máxima que recorre el proyectil (m).")]
        [Min(0f)] [SerializeField] private float alcance;

        [Tooltip("Radio de la explosión al pegar (m).")]
        [Min(0f)] [SerializeField] private float radioExplosion;

        [Tooltip("Cuántos proyectiles salen (entero).")]
        [Min(1)] [SerializeField] private int cantidad = 1;

        [Tooltip("Dispersión entre proyectiles (grados).")]
        [Min(0f)] [SerializeField] private float dispersion;

        [Header("Cono")]
        [Tooltip("Alcance del cono (m).")]
        [Min(0f)] [SerializeField] private float alcanceCono;

        [Tooltip("Ángulo total del cono (grados). 360 = alrededor.")]
        [Range(1f, 360f)] [SerializeField] private float angulo = 60f;

        [Tooltip("Cuánto aturde al rival (s). 0 = no aturde.")]
        [Min(0f)] [SerializeField] private float aturdir;

        [Tooltip("Cuánto empuja al rival (m). 0 = no empuja.")]
        [Min(0f)] [SerializeField] private float empuje;

        [Tooltip("Si rompe la guardia del rival.")]
        [SerializeField] private bool rompeGuardia;

        [Header("Escudo")]
        [Tooltip("Fracción del daño recibido que se quita (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float reduccion;

        [Tooltip("Cuánto dura el escudo a nivel I (s).")]
        [Min(0f)] [SerializeField] private float duracion;

        [Header("Embestida")]
        [Tooltip("Distancia que recorre (m).")]
        [Min(0f)] [SerializeField] private float distancia;

        [Tooltip("Tiempo que tarda en recorrerla (s).")]
        [Min(0f)] [SerializeField] private float duracionEmbestida;

        public string Id => id;
        public string Nombre => nombre;
        public TipoEfectoHabilidad Efecto => efecto;
        public float Danio => danio;
        public float Enfriamiento => enfriamiento;
        public Color ColorRama => colorRama;
        public float Velocidad => velocidad;
        public float Alcance => alcance;
        public float RadioExplosion => radioExplosion;
        public int Cantidad => cantidad;
        public float Dispersion => dispersion;
        public float AlcanceCono => alcanceCono;
        public float Angulo => angulo;
        public float Aturdir => aturdir;
        public float Empuje => empuje;
        public bool RompeGuardia => rompeGuardia;
        public float Reduccion => reduccion;
        public float Duracion => duracion;
        public float Distancia => distancia;
        public float DuracionEmbestida => duracionEmbestida;
    }
}
