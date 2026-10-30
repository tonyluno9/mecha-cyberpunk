using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Presentación de la ambientación de noche: salpicaduras de la lluvia y capa de nubes.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Mundo/Config Ambiente Noche", fileName = "ConfigAmbienteNoche_Mundo")]
    public class ConfigAmbienteNoche : ScriptableObject
    {
        [Header("Salpicaduras")]
        [Tooltip("Lado (m) del área de salpicaduras alrededor del jugador.")]
        [SerializeField, Min(1f)] private float ladoSalpicaduras = 18f;

        [Tooltip("Salpicaduras por segundo.")]
        [SerializeField, Min(1f)] private float salpicadurasPorSegundo = 650f;

        [Tooltip("Segundos que dura una salpicadura.")]
        [SerializeField, Min(0.05f)] private float vidaSalpicadura = 0.35f;

        [Tooltip("Tamaño (m) del anillo al nacer.")]
        [SerializeField, Min(0.01f)] private float tamanoInicialSalpicadura = 0.05f;

        [Tooltip("Tamaño (m) del anillo al morir.")]
        [SerializeField, Min(0.01f)] private float tamanoFinalSalpicadura = 0.5f;

        [Tooltip("Color y transparencia de la salpicadura.")]
        [SerializeField] private Color colorSalpicadura = new Color(0.8f, 0.88f, 1f, 0.7f);

        [Tooltip("Máximo de salpicaduras vivas.")]
        [SerializeField, Min(10)] private int maximoSalpicaduras = 600;

        [Tooltip("Altura (m) sobre el piso a la que se dibujan.")]
        [SerializeField, Min(0f)] private float alturaSalpicadura = 0.04f;

        [Header("Nubes")]
        [Tooltip("Opacidad de las nubes en noche cerrada (0 a 1); de día se apagan con el peso de noche del ciclo.")]
        [SerializeField, Range(0f, 1f)] private float opacidadNubes = 0.85f;

        [Tooltip("Velocidad de las nubes (uv/s).")]
        [SerializeField] private Vector2 velocidadNubes = new Vector2(0.004f, 0.0015f);

        [Tooltip("Radio (m) del domo de nubes; menor que el plano lejano de la cámara.")]
        [SerializeField, Min(10f)] private float radioDomo = 700f;

        public float LadoSalpicaduras => ladoSalpicaduras;
        public float SalpicadurasPorSegundo => salpicadurasPorSegundo;
        public float VidaSalpicadura => vidaSalpicadura;
        public float TamanoInicialSalpicadura => tamanoInicialSalpicadura;
        public float TamanoFinalSalpicadura => tamanoFinalSalpicadura;
        public Color ColorSalpicadura => colorSalpicadura;
        public int MaximoSalpicaduras => maximoSalpicaduras;
        public float AlturaSalpicadura => alturaSalpicadura;
        public float OpacidadNubes => opacidadNubes;
        public Vector2 VelocidadNubes => velocidadNubes;
        public float RadioDomo => radioDomo;
    }
}
