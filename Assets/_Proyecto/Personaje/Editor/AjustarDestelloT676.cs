using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-676: ajusta el destello del cañón en el asset DatosSensacion: tamaños chicos, duración corta y, si F01_Destello ya está
    /// en el proyecto (se busca por nombre), la asigna como textura. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarDestelloT676.Ajustar
    /// </summary>
    public static class AjustarDestelloT676
    {
        private const string RutaDatos = "Assets/_Proyecto/Personaje/Datos/Resources/DatosSensacion.asset";
        // En orden: Puños, Pistola, Escopeta, Fusil, Sniper (m). T-678: con F01_Destello (la llama ocupa menos de la mitad de la imagen) el quad es el doble de grande.
        private static readonly float[] Tamanos = { 0f, 0.26f, 0.5f, 0.3f, 0.4f };

        [MenuItem("Mecha Cyberpunk/Ajustar T-676 destello F01")]
        public static void Ajustar()
        {
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[Personaje] Falta " + RutaDatos);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            SerializedProperty armas = so.FindProperty("armas");
            for (int i = 0; i < Tamanos.Length && i < armas.arraySize; i++)
            {
                armas.GetArrayElementAtIndex(i).FindPropertyRelative("destello").floatValue = Tamanos[i];
            }
            so.FindProperty("duracionDestello").floatValue = 0.04f;
            string[] guids = AssetDatabase.FindAssets("F01_Destello t:Texture2D");
            if (guids.Length > 0)
            {
                so.FindProperty("texturaDestello").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            else
            {
                Debug.Log("[Personaje] F01_Destello todavía no está importada: el destello usa la chispa procedural.");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
        }
    }
}
