using System.Linq;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1202: los policías van sentados dentro de la patrulla y se bajan con las animaciones de bajar del carro del jugador. Le asigna a Patrulla.prefab
    /// los clips PJ01_Carro_Manejar y PJ01_Carro_Salir (solo por ruta de asset). Batchmode:
    /// -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPoliciasSentadosT1202.Crear
    /// </summary>
    public static class CreadorPoliciasSentadosT1202
    {
        public const string RutaManejar = "Assets/Arte/Personajes/PJ01/Clips/PJ01_Carro_Manejar.fbx";
        public const string RutaSalir = "Assets/Arte/Personajes/PJ01/Clips/PJ01_Carro_Salir.fbx";

        public static AnimationClip Clip(string ruta)
        {
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        [MenuItem("Mecha Cyberpunk/T-1202/Policías sentados en la patrulla")]
        public static void Crear()
        {
            AnimationClip manejar = Clip(RutaManejar);
            AnimationClip salir = Clip(RutaSalir);
            if (manejar == null || salir == null)
            {
                Debug.LogError("[Trafico] T-1202: faltan los clips " + RutaManejar + " o " + RutaSalir);
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(CreadorPatrullaT707.RutaPatrulla);
            try
            {
                SerializedObject so = new SerializedObject(raiz.GetComponent<PatrullaMundo>());
                so.FindProperty("clipPoliciaManejar").objectReferenceValue = manejar;
                so.FindProperty("clipPoliciaSalir").objectReferenceValue = salir;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, CreadorPatrullaT707.RutaPatrulla);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Debug.Log("[Trafico] T-1202: Patrulla.prefab lleva a sus policías sentados (" + manejar.name + ", " + salir.name + ")");
        }
    }
}
