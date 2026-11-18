using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1362: cuenta, por zona y por tipo de prop, cuántas piezas hay, sus triángulos (LOD0 y LOD más bajo de Mesh LOD),
    /// materiales, LODGroup y banderas estáticas. Escribe Logs/t1362-conteo.txt. Se llama con -executeMethod
    /// MechaCyberpunk.Arte.Editor.MedirZonasT1362.Contar. Lee los prefabs sin instanciarlos.
    /// </summary>
    public static class MedirZonasT1362
    {
        private const string CarpetaZonas = "Assets/Arte/Mundo/Zonas";
        private const string RutaSalida = "Logs/t1362-conteo.txt";

        private class Tipo
        {
            public int props;
            public long triLod0;
            public long triLodBajo;
            public bool hayLodBajo;
            public int renderers;
            public readonly HashSet<Material> materiales = new HashSet<Material>();
            public bool lodGroup;
            public int estaticos;
            public int occluders;
            public int occludees;
        }

        public static void Contar()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1362 conteo por zona y tipo de prop (prefabs de " + CarpetaZonas + ")");
            sb.AppendLine("Props = objetos hijos de la carpeta de la zona (Casas, Escaleras...); un prop agrupa sus renderers.");
            sb.AppendLine("tri/prop = triángulos LOD0 por prop (promedio); tri LODbajo = con el LOD más bajo de Mesh LOD de cada malla (= LOD0 si no tiene).");
            sb.AppendLine("Los datos de oclusión (Occlusion Culling baked) viven en la escena, no en el prefab: no se pueden saber desde aquí. Revisar la escena Ciudad (Window > Rendering > Occlusion Culling).");
            sb.AppendLine("static/occluder/occludee = cuántos renderers del tipo llevan la bandera.");
            sb.AppendLine();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { CarpetaZonas });
            foreach (string guid in guids)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null)
                {
                    continue;
                }
                ContarZona(prefab, sb);
            }
            string completa = Path.GetFullPath(RutaSalida);
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllText(completa, sb.ToString());
            Debug.Log("[Arte] T-1362: conteo escrito en " + RutaSalida);
        }

        private static void ContarZona(GameObject prefab, StringBuilder sb)
        {
            Dictionary<string, Tipo> tipos = new Dictionary<string, Tipo>();
            Dictionary<Transform, bool> propsVistos = new Dictionary<Transform, bool>();
            Transform raiz = prefab.transform;
            long totalLod0 = 0;
            long totalBajo = 0;
            int totalRenderers = 0;
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }
                Transform prop = RaizDeProp(raiz, r.transform);
                string nombre = NombreSinNumeros(prop.name);
                if (!tipos.TryGetValue(nombre, out Tipo t))
                {
                    t = new Tipo();
                    tipos[nombre] = t;
                }
                if (!propsVistos.ContainsKey(prop))
                {
                    propsVistos[prop] = true;
                    t.props++;
                    if (prop.GetComponentInChildren<LODGroup>(true) != null || prop.GetComponentInParent<LODGroup>() != null)
                    {
                        t.lodGroup = true;
                    }
                }
                long lod0 = Triangulos(mf.sharedMesh, 0, out _);
                long bajo = Triangulos(mf.sharedMesh, -1, out bool hay);
                t.triLod0 += lod0;
                t.triLodBajo += bajo;
                t.hayLodBajo |= hay;
                t.renderers++;
                totalLod0 += lod0;
                totalBajo += bajo;
                totalRenderers++;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null)
                    {
                        t.materiales.Add(m);
                    }
                }
                StaticEditorFlags f = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if (f != 0)
                {
                    t.estaticos++;
                }
                if ((f & StaticEditorFlags.OccluderStatic) != 0)
                {
                    t.occluders++;
                }
                if ((f & StaticEditorFlags.OccludeeStatic) != 0)
                {
                    t.occludees++;
                }
            }
            sb.AppendLine("== " + prefab.name + ": " + totalRenderers + " renderers, " + totalLod0 + " triángulos LOD0, " + totalBajo + " con LOD más bajo ==");
            sb.AppendLine("tipo\tprops\trenderers\ttri/prop LOD0\ttri/prop LODbajo\ttri total LOD0\ttri total LODbajo\tmateriales\tLODGroup\tstatic\toccluder\toccludee\tMeshLOD");
            List<KeyValuePair<string, Tipo>> lista = new List<KeyValuePair<string, Tipo>>(tipos);
            lista.Sort((a, b) => b.Value.triLod0.CompareTo(a.Value.triLod0));
            foreach (KeyValuePair<string, Tipo> kv in lista)
            {
                Tipo t = kv.Value;
                int p = Mathf.Max(1, t.props);
                sb.AppendLine(kv.Key + "\t" + t.props + "\t" + t.renderers + "\t" + (t.triLod0 / p) + "\t" + (t.triLodBajo / p) + "\t" + t.triLod0 + "\t" + t.triLodBajo
                    + "\t" + t.materiales.Count + "\t" + (t.lodGroup ? "si" : "no") + "\t" + t.estaticos + "/" + t.renderers + "\t" + t.occluders + "\t" + t.occludees + "\t" + (t.hayLodBajo ? "si" : "no"));
            }
            sb.AppendLine();
        }

        /// <summary>Triángulos de la malla en el LOD pedido; lod -1 = el más bajo. Usa reflexión porque la API de Mesh LOD es nueva.</summary>
        private static long Triangulos(Mesh malla, int lod, out bool tieneLods)
        {
            tieneLods = false;
            int cantidad = 1;
            PropertyInfo prop = typeof(Mesh).GetProperty("lodCount", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo metodo = typeof(Mesh).GetMethod("GetLodSubMesh", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), typeof(int) }, null);
            if (prop != null && metodo != null)
            {
                cantidad = (int)prop.GetValue(malla);
            }
            if (cantidad <= 1 || metodo == null)
            {
                return TriangulosLod0(malla);
            }
            tieneLods = true;
            int elegido = lod < 0 ? cantidad - 1 : Mathf.Min(lod, cantidad - 1);
            long suma = 0;
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                SubMeshDescriptor d = (SubMeshDescriptor)metodo.Invoke(malla, new object[] { s, elegido });
                suma += d.indexCount / 3;
            }
            return suma;
        }

        private static long TriangulosLod0(Mesh malla)
        {
            long suma = 0;
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                suma += malla.GetIndexCount(s) / 3;
            }
            return suma;
        }

        private static Transform RaizDeProp(Transform raiz, Transform t)
        {
            Transform actual = t;
            while (actual.parent != null && actual.parent != raiz && actual.parent.parent != raiz)
            {
                actual = actual.parent;
            }
            return actual;
        }

        private static string NombreSinNumeros(string n)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in n.Replace("(Clone)", ""))
            {
                if (!char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString().Trim('_', ' ');
        }
    }
}
