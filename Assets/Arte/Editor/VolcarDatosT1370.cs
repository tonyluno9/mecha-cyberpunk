using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>T-1370: vuelca los nombres de las mallas de T1355_Cerro_Mallas.asset y las patas (vértices bajos) de la grúa PU01 en Logs/t1370-datos.txt. No cambia nada.</summary>
    public static class VolcarDatosT1370
    {
        public static void Volcar()
        {
            StringBuilder s = new StringBuilder();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath("Assets/_Proyecto/Mundo/Datos/Zonas/T1355_Cerro_Mallas.asset"))
            {
                Mesh m = o as Mesh;
                s.AppendLine("malla " + o.name + (m != null ? " vértices " + m.vertexCount + " bounds " + m.bounds : " (" + o.GetType().Name + ")"));
            }
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Zonas/PU/PU01_Grua.prefab");
            if (g != null)
            {
                GameObject i = Object.Instantiate(g);
                i.transform.position = Vector3.zero;
                i.transform.rotation = Quaternion.identity;
                Bounds b = ArteT250.Limites(i);
                s.AppendLine("grúa límites " + b.min + " .. " + b.max);
                var bajos = i.GetComponentsInChildren<MeshFilter>().SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).Where(p => p.y < b.min.y + 4f).ToList();
                foreach (var grupo in bajos.GroupBy(p => (Mathf.RoundToInt(p.x / 3f), Mathf.RoundToInt(p.z / 3f))).OrderBy(gr => gr.Key.Item2).ThenBy(gr => gr.Key.Item1))
                {
                    s.AppendLine("  pie cerca de x " + (grupo.Key.Item1 * 3) + " z " + (grupo.Key.Item2 * 3) + ": " + grupo.Count() + " vértices");
                }
                Bounds alto = new Bounds(); bool primero = true;
                foreach (var p in i.GetComponentsInChildren<MeshFilter>().SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).Where(p => p.y > b.min.y + 4f && p.y < b.min.y + 8f))
                {
                    if (primero) { alto = new Bounds(p, Vector3.zero); primero = false; } else { alto.Encapsulate(p); }
                }
                s.AppendLine("  entre 4 y 8 m: " + alto.min + " .. " + alto.max);
                Object.DestroyImmediate(i);
            }
            File.WriteAllText("Logs/t1370-datos.txt", s.ToString());
        }
    }
}
