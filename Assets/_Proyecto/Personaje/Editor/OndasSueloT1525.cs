using System.IO;
using MechaCyberpunk.Personaje.Jugador;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1525: crea el material de las ondas de pisada sobre piso mojado (shader OndaPisadaSuelo) en Personaje/Resources, para que el shader entre en las builds
    /// y AnillosSuelo lo cargue por nombre. Se puede repetir. Batchmode, con Unity cerrado:
    ///   Unity -batchmode -quit -executeMethod MechaCyberpunk.Personaje.Editor.OndasSueloT1525.Diagnostico
    ///   Unity -batchmode -quit -executeMethod MechaCyberpunk.Personaje.Editor.OndasSueloT1525.Aplicar
    /// </summary>
    public static class OndasSueloT1525
    {
        public const string RutaMaterial = "Assets/_Proyecto/Personaje/Resources/" + AnillosSuelo.NombreMaterial + ".mat";

        [MenuItem("Mecha Cyberpunk/T-1525/Diagnostico ondas de piso mojado")]
        public static void Diagnostico()
        {
            Shader s = Shader.Find(AnillosSuelo.NombreShader);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            Debug.Log("[T-1525] shader " + (s != null ? "ok" : "FALTA") + ", material " + (m != null ? "ok" : "falta (corre Aplicar)")
                + ", shader del charco " + (Shader.Find("MechaCyberpunk/Mundo/Charco") != null ? "ok" : "FALTA"));
        }

        [MenuItem("Mecha Cyberpunk/T-1525/Aplicar ondas de piso mojado")]
        public static void Aplicar()
        {
            Shader s = Shader.Find(AnillosSuelo.NombreShader);
            if (s == null)
            {
                throw new System.Exception("[T-1525] no existe el shader " + AnillosSuelo.NombreShader);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaMaterial));
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, RutaMaterial);
            }
            m.shader = s;
            m.enableInstancing = false;
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1525] material de ondas de piso listo: " + RutaMaterial);
        }
    }
}
