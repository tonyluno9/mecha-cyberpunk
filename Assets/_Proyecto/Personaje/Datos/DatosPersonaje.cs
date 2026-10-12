using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Estadísticas del jugador a pie que cambian con los implantes (vida, velocidad, salto).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del personaje", fileName = "DatosPersonaje_Nuevo")]
    public class DatosPersonaje : ScriptableObject
    {
        [Tooltip("Vida máxima del jugador a pie (puntos).")]
        [Min(1f)] [SerializeField] private float vidaMaxima = 100f;

        [Tooltip("Velocidad al caminar (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadCaminar = 4.5f;

        [Tooltip("Velocidad al correr (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadCorrer = 7.5f;

        [Tooltip("Altura máxima del salto (m).")]
        [Min(0.1f)] [SerializeField] private float alturaSalto = 1.1f;

        public float VidaMaxima => vidaMaxima;
        public float VelocidadCaminar => velocidadCaminar;
        public float VelocidadCorrer => velocidadCorrer;
        public float AlturaSalto => alturaSalto;
    }
}
