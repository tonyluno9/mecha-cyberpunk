using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-545 (plan de la noche 3-nov, paso 3): la pistola W01 de imageassets/modelos3d/armas/W01_Pistola (LEEME.md). Las animaciones se exportaron
    /// de los .blend a FBX con Blender 5.2 en línea de comandos (bake, escala 1, Y arriba): W01_Pistola_Disparo.fbx (10 cuadros a 24 fps) y
    /// W01_Pistola_Recarga.fbx (40 cuadros). Aquí: informe de la estructura (partes y materiales) para armar el prefab.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarT545.Informe
    /// </summary>
    public static class ImportarT545
    {
        public const string Carpeta = "Assets/Arte/Armas/W01_Pistola/";

        public static void Informe()
        {
            AssetDatabase.Refresh();
            StringBuilder sb = new StringBuilder();
            foreach (string f in new[] { "W01_Pistola.fbx", "W01_Pistola_Disparo.fbx", "W01_Pistola_Recarga.fbx" })
            {
                GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + f);
                sb.AppendLine("== " + f);
                if (g == null)
                {
                    continue;
                }
                foreach (Transform t in g.GetComponentsInChildren<Transform>())
                {
                    Renderer r = t.GetComponent<Renderer>();
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    sb.AppendLine("  " + t.name + " lpos=" + t.localPosition.ToString("F3") + " lrot=" + t.localEulerAngles.ToString("F0") + " esc=" + t.localScale.ToString("F3")
                        + (mf != null && mf.sharedMesh != null ? " malla=" + mf.sharedMesh.name + " sub=" + mf.sharedMesh.subMeshCount + " b=" + mf.sharedMesh.bounds.size.ToString("F3") : "")
                        + (r != null ? " mats=" + string.Join(",", r.sharedMaterials.Select(m => m != null ? m.name : "null")) : ""));
                }
                foreach (AnimationClip c in AssetDatabase.LoadAllAssetsAtPath(Carpeta + f).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
                {
                    sb.AppendLine("  CLIP " + c.name + " dur=" + c.length.ToString("F2") + " curvas=" + AnimationUtility.GetCurveBindings(c).Length + " ej=" + string.Join(";", AnimationUtility.GetCurveBindings(c).Select(b => b.path + "." + b.propertyName).Distinct().Take(8)));
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t545-informe.txt", sb.ToString());
        }
    }
}
