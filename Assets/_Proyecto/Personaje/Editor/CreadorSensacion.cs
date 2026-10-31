using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-420: crea el material de efectos y Resources/DatosSensacion.asset. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorSensacion.Crear
    /// </summary>
    public static class CreadorSensacion
    {
        private const string RutaMaterial = "Assets/_Proyecto/Personaje/Materiales/Efecto_Disparo.mat";
        private const string CarpetaResources = "Assets/_Proyecto/Personaje/Datos/Resources";
        private const string RutaDatos = CarpetaResources + "/DatosSensacion.asset";

        public static void Crear()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default")) { name = "Efecto_Disparo" };
                AssetDatabase.CreateAsset(material, RutaMaterial);
            }
            Directory.CreateDirectory(CarpetaResources);
            DatosSensacion datos = AssetDatabase.LoadAssetAtPath<DatosSensacion>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosSensacion>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("materialEfecto").objectReferenceValue = material;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] DatosSensacion creado en " + RutaDatos);
        }
    }
}
