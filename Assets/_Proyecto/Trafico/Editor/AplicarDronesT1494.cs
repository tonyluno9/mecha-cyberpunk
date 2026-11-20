using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1494: le pone al prefab del dron una luz puntual (cyan) hija "LuzDron" para que se vea de noche y de lejos. Si ya la tiene, la deja igual.
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.Trafico.Editor.AplicarDronesT1494.Aplicar
    /// </summary>
    public static class AplicarDronesT1494
    {
        public const string RutaPrefab = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        public const string NombreLuz = "LuzDron";

        [MenuItem("Mecha Cyberpunk/T1494/Aplicar luz del dron")]
        public static void Aplicar()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform luz = raiz.transform.Find(NombreLuz);
                if (luz == null)
                {
                    GameObject objeto = new GameObject(NombreLuz);
                    objeto.transform.SetParent(raiz.transform, false);
                    objeto.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                    luz = objeto.transform;
                }
                Light l = luz.GetComponent<Light>();
                if (l == null)
                {
                    l = luz.gameObject.AddComponent<Light>();
                }
                l.type = LightType.Point;
                l.color = new Color(0.3f, 0.9f, 1f);
                l.range = 12f;
                l.intensity = 3f;
                l.shadows = LightShadows.None;
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                Debug.Log("[T-1494] luz del dron aplicada");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
