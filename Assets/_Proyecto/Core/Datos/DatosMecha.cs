using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Estadísticas de un mecha. Se balancean en el asset, no en el código.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del mecha", fileName = "DatosMecha_Nuevo")]
    public class DatosMecha : ScriptableObject
    {
        [Tooltip("Nombre en pantallas (texto).")]
        [SerializeField] private string nombre = "Mecha";

        [Tooltip("Vida al empezar la pelea (puntos).")]
        [Min(1f)] [SerializeField] private float vidaMaxima = 100f;

        [Tooltip("Velocidad al caminar (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadMaxima = 4f;

        [Tooltip("Qué tan rápido arranca (m/s²).")]
        [Min(0.1f)] [SerializeField] private float aceleracion = 6f;

        [Tooltip("Qué tan rápido frena (m/s²).")]
        [Min(0.1f)] [SerializeField] private float desaceleracion = 10f;

        [Tooltip("Giro máximo del cuerpo (°/s).")]
        [Min(1f)] [SerializeField] private float velocidadGiroMaxima = 90f;

        [Tooltip("Daño de un golpe (puntos).")]
        [Min(0.1f)] [SerializeField] private float danioGolpe = 10f;

        [Tooltip("Alcance del golpe, medido desde el centro del mecha hacia adelante (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceGolpe = 3f;

        [Tooltip("Del clic (o la decisión del rival) al impacto (s). En el rival es el aviso visible.")]
        [Min(0.01f)] [SerializeField] private float preparacionGolpe = 0.25f;

        [Tooltip("Del impacto a poder golpear otra vez (s).")]
        [Min(0.01f)] [SerializeField] private float recargaGolpe = 0.8f;

        [Tooltip("Del clic derecho a que la guardia quede arriba (s).")]
        [Min(0.01f)] [SerializeField] private float tiempoSubirGuardia = 0.15f;

        [Tooltip("Parte del daño que quita un bloqueo normal (de 0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float reduccionBloqueo = 0.7f;

        [Tooltip("Si la guardia quedó arriba hace menos que esto, no entra daño (s).")]
        [Min(0.01f)] [SerializeField] private float ventanaBloqueoPerfecto = 0.2f;

        public string Nombre => nombre;
        public float VidaMaxima => vidaMaxima;
        public float VelocidadMaxima => velocidadMaxima;
        public float Aceleracion => aceleracion;
        public float Desaceleracion => desaceleracion;
        public float VelocidadGiroMaxima => velocidadGiroMaxima;
        public float DanioGolpe => danioGolpe;
        public float AlcanceGolpe => alcanceGolpe;
        public float PreparacionGolpe => preparacionGolpe;
        public float RecargaGolpe => recargaGolpe;
        public float TiempoSubirGuardia => tiempoSubirGuardia;
        public float ReduccionBloqueo => reduccionBloqueo;
        public float VentanaBloqueoPerfecto => ventanaBloqueoPerfecto;
    }
}
