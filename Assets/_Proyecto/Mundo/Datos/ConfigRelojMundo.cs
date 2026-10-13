using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Datos del reloj del mundo: cuándo amanece y anochece, cuánto dura cada franja en tiempo real y dónde empieza.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config reloj del mundo", fileName = "ConfigRelojMundo_Nuevo")]
    public class ConfigRelojMundo : ScriptableObject
    {
        [Tooltip("Hora a la que empieza el día (h).")]
        [SerializeField, Range(0f, 23f)] private float horaAmanecer = 7f;

        [Tooltip("Hora a la que empieza la noche (h). Debe ser mayor que el amanecer.")]
        [SerializeField, Range(1f, 23.5f)] private float horaAnochecer = 19f;

        [Tooltip("Minutos reales que dura la franja de día (min).")]
        [SerializeField, Min(0.1f)] private float minutosRealesDia = 8f;

        [Tooltip("Minutos reales que dura la franja de noche (min).")]
        [SerializeField, Min(0.1f)] private float minutosRealesNoche = 12f;

        [Tooltip("Hora con la que empieza la escena (h).")]
        [SerializeField, Range(0f, 23.99f)] private float horaInicial = 21f;

        [Tooltip("Número de día con el que empieza la escena.")]
        [SerializeField, Min(1)] private int diaInicial = 1;

        public float HoraAmanecer => horaAmanecer;
        public float HoraAnochecer => horaAnochecer;
        public float MinutosRealesDia => minutosRealesDia;
        public float MinutosRealesNoche => minutosRealesNoche;
        public float HoraInicial => horaInicial;
        public int DiaInicial => diaInicial;
    }
}
