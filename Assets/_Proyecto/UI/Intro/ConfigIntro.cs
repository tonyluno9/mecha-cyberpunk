using UnityEngine;
using UnityEngine.Video;

namespace MechaCyberpunk.UI.Intro
{
    /// <summary>Presentación de la intro del juego (T-1646): el video del logo y sus tiempos. Sin video asignado la intro se salta.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config intro", fileName = "ConfigIntro")]
    public class ConfigIntro : ScriptableObject
    {
        [Tooltip("Video del logo (Assets/Arte/Video/videologo.mp4). Vacío: no hay intro.")]
        [SerializeField] private VideoClip videoLogo;
        [Tooltip("s: si el video no está listo en este tiempo, se salta la intro.")]
        [Min(1f)] [SerializeField] private float segundosMaximoPreparar = 8f;
        [Tooltip("Fondo detrás del video (barras si la pantalla tiene otra proporción).")]
        [SerializeField] private Color colorFondo = Color.black;

        public VideoClip VideoLogo => videoLogo;
        public float SegundosMaximoPreparar => segundosMaximoPreparar;
        public Color ColorFondo => colorFondo;

#if UNITY_EDITOR
        /// <summary>Solo editor: la herramienta de asignación del video.</summary>
        public void AsignarVideo(VideoClip clip) => videoLogo = clip;
#endif
    }
}
