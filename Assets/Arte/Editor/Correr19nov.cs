using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Noche del 19-nov: corre en una sola sesión de Unity los pasos de las tareas T-1382..T-1389, cada uno aislado con try/catch.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.Correr19nov.Correr -pasos Tipo.Metodo,Tipo.Metodo...
    /// Los tipos se buscan por nombre corto en todos los ensamblados (no hace falta referenciar sus asmdef).
    /// Escribe Logs/correr19nov.txt.
    /// </summary>
    public static class Correr19nov
    {
        public static void Correr()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-pasos");
            string lista = i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
            var log = new StringBuilder();
            foreach (var paso in lista.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t0 = DateTime.Now;
                try
                {
                    int punto = paso.LastIndexOf('.');
                    string tipo = paso.Substring(0, punto), metodo = paso.Substring(punto + 1);
                    var t = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                        .FirstOrDefault(x => x.Name == tipo || x.FullName == tipo);
                    if (t == null) throw new Exception("no existe el tipo " + tipo);
                    var m = t.GetMethod(metodo, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (m == null) throw new Exception("no existe el método " + paso);
                    m.Invoke(null, null);
                    log.AppendLine($"OK\t{paso}\t{(DateTime.Now - t0).TotalSeconds:0}s");
                }
                catch (Exception e)
                {
                    var real = e is System.Reflection.TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                    log.AppendLine($"FALLO\t{paso}\t{(DateTime.Now - t0).TotalSeconds:0}s\t{real.GetType().Name}: {real.Message}\n{real.StackTrace}");
                    Debug.LogException(real);
                }
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/correr19nov.txt", log.ToString());
            }
        }

        /// <summary>Causa del piso de escarcha (T-1383): la normal del asfalto S2c estaba importada como textura Default y en sRGB.</summary>
        public static void ArreglarNormalesAsfalto()
        {
            foreach (var ruta in new[] { "Assets/Arte/Exteriores/Texturas/T535_Asfalto_Normal.png" })
            {
                var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (imp == null) { Debug.LogWarning("Correr19nov: no existe " + ruta); continue; }
                if (imp.textureType != TextureImporterType.NormalMap)
                {
                    imp.textureType = TextureImporterType.NormalMap;
                    imp.sRGBTexture = false;
                    imp.SaveAndReimport();
                    Debug.Log("Correr19nov: " + ruta + " ahora es NormalMap");
                }
            }
        }
    }
}
