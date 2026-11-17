using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// T-1013: cómo se ven y se sienten las armas especiales a pie (lanzagranadas, granada y rifle de plasma). El daño, el radio, la velocidad, la carga
    /// y la duración del rayo son de cada arma (<see cref="DatosArma"/>); aquí va lo de presentación. Vive en Personaje/Datos/Resources para que el
    /// componente lo cargue sin referencias de escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config armas especiales", fileName = "ConfigArmasEspeciales")]
    public class ConfigArmasEspeciales : ScriptableObject
    {
        public const string NombreRecurso = "ConfigArmasEspeciales";
        private static ConfigArmasEspeciales actual;

        [Header("Explosión")]
        [Tooltip("Efecto de la explosión (Particle Pack). Se escala con el radio.")]
        [SerializeField] private GameObject prefabExplosion;
        [Tooltip("Radio que tiene el efecto sin escalar (m).")]
        [Min(0.1f)] [SerializeField] private float radioDelEfecto = 3f;
        [Tooltip("Segundos que vive el efecto antes de borrarse (s).")]
        [Min(0.1f)] [SerializeField] private float segundosEfecto = 4f;
        [Tooltip("Parte del daño que llega al borde del radio (0 a 1); en el centro es el daño completo.")]
        [Range(0f, 1f)] [SerializeField] private float danioEnElBorde = 0.3f;
        [Tooltip("T-1325: parte del radio (0 a 1) dentro de la cual la explosión mata a cualquier persona que no sea el jugador (policías, pandilleros).")]
        [Range(0f, 1f)] [SerializeField] private float fraccionLetal = 0.6f;
        [Tooltip("Parte del daño que recibe el propio jugador si está dentro del radio (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float danioAlJugador = 0.5f;
        [Tooltip("Empuje a lo que tiene Rigidbody (cambio de velocidad en el centro, m/s).")]
        [Min(0f)] [SerializeField] private float empuje = 9f;
        [Tooltip("Color e intensidad del destello de luz de la explosión.")]
        [SerializeField] private Color colorDestello = new Color(1f, 0.6f, 0.25f, 1f);
        [Min(0f)] [SerializeField] private float intensidadDestello = 12f;

        [Header("Granada y proyectil")]
        [Tooltip("Modelo de la granada que vuela (lanzada o del lanzagranadas). Vacío = una esfera.")]
        [SerializeField] private GameObject prefabGranada;
        [Tooltip("Lado de la esfera de la granada cuando no hay modelo (m).")]
        [Min(0.01f)] [SerializeField] private float tamanoGranada = 0.12f;
        [Tooltip("Segundos de la mecha de la granada lanzada; el proyectil del lanzagranadas explota al chocar.")]
        [Min(0.1f)] [SerializeField] private float mechaGranada = 2.2f;
        [Tooltip("Grados hacia arriba que se suman a la mira al lanzar la granada con la mano (°).")]
        [Range(0f, 60f)] [SerializeField] private float elevacionLanzada = 14f;
        [Tooltip("Rebote de la granada lanzada al tocar el piso (0 a 1 de la velocidad).")]
        [Range(0f, 1f)] [SerializeField] private float rebote = 0.35f;
        [Tooltip("Puntos de la línea del arco que se ve al apuntar la granada.")]
        [Range(4, 120)] [SerializeField] private int puntosArco = 40;
        [Tooltip("Segundos de vuelo que dibuja el arco (s).")]
        [Min(0.1f)] [SerializeField] private float segundosArco = 2.2f;
        [Tooltip("Ancho de la línea del arco (m).")]
        [Min(0.001f)] [SerializeField] private float anchoArco = 0.04f;
        [SerializeField] private Color colorArco = new Color(1f, 0.85f, 0.2f, 0.85f);

        [Header("Rifle de plasma")]
        [Tooltip("Material del rayo y de la bola de carga (aditivo o emisivo).")]
        [SerializeField] private Material materialRayo;
        [SerializeField] private Color colorRayo = new Color(0.35f, 0.85f, 1f, 1f);
        [Tooltip("Ancho del rayo (m); late un poco alrededor de este valor.")]
        [Min(0.01f)] [SerializeField] private float anchoRayo = 0.22f;
        [Tooltip("Radio con que el rayo toca lo que cruza (m).")]
        [Min(0.01f)] [SerializeField] private float radioRayo = 0.35f;
        [Tooltip("T-1324: cuántos colliders que no son gente (paredes, carros, objetos) atraviesa el rayo antes de pararse.")]
        [Min(0)] [SerializeField] private int paredesQueAtraviesa = 3;
        [Tooltip("T-1324: multiplica el daño del rayo a objetos y carros (x).")]
        [Min(1f)] [SerializeField] private float multiplicadorObjetos = 3f;
        [Tooltip("T-1324: a una persona que toca, el rayo le hace tanto daño como su vida actual por este factor (x): un toque la mata.")]
        [Min(1f)] [SerializeField] private float factorMataGente = 20f;
        [Tooltip("Lado máximo de la bola de energía en la boca al terminar de cargar (m).")]
        [Min(0.01f)] [SerializeField] private float tamanoCarga = 0.3f;
        [Tooltip("Intensidad de la luz de la carga y del rayo.")]
        [Min(0f)] [SerializeField] private float intensidadLuzRayo = 6f;
        [Tooltip("Cada cuántos segundos suena el zumbido del rayo (s).")]
        [Min(0.05f)] [SerializeField] private float segundosZumbido = 0.35f;

        [Header("Tambor (revólver y lanzagranadas)")]
        [Tooltip("Segundos que tarda el tambor en girar a la siguiente recámara (s).")]
        [Min(0.01f)] [SerializeField] private float segundosGiroTambor = 0.14f;

        public static ConfigArmasEspeciales Actual
        {
            get
            {
                if (actual == null)
                {
                    actual = Resources.Load<ConfigArmasEspeciales>(NombreRecurso);
                    if (actual == null)
                    {
                        actual = CreateInstance<ConfigArmasEspeciales>();
                    }
                }
                return actual;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;

        public GameObject PrefabExplosion => prefabExplosion;
        public float RadioDelEfecto => radioDelEfecto;
        public float SegundosEfecto => segundosEfecto;
        public float DanioEnElBorde => danioEnElBorde;
        public float DanioAlJugador => danioAlJugador;
        public float FraccionLetal => fraccionLetal;
        public float Empuje => empuje;
        public Color ColorDestello => colorDestello;
        public float IntensidadDestello => intensidadDestello;
        public GameObject PrefabGranada => prefabGranada;
        public float TamanoGranada => tamanoGranada;
        public float MechaGranada => mechaGranada;
        public float ElevacionLanzada => elevacionLanzada;
        public float Rebote => rebote;
        public int PuntosArco => puntosArco;
        public float SegundosArco => segundosArco;
        public float AnchoArco => anchoArco;
        public Color ColorArco => colorArco;
        public Material MaterialRayo => materialRayo;
        public Color ColorRayo => colorRayo;
        public float AnchoRayo => anchoRayo;
        public float RadioRayo => radioRayo;
        public int ParedesQueAtraviesa => paredesQueAtraviesa;
        public float MultiplicadorObjetos => multiplicadorObjetos;
        public float FactorMataGente => factorMataGente;
        public float TamanoCarga => tamanoCarga;
        public float IntensidadLuzRayo => intensidadLuzRayo;
        public float SegundosZumbido => segundosZumbido;
        public float SegundosGiroTambor => segundosGiroTambor;
    }
}
