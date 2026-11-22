using System.IO;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T25: arreglos de la policía en los datos. (1) Todos los prefabs de patrulla y de modelo de policía llevan <see cref="DanioCarro"/> (reciben balas y explosiones) y,
    /// al robarlos, suman calor de patrulla (55 = nivel 2, como V05); (2) las patrullas de ronda bajan a 2 en todos los ConfigPersecucion. Escribe Logs/t25.txt. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.AplicarPoliciaT25.Aplicar
    /// </summary>
    public static class AplicarPoliciaT25
    {
        public const int PatrullasDeRonda = 2;
        public const float CalorAlRobarPatrulla = 55f;
        public const string RutaInforme = "Logs/t25.txt";

        private static readonly string[] Prefabs =
        {
            "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/PatrullaCamioneta.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_PA01.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_PA02.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V06.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V08.prefab",
            "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V11.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V05.prefab",
            "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V06.prefab",
        };

        [MenuItem("Mecha Cyberpunk/T25/Aplicar arreglos de policía")]
        public static void Aplicar()
        {
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("T25 Aplicar");
            foreach (string ruta in Prefabs)
            {
                informe.AppendLine(ArreglarPrefab(ruta));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:ConfigPersecucion"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Object config = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                if (config == null)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(config);
                SerializedProperty p = so.FindProperty("patrullasDeRonda");
                if (p != null)
                {
                    informe.AppendLine(ruta + ": patrullasDeRonda " + p.intValue + " -> " + PatrullasDeRonda);
                    p.intValue = PatrullasDeRonda;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(config);
                }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Trafico] T25: " + informe);
        }

        private static string ArreglarPrefab(string ruta)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
            {
                return ruta + ": no existe (se salta)";
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                StringBuilder s = new StringBuilder(ruta + ":");
                if (raiz.GetComponent<DanioCarro>() == null)
                {
                    raiz.AddComponent<DanioCarro>();
                    s.Append(" +DanioCarro");
                }
                int colliders = 0;
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>(true))
                {
                    if (!c.isTrigger)
                    {
                        colliders++;
                    }
                }
                s.Append(" colliders=" + colliders);
                CarroEstacionado estacionado = raiz.GetComponent<CarroEstacionado>();
                if (estacionado != null)
                {
                    SerializedObject so = new SerializedObject(estacionado);
                    SerializedProperty calor = so.FindProperty("calorAlRobar");
                    if (calor != null && calor.floatValue < CalorAlRobarPatrulla)
                    {
                        s.Append(" calorAlRobar " + calor.floatValue + " -> " + CalorAlRobarPatrulla);
                        calor.floatValue = CalorAlRobarPatrulla;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                return s.ToString();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
