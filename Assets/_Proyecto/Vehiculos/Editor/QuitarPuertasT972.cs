using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-972: los carros V01a, V01b, V05 y la patrulla van sin puertas (decisión del 9-nov). Borra Puerta_Izq y Puerta_Der de cada prefab y quita el
    /// componente PuertasCarro (ya borrado del código: queda como script faltante, que se limpia aquí). Idempotente: correrlo otra vez no cambia nada.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.QuitarPuertasT972.QuitarTodas
    /// </summary>
    public static class QuitarPuertasT972
    {
        public static readonly string[] Prefabs =
        {
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01a.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V01b.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab",
        };

        private static readonly string[] NombresPuerta = { "Puerta_Izq", "Puerta_Der" };

        [MenuItem("Mecha Cyberpunk/T-972/Quitar puertas de los carros")]
        public static void QuitarTodas()
        {
            StringBuilder informe = new StringBuilder();
            foreach (string ruta in Prefabs)
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    informe.AppendLine(Quitar(raiz, ruta));
                    PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t972-puertas.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-972:\n" + informe);
        }

        /// <summary>Borra las puertas y los scripts faltantes de <paramref name="raiz"/>. Devuelve lo que borró.</summary>
        public static string Quitar(GameObject raiz, string nombre)
        {
            List<string> borrados = new List<string>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => NombresPuerta.Contains(x.name)).ToList())
            {
                borrados.Add(t.name + " (" + t.GetComponentsInChildren<Transform>(true).Length + " objetos)");
                Object.DestroyImmediate(t.gameObject);
            }
            int scripts = 0;
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                scripts += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            if (scripts > 0)
            {
                borrados.Add(scripts + " script(s) faltante(s) (PuertasCarro)");
            }
            return nombre + "\t" + (borrados.Count == 0 ? "nada que borrar" : string.Join(", ", borrados));
        }
    }
}
