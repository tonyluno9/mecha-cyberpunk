using MechaCyberpunk.Progresion.Prologo.Clinica;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Crea lo de T-196: DatosDoctor_Base.asset y DoctorClinica.prefab (un objeto vacío con el <see cref="DoctorClinica"/>). Idempotente:
    /// si ya existen no los toca.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorDoctorClinica.Crear
    /// </summary>
    public static class CreadorDoctorClinica
    {
        public const string RutaDatos = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosDoctor_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/Progresion/Prologo/Prefabs/DoctorClinica.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-196 doctor de la clínica")]
        public static void Crear()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosDoctor>(RutaDatos) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DatosDoctor>(), RutaDatos);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                GameObject raiz = new GameObject("DoctorClinica");
                try
                {
                    DoctorClinica doctor = raiz.AddComponent<DoctorClinica>();
                    SerializedObject so = new SerializedObject(doctor);
                    so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosDoctor>(RutaDatos);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
                finally
                {
                    Object.DestroyImmediate(raiz);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-196 doctor de la clínica listo.");
        }
    }
}
