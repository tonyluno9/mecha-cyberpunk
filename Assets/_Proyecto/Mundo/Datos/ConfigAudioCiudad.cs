using MechaCyberpunk.Mundo.Ambiente;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Volúmenes del ambiente sonoro de la ciudad (T-097).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config audio ciudad", fileName = "ConfigAudioCiudad_Nuevo")]
    public class ConfigAudioCiudad : ScriptableObject
    {
        [Tooltip("Volumen de la lluvia afuera (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float volumenLluvia = 0.5f;

        [Tooltip("Volumen del zumbido de los neones de noche (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float volumenNeon = 0.15f;

        [Tooltip("Volumen del tráfico lejano (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float volumenTrafico = 0.3f;

        [Tooltip("Volumen de la música de un local cuando el jugador está dentro (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float volumenMusica = 0.5f;

        [Tooltip("Cuánto del sonido de afuera se oye dentro de un local (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float factorInterior = 0.25f;

        [Tooltip("Cuánto tráfico queda de noche respecto al de día (0 a 1).")]
        [SerializeField, Range(0f, 1f)] private float factorTraficoNoche = 0.6f;

        [Tooltip("Segundos que tarda un volumen en pasar de 0 a 1 (suaviza el paso entre afuera y adentro).")]
        [SerializeField, Min(0f)] private float segundosCambio = 1f;

        public NivelesAmbiente Niveles => new NivelesAmbiente(volumenLluvia, volumenNeon, volumenTrafico, volumenMusica, factorInterior, factorTraficoNoche);
        public float SegundosCambio => segundosCambio;
    }
}
