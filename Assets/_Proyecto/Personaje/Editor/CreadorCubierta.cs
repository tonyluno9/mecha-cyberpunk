using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-421: crea Resources/DatosCubierta.asset. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorCubierta.Crear
    /// </summary>
    public static class CreadorCubierta
    {
        private const string CarpetaResources = "Assets/_Proyecto/Personaje/Datos/Resources";
        private const string RutaDatos = CarpetaResources + "/DatosCubierta.asset";

        public static void Crear()
        {
            Directory.CreateDirectory(CarpetaResources);
            if (AssetDatabase.LoadAssetAtPath<DatosCubierta>(RutaDatos) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DatosCubierta>(), RutaDatos);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] DatosCubierta creado en " + RutaDatos);
        }
    }
}
