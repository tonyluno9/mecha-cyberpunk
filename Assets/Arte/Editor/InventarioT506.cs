using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-506: inventario de greybox de Ciudad_Prologo. Un renderer cuenta como greybox si es una primitiva de Unity (Cube, Sphere, Capsule, Cylinder, Plane, Quad)
    /// o si su material no tiene ninguna textura y no es un efecto (emisivo, transparente). Excepciones invisibles: sin renderer, colliders, triggers.
    /// </summary>
    public static class InventarioT506
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private static readonly string[] MallasPrimitivas = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

        public struct Hallazgo
        {
            public Renderer renderer;
            public string ruta;
            public string malla;
            public string material;
            public Vector3 centro;
            public Vector3 tamano;
            public string motivo;
        }

        public static string Ruta(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent) { sb.Insert(0, p.name + "/"); }
            return sb.ToString();
        }

        public static bool TieneTextura(Material m)
        {
            if (m == null) { return false; }
            Shader s = m.shader;
            for (int i = 0; i < s.GetPropertyCount(); i++)
            {
                if (s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture && m.GetTexture(s.GetPropertyName(i)) != null) { return true; }
            }
            return false;
        }

        public static bool EsEfecto(Material m)
        {
            if (m == null) { return false; }
            if (m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.3f) { return true; }
            if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) { return true; }
            string sh = m.shader.name;
            return sh.Contains("Unlit") || sh.Contains("Particles") || sh.Contains("Sprites") || sh.Contains("Skybox") || sh.StartsWith("MechaCyberpunk/") || sh.Contains("TextMeshPro") || sh.Contains("UI/");
        }

        public static List<Hallazgo> Buscar()
        {
            var lista = new List<Hallazgo>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!(r is MeshRenderer) || !r.enabled) { continue; }
                MeshFilter mf = r.GetComponent<MeshFilter>();
                string malla = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "(sin malla)";
                bool prim = MallasPrimitivas.Contains(malla);
                foreach (Material m in r.sharedMaterials)
                {
                    string motivo = null;
                    if (m == null) { motivo = "sin material"; }
                    else if (!TieneTextura(m) && !EsEfecto(m)) { motivo = prim ? "primitiva sin textura" : "material sin textura"; }
                    else if (prim && !EsEfecto(m) && !m.name.StartsWith("T5")) { motivo = "primitiva"; }
                    if (motivo == null) { continue; }
                    lista.Add(new Hallazgo { renderer = r, ruta = Ruta(r.transform), malla = malla, material = m != null ? m.name : "-", centro = r.bounds.center, tamano = r.bounds.size, motivo = motivo });
                    break;
                }
            }
            return lista;
        }

        public static void Ejecutar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var sb = new StringBuilder();
            var lista = Buscar();
            sb.AppendLine("total " + lista.Count);
            foreach (Hallazgo h in lista.OrderBy(x => x.ruta))
            {
                sb.AppendLine(string.Format("{0} | {1} | {2} | {3} | c=({4:F0},{5:F0},{6:F0}) t=({7:F1},{8:F1},{9:F1})", h.motivo, h.ruta, h.malla, h.material, h.centro.x, h.centro.y, h.centro.z, h.tamano.x, h.tamano.y, h.tamano.z));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/inventario-t506.txt", sb.ToString());
        }
    }
}
