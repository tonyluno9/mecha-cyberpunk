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
    /// T-425: lista qué mallas de Ciudad_Prologo pesan más (copias x triángulos), con sombras y LOD, para decidir dónde bajar el costo.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.AnalisisTriangulos.Analizar. Escribe Logs/analisis-triangulos.txt.
    /// </summary>
    public static class AnalisisTriangulos
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        [MenuItem("Mecha Cyberpunk/T-425/Analizar triángulos de la ciudad")]
        public static void Analizar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var filas = new Dictionary<string, (int copias, int tris, bool sombra, bool lod, string ruta)>();
            long total = 0;
            int renderers = 0;
            int luces = 0;
            int lucesSombra = 0;
            int animadores = 0;
            int skinned = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                renderers++;
                Mesh malla = null;
                if (r is MeshRenderer mr)
                {
                    malla = mr.GetComponent<MeshFilter>() != null ? mr.GetComponent<MeshFilter>().sharedMesh : null;
                }
                else if (r is SkinnedMeshRenderer sk)
                {
                    malla = sk.sharedMesh;
                    skinned++;
                }
                if (malla == null)
                {
                    continue;
                }
                int tris = 0;
                for (int i = 0; i < malla.subMeshCount; i++)
                {
                    tris += (int)(malla.GetIndexCount(i) / 3);
                }
                total += tris;
                string clave = malla.name;
                bool lod = r.GetComponentInParent<LODGroup>() != null;
                filas.TryGetValue(clave, out var f);
                filas[clave] = (f.copias + 1, tris, r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off, lod, f.ruta ?? (AssetDatabase.GetAssetPath(malla) + " @" + r.transform.position.ToString("F0")));
            }
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                luces++;
                if (l.shadows != LightShadows.None)
                {
                    lucesSombra++;
                }
            }
            animadores = Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("renderers=" + renderers + " skinned=" + skinned + " animadores=" + animadores + " luces=" + luces + " lucesConSombra=" + lucesSombra + " triangulosTotales=" + total);
            foreach (var par in filas.OrderByDescending(p => (long)p.Value.copias * p.Value.tris).Take(40))
            {
                sb.AppendLine(par.Key + " copias=" + par.Value.copias + " tris=" + par.Value.tris + " total=" + (long)par.Value.copias * par.Value.tris
                    + " sombra=" + par.Value.sombra + " lod=" + par.Value.lod + " " + par.Value.ruta);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/analisis-triangulos.txt", sb.ToString());
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
