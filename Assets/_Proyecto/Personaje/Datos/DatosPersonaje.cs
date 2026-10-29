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

        [Header("Golpe a pie")]
        [Tooltip("Daño de cada golpe de puño (puntos).")]
        [Min(0f)] [SerializeField] private float danioGolpe = 15f;

        [Tooltip("Alcance del golpe desde la cámara hasta el borde de la esfera (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceGolpe = 1.6f;

        [Tooltip("Radio de la esfera que busca al objetivo (m).")]
        [Min(0.01f)] [SerializeField] private float radioGolpe = 0.3f;

        [Tooltip("Tiempo entre el clic y el impacto (s).")]
        [Min(0.01f)] [SerializeField] private float preparacionGolpe = 0.12f;

        [Tooltip("Tiempo después del impacto en que no se puede golpear otra vez (s).")]
        [Min(0.01f)] [SerializeField] private float recargaGolpe = 0.45f;

        [Header("Regeneración")]
        [Tooltip("Vida que recupera por segundo cuando lleva un rato sin recibir daño (puntos/s).")]
        [Min(0f)] [SerializeField] private float regeneracionPorSegundo = 5f;

        [Tooltip("Segundos sin recibir daño antes de empezar a regenerar (s).")]
        [Min(0f)] [SerializeField] private float segundosParaRegenerar = 6f;

        [Header("KO a pie")]
        [Tooltip("Altura de la cámara sobre el piso cuando el jugador cae en KO (m).")]
        [Min(0.05f)] [SerializeField] private float alturaCamaraKO = 0.4f;

        [Tooltip("Ladeo de la cámara en KO (°).")]
        [Range(0f, 90f)] [SerializeField] private float ladeoCamaraKO = 40f;

        [Tooltip("Qué tan rápido cae la cámara al llegar a KO (1/s; más alto es más rápido).")]
        [Min(0.1f)] [SerializeField] private float velocidadCaidaKO = 6f;

        public float VidaMaxima => vidaMaxima;
        public float RegeneracionPorSegundo => regeneracionPorSegundo;
        public float SegundosParaRegenerar => segundosParaRegenerar;
        public float DanioGolpe => danioGolpe;
        public float AlcanceGolpe => alcanceGolpe;
        public float RadioGolpe => radioGolpe;
        public float PreparacionGolpe => preparacionGolpe;
        public float RecargaGolpe => recargaGolpe;
        public float AlturaCamaraKO => alturaCamaraKO;
        public float LadeoCamaraKO => ladeoCamaraKO;
        public float VelocidadCaidaKO => velocidadCaidaKO;
        public float VelocidadCaminar => velocidadCaminar;
        public float VelocidadCorrer => velocidadCorrer;
        public float AlturaSalto => alturaSalto;
    }
}
