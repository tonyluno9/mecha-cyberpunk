using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>Lo que paga una pelea: premio por ganar y por perder (sin apuestas todavía, perder paga 0).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Economia/Datos premio de pelea", fileName = "DatosPremioPelea")]
    public class DatosPremioPelea : ScriptableObject
    {
        [Tooltip("Dinero que se abona al ganar la pelea (Ƀ).")]
        [Min(0)] [SerializeField] private int premioGanar;
        [Tooltip("Dinero que se abona al perder la pelea (Ƀ).")]
        [Min(0)] [SerializeField] private int premioPerder;

        public int PremioGanar => Mathf.Max(0, premioGanar);
        public int PremioPerder => Mathf.Max(0, premioPerder);
    }
}
