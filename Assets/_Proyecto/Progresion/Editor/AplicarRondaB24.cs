using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>Ronda B24 (23-nov): corre por reflexión los Aplicar() de varias áreas en orden. -executeMethod ...AplicarRondaB24.Todo</summary>
    public static class AplicarRondaB24
    {
        public static void Todo()
        {
            Correr("MechaCyberpunk.Progresion.Editor.CreadorCatalogoSonidos", "Crear");
            Correr("MechaCyberpunk.Personaje.Visual.Editor.HumanoJugadorT53", "Aplicar");
            Correr("MechaCyberpunk.Personaje.Visual.Editor.EventosArmaLargaT60", "Aplicar");
            AssetDatabase.SaveAssets();
        }

        public static void Correr(string tipo, string metodo)
        {
            Type t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(tipo)).FirstOrDefault(x => x != null);
            if (t == null)
            {
                Debug.LogError("[RondaB24] No existe " + tipo);
                return;
            }
            Debug.Log("[RondaB24] Corriendo " + tipo + "." + metodo);
            t.GetMethod(metodo, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            Debug.Log("[RondaB24] Listo " + tipo + "." + metodo);
        }
    }
}
