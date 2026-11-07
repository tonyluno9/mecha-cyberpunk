using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-861 (playtest 6-nov tarde): las calles tienen nombre y cada zona se reconoce. En cada cruce va un letrero de bandera del kit (C15b) por
    /// calle, colgado del poste de luz más cercano a la esquina, con el nombre de la calle en el color de su zona (el de la luz de la zona de
    /// T-512): turquesa en Neón Alto, naranja en el tianguis y el eje, magenta en Suburbios y el callejón, verde en el mercado. Idempotente
    /// (rehace su raíz S2t_Calles).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CallesS2t.Colocar
    /// </summary>
    public static class CallesS2t
    {
        public const string NombreRaiz = "S2t_Calles";
        private const string RutaLetrero = "Assets/Arte/Calle/Prefabs/C15b_LetreroBandera.prefab";
        public static readonly Color Turquesa = new Color(0.25f, 0.95f, 0.95f);
        public static readonly Color Naranja = new Color(1f, 0.6f, 0.2f);
        public static readonly Color Magenta = new Color(1f, 0.3f, 0.8f);
        public static readonly Color Verde = new Color(0.45f, 1f, 0.45f);

        /// <summary>Calles: nombre, si corre de este a oeste (true) o de norte a sur, su eje (z o x) y el color de su zona.</summary>
        public static readonly (string nombre, bool esteOeste, float eje, Color color)[] Calles =
        {
            ("EJE CENTRAL", true, 0f, Naranja),
            ("CALLE DE LOS CABLES", true, -86f, Magenta),
            ("CALLE DEL MERCADO", true, 85f, Verde),
            ("CALLE OBRERA", true, -122f, Magenta),
            ("AV. COYOTE", false, -47f, Naranja),
            ("AV. NEÓN", false, 47f, Turquesa),
            ("PERIFÉRICO PONIENTE", false, -133f, Magenta),
            ("PERIFÉRICO ORIENTE", false, 133f, Turquesa),
        };

        /// <summary>Cruces (x, z) donde van los letreros.</summary>
        public static readonly Vector2[] Cruces =
        {
            new Vector2(-47f, 0f), new Vector2(47f, 0f), new Vector2(-47f, -86f), new Vector2(47f, -86f),
            new Vector2(-47f, 85f), new Vector2(47f, 85f), new Vector2(-133f, 0f), new Vector2(133f, 0f), new Vector2(-47f, -122f), new Vector2(47f, -122f),
        };

        private const float BuscarPoste = 16f;
        private const float AltoLetrero = 3.6f;
        private const float SeparacionLetreros = 0.55f;
        private const float AnchoLetrero = 1.5f;

        [MenuItem("Mecha Cyberpunk/S2t/9 Nombres de calles")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Transform[] postes = PostesS2t.Postes();
            int puestos = 0;
            foreach (Vector2 cruce in Cruces)
            {
                Transform poste = postes.OrderBy(p => new Vector2(p.position.x - cruce.x, p.position.z - cruce.y).sqrMagnitude)
                    .FirstOrDefault(p => new Vector2(p.position.x - cruce.x, p.position.z - cruce.y).magnitude < BuscarPoste);
                if (poste == null)
                {
                    ColocarS2.Informe.AppendLine("cruce " + cruce + ": sin poste cerca, sin letrero");
                    continue;
                }
                var calles = Calles.Where(c => Mathf.Abs((c.esteOeste ? cruce.y : cruce.x) - c.eje) < 1f).ToArray();
                Bounds bp = PostesS2t.ModeloVisible(poste).GetComponent<Renderer>().bounds;
                Vector3 tubo = new Vector3(poste.position.x, bp.min.y, poste.position.z);
                for (int i = 0; i < calles.Length; i++)
                {
                    var c = calles[i];
                    // El letrero corre a lo largo de su calle y sale del poste hacia el cruce.
                    Vector3 largo = c.esteOeste ? Vector3.right : Vector3.forward;
                    Vector3 haciaCruce = new Vector3(cruce.x - tubo.x, 0f, cruce.y - tubo.z);
                    Vector3 sale = Vector3.Project(haciaCruce, largo).normalized;
                    if (sale.sqrMagnitude < 0.5f)
                    {
                        sale = largo;
                    }
                    float y = tubo.y + AltoLetrero + i * SeparacionLetreros;
                    Vector3 centro = tubo + sale * (AnchoLetrero * 0.5f + 0.15f);
                    GameObject l = ColocarS2Interiores.Modelo(raiz, RutaLetrero, new Vector3(centro.x, 0f, centro.z), 0f, y, 1f, sinCollider: true);
                    if (l == null)
                    {
                        ColocarS2.Informe.AppendLine("FALTA " + RutaLetrero);
                        ColocarS2.Guardar("s2t-calles");
                        return;
                    }
                    l.name = "Letrero_" + c.nombre;
                    // Lo más largo del modelo va a lo largo de la calle.
                    Bounds b = ArteT250.Limites(l);
                    if ((b.size.x > b.size.z) != c.esteOeste)
                    {
                        l.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * l.transform.rotation;
                    }
                    b = ArteT250.Limites(l);
                    l.transform.localScale *= AnchoLetrero / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
                    b = ArteT250.Limites(l);
                    l.transform.position += new Vector3(centro.x - b.center.x, y - b.min.y, centro.z - b.center.z);
                    b = ArteT250.Limites(l);
                    // El nombre por las dos caras.
                    Vector3 cara = c.esteOeste ? Vector3.forward : Vector3.right;
                    float grosor = Mathf.Abs(Vector3.Dot(b.extents, cara)) + 0.02f;
                    foreach (int lado in new[] { 1, -1 })
                    {
                        ColocarS2t.TextoMundo(raiz, "Nombre_" + c.nombre, c.nombre, b.center + cara * lado * grosor, new Vector2(AnchoLetrero * 0.9f, b.size.y * 0.6f), cara * lado, c.color);
                    }
                    puestos++;
                }
            }
            ColocarS2.Informe.AppendLine("T-861: " + puestos + " letreros con nombre de calle en " + Cruces.Length + " cruces");
            ColocarS2.Guardar("s2t-calles");
        }

        public static void Fotos()
        {
            ColocarS2.Abrir();
            Transform raiz = ColocarS2.Buscar(NombreRaiz);
            int n = 0;
            foreach (Transform l in raiz.Cast<Transform>().Where(t => t.name.StartsWith("Letrero_")).Take(16).Where((t, k) => k % 4 == 0))
            {
                Bounds b = ArteT250.Limites(l.gameObject);
                // Desde la calle, de frente a una de sus caras, a 7 m y a la altura de los ojos.
                Vector3 cara = b.size.x > b.size.z ? Vector3.forward : Vector3.right;
                Vector3 desde = new Vector3(b.center.x, 1.7f, b.center.z) + cara * 7f;
                ColocarS2t.FotoHora(n % 2 == 0 ? 13f : 22f, desde, b.center, ColocarS2t.CarpetaCapturas + "s2t-calles-" + n + ".png", 50f);
                n++;
            }
        }
    }
}
