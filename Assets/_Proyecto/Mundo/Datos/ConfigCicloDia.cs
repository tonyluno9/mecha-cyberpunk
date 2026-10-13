using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>
    /// Cómo se ve el mundo a cada hora: luz direccional (sol de día, luna de noche), ambiente y niebla.
    /// Los gradientes van de 0 (00:00) a 1 (24:00); las curvas usan la hora en horas (0 a 24).
    /// Cada gradiente termina con el mismo color con que empieza, para que 24:00 empalme con 00:00.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config ciclo de día", fileName = "ConfigCicloDia_Nuevo")]
    public class ConfigCicloDia : ScriptableObject
    {
        [Header("Luz direccional")]
        [Tooltip("Color de la luz según la hora del día (0 = 00:00, 1 = 24:00).")]
        [SerializeField] private Gradient colorLuz = Crear(
            (0f, new Color(0.45f, 0.55f, 1f)), (0.25f, new Color(0.42f, 0.5f, 0.95f)), (0.31f, new Color(1f, 0.55f, 0.3f)),
            (0.4f, new Color(1f, 0.93f, 0.8f)), (0.54f, new Color(1f, 0.97f, 0.9f)), (0.7f, new Color(1f, 0.85f, 0.65f)),
            (0.79f, new Color(0.95f, 0.4f, 0.45f)), (0.87f, new Color(0.45f, 0.55f, 1f)));

        [Tooltip("Intensidad de la luz según la hora (h).")]
        [SerializeField] private AnimationCurve intensidadLuz = new AnimationCurve(
            new Keyframe(0f, 1.1f), new Keyframe(6f, 0.8f), new Keyframe(8f, 1.1f), new Keyframe(13f, 1.5f),
            new Keyframe(18f, 1.1f), new Keyframe(19.5f, 0.8f), new Keyframe(21f, 1.1f), new Keyframe(24f, 1.1f));

        [Tooltip("Elevación del sol o la luna sobre el horizonte según la hora (°).")]
        [SerializeField] private AnimationCurve elevacion = new AnimationCurve(
            new Keyframe(0f, 50f), new Keyframe(7f, 8f), new Keyframe(13f, 65f), new Keyframe(19f, 8f), new Keyframe(24f, 50f));

        [Tooltip("Giro de la luz alrededor del eje vertical (°).")]
        [SerializeField, Range(-180f, 180f)] private float giro = -30f;

        [Header("Ambiente y niebla")]
        [Tooltip("Color del ambiente según la hora del día.")]
        [SerializeField] private Gradient colorAmbiente = Crear(
            (0f, new Color(0.27f, 0.29f, 0.58f)), (0.27f, new Color(0.3f, 0.3f, 0.6f)), (0.31f, new Color(0.4f, 0.32f, 0.4f)),
            (0.4f, new Color(0.65f, 0.7f, 0.78f)), (0.54f, new Color(0.7f, 0.74f, 0.82f)), (0.75f, new Color(0.5f, 0.45f, 0.5f)),
            (0.82f, new Color(0.3f, 0.25f, 0.5f)), (0.88f, new Color(0.27f, 0.29f, 0.58f)));

        [Tooltip("Color de la niebla según la hora del día.")]
        [SerializeField] private Gradient colorNiebla = Crear(
            (0f, new Color(0.12f, 0.09f, 0.26f)), (0.27f, new Color(0.13f, 0.1f, 0.28f)), (0.31f, new Color(0.45f, 0.38f, 0.45f)),
            (0.4f, new Color(0.55f, 0.62f, 0.7f)), (0.54f, new Color(0.6f, 0.66f, 0.72f)), (0.75f, new Color(0.55f, 0.5f, 0.55f)),
            (0.82f, new Color(0.2f, 0.12f, 0.3f)), (0.88f, new Color(0.12f, 0.09f, 0.26f)));

        [Tooltip("Densidad de la niebla (exponencial al cuadrado) según la hora (h).")]
        [SerializeField] private AnimationCurve densidadNiebla = new AnimationCurve(
            new Keyframe(0f, 0.02f), new Keyframe(6f, 0.02f), new Keyframe(8f, 0.005f), new Keyframe(13f, 0.003f),
            new Keyframe(18f, 0.005f), new Keyframe(20f, 0.02f), new Keyframe(24f, 0.02f));

        [Header("Posproceso")]
        [Tooltip("Peso del Volume de noche según la hora (h): 1 es noche completa, 0 es día. El de día es 1 menos este.")]
        [SerializeField] private AnimationCurve pesoNoche = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(6.5f, 1f), new Keyframe(7.5f, 0f), new Keyframe(18.5f, 0f),
            new Keyframe(19.5f, 1f), new Keyframe(24f, 1f));

        public float PesoNoche(float hora) => Mathf.Clamp01(pesoNoche.Evaluate(Horas(hora)));
        public Color ColorLuz(float hora) => colorLuz.Evaluate(Fraccion(hora));
        public float IntensidadLuz(float hora) => intensidadLuz.Evaluate(Horas(hora));
        public float Elevacion(float hora) => elevacion.Evaluate(Horas(hora));
        public float Giro => giro;
        public Color ColorAmbiente(float hora) => colorAmbiente.Evaluate(Fraccion(hora));
        public Color ColorNiebla(float hora) => colorNiebla.Evaluate(Fraccion(hora));
        public float DensidadNiebla(float hora) => densidadNiebla.Evaluate(Horas(hora));

        private static float Horas(float hora) => Mathf.Repeat(hora, 24f);
        private static float Fraccion(float hora) => Horas(hora) / 24f;

        // Un gradiente de a lo más 8 claves de color (límite de Unity).
        private static Gradient Crear(params (float tiempo, Color color)[] puntos)
        {
            GradientColorKey[] claves = new GradientColorKey[puntos.Length];
            for (int i = 0; i < puntos.Length; i++)
            {
                claves[i] = new GradientColorKey(puntos[i].color, puntos[i].tiempo);
            }
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(claves, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradiente;
        }
    }
}
