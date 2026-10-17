using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Sensación de la lluvia y la niebla baja que siguen al jugador (T-096).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config lluvia", fileName = "ConfigLluvia_Nuevo")]
    public class ConfigLluvia : ScriptableObject
    {
        [Header("Cuándo")]
        [Tooltip("Si es verdadero llueve también de día; si no, solo de noche.")]
        [SerializeField] private bool lluviaDeDia;

        [Header("Gotas")]
        [Tooltip("Lado (m) de la caja de lluvia alrededor del jugador.")]
        [SerializeField, Min(5f)] private float ladoCaja = 30f;

        [Tooltip("Alto (m) de la caja de lluvia sobre el jugador.")]
        [SerializeField, Min(3f)] private float altoCaja = 15f;

        [Tooltip("Gotas por segundo.")]
        [SerializeField, Min(1f)] private float gotasPorSegundo = 1500f;

        [Tooltip("Velocidad de caída (m/s).")]
        [SerializeField, Min(1f)] private float velocidadCaida = 18f;

        [Tooltip("Ancho de la gota (m).")]
        [SerializeField, Min(0.001f)] private float anchoGota = 0.02f;

        [Tooltip("Largo de la gota respecto a su ancho (veces).")]
        [SerializeField, Min(1f)] private float largoGota = 40f;

        [Tooltip("Color y transparencia de la gota.")]
        [SerializeField] private Color colorGota = new Color(0.7f, 0.8f, 1f, 0.35f);

        [Tooltip("Máximo de gotas vivas.")]
        [SerializeField, Min(10)] private int maximoGotas = 3000;

        [Header("Niebla baja")]
        [Tooltip("Lado (m) del área de niebla baja alrededor del jugador.")]
        [SerializeField, Min(5f)] private float ladoNiebla = 60f;

        [Tooltip("Alto (m) de la capa de niebla baja.")]
        [SerializeField, Min(0.5f)] private float altoNiebla = 2.5f;

        [Tooltip("Nubes de niebla por segundo.")]
        [SerializeField, Min(0.1f)] private float nubesPorSegundo = 3f;

        [Tooltip("Segundos que vive una nube de niebla.")]
        [SerializeField, Min(1f)] private float vidaNube = 14f;

        [Tooltip("Tamaño (m) de una nube de niebla.")]
        [SerializeField, Min(1f)] private float tamanoNube = 9f;

        [Tooltip("Color y transparencia de la niebla baja.")]
        [SerializeField] private Color colorNiebla = new Color(0.45f, 0.5f, 0.75f, 0.08f);

        [Tooltip("Máximo de nubes de niebla vivas.")]
        [SerializeField, Min(1)] private int maximoNubes = 80;

        public bool LluviaDeDia => lluviaDeDia;
        public float LadoCaja => ladoCaja;
        public float AltoCaja => altoCaja;
        public float GotasPorSegundo => gotasPorSegundo;
        public float VelocidadCaida => velocidadCaida;
        public float AnchoGota => anchoGota;
        public float LargoGota => largoGota;
        public Color ColorGota => colorGota;
        public int MaximoGotas => maximoGotas;
        public float LadoNiebla => ladoNiebla;
        public float AltoNiebla => altoNiebla;
        public float NubesPorSegundo => nubesPorSegundo;
        public float VidaNube => vidaNube;
        public float TamanoNube => tamanoNube;
        public Color ColorNiebla => colorNiebla;
        public int MaximoNubes => maximoNubes;
    }
}
