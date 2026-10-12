using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>Cómo se ve el rival: aviso del golpe, parpadeo al recibir daño y caída en KO.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config presentación del rival", fileName = "ConfigPresentacionRival_Nuevo")]
    public class ConfigPresentacionRival : ScriptableObject
    {
        [Header("Aviso del golpe")]
        [Tooltip("Color del brazo mientras prepara el golpe (naranja emisivo).")]
        [ColorUsage(false, true)] [SerializeField] private Color colorAviso = new Color(1f, 0.45f, 0.05f);
        [Tooltip("Cuánto se echa atrás el brazo al preparar el golpe (°, el brazo sube hacia atrás).")]
        [Range(0f, 120f)] [SerializeField] private float anguloBrazoAtras = 70f;

        [Header("Parpadeo al recibir daño")]
        [Tooltip("Color del cuerpo durante el parpadeo.")]
        [SerializeField] private Color colorParpadeo = new Color(1f, 1f, 1f);
        [Tooltip("Cuánto dura el parpadeo (s).")]
        [Min(0.01f)] [SerializeField] private float duracionParpadeo = 0.12f;

        [Header("KO")]
        [Tooltip("Cuánto tarda en quedar tirado (s).")]
        [Min(0.05f)] [SerializeField] private float duracionCaida = 1f;

        public Color ColorAviso => colorAviso;
        public float AnguloBrazoAtras => anguloBrazoAtras;
        public Color ColorParpadeo => colorParpadeo;
        public float DuracionParpadeo => duracionParpadeo;
        public float DuracionCaida => duracionCaida;
    }
}
