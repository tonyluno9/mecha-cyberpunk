using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-885 (playtest 7-nov, sesión 2): "el puente que atraviesa dos edificios sin sentido se quita, o se rehace según K07". Los dos puentes
    /// N02 de la Av. Neón entraban de un edificio al de enfrente sin escaleras ni banquetas. Rehacerlo como K07 (pasarela de banqueta a
    /// banqueta con escaleras) pide un modelo que no hay: se quitan y la pasarela va al pedido de arte (docs/pedido-arte-08nov-c.md, PS01).
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oPuentes.Colocar
    /// </summary>
    public static class ColocarS2oPuentes
    {
        public const string NombrePuente = "N02_T505";

        [MenuItem("Mecha Cyberpunk/S2o/6 Quitar los puentes sin sentido")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            int n = 0;
            foreach (Transform t in ColocarS2.Todos().Where(t => t.name == NombrePuente && t.gameObject.activeSelf).ToArray())
            {
                t.gameObject.SetActive(false);
                ColocarS2.Informe.AppendLine("apagado " + ColocarS2.Ruta(t) + " en " + t.position.ToString("F0"));
                n++;
            }
            ColocarS2.Informe.AppendLine("Puentes N02 quitados: " + n);
            ColocarS2.Guardar("s2o-puentes");
        }
    }
}
