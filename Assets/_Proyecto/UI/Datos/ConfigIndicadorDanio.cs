using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// T-indicador-24nov: el destello rojo en la orilla de la vista hacia donde vino el daño (izquierda, derecha, atrás, arriba). En VR se ve en el
    /// HUD de casco, más suave y más angosto para que no maree. El asset va en UI/Datos/Resources.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config indicador de daño", fileName = "ConfigIndicadorDanio_Nueva")]
    public class ConfigIndicadorDanio : ScriptableObject
    {
        public const string NombreRecurso = "ConfigIndicadorDanio_Base";

        [Tooltip("Color del destello (el alfa lo pone la opacidad de abajo).")]
        [SerializeField] private Color color = new Color(1f, 0.08f, 0.05f, 1f);
        [Tooltip("Segundos que tarda en desvanecerse (s).")]
        [Min(0.1f)] [SerializeField] private float duracion = 1f;
        [Tooltip("Opacidad máxima en PC (0 a 1), en la orilla misma.")]
        [Range(0f, 1f)] [SerializeField] private float opacidadPC = 0.55f;
        [Tooltip("Opacidad máxima en VR (0 a 1): más suave, que no maree.")]
        [Range(0f, 1f)] [SerializeField] private float opacidadVR = 0.3f;
        [Tooltip("Ancho del destello en PC, como parte del lado de la pantalla (0 a 0.5).")]
        [Range(0.02f, 0.5f)] [SerializeField] private float anchoPC = 0.16f;
        [Tooltip("Ancho del destello en VR, como parte del lado del panel del casco (0 a 0.5).")]
        [Range(0.02f, 0.5f)] [SerializeField] private float anchoVR = 0.12f;
        [Tooltip("Grados sobre el horizonte (°) a partir de los que un disparo o explosión cuenta como que vino de arriba.")]
        [Range(10f, 89f)] [SerializeField] private float gradosArriba = 40f;
        [Tooltip("Medio ángulo (°) del frente: dentro de él no destella ninguna orilla (la marca del anillo sí sale).")]
        [Range(0f, 90f)] [SerializeField] private float gradosFrente = 35f;
        [Tooltip("Medio ángulo (°) de atrás: dentro de él destella la orilla de abajo (atrás).")]
        [Range(0f, 90f)] [SerializeField] private float gradosAtras = 50f;

        public Color Color => color;
        public float Duracion => duracion;
        public float GradosArriba => gradosArriba;
        public float GradosFrente => gradosFrente;
        public float GradosAtras => gradosAtras;
        public float Opacidad(bool vr) => vr ? opacidadVR : opacidadPC;
        public float Ancho(bool vr) => vr ? anchoVR : anchoPC;

        /// <summary>El asset de Resources, o uno con los valores por defecto si falta.</summary>
        public static ConfigIndicadorDanio Cargar()
        {
            ConfigIndicadorDanio config = Resources.Load<ConfigIndicadorDanio>(NombreRecurso);
            if (config == null)
            {
                config = CreateInstance<ConfigIndicadorDanio>();
                config.name = NombreRecurso + "_PorDefecto";
            }
            return config;
        }
    }
}
