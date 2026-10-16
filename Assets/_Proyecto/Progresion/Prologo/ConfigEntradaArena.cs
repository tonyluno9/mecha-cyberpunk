using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>Ritmo y destino de salida de la entrada a la arena con el mecha de la historia (T-128).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config entrada a la arena", fileName = "ConfigEntradaArena_Nueva")]
    public class ConfigEntradaArena : ScriptableObject
    {
        [Tooltip("Id de la aparición donde queda el jugador a pie al salir del clandestino.")]
        [SerializeField] private string idAparicionSalida = "Clandestino_Salida";

        [Tooltip("Segundos que pasan entre el resultado de la pelea y el fundido de salida (s).")]
        [Min(0f)] [SerializeField] private float segundosTrasResultado = 3f;

        [Tooltip("Segundos máximos que espera a que el mecha se detenga para poder bajar antes de rendirse (s).")]
        [Min(0f)] [SerializeField] private float segundosMaximosParaBajar = 5f;

        public string IdAparicionSalida => idAparicionSalida;
        public float SegundosTrasResultado => segundosTrasResultado;
        public float SegundosMaximosParaBajar => segundosMaximosParaBajar;
    }
}
