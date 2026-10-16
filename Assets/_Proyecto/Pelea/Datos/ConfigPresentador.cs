using UnityEngine;

namespace MechaCyberpunk.Pelea.Datos
{
    /// <summary>
    /// Lo que dice el presentador antes del conteo (T-070). En las líneas, {alias} se cambia por el alias del jugador y
    /// {rival} por el nombre del rival. Las líneas son provisionales (demo).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Pelea/Config presentador", fileName = "ConfigPresentador")]
    public class ConfigPresentador : ScriptableObject
    {
        [Tooltip("Líneas en orden. Se muestran en mayúsculas. {alias} = jugador, {rival} = rival.")]
        [SerializeField] private string[] lineas =
        {
            "¡BIENVENIDOS AL CLANDESTINO!",
            "POR EL CAMPEONATO DEL BARRIO...",
            "EN ESTA ESQUINA... ¡{rival}!",
            "Y EN ESTA OTRA... ¡¡{alias}!!"
        };
        [Tooltip("Cuánto dura cada línea en pantalla (s).")]
        [Min(0.1f)] [SerializeField] private float segundosPorLinea = 1.6f;
        [Tooltip("Índice (desde 0) de la línea del grito: más grande y de otro color. -1 = ninguna.")]
        [Min(-1)] [SerializeField] private int indiceGrito = 3;
        [Tooltip("Alias que se dice si no hay perfil del jugador registrado.")]
        [SerializeField] private string aliasRespaldo = "NOVATO";
        [Tooltip("Si es verdadero, la revancha también tiene presentación.")]
        [SerializeField] private bool presentarEnRevancha;

        [Header("Pantalla")]
        [Tooltip("Tamaño de la letra de una línea normal (puntos).")]
        [Min(1)] [SerializeField] private int tamanoLetraLinea = 100;
        [Tooltip("Tamaño de la letra de la línea del grito (puntos).")]
        [Min(1)] [SerializeField] private int tamanoLetraGrito = 150;
        [SerializeField] private Color colorLinea = Color.white;
        [SerializeField] private Color colorGrito = new Color(1f, 0.85f, 0.1f);

        public string[] Lineas => lineas;
        public float SegundosPorLinea => segundosPorLinea;
        public int IndiceGrito => indiceGrito;
        public string AliasRespaldo => aliasRespaldo;
        public bool PresentarEnRevancha => presentarEnRevancha;
        public int TamanoLetraLinea => tamanoLetraLinea;
        public int TamanoLetraGrito => tamanoLetraGrito;
        public Color ColorLinea => colorLinea;
        public Color ColorGrito => colorGrito;
    }
}
