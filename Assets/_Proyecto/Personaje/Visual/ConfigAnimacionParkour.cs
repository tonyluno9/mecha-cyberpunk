using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1310: clips de la ronda 2 de Blender que el jugador usa fuera del controlador base (parkour, caída y robo de carro) y los números que los acompañan.
    /// Sin clips no hay parkour: el salto y el agarre de bordes siguen como antes. El asset lo crea ImportarClipsRonda2T1310 en Resources/ConfigAnimacionParkour.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigAnimacionParkour", menuName = "Mecha Cyberpunk/Personaje/Config animación de parkour")]
    public class ConfigAnimacionParkour : ScriptableObject
    {
        private const string NombreRecurso = "ConfigAnimacionParkour";

        private static ConfigAnimacionParkour actual;

        [Header("Clips (PJ01, Humanoid)")]
        [SerializeField, Tooltip("PJ01_Parkour_SaltarBarda: pasa por encima de un muro bajo o barda y cae del otro lado.")]
        private AnimationClip saltarBarda;

        [SerializeField, Tooltip("PJ01_Parkour_TreparBorde: sube a un borde alto (se agarra, se impulsa y queda de pie encima).")]
        private AnimationClip treparBorde;

        [SerializeField, Tooltip("PJ01_Parkour_RodarCaida: al aterrizar de una caída alta, amortigua, rueda y se levanta.")]
        private AnimationClip rodarCaida;

        [SerializeField, Tooltip("PJ01_Carro_SacarConductor: el jugador saca al conductor del carro (80 cuadros, empieza junto con el del conductor).")]
        private AnimationClip carroSacarConductor;

        [SerializeField, Tooltip("PJ01_Conductor_SerSacado: el conductor al que sacan (80 cuadros, empieza junto con el del jugador).")]
        private AnimationClip conductorSerSacado;

        [Header("Saltar barda")]
        [SerializeField, Min(0.1f), Tooltip("Altura mínima del obstáculo sobre los pies para saltarlo (m).")]
        private float alturaMinimaBarda = 0.6f;

        [SerializeField, Min(0.2f), Tooltip("Altura máxima del obstáculo que se salta de largo (m); más alto se trepa.")]
        private float alturaMaximaBarda = 1.3f;

        [SerializeField, Min(0.1f), Tooltip("Grosor máximo del obstáculo que se salta de largo (m). Más grueso (un contenedor) no se salta: se trepa.")]
        private float grosorMaximoBarda = 0.8f;

        [Header("Trepar borde")]
        [SerializeField, Min(0.2f), Tooltip("Altura del borde sobre los pies desde la que el agarre juega el clip de trepar (m).")]
        private float alturaMinimaBorde = 1.3f;

        [Header("Caída")]
        [SerializeField, Min(0.5f), Tooltip("Caída (m) desde la que el jugador rueda al aterrizar.")]
        private float alturaRodar = 4f;

        [SerializeField, Min(0f), Tooltip("Caída (m) a partir de la cual duele.")]
        private float umbralDanioCaida = 4f;

        [SerializeField, Min(0f), Tooltip("Vida que se pierde por cada metro que pasa del umbral (puntos/m).")]
        private float danioPorMetro = 2f;

        [SerializeField, Range(0f, 1f), Tooltip("Tope del daño de una caída como fracción de la vida máxima. Una caída nunca mata.")]
        private float fraccionDanioMaximo = 0.3f;

        [SerializeField, Min(0f), Tooltip("Lo que avanza el cuerpo rodando (m). El clip trae 5.5 m con la carrera de salida; solo se usa la parte de la rodada.")]
        private float metrosRodando = 2.2f;

        public AnimationClip SaltarBarda => saltarBarda;
        public AnimationClip TreparBorde => treparBorde;
        public AnimationClip RodarCaida => rodarCaida;
        public AnimationClip CarroSacarConductor => carroSacarConductor;
        public AnimationClip ConductorSerSacado => conductorSerSacado;
        public float AlturaMinimaBarda => alturaMinimaBarda;
        public float AlturaMaximaBarda => alturaMaximaBarda;
        public float GrosorMaximoBarda => grosorMaximoBarda;
        public float AlturaMinimaBorde => alturaMinimaBorde;
        public float AlturaRodar => alturaRodar;
        public float UmbralDanioCaida => umbralDanioCaida;
        public float DanioPorMetro => danioPorMetro;
        public float FraccionDanioMaximo => fraccionDanioMaximo;
        public float MetrosRodando => metrosRodando;

        /// <summary>Config cargada de Resources (null si no existe el asset).</summary>
        public static ConfigAnimacionParkour Actual
        {
            get
            {
                if (actual == null)
                {
                    actual = Resources.Load<ConfigAnimacionParkour>(NombreRecurso);
                }
                return actual;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico()
        {
            actual = null;
        }
    }
}
