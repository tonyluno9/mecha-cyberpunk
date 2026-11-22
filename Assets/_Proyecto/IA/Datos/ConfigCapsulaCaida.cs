using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>
    /// T-1621: lo que necesita la cápsula de caída CP01: su prefab, los cuatro clips de salida de los robots y los prefabs de prueba. El asset va en Resources/ConfigCapsulaCaida;
    /// lo crea el método de editor CapsulasCaidaT1621.Aplicar. Los tiempos y las escalas están en CalculoCapsulaCaida (no cambian con el balance).
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigCapsulaCaida", menuName = "Mecha/Config cápsula de caída")]
    public class ConfigCapsulaCaida : ScriptableObject
    {
        public const string NombreRecurso = "ConfigCapsulaCaida";

        [Tooltip("Prefab CP01_Capsula: la cápsula con su Animation (CP01_Abrir), los materiales holográficos y el script CapsulaCaida.")]
        [SerializeField] private GameObject prefabCapsula;

        [Tooltip("E15_Capsula_Salida: héroe 1 s y se levanta (cuadros 1 a 122, 24 fps).")]
        [SerializeField] private AnimationClip salidaE15;

        [Tooltip("E16_Capsula_Salida: héroe 1 s y se levanta (cuadros 1 a 140).")]
        [SerializeField] private AnimationClip salidaE16;

        [Tooltip("E18_Capsula_Salida: aterrizaje de héroe, 1 s y se levanta (cuadros 1 a 96).")]
        [SerializeField] private AnimationClip salidaE18;

        [Tooltip("E17_Capsula_Salida: agachado 1 s y ladra dos veces (cuadros 1 a 98).")]
        [SerializeField] private AnimationClip salidaE17;

        [Tooltip("Prefabs para la prueba en el editor (CapsulaCaida.ProbarTodos), en el orden de TipoCapsulaCaida: E16, E18, E15, E17, DR02_a, E13, DR01, DR04_a.")]
        [SerializeField] private GameObject[] prefabsPrueba;

        private static ConfigCapsulaCaida actual;

        public GameObject PrefabCapsula => prefabCapsula;
        public AnimationClip SalidaE15 => salidaE15;
        public AnimationClip SalidaE16 => salidaE16;
        public AnimationClip SalidaE18 => salidaE18;
        public AnimationClip SalidaE17 => salidaE17;
        public GameObject[] PrefabsPrueba => prefabsPrueba;

        /// <summary>El asset de Resources (null si no existe).</summary>
        public static ConfigCapsulaCaida Actual => actual != null ? actual : (actual = Resources.Load<ConfigCapsulaCaida>(NombreRecurso));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;

        /// <summary>Para el creador de editor y las pruebas.</summary>
        public void Configurar(GameObject capsula, AnimationClip e15, AnimationClip e16, AnimationClip e18, AnimationClip e17, GameObject[] prueba)
        {
            prefabCapsula = capsula;
            salidaE15 = e15;
            salidaE16 = e16;
            salidaE18 = e18;
            salidaE17 = e17;
            prefabsPrueba = prueba;
        }
    }
}
