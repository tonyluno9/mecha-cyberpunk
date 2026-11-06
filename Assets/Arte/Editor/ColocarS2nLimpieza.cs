using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-803 (playtest del 6-nov, sesión 2): limpieza de Ciudad_Prologo. Quita la bolsa de basura del centro del cuarto y para la mochila (el modelo
    /// I05 ya viene derecho: se recarga en la pared, en el piso); quita el papel picado de todos lados (T504_Mercado, casino y entrada de armas de
    /// T-546, consultorio y taller del mecánico de S2) y los grafitis-logo (cruz, calavera, rayo...) pegados en las fachadas de las casas
    /// (Barrio/Grafitis se apaga en esta escena; Barrio.prefab no cambia). Los generadores ya no crean papel picado, así que volver a correrlos no
    /// lo regresa. Idempotente. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nLimpieza.Todo
    /// </summary>
    public static class ColocarS2nLimpieza
    {
        /// <summary>Materiales del papel picado (T-504, T-546 y S2).</summary>
        public static readonly string[] MaterialesPapel = { "Papel_0", "Papel_1", "Papel_2", "Papel_3", "Papel_4", "Papel_5", "Hilo_Papel", "T546_PapelPicado", "S2_PapelPicado" };
        private const string RutaCuarto = "T550_Cuartos/Cuarto_Habitacion";
        private const float AltoMochila = 0.5f;
        private const float SeparacionPared = 0.03f;

        [MenuItem("Mecha Cyberpunk/S2n/1 Limpieza (basura del cuarto, mochila, papel picado, grafitis-logo)")]
        public static void Limpiar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            HashSet<Object> referidos = ColocarS2.Referidos();
            Cuarto(referidos);
            int papel = QuitarPapelPicado(referidos);
            ColocarS2.Informe.AppendLine("Papel picado quitado: " + papel);
            Transform grafitis = ColocarS2.Buscar("Barrio/Grafitis");
            if (grafitis != null)
            {
                ColocarS2.Informe.AppendLine("Grafitis-logo de las fachadas: " + grafitis.childCount);
                ColocarS2.Quitar(grafitis.gameObject, referidos);
            }
            ColocarS2.Guardar("s2n-limpieza");
        }

        /// <summary>Capturas antes (solo si no existen), limpieza, capturas después y tiras antes | después en docs/capturas/s2n-limpieza-*.png.</summary>
        public static void Todo()
        {
            ColocarS2.Abrir();
            if (!File.Exists("Logs/s2n/antes-cuarto.png"))
            {
                Capturas("antes");
            }
            Limpiar();
            Capturas("despues");
            foreach ((string nombre, Vector3 _, Vector3 _) in Tomas())
            {
                string antes = "Logs/s2n/antes-" + nombre + ".png";
                if (File.Exists(antes))
                {
                    ColocarS2.LadoALado(antes, "Logs/s2n/despues-" + nombre + ".png", "docs/capturas/s2n-limpieza-" + nombre + ".png");
                }
            }
        }

        // ---------------------------------------------------------------- Cuarto

        private static void Cuarto(HashSet<Object> referidos)
        {
            Transform r = ColocarS2.Buscar(RutaCuarto);
            if (r == null)
            {
                return;
            }
            // La bolsa de basura (N06) en el centro del piso.
            foreach (Transform t in r.GetComponentsInChildren<Transform>(true).Where(t => t.name == "N06").ToList())
            {
                ColocarS2.Quitar(t.gameObject, referidos);
            }
            Transform mochila = r.Find("I05");
            if (mochila != null)
            {
                PararMochila(r, mochila);
                ColocarS2.Informe.AppendLine("Mochila derecha: " + ArteT250.Limites(mochila.gameObject).size.ToString("F2"));
            }
        }

        /// <summary>
        /// La mochila I05 viene derecha en el modelo (su alto queda en Y): solo se gira en Y y se apoya en el piso, recargada en la pared izquierda
        /// del cuarto (x local -1.5), entre la puerta y la cama.
        /// </summary>
        public static void PararMochila(Transform cuarto, Transform mochila)
        {
            mochila.localScale = Vector3.one;
            mochila.rotation = cuarto.rotation * Quaternion.Euler(0f, 90f, 0f); // de lado hacia la pared, derecha
            Bounds b = ArteT250.Limites(mochila.gameObject);
            float factor = AltoMochila / Mathf.Max(b.size.y, 0.01f);
            mochila.localScale = Vector3.one * factor;
            b = ArteT250.Limites(mochila.gameObject);
            // Fondo de la mochila en planta, medido en los ejes del cuarto.
            Vector3 eje = cuarto.right;
            float fondo = Mathf.Abs(eje.x) * b.size.x + Mathf.Abs(eje.z) * b.size.z;
            Vector3 centro = cuarto.TransformPoint(new Vector3(-1.5f + fondo * 0.5f + SeparacionPared, 0f, -0.9f));
            mochila.position += new Vector3(centro.x - b.center.x, cuarto.position.y - b.min.y, centro.z - b.center.z);
        }

        // ---------------------------------------------------------------- Papel picado

        /// <summary>Quita todo el papel picado de la escena: grupos y tiras llamados PapelPicado o Bandera y lo que use sus materiales.</summary>
        public static int QuitarPapelPicado(HashSet<Object> referidos)
        {
            int n = 0;
            foreach (Transform t in ColocarS2.Todos().Where(EsPapelPorNombre).ToList())
            {
                if (t != null && t.gameObject.activeSelf)
                {
                    n += ColocarS2.Quitar(t.gameObject, referidos);
                }
            }
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(EsPapelPorMaterial).ToList())
            {
                if (r != null && r.gameObject.activeSelf)
                {
                    n += ColocarS2.Quitar(r.gameObject, referidos);
                }
            }
            return n;
        }

        public static bool EsPapelPorNombre(Transform t)
        {
            return t.name == "PapelPicado" || t.name == "Bandera" || (t.name.StartsWith("Hilo_") && t.GetComponent<LineRenderer>() != null && EsPapelPorMaterial(t.GetComponent<LineRenderer>()));
        }

        public static bool EsPapelPorMaterial(Renderer r)
        {
            return r.sharedMaterials.Any(m => m != null && MaterialesPapel.Contains(m.name));
        }

        // ---------------------------------------------------------------- Capturas

        private static IEnumerable<(string nombre, Vector3 desde, Vector3 hacia)> Tomas()
        {
            Transform cuarto = ColocarS2.Buscar(RutaCuarto);
            if (cuarto != null)
            {
                yield return ("cuarto", cuarto.TransformPoint(new Vector3(0.9f, 1.65f, -1.8f)), cuarto.TransformPoint(new Vector3(-0.9f, 0.5f, 0.2f)));
            }
            yield return ("mercado", new Vector3(-22f, 1.7f, 12f), new Vector3(-22f, 5f, 26f));
            yield return ("casino", new Vector3(72f, 1.7f, 36f), new Vector3(72f, 4f, 50f));
            yield return ("entrada-armas", new Vector3(-31.3f, 1.7f, 21f), new Vector3(-31.3f, 3.4f, 28f));
            yield return ("consultorio", new Vector3(-100f, 1.7f, -10.8f), new Vector3(-100f, 3f, -14f));
            yield return ("casas", new Vector3(-51f, 2.2f, -67f), new Vector3(-57f, 2.2f, -67f));
        }

        public static void Capturas(string sufijo)
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("Logs/s2n");
            foreach ((string nombre, Vector3 desde, Vector3 hacia) in Tomas())
            {
                ColocarS2.Foto(desde, hacia, "Logs/s2n/" + sufijo + "-" + nombre + ".png");
            }
        }
    }
}
