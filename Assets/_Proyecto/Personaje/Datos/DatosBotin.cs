using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Con qué modelos y reglas se pone en el piso el botín de los enemigos (T-686).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de botín", fileName = "DatosBotin_Nuevo")]
    public class DatosBotin : ScriptableObject
    {
        [Tooltip("Modelo de la pistola (W01) que queda en el piso.")]
        [SerializeField] private GameObject modeloPistola;

        [Tooltip("Prefab de la escopeta en el piso (W02, ya trae ArmaEnPiso).")]
        [SerializeField] private GameObject prefabEscopeta;

        [Tooltip("Segundos que el botín espera en el piso antes de desaparecer (s).")]
        [Min(5f)] [SerializeField] private float segundosEnPiso = 120f;

        [Tooltip("Separación entre el arma y el dinero soltados (m).")]
        [Min(0.1f)] [SerializeField] private float separacion = 1f;

        [Tooltip("Distancia mínima a otra arma igual en el piso para soltar la propia (m); evita duplicar la pistola de la historia.")]
        [Min(0f)] [SerializeField] private float distanciaArmaRepetida = 1.8f;

        [Tooltip("Probabilidad (0 a 1) de que un enemigo suelte también balas (T-746), si el jugador tiene un arma que las use.")]
        [Range(0f, 1f)] [SerializeField] private float probabilidadMunicion = 0.6f;

        public float ProbabilidadMunicion => probabilidadMunicion;
        public GameObject ModeloPistola => modeloPistola;
        public GameObject PrefabEscopeta => prefabEscopeta;
        public float SegundosEnPiso => segundosEnPiso;
        public float Separacion => separacion;
        public float DistanciaArmaRepetida => distanciaArmaRepetida;
    }
}
