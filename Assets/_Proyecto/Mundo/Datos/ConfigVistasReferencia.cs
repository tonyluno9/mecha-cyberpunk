using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>
    /// Las poses fijas de las capturas de arte de la fase (V1 a V4): dónde están los ojos de la cámara y hacia dónde miran.
    /// Todas las capturas de arte usan estas vistas para poder compararlas entre tareas.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config vistas de referencia", fileName = "ConfigVistasReferencia")]
    public class ConfigVistasReferencia : ScriptableObject
    {
        /// <summary>Una pose de cámara.</summary>
        [System.Serializable]
        public struct Vista
        {
            [Tooltip("Nombre corto de la vista (V1 Esquina, V2 Clandestino...).")]
            public string nombre;

            [Tooltip("Posición de los ojos de la cámara en el mundo (m).")]
            public Vector3 ojos;

            [Tooltip("Giro hacia donde mira, en ° desde el norte (+Z) hacia el este (+X): 90 es el este, 180 el sur, 270 el oeste.")]
            [Range(0f, 360f)] public float giro;

            [Tooltip("Inclinación de la mirada en °: positivo mira hacia abajo.")]
            [Range(-80f, 80f)] public float inclinacion;

            [Tooltip("Si la cámara vuela sin colisiones en esta vista (la aérea).")]
            public bool vuela;
        }

        [Tooltip("Altura de los ojos sobre la raíz de la cámara libre (m); debe coincidir con el prefab CamaraLibre.")]
        [SerializeField, Min(0f)] private float alturaOjos = 1.75f;

        [SerializeField]
        private Vista[] vistas =
        {
            new Vista { nombre = "V1 Esquina", ojos = new Vector3(-44f, 1.75f, -4f), giro = 90f },
            new Vista { nombre = "V2 Clandestino", ojos = new Vector3(0f, 1.95f, -8f), giro = 0f },
            // La casa está en (-63, -77): desde (-45, -72) queda al oeste-suroeste.
            new Vista { nombre = "V3 Casa", ojos = new Vector3(-45f, 1.75f, -72f), giro = 254.5f },
            // Mira al centro del barrio (0, 0, 0): 45° al noreste y 25° hacia abajo.
            new Vista { nombre = "V4 Aérea", ojos = new Vector3(-60f, 40f, -60f), giro = 45f, inclinacion = 25.2f, vuela = true }
        };

        public float AlturaOjos => alturaOjos;
        public int Cantidad => vistas.Length;
        public Vista this[int indice] => vistas[indice];
    }
}
