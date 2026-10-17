using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Datos de las ligas del clandestino (T-154): nombres, rivales por liga y fracción del premio en revancha.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de ligas", fileName = "DatosLigas_Nuevo")]
    public class DatosLigas : ScriptableObject
    {
        [Tooltip("Un nombre por liga, en orden (la primera es la más baja).")]
        [SerializeField] private string[] nombres = { "SÓTANO", "CALLE", "DISTRITO", "CAMPEONATO" };

        [Tooltip("Rivales por liga (mínimo 2). El último de cada liga es el campeón.")]
        [Min(2)] [SerializeField] private int rivalesPorLiga = 5;

        [Tooltip("Fracción del premio que se cobra al revancharse con un rival ya vencido (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float fraccionPremioRevancha = 0.5f;

        public string[] Nombres => nombres;
        public int CantidadLigas => nombres == null ? 0 : nombres.Length;
        public int RivalesPorLiga => rivalesPorLiga;
        public float FraccionPremioRevancha => fraccionPremioRevancha;

        private void OnValidate()
        {
            if (rivalesPorLiga < 2) rivalesPorLiga = 2;
            fraccionPremioRevancha = Mathf.Clamp01(fraccionPremioRevancha);
        }
    }
}
