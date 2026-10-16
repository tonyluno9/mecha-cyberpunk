using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>Piedad del rival: piso de vida del objetivo, cuándo se debilita y cómo cambia al debilitarse.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config piedad del rival", fileName = "ConfigPiedadRival_Nuevo")]
    public class ConfigPiedadRival : ScriptableObject
    {
        [Header("Piso y umbral")]
        [Tooltip("Sus golpes nunca dejan al objetivo por debajo de esta fracción de su vida máxima (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float fraccionPiso = 0.15f;
        [Tooltip("Con la vida del objetivo en esta fracción de su máxima o menos, el rival se debilita (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float umbralDebilitar = 0.35f;

        [Header("Al debilitarse")]
        [Tooltip("Multiplica el daño de sus golpes (veces).")]
        [Min(0f)] [SerializeField] private float multiplicadorDanio = 0.5f;
        [Tooltip("Multiplica la espera entre golpes (veces).")]
        [Min(1f)] [SerializeField] private float multiplicadorEspera = 1.6f;
        [Tooltip("Multiplica su velocidad y aceleración (veces).")]
        [Range(0.1f, 1f)] [SerializeField] private float multiplicadorVelocidad = 0.7f;
        [Tooltip("Multiplica el daño que recibe (veces).")]
        [Min(1f)] [SerializeField] private float multiplicadorDanioRecibido = 1.5f;

        [Header("Aviso de debilidad")]
        [Tooltip("Primer color del cuerpo mientras está débil (oscuro).")]
        [SerializeField] private Color colorDebil = new Color(0.12f, 0.12f, 0.14f);
        [Tooltip("Segundo color del cuerpo mientras está débil; el cuerpo pulsa entre los dos.")]
        [SerializeField] private Color colorDebilAlterno = new Color(0.45f, 0.4f, 0.1f);
        [Tooltip("Pulsos por segundo (Hz).")]
        [Min(0.1f)] [SerializeField] private float frecuenciaPulso = 3f;

        public float FraccionPiso => fraccionPiso;
        public float UmbralDebilitar => umbralDebilitar;
        public float MultiplicadorDanio => multiplicadorDanio;
        public float MultiplicadorEspera => multiplicadorEspera;
        public float MultiplicadorVelocidad => multiplicadorVelocidad;
        public float MultiplicadorDanioRecibido => multiplicadorDanioRecibido;
        public Color ColorDebil => colorDebil;
        public Color ColorDebilAlterno => colorDebilAlterno;
        public float FrecuenciaPulso => frecuenciaPulso;
    }
}
