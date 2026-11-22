using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1561: clips de NPC que el código juega de una vez con un PlayableGraph (ReproductorClipUnico): civil herido pidiendo ayuda o herido de pie, civil que llama a la policía y
    /// conductor al que sacan del carro. Va aparte de ConfigClipsNovaNpc para no chocar con T-1560. El asset va en Resources/ConfigClipsNpcT1561; lo crea TransicionesT1561.Aplicar.
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigClipsNpcT1561", menuName = "Mecha/Config clips NPC T-1561")]
    public class ConfigClipsNpcT1561 : ScriptableObject
    {
        public const string NombreRecurso = "ConfigClipsNpcT1561";

        [Tooltip("NPC_Herido_PedirAyuda: civil herido haciendo señas pidiendo ayuda (bucle).")]
        [SerializeField] private AnimationClip heridoPedirAyuda;

        [Tooltip("PJ01_Herido_Idle: herido de pie (bucle); el civil herido que no pide ayuda.")]
        [SerializeField] private AnimationClip heridoIdle;

        [Tooltip("NPC_LlamarPolicia: civil que llama a la policía tras ver al jugador matar a alguien (bucle).")]
        [SerializeField] private AnimationClip llamarPolicia;

        [Tooltip("NPC_SacadoDelCarro_Huir: el conductor al que sacan del carro (174 cuadros; arranca 18 cuadros después del clip del jugador).")]
        [SerializeField] private AnimationClip sacadoDelCarroHuir;

        public AnimationClip HeridoPedirAyuda => heridoPedirAyuda;
        public AnimationClip HeridoIdle => heridoIdle;
        public AnimationClip LlamarPolicia => llamarPolicia;
        public AnimationClip SacadoDelCarroHuir => sacadoDelCarroHuir;

        /// <summary>Para el creador de editor y las pruebas.</summary>
        public void Configurar(AnimationClip pedirAyuda, AnimationClip herido, AnimationClip llamar, AnimationClip sacado)
        {
            heridoPedirAyuda = pedirAyuda;
            heridoIdle = herido;
            llamarPolicia = llamar;
            sacadoDelCarroHuir = sacado;
        }

        private static ConfigClipsNpcT1561 actual;

        /// <summary>El asset de Resources (null si no existe).</summary>
        public static ConfigClipsNpcT1561 Actual => actual != null ? actual : (actual = Resources.Load<ConfigClipsNpcT1561>(NombreRecurso));

        /// <summary>Para las pruebas: usa esta config en lugar de la de Resources.</summary>
        public static void Forzar(ConfigClipsNpcT1561 config) => actual = config;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;
    }
}
