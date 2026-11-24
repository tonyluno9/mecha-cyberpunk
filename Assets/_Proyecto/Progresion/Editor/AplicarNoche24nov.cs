using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// Noche 24-nov: corre en orden todos los pasos de editor de las tareas de la noche (por nombre, con reflexión, para no referenciar otros ensamblados).
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.AplicarNoche24nov.Aplicar
    /// </summary>
    public static class AplicarNoche24nov
    {
        private static readonly string[] Pasos =
        {
            "MechaCyberpunk.Progresion.Sonido.Editor.ImportarMusicaFases.Aplicar",
            "MechaCyberpunk.Progresion.Recorrido.Editor.VideosCinematicas23nov.Aplicar",
            "MechaCyberpunk.Personaje.Editor.RecompensasEnemigoT24nov.Aplicar",
            "MechaCyberpunk.UI.Editor.IndicadorDanioT24nov.Aplicar",
            "MechaCyberpunk.CampoTiro.Editor.AplicarCampoTiro.AplicarMenu",
        };

        /// <summary>Pasos extra (los agrega quien los necesite antes de correr).</summary>
        public static string[] Extra = new string[0];

        [MenuItem("Mecha Cyberpunk/Noche 24-nov/Aplicar todo")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            foreach (string paso in Pasos.Concat(Extra))
            {
                Correr(paso);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Correr(string completo)
        {
            int punto = completo.LastIndexOf('.');
            string tipo = completo.Substring(0, punto);
            string metodo = completo.Substring(punto + 1);
            Type t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(tipo)).FirstOrDefault(x => x != null);
            MethodInfo m = t?.GetMethod(metodo, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (m == null)
            {
                Debug.LogError("[Noche24] no existe " + completo);
                return;
            }
            try
            {
                m.Invoke(null, null);
                Debug.Log("[Noche24] OK " + completo);
            }
            catch (Exception e)
            {
                Debug.LogError("[Noche24] FALLÓ " + completo + ": " + (e.InnerException ?? e));
            }
        }
    }
}
