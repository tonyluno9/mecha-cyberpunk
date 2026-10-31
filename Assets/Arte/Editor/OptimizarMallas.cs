using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-425: activa "Generate Mesh LODs" (Unity 6.2+) en los FBX de Assets/Arte cuya malla más grande pasa de <see cref="UmbralTriangulos"/>.
    /// El motor elige solo la malla simplificada según el tamaño en pantalla (QualitySettings.meshLodThreshold); de cerca no cambia nada.
    /// No toca las mallas con LODGroup propio (kit de pisos). Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.OptimizarMallas.Aplicar
    /// </summary>
    public static class OptimizarMallas
    {
        public const int UmbralTriangulos = 3000;
        private static readonly string[] ConLodGroup = { "C20a_", "C20b_", "C20d_", "C21_", "C22_", "C23_" };

        [MenuItem("Mecha Cyberpunk/T-425/Generar Mesh LOD en modelos pesados")]
        public static void Aplicar()
        {
            StringBuilder informe = new StringBuilder();
            int cambiados = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Arte" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!ruta.EndsWith(".fbx") || SaltarPorLodGroup(ruta))
                {
                    continue;
                }
                int maximo = 0;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                {
                    if (o is Mesh m)
                    {
                        int tris = 0;
                        for (int i = 0; i < m.subMeshCount; i++)
                        {
                            tris += (int)(m.GetIndexCount(i) / 3);
                        }
                        maximo = Mathf.Max(maximo, tris);
                    }
                }
                if (maximo < UmbralTriangulos)
                {
                    continue;
                }
                ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(ruta);
                if (importador == null || importador.generateMeshLods)
                {
                    continue;
                }
                importador.generateMeshLods = true;
                importador.SaveAndReimport();
                cambiados++;
                informe.AppendLine(ruta + " tris=" + maximo);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/optimizar-mallas.txt", "cambiados=" + cambiados + "\n" + informe);
            Debug.Log("[Arte] OptimizarMallas: " + cambiados + " modelos con Mesh LOD.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static bool SaltarPorLodGroup(string ruta)
        {
            string nombre = Path.GetFileName(ruta);
            foreach (string prefijo in ConLodGroup)
            {
                if (nombre.StartsWith(prefijo))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
