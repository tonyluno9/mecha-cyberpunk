using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1506: clips de una sola vez que no van en el controlador (el código los juega con un PlayableGraph): la caída de Nova (aire, aterrizaje, levantarse)
    /// y el susto de los civiles. El asset va en Resources/ConfigClipsNovaNpc; lo crea el método de editor ClipsNpcNovaT1506.CrearConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigClipsNovaNpc", menuName = "Mecha/Config clips Nova y NPC")]
    public class ConfigClipsNovaNpc : ScriptableObject
    {
        public const string NombreRecurso = "ConfigClipsNovaNpc";

        [Tooltip("E18_Caida_Aire: cayendo en bucle mientras baja del cielo o de la nave.")]
        [SerializeField] private AnimationClip caidaAire;

        [Tooltip("E18_Caida_Aterrizaje: toca el piso (1.03 s, impacto en el cuadro 5).")]
        [SerializeField] private AnimationClip caidaAterrizaje;

        [Tooltip("E18_Caida_Levantarse: se levanta despacio (1.37 s).")]
        [SerializeField] private AnimationClip caidaLevantarse;

        [Tooltip("NPC_Asustarse: brinco hacia atrás con las manos a la cara (0.67 s) al empezar a huir.")]
        [SerializeField] private AnimationClip asustarse;

        public AnimationClip CaidaAire => caidaAire;
        public AnimationClip CaidaAterrizaje => caidaAterrizaje;
        public AnimationClip CaidaLevantarse => caidaLevantarse;
        public AnimationClip Asustarse => asustarse;

        private static ConfigClipsNovaNpc actual;

        /// <summary>El asset de Resources (null si no existe).</summary>
        public static ConfigClipsNovaNpc Actual => actual != null ? actual : (actual = Resources.Load<ConfigClipsNovaNpc>(NombreRecurso));

        /// <summary>Para los creadores y las pruebas.</summary>
        public void Configurar(AnimationClip aire, AnimationClip aterrizaje, AnimationClip levantarse, AnimationClip susto)
        {
            caidaAire = aire;
            caidaAterrizaje = aterrizaje;
            caidaLevantarse = levantarse;
            asustarse = susto;
        }

        /// <summary>Para las pruebas: usa esta config en lugar de la de Resources.</summary>
        public static void Forzar(ConfigClipsNovaNpc config) => actual = config;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;
    }
}
