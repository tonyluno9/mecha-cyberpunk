using UnityEngine;

namespace MechaCyberpunk.Combate.Datos
{
    /// <summary>Presentación de los efectos de las habilidades (greybox). Los valores de juego salen de DatosHabilidad.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config efectos de habilidad", fileName = "ConfigEfectosHabilidad_Nuevo")]
    public class ConfigEfectosHabilidad : ScriptableObject
    {
        [Header("Proyectil")]
        [Tooltip("Radio de la cápsula del proyectil y de su barrido de choque (m).")]
        [Min(0.02f)] [SerializeField] private float radioProyectil = 0.15f;

        [Tooltip("Largo de la cápsula del proyectil (m).")]
        [Min(0.05f)] [SerializeField] private float largoProyectil = 0.7f;

        [Tooltip("Duración de la estela (s).")]
        [Min(0.01f)] [SerializeField] private float duracionEstela = 0.15f;

        [Header("Cono")]
        [Tooltip("Cuánto dura el destello del cono (s).")]
        [Min(0.05f)] [SerializeField] private float duracionDestello = 0.25f;

        [Tooltip("Transparencia máxima del destello (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float alfaDestello = 0.55f;

        [Tooltip("Cuánto tarda el empuje en completarse (s).")]
        [Min(0.02f)] [SerializeField] private float duracionEmpuje = 0.2f;

        [Header("Escudo")]
        [Tooltip("Color del escudo (turquesa).")]
        [SerializeField] private Color colorEscudo = new Color(0.15f, 0.9f, 0.85f);

        [Tooltip("Transparencia del escudo (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float alfaEscudo = 0.25f;

        [Tooltip("Diámetro de la esfera del escudo (m).")]
        [Min(0.5f)] [SerializeField] private float diametroEscudo = 3f;

        public float RadioProyectil => radioProyectil;
        public float LargoProyectil => largoProyectil;
        public float DuracionEstela => duracionEstela;
        public float DuracionDestello => duracionDestello;
        public float AlfaDestello => alfaDestello;
        public float DuracionEmpuje => duracionEmpuje;
        public Color ColorEscudo => colorEscudo;
        public float AlfaEscudo => alfaEscudo;
        public float DiametroEscudo => diametroEscudo;
    }
}
