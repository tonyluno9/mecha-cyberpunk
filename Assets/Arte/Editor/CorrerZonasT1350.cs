using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1350 (orquestación de las zonas nuevas): corre en una sola sesión de Unity los <c>Aplicar</c> y los <c>Fotos</c> de las zonas
    /// (por reflexión, para tolerar zonas que aún no están). Batchmode, por ejemplo:
    /// -executeMethod MechaCyberpunk.Arte.Editor.CorrerZonasT1350.Correr -zonas T1358Orillas.Aplicar,T1354Malecon.Aplicar,T1354Malecon.Fotos
    /// </summary>
    public static class CorrerZonasT1350
    {
        private const string Prefijo = "MechaCyberpunk.Arte.Editor.Colocar";

        public static void Correr()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-zonas");
            if (i < 0 || i + 1 >= args.Length)
            {
                Debug.LogError("[Arte] CorrerZonasT1350: falta -zonas Clase.Metodo,...");
                return;
            }
            Type[] tipos = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
            {
                try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
            }).ToArray();
            foreach (string paso in args[i + 1].Split(','))
            {
                string[] partes = paso.Trim().Split('.');
                Type tipo = tipos.FirstOrDefault(t => t.FullName == Prefijo + partes[0]);
                MethodInfo metodo = tipo?.GetMethod(partes[1], BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (metodo == null)
                {
                    Debug.LogError("[Arte] CorrerZonasT1350: no existe " + paso);
                    continue;
                }
                Debug.Log("[Arte] CorrerZonasT1350: " + paso);
                try
                {
                    metodo.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Arte] CorrerZonasT1350: " + paso + " falló: " + (e.InnerException ?? e));
                }
            }
        }
    }
}
