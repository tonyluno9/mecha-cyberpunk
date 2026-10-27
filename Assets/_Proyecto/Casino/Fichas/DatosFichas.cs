using UnityEngine;

namespace MechaCyberpunk.Casino.Fichas
{
    /// <summary>El tipo de cambio de la cajera (T-302): cada lote de dinero vale un lote de fichas, en los dos sentidos.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de fichas", fileName = "DatosFichas_Nuevo")]
    public class DatosFichas : ScriptableObject
    {
        [Min(1)] [Tooltip("Dinero que cuesta un lote de fichas ($).")]
        [SerializeField] private int dineroPorLote = 100;
        [Min(1)] [Tooltip("Fichas que se dan por cada lote de dinero (fichas).")]
        [SerializeField] private int fichasPorLote = 100;

        public int DineroPorLote => dineroPorLote;
        public int FichasPorLote => fichasPorLote;

        /// <summary>Para crear los datos desde código (editor y pruebas).</summary>
        public void Fijar(int dinero, int fichas)
        {
            dineroPorLote = Mathf.Max(1, dinero);
            fichasPorLote = Mathf.Max(1, fichas);
        }
    }
}
