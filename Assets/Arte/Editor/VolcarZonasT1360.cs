using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1360: vuelca la jerarquía de los prefabs de zona del Cerro y del Puerto (Assets/Arte/Mundo/Zonas/) para planear el barrido:
    /// los primeros niveles con cuántos hijos, componentes y límites, y cuántos objetos hay de cada nombre (sin números).
    /// Escribe Logs/t1360-jerarquia-&lt;zona&gt;.txt. No cambia nada. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.VolcarZonasT1360.Volcar
    /// </summary>
    public static class VolcarZonasT1360
    {
        public static readonly string[] Prefabs = { "Assets/Arte/Mundo/Zonas/T1355_Cerro.prefab", "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab" };
        private const int Profundidad = 4;

        public static void Volcar()
        {
            Directory.CreateDirectory("Logs");
            foreach (string ruta in Prefabs)
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    StringBuilder s = new StringBuilder();
                    s.AppendLine(ruta + "  posición " + raiz.transform.position);
                    Escribir(raiz.transform, 0, s);
                    s.AppendLine();
                    s.AppendLine("== Nombres (sin números) ==");
                    Dictionary<string, int> cuenta = new Dictionary<string, int>();
                    foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                    {
                        string n = Regex.Replace(t.name, @"[_ ]?\(?\d+\)?$", "");
                        cuenta[n] = cuenta.TryGetValue(n, out int c) ? c + 1 : 1;
                    }
                    foreach (var kv in cuenta.OrderByDescending(k => k.Value).Take(150))
                    {
                        s.AppendLine(kv.Value + "\t" + kv.Key);
                    }
                    s.AppendLine();
                    s.AppendLine("Renderers " + raiz.GetComponentsInChildren<Renderer>(true).Length + ", colliders " + raiz.GetComponentsInChildren<Collider>(true).Length
                        + ", luces " + raiz.GetComponentsInChildren<Light>(true).Length + ", triángulos " + raiz.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => (long)f.sharedMesh.triangles.Length / 3));
                    File.WriteAllText("Logs/t1360-jerarquia-" + raiz.name + ".txt", s.ToString());
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
        }

        private static void Escribir(Transform t, int nivel, StringBuilder s)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.zero);
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            string comps = string.Join(",", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
            s.AppendLine(new string(' ', nivel * 2) + t.name + "  [" + t.childCount + " hijos] {" + comps + "} centro " + b.center.ToString("F1") + " tam " + b.size.ToString("F1"));
            if (nivel >= Profundidad)
            {
                return;
            }
            // Muchos hermanos: solo los primeros 6 y el resumen.
            int i = 0;
            foreach (Transform h in t)
            {
                if (i++ >= 6)
                {
                    s.AppendLine(new string(' ', (nivel + 1) * 2) + "… " + (t.childCount - 6) + " más");
                    break;
                }
                Escribir(h, nivel + 1, s);
            }
        }
    }
}
