using System;
using System.Collections.Generic;
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

        /// <summary>Cuenta las Light activas por raíz y tipo (Logs/luces-zonas.txt). Batchmode: -executeMethod ...CorrerZonasT1350.ContarLuces</summary>
        public static void ContarLuces()
        {
            ColocarS2.Abrir();
            Light[] luces = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            System.Text.StringBuilder sb = new System.Text.StringBuilder("Total " + luces.Length + System.Environment.NewLine);
            foreach (IGrouping<string, Light> g in luces.GroupBy(l => l.transform.root.name + " " + l.type).OrderByDescending(g => g.Count()))
            {
                sb.AppendLine(g.Key + ": " + g.Count());
            }
            System.IO.File.WriteAllText("Logs/luces-zonas.txt", sb.ToString());
        }

        /// <summary>
        /// Raíces de las zonas nuevas y separación mínima (m) entre luces reales que se conservan: spots (postes, faroles, reflectores) y
        /// puntuales (neones, focos). Con todas las luces de los kits la escena pasaba de 1400 Light y el URP ya no podía dibujar (búfer de luces).
        /// </summary>
        private static readonly string[] RaicesZonas = { "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto" };
        private const float SeparacionSpot = 10f;
        private const float SeparacionPunto = 20f;

        /// <summary>Quita las Light de más en las zonas nuevas (lo emisivo se queda). Idempotente. Se corre después de los Aplicar.</summary>
        public static void PodarLuces()
        {
            ColocarS2.Abrir();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string nombre in RaicesZonas)
            {
                Transform raiz = ColocarS2.Buscar(nombre);
                if (raiz == null)
                {
                    continue;
                }
                Light[] luces = raiz.GetComponentsInChildren<Light>(true);
                List<Vector3> spots = new List<Vector3>();
                List<Vector3> puntos = new List<Vector3>();
                int quitadas = 0;
                // Primero las más fuertes: esas son las que se ven en el piso.
                foreach (Light l in luces.OrderByDescending(l => l.intensity * l.range))
                {
                    bool esSpot = l.type == LightType.Spot;
                    List<Vector3> puestas = esSpot ? spots : puntos;
                    float sep = esSpot ? SeparacionSpot : SeparacionPunto;
                    Vector3 p = l.transform.position;
                    if (puestas.Any(q => (q - p).sqrMagnitude < sep * sep))
                    {
                        // El dato de luz de URP depende de la Light: se quita primero.
                        foreach (Component extra in l.GetComponents<Component>().Where(c => c != null && c.GetType().Name == "UniversalAdditionalLightData"))
                        {
                            UnityEngine.Object.DestroyImmediate(extra);
                        }
                        UnityEngine.Object.DestroyImmediate(l);
                        quitadas++;
                        continue;
                    }
                    puestas.Add(p);
                }
                sb.AppendLine(nombre + ": " + luces.Length + " luces, quedan " + (spots.Count + puntos.Count) + " (spots " + spots.Count + ", puntuales " + puntos.Count + "), quitadas " + quitadas);
            }
            ColocarS2.Informe.Clear();
            ColocarS2.Informe.Append(sb);
            ColocarS2.Guardar("t1350-podar-luces");
        }

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
                Type tipo = tipos.FirstOrDefault(t => t.FullName == Prefijo + partes[0] || t.FullName == "MechaCyberpunk.Arte.Editor." + partes[0]);
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
