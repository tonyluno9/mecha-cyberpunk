using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-861 puso letreros con el nombre de la calle (EJE CENTRAL, AV. COYOTE...) colgados de los postes; T-921 (playtest del 9-nov) los quitó
    /// de toda la ciudad. <see cref="Colocar"/> ya no coloca nada: solo quita los que queden, para que no regresen si alguien vuelve a correr
    /// el paso "9 Nombres de calles". Los nombres de calle del GPS y de los datos no se tocan; el letrero blanco sin texto del semáforo ES01 se queda.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CallesS2t.Colocar
    /// </summary>
    public static class CallesS2t
    {
        public const string NombreRaiz = "S2t_Calles";

        /// <summary>Los nombres de calle que tuvieron letrero.</summary>
        public static readonly string[] NombresDeCalle =
        {
            "EJE CENTRAL", "CALLE DE LOS CABLES", "CALLE DEL MERCADO", "CALLE OBRERA", "AV. COYOTE", "AV. NEÓN", "PERIFÉRICO PONIENTE", "PERIFÉRICO ORIENTE",
        };

        /// <summary>Objetos de la escena abierta que son un letrero con nombre de calle (la raíz S2t_Calles, Letrero_/Nombre_ de una calle, o un texto con ese nombre).</summary>
        public static GameObject[] Encontrar()
        {
            var lista = new System.Collections.Generic.HashSet<GameObject>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = t.name;
                if (n == NombreRaiz || NombresDeCalle.Any(c => n == "Letrero_" + c || n == "Nombre_" + c))
                {
                    lista.Add(t.gameObject);
                }
            }
            foreach (UnityEngine.UI.Text x in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (NombresDeCalle.Contains(x.text))
                {
                    lista.Add(x.transform.parent != null && x.transform.parent.GetComponent<Canvas>() != null ? x.transform.parent.gameObject : x.gameObject);
                }
            }
            foreach (TextMesh x in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (NombresDeCalle.Contains(x.text))
                {
                    lista.Add(x.gameObject);
                }
            }
            return lista.ToArray();
        }

        [MenuItem("Mecha Cyberpunk/S2t/9 Quitar nombres de calles (T-921)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            int n = 0;
            foreach (GameObject go in Encontrar())
            {
                if (go != null)
                {
                    ColocarS2.Informe.AppendLine("quito " + go.name + " en " + go.transform.root.name);
                    Object.DestroyImmediate(go);
                    n++;
                }
            }
            ColocarS2.Informe.AppendLine("T-921: " + n + " objetos con nombre de calle quitados; quedan " + Encontrar().Length);
            ColocarS2.Guardar("t921-sin-letreros");
        }
    }
}
