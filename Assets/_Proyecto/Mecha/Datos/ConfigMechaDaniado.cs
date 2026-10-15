using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Reglas del daño persistente del mecha del jugador (D-tony-42).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config mecha dañado", fileName = "ConfigMechaDaniado_Nueva")]
    public class ConfigMechaDaniado : ScriptableObject
    {
        [Tooltip("Vida mínima con la que empieza una pelea, como fracción de la vida máxima (0.15 = 15 %). " +
                 "Si el mecha quedó por debajo (o en KO), la siguiente pelea lo levanta a este piso.")]
        [Range(0.01f, 1f)] [SerializeField] private float pisoVidaPersistente = 0.15f;

        public float PisoVidaPersistente => pisoVidaPersistente;
    }
}
