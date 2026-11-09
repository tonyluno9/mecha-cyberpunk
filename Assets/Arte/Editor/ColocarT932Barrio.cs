using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-932 (playtest 9-nov): "barrio junto a la casa: casas sin terminar, huecos". Las caras lisas que se ven desde la calle de la casa (el edificio de las afueras, el costado de la casa y el
    /// de Edificio_33) se visten con el kit de pisos C20/C21 y cornisa C22, y la callejuela que deja ver lo de atrás se cierra con una reja PU03. Idempotente: rehace su raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT932Barrio.Colocar
    /// </summary>
    public static class ColocarT932Barrio
    {
        public const string Raiz = "T932_Barrio";
        private const float AltoPiso = 3f;
        private const float Salida = 0.02f;
        private static readonly string[] Pisos = { "C20b_PisoDosVentanas_cy", "C20a_PisoVentana_cy", "C21_PisoLiso_cy", "C20a_PisoVentana_cy" };

        [MenuItem("Mecha Cyberpunk/T-932/Vestir el barrio junto a la casa")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(Raiz));
            Transform raiz = new GameObject(Raiz).transform;
            int n = 0;
            // Edificio de las afueras: la cara que da a la callejuela (la de la calle ya tiene su kit).
            n += Muro(raiz, new Vector3(-68.2f, 0.2f, -87.5f), new Vector3(-57f, 0.2f, -87.5f), Vector3.forward, 5);
            // El costado de la casa hacia la callejuela.
            n += Muro(raiz, new Vector3(-68.2f, 0.2f, -84f), new Vector3(-57f, 0.2f, -84f), Vector3.back, 2);
            // El costado de Edificio_33 hacia el hueco del norte.
            n += Muro(raiz, new Vector3(-69f, 0.2f, -64f), new Vector3(-57f, 0.2f, -64f), Vector3.forward, 3);
            GameObject reja = KitS2c.Prefab("PU03_Reja") != null ? KitS2c.Pieza(raiz, "PU03_Reja", new Vector3(-57.1f, 0.2f, -85.75f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * EscalaReja(), true) : null;
            if (reja != null) { reja.name = "RejaCallejuela"; }
            ColocarS2.Informe.AppendLine("T-932: " + n + " piezas de fachada" + (reja == null ? "; FALTA PU03_Reja" : "; callejuela cerrada con reja"));
            ColocarS2.Guardar("t932-barrio");
        }

        private static float EscalaReja()
        {
            KitS2c.Medida m = KitS2c.Medir("PU03_Reja");
            return 3.2f / Mathf.Max(m.Caja.size.x, m.Caja.size.z);
        }

        /// <summary>Viste un muro recto (de <paramref name="a"/> a <paramref name="b"/>, cara hacia <paramref name="normal"/>) con <paramref name="pisos"/> filas y su cornisa. Devuelve las piezas puestas.</summary>
        private static int Muro(Transform raiz, Vector3 a, Vector3 b, Vector3 normal, int pisos)
        {
            Quaternion giro = Quaternion.LookRotation(normal);
            Vector3 dir = (b - a).normalized;
            float largo = Vector3.Distance(a, b);
            int n = 0;
            for (int p = 0; p < pisos; p++)
            {
                string codigo = Pisos[p % Pisos.Length];
                KitS2c.Medida m = KitS2c.Medir(codigo);
                float escala = AltoPiso / m.Caja.size.y;
                float ancho = m.Caja.size.x * escala;
                int cuantos = Mathf.Max(1, Mathf.RoundToInt(largo / ancho));
                float paso = largo / cuantos;
                for (int i = 0; i < cuantos; i++)
                {
                    string c = (i + p) % 2 == 0 ? codigo : Pisos[(p + 1) % Pisos.Length];
                    Vector3 pos = a + dir * (paso * (i + 0.5f)) + Vector3.up * (AltoPiso * p) + normal * Salida;
                    KitS2c.Pieza(raiz, c, pos, giro, new Vector3(paso / m.Caja.size.x, escala, escala), false).name = "Fachada";
                    n++;
                }
            }
            KitS2c.Medida cornisa = KitS2c.Medir("C22_Cornisa_cy");
            float escCornisa = 0.8f / cornisa.Caja.size.y;
            KitS2c.Pieza(raiz, "C22_Cornisa_cy", a + dir * (largo * 0.5f) + Vector3.up * (AltoPiso * pisos) + normal * Salida, giro, new Vector3(largo / cornisa.Caja.size.x, escCornisa, escCornisa), false).name = "Cornisa";
            return n + 1;
        }
    }
}
