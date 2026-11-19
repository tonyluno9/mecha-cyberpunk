using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1360: corre varios métodos estáticos de editor en una sola sesión de Unity, en orden: -metodos Clase.Metodo,Clase.Metodo
    /// (clases de MechaCyberpunk.Arte.Editor o con nombre completo). Si uno lanza una excepción, la anota y sigue con el siguiente.
    /// Escribe Logs/t1360-cadena.txt. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.CorrerCadenaT1360.Correr -metodos ...
    /// </summary>
    public static class CorrerCadenaT1360
    {
        public static void Correr()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-metodos");
            string[] metodos = i >= 0 && i + 1 < args.Length ? args[i + 1].Split(',') : new string[0];
            Directory.CreateDirectory("Logs");
            using (StreamWriter log = new StreamWriter("Logs/t1360-cadena.txt", true))
            {
                foreach (string m in metodos.Select(x => x.Trim()).Where(x => x.Length > 0))
                {
                    int punto = m.LastIndexOf('.');
                    string clase = m.Substring(0, punto);
                    string nombre = m.Substring(punto + 1);
                    Type tipo = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(clase) ?? a.GetType("MechaCyberpunk.Arte.Editor." + clase)).FirstOrDefault(t => t != null);
                    MethodInfo metodo = tipo?.GetMethod(nombre, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                    DateTime inicio = DateTime.Now;
                    if (metodo == null)
                    {
                        log.WriteLine(inicio.ToString("HH:mm:ss") + "\tNO EXISTE\t" + m);
                        continue;
                    }
                    try
                    {
                        metodo.Invoke(null, null);
                        AssetDatabase.SaveAssets();
                        log.WriteLine(inicio.ToString("HH:mm:ss") + "\tOK\t" + m + "\t" + (DateTime.Now - inicio).TotalSeconds.ToString("F0") + " s");
                    }
                    catch (Exception e)
                    {
                        Exception real = e.InnerException ?? e;
                        log.WriteLine(inicio.ToString("HH:mm:ss") + "\tERROR\t" + m + "\t" + real.GetType().Name + ": " + real.Message);
                        Debug.LogException(real);
                    }
                    log.Flush();
                }
            }
        }
    }
}
