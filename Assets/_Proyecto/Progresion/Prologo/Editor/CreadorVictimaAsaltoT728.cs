using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-728: la persona de afuera a la que asaltan en la historia del dinero. Crea Progresion/Prologo/Resources/VictimaAsalto.prefab a partir del modelo visual de Personaje
    /// (Visual_man, solo cuerpo y animador: no pelea ni camina sola). Se carga por Resources.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorVictimaAsaltoT728.Crear
    /// </summary>
    public static class CreadorVictimaAsaltoT728
    {
        public const string RutaModelo = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_woman2.prefab";
        public const string RutaVictima = "Assets/_Proyecto/Progresion/Prologo/Resources/VictimaAsalto.prefab";

        [MenuItem("Mecha Cyberpunk/T-728/Victima del asalto")]
        public static void Crear()
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            if (modelo == null)
            {
                Debug.LogError("[Progresion] T-728: falta " + RutaModelo);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaVictima));
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                instancia.name = "VictimaAsalto";
                PrefabUtility.SaveAsPrefabAsset(instancia, RutaVictima);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-728: " + RutaVictima + " creado.");
        }
    }
}
