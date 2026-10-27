using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>Furia del jefe: cuándo entra (fracción de su propia vida) y cómo cambia su pelea.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config furia del jefe", fileName = "ConfigFuriaJefe_Nuevo")]
    public class ConfigFuriaJefe : ScriptableObject
    {
        [Tooltip("Con su vida en esta fracción de la máxima o menos, el jefe entra en furia (0 a 1).")]
        [Range(0.01f, 1f)] [SerializeField] private float umbralVida = 0.5f;
        [Tooltip("Multiplica el daño de sus golpes (veces).")]
        [Min(1f)] [SerializeField] private float multiplicadorDanio = 1.25f;
        [Tooltip("Multiplica la espera entre golpes (veces; menos de 1 ataca más seguido).")]
        [Range(0.1f, 1f)] [SerializeField] private float multiplicadorEspera = 0.6f;
        [Tooltip("Multiplica su velocidad y aceleración (veces).")]
        [Min(1f)] [SerializeField] private float multiplicadorVelocidad = 1.2f;
        [Tooltip("Color al que pulsa el cuerpo en furia.")]
        [SerializeField] private Color colorFuria = new Color(0.9f, 0.05f, 0.05f);
        [Tooltip("Pulsos por segundo (Hz).")]
        [Min(0.1f)] [SerializeField] private float frecuenciaPulso = 3f;

        public float UmbralVida => umbralVida;
        public float MultiplicadorDanio => multiplicadorDanio;
        public float MultiplicadorEspera => multiplicadorEspera;
        public float MultiplicadorVelocidad => multiplicadorVelocidad;
        public Color ColorFuria => colorFuria;
        public float FrecuenciaPulso => frecuenciaPulso;
    }
}
