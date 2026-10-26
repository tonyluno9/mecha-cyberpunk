using MechaCyberpunk.Combate.Habilidades;
using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>
    /// La habilidad que usa un jefe y cuándo (T-178, D-tony-139). Vive en IA porque Core no puede referenciar Combate.
    /// El rival no gasta munición: solo espera su enfriamiento, avisa y lanza.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de habilidad del rival", fileName = "DatosHabilidadRival_Nueva")]
    public class DatosHabilidadRival : ScriptableObject
    {
        [Tooltip("La habilidad que lanza.")]
        [SerializeField] private DatosHabilidad habilidad;

        [Tooltip("Nivel de la habilidad (1 a 4). Da la potencia: 1 + 0.25 por nivel sobre el primero.")]
        [Range(1, 4)] [SerializeField] private int nivel = 1;

        [Tooltip("Multiplica el enfriamiento base de la habilidad (veces).")]
        [Min(0.01f)] [SerializeField] private float factorEnfriamiento = 1.5f;

        [Tooltip("Se multiplica encima del anterior mientras el jefe está en furia (veces).")]
        [Min(0.01f)] [SerializeField] private float factorEnfriamientoEnFuria = 0.7f;

        [Tooltip("Cuánto dura el aviso antes de lanzar: el rival se detiene y brilla (s).")]
        [Min(0f)] [SerializeField] private float segundosAviso = 0.6f;

        [Tooltip("Distancia mínima al objetivo para empezar a usarla (m).")]
        [Min(0f)] [SerializeField] private float distanciaMinima;

        [Tooltip("Distancia máxima al objetivo para empezar a usarla (m).")]
        [Min(0f)] [SerializeField] private float distanciaMaxima = 8f;

        public DatosHabilidad Habilidad => habilidad;
        public int Nivel => nivel;
        public float FactorEnfriamiento => factorEnfriamiento;
        public float FactorEnfriamientoEnFuria => factorEnfriamientoEnFuria;
        public float SegundosAviso => segundosAviso;
        public float DistanciaMinima => distanciaMinima;
        public float DistanciaMaxima => distanciaMaxima;
    }
}
