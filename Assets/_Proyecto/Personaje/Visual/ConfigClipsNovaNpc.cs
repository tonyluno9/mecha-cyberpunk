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

        [Tooltip("T-1516: NPC_Platicar_1 (gestos con las dos manos), en bucle.")]
        [SerializeField] private AnimationClip platicar1;

        [Tooltip("T-1516: NPC_Platicar_2 (mano en la cadera, la otra señala), en bucle.")]
        [SerializeField] private AnimationClip platicar2;

        [Tooltip("T-1516: NPC_Sentado_Banca (asiento a 0.46 m, manos en los muslos), en bucle.")]
        [SerializeField] private AnimationClip sentadoBanca;

        [Tooltip("T-1516: NPC_Agachado_CubriendoCabeza (en cuclillas con las manos en la cabeza), en bucle.")]
        [SerializeField] private AnimationClip agachado;

        [Tooltip("T-1553: E18_Jetpack_Volar: el elite vuela con el jetpack hacia el jugador, en bucle.")]
        [SerializeField] private AnimationClip jetpackVolar;

        /// <summary>T-1553: E18_Jetpack_Volar (null si falta: el elite no vuela).</summary>
        public AnimationClip JetpackVolar => jetpackVolar;

        /// <summary>T-1553: pone el clip de vuelo del E18 (lo llama el método de editor RobotsNovaT1553.Aplicar).</summary>
        public void ConfigurarVueloElite(AnimationClip volar) => jetpackVolar = volar;

        [Tooltip("T-1553: giro extra (grados Euler, en el marco de la mano) de la W10 de los E15 para que apunte bien con E15_Caminar y E15_Correr. Lo escribe RobotsNovaT1553.Aplicar y lo lee EscalaNovaT1497.PonerArma; cero = sin corrección.")]
        [SerializeField] private Vector3 giroArmaE15;

        /// <summary>T-1553: corrección de giro de la W10 en la mano de los E15 (Euler en grados; cero si no hace falta).</summary>
        public Vector3 GiroArmaE15 => giroArmaE15;

        public void ConfigurarGiroArmaE15(Vector3 giro) => giroArmaE15 = giro;

        public AnimationClip Platicar1 => platicar1;
        public AnimationClip Platicar2 => platicar2;
        public AnimationClip SentadoBanca => sentadoBanca;
        public AnimationClip Agachado => agachado;

        /// <summary>T-1516: platicar, sentado y agachado (los pone el método de editor ClipsNpcPlaticarT1516).</summary>
        public void ConfigurarNpc(AnimationClip platica1, AnimationClip platica2, AnimationClip sentado, AnimationClip clipAgachado)
        {
            platicar1 = platica1;
            platicar2 = platica2;
            sentadoBanca = sentado;
            agachado = clipAgachado;
        }

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
