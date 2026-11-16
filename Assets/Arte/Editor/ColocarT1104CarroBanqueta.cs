using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1104: el carro robable 7 (GTA_T511) estaba parado a 53 m de x en medio de la banqueta que antes ocupaban las pasarelas y cortaba el paso a pie
    /// (x 53, z 75). Se pega al borde de la calle (x 55.3). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1104CarroBanqueta.Mover
    /// </summary>
    public static class ColocarT1104CarroBanqueta
    {
        public const string RutaCarro = "GTA_T511/CarroRobable_7";
        public const float XBorde = 55.3f;

        [MenuItem("Mecha Cyberpunk/T-1104/Pegar carro al borde")]
        public static void Mover()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Transform carro = ColocarS2.Buscar(RutaCarro);
            if (carro == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaCarro);
            }
            else
            {
                Vector3 antes = carro.position;
                carro.position = new Vector3(XBorde, antes.y, antes.z);
                ColocarS2.Informe.AppendLine("Carro de " + antes.ToString("F2") + " a " + carro.position.ToString("F2"));
            }
            Physics.SyncTransforms();
            ColocarS2.Guardar("t1104-carro");
        }
    }
}
