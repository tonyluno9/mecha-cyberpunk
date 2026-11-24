using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// T-drops-24nov: qué sueltan los enemigos al caer (balas de su arma o de la tuya, y de vez en cuando un botiquín), cuánto duran en el piso,
    /// cómo brillan y desde qué distancia se recogen (con el cuerpo o, en VR, con la mano). El asset va en Personaje/Datos/Resources.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config de recompensas de enemigos", fileName = "ConfigRecompensasEnemigo_Nuevo")]
    public class ConfigRecompensasEnemigo : ScriptableObject
    {
        public const string NombreRecurso = "ConfigRecompensasEnemigo_Base";

        [Header("Probabilidad de soltar balas (0 a 1)")]
        [Tooltip("Policía: casi siempre.")]
        [Range(0f, 1f)] [SerializeField] private float municionPolicia = 0.9f;
        [Tooltip("Guardias y robots de NOVA: casi siempre.")]
        [Range(0f, 1f)] [SerializeField] private float municionNova = 0.85f;
        [Tooltip("SKULLS: a veces.")]
        [Range(0f, 1f)] [SerializeField] private float municionSkulls = 0.35f;
        [Tooltip("Las demás pandillas y maleantes.")]
        [Range(0f, 1f)] [SerializeField] private float municionOtros = 0.3f;

        [Header("Botiquín")]
        [Tooltip("Probabilidad (0 a 1) de que cualquier enemigo suelte un botiquín pequeño.")]
        [Range(0f, 1f)] [SerializeField] private float probabilidadBotiquin = 0.15f;
        [Tooltip("Vida que cura el botiquín (puntos).")]
        [Min(1f)] [SerializeField] private float curacionBotiquin = 25f;

        [Header("En el piso")]
        [Tooltip("Segundos que duran en el piso antes de desaparecer (s).")]
        [Min(1f)] [SerializeField] private float segundosEnPiso = 30f;
        [Tooltip("Radio (m, en el plano) alrededor del jugador para recogerlos al pasar.")]
        [Min(0.1f)] [SerializeField] private float radioRecoger = 1.3f;
        [Tooltip("Radio (m) alrededor de una mano en VR para recogerlos al acercarla.")]
        [Min(0.05f)] [SerializeField] private float radioMano = 0.35f;
        [Tooltip("Separación (m) entre las balas y el botiquín, para que no queden uno encima del otro.")]
        [Min(0f)] [SerializeField] private float separacion = 0.7f;

        [Header("Brillo suave")]
        [Tooltip("Color de la luz de las balas.")]
        [SerializeField] private Color colorMunicion = new Color(1f, 0.65f, 0.15f);
        [Tooltip("Color de la luz del botiquín.")]
        [SerializeField] private Color colorBotiquin = new Color(0.3f, 1f, 0.45f);
        [Tooltip("Intensidad de la luz (sin unidad de Unity).")]
        [Min(0f)] [SerializeField] private float intensidadBrillo = 0.6f;
        [Tooltip("Alcance de la luz (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceBrillo = 1.3f;

        public float CuracionBotiquin => curacionBotiquin;
        public float ProbabilidadBotiquin => probabilidadBotiquin;
        public float SegundosEnPiso => segundosEnPiso;
        public float RadioRecoger => radioRecoger;
        public float RadioMano => radioMano;
        public float Separacion => separacion;
        public Color ColorMunicion => colorMunicion;
        public Color ColorBotiquin => colorBotiquin;
        public float IntensidadBrillo => intensidadBrillo;
        public float AlcanceBrillo => alcanceBrillo;

        /// <summary>Probabilidad de soltar balas según el bando del enemigo.</summary>
        public float ProbabilidadMunicion(FaccionRecompensa faccion)
        {
            switch (faccion)
            {
                case FaccionRecompensa.Policia: return municionPolicia;
                case FaccionRecompensa.Nova: return municionNova;
                case FaccionRecompensa.Skulls: return municionSkulls;
                default: return municionOtros;
            }
        }

        private static ConfigRecompensasEnemigo cargada;

        /// <summary>El asset de Resources (o uno con los valores por defecto si falta).</summary>
        public static ConfigRecompensasEnemigo Actual
        {
            get
            {
                if (cargada == null)
                {
                    cargada = Resources.Load<ConfigRecompensasEnemigo>(NombreRecurso);
                    if (cargada == null)
                    {
                        cargada = CreateInstance<ConfigRecompensasEnemigo>();
                        cargada.name = NombreRecurso + "_PorDefecto";
                    }
                }
                return cargada;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => cargada = null;
    }
}
