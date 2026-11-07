using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-709: la patrulla baja a sus policías. Le asigna a Patrulla.prefab el prefab IA/Prefabs/PoliciaAPie.prefab (solo por ruta de asset: Trafico no
    /// referencia a IA). Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPoliciasT709.Crear
    /// </summary>
    public static class CreadorPoliciasT709
    {
        public const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";

        [MenuItem("Mecha Cyberpunk/T-709/Policías en la patrulla")]
        public static void Crear()
        {
            GameObject policia = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia);
            if (policia == null)
            {
                Debug.LogError("[Trafico] T-709: falta " + RutaPolicia);
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(CreadorPatrullaT707.RutaPatrulla);
            try
            {
                SerializedObject so = new SerializedObject(raiz.GetComponent<PatrullaMundo>());
                so.FindProperty("prefabPolicia").objectReferenceValue = policia;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, CreadorPatrullaT707.RutaPatrulla);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Debug.Log("[Trafico] T-709: Patrulla.prefab baja policías de " + RutaPolicia);
        }
    }
}
