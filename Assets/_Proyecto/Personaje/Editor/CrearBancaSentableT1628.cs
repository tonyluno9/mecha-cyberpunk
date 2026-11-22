using System.IO;
using MechaCyberpunk.Personaje.Interaccion;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1628: crea el prefab Banca_Sentable (PZ01_Banca con dos asientos interactuables, uno por lado) en Personaje/Interaccion/Prefabs.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CrearBancaSentableT1628.Aplicar
    /// </summary>
    public static class CrearBancaSentableT1628
    {
        private const string RutaBanca = "Assets/Arte/Zonas/PZ/PZ01_Banca.prefab";
        private const string Carpeta = "Assets/_Proyecto/Personaje/Interaccion/Prefabs";
        private const string RutaPrefab = Carpeta + "/Banca_Sentable.prefab";
        private const float LateralAsiento = 0.4f;

        [MenuItem("Mecha Cyberpunk/Crear banca sentable T-1628")]
        public static void Aplicar()
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBanca);
            if (modelo == null)
            {
                Debug.LogError("[Personaje] Falta " + RutaBanca);
                return;
            }
            Directory.CreateDirectory(Carpeta);
            GameObject raiz = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            raiz.name = "Banca_Sentable";
            try
            {
                CrearAsiento(raiz.transform, "Asiento_Izquierdo", -LateralAsiento);
                CrearAsiento(raiz.transform, "Asiento_Derecho", LateralAsiento);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                Debug.Log("[Personaje] Prefab creado: " + RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
        }

        private static void CrearAsiento(Transform padre, string nombre, float x)
        {
            GameObject asiento = new GameObject(nombre);
            asiento.transform.SetParent(padre, false);
            asiento.transform.localPosition = new Vector3(x, 0f, 0f);
            asiento.AddComponent<AsientoInteractuable>();
        }
    }
}
