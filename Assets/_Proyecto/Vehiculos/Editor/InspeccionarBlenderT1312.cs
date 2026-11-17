using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>T-1312: vuelca la jerarquía, las medidas y los materiales de los FBX nuevos de Blender a Logs/t1312-inspeccion.txt.</summary>
    public static class InspeccionarBlenderT1312
    {
        public static readonly string[] Codigos = { "V02", "V03", "V04", "V06", "V09", "V10", "C19" };

        [MenuItem("Mecha Cyberpunk/T-1312/Inspeccionar FBX")]
        public static void Inspeccionar()
        {
            AssetDatabase.Refresh();
            StringBuilder sb = new StringBuilder();
            foreach (string c in Codigos)
            {
                string ruta = "Assets/Arte/Vehiculos/Piezas/" + c + "/" + c + ".fbx";
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                sb.AppendLine("== " + c + (fbx == null ? " NO CARGA" : ""));
                if (fbx == null)
                {
                    continue;
                }
                Volcar(fbx.transform, fbx.transform, 0, sb);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1312-inspeccion.txt", sb.ToString());
        }

        private static void Volcar(Transform t, Transform raiz, int nivel, StringBuilder sb)
        {
            string linea = new string(' ', nivel * 2) + t.name + " pos " + t.localPosition.ToString("F3") + " rot " + t.localEulerAngles.ToString("F1") + " esc " + t.localScale.ToString("F3");
            MeshFilter f = t.GetComponent<MeshFilter>();
            if (f != null && f.sharedMesh != null)
            {
                Mesh m = f.sharedMesh;
                Bounds b = default;
                bool primero = true;
                Matrix4x4 aRaiz = raiz.worldToLocalMatrix * t.localToWorldMatrix;
                foreach (Vector3 v in m.vertices)
                {
                    Vector3 p = aRaiz.MultiplyPoint3x4(v);
                    if (primero) { b = new Bounds(p, Vector3.zero); primero = false; } else { b.Encapsulate(p); }
                }
                linea += " | tri " + (m.triangles.Length / 3) + " centro " + b.center.ToString("F3") + " tam " + b.size.ToString("F3") + " min.y " + b.min.y.ToString("F3");
                Renderer r = t.GetComponent<Renderer>();
                if (r != null)
                {
                    foreach (Material mat in r.sharedMaterials)
                    {
                        linea += " | mat " + (mat == null ? "null" : mat.name + " (" + mat.shader.name + ", tex " + (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null ? mat.GetTexture("_BaseMap").name : "ninguna") + ")");
                    }
                }
            }
            sb.AppendLine(linea);
            foreach (Transform h in t)
            {
                Volcar(h, raiz, nivel + 1, sb);
            }
        }
    }
}
