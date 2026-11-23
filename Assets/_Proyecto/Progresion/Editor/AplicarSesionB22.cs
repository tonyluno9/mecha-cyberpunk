using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// Sesión B del 22-nov (T-1620 a T-1628): corre en orden los Aplicar() de cada tarea en una sola pasada de Unity. Los busca por nombre
    /// (reflexión) porque viven en ensamblados que este no referencia. Escribe el informe en Logs/aplicar-b22.txt.
    /// </summary>
    public static class AplicarSesionB22
    {
        private static readonly string[] Metodos =
        {
            "MechaCyberpunk.Progresion.Editor.AplicarVocesSkullsT1623.Aplicar",
            "MechaCyberpunk.Trafico.Editor.AplicarRespuestaT1620.Aplicar",
            "MechaCyberpunk.IA.Editor.CapsulasCaidaT1621.Aplicar",
            "MechaCyberpunk.Arte.Editor.RobotsNovaT1553.Aplicar",
            "MechaCyberpunk.Personaje.Visual.Editor.TransicionesT1561.CrearConfigNpc",
            "MechaCyberpunk.Personaje.Editor.CrearBancaSentableT1628.Aplicar",
            "MechaCyberpunk.IA.Editor.CrearEscenasBarrioT1624.Aplicar",
            "MechaCyberpunk.UI.Editor.CreadorPantallaOpciones.Crear",
            "MechaCyberpunk.Progresion.Recorrido.Editor.CreadorRecorrido.Aplicar",
            "MechaCyberpunk.Trafico.Editor.AplicarPoliciaT25.Aplicar",
            "MechaCyberpunk.IA.Editor.AplicarConfrontacionT1651.Aplicar",
            // T-1655: AplicarPasosT1654 ya no corre (sus pasos tenían un siseo de fondo); los finales de pasos los deja RevertirPasosT1655.
            "MechaCyberpunk.Progresion.Editor.RevertirPasosT1655.Aplicar",
        };

        [MenuItem("Mecha Cyberpunk/Sesión B 22-nov/Aplicar todo")]
        public static void Aplicar() => Correr(Metodos);

        /// <summary>Corre solo los métodos cuyo nombre completo contenga alguno de los textos de la variable de entorno APLICAR_B22 (separados por comas).</summary>
        public static void AplicarFiltrado()
        {
            string filtro = Environment.GetEnvironmentVariable("APLICAR_B22") ?? string.Empty;
            string[] partes = filtro.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            Correr(Metodos.Where(m => partes.Any(m.Contains)).ToArray());
        }

        private static void Correr(string[] metodos)
        {
            var sb = new StringBuilder("[B22] Aplicar\n");
            Assembly[] ensamblados = AppDomain.CurrentDomain.GetAssemblies();
            foreach (string completo in metodos)
            {
                int punto = completo.LastIndexOf('.');
                string tipoNombre = completo.Substring(0, punto), metodoNombre = completo.Substring(punto + 1);
                Type tipo = ensamblados.Select(a => a.GetType(tipoNombre)).FirstOrDefault(t => t != null);
                MethodInfo metodo = tipo?.GetMethod(metodoNombre, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (metodo == null)
                {
                    sb.AppendLine("NO ENCONTRADO " + completo);
                    continue;
                }
                try
                {
                    object r = metodo.Invoke(null, null);
                    sb.AppendLine("ok " + completo + (r != null ? " -> " + r : string.Empty));
                }
                catch (Exception e)
                {
                    sb.AppendLine("ERROR " + completo + ": " + (e.InnerException ?? e));
                }
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
            System.IO.File.WriteAllText("Logs/aplicar-b22.txt", sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
