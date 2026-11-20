using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Corre varios métodos estáticos de editor en una sola sesión de Unity (integración 20-nov B).
    /// Uso: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CorrerMetodos20nB.Correr -metodos "Ns.Clase.Metodo;Ns.Otra.Metodo"
    /// Sale con 0 si todos terminaron sin excepción, 1 si alguno falló.
    /// </summary>
    public static class CorrerMetodos20nB
    {
        public static void Correr()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-metodos");
            if (i < 0 || i + 1 >= args.Length)
            {
                Debug.LogError("[Correr20nB] falta -metodos");
                EditorApplication.Exit(1);
                return;
            }

            int fallos = 0;
            foreach (string nombre in args[i + 1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string completo = nombre.Trim();
                int punto = completo.LastIndexOf('.');
                string tipo = completo.Substring(0, punto);
                string metodo = completo.Substring(punto + 1);
                Type t = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(tipo, false))
                    .FirstOrDefault(x => x != null);
                MethodInfo m = t?.GetMethod(metodo, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (m == null)
                {
                    Debug.LogError($"[Correr20nB] no encontré {completo}");
                    fallos++;
                    continue;
                }

                try
                {
                    Debug.Log($"[Correr20nB] >>> {completo}");
                    m.Invoke(null, null);
                    Debug.Log($"[Correr20nB] OK {completo}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Correr20nB] FALLÓ {completo}: {(e.InnerException ?? e)}");
                    fallos++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorApplication.Exit(fallos == 0 ? 0 : 1);
        }
    }
}
