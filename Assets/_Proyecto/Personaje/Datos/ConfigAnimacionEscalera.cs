using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// T-1204: clips Humanoid para subir y bajar escaleras. Si no hay clips, la escalera sigue sin animación.
    /// El asset lo crea ImportarAnimacionesEscalera en Resources/ConfigAnimacionEscalera.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigAnimacionEscalera", menuName = "Mecha Cyberpunk/Personaje/Config animación de escalera")]
    public class ConfigAnimacionEscalera : ScriptableObject
    {
        private const string NombreRecurso = "ConfigAnimacionEscalera";

        private static ConfigAnimacionEscalera actual;

        [SerializeField, Tooltip("Clip de subir la escalera (Humanoid). Vacío si aún no existe.")]
        private AnimationClip subir;

        [SerializeField, Tooltip("Clip de bajar la escalera (Humanoid). Vacío: se usa el de subir.")]
        private AnimationClip bajar;

        [SerializeField, Min(0.1f), Tooltip("Metros que se suben por cada vuelta completa del clip (m).")]
        private float metrosPorCiclo = 0.9f;

        public AnimationClip Subir => subir;
        public AnimationClip Bajar => bajar;
        public float MetrosPorCiclo => metrosPorCiclo;
        public bool HayClips => subir != null || bajar != null;

        /// <summary>Config cargada de Resources (null si no existe el asset).</summary>
        public static ConfigAnimacionEscalera Actual
        {
            get
            {
                if (actual == null)
                {
                    actual = Resources.Load<ConfigAnimacionEscalera>(NombreRecurso);
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
