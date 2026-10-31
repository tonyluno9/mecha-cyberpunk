using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>T-424: crea DatosApuntadoAsistido_Base y agrega ApuntadoAsistido al prefab JugadorAPie (sin regenerarlo entero).</summary>
    public static class CreadorApuntadoAsistido
    {
        public const string RutaDatos = "Assets/_Proyecto/Personaje/Datos/DatosApuntadoAsistido_Base.asset";
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-424 apuntado asistido")]
        public static void Crear()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                AgregarA(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>T-425: lo llama también CreadorJugadorAPie para que regenerar el prefab no pierda el apuntado asistido.</summary>
        public static void AgregarA(GameObject raiz)
        {
            DatosApuntadoAsistido datos = AssetDatabase.LoadAssetAtPath<DatosApuntadoAsistido>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosApuntadoAsistido>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            ApuntadoAsistido comp = raiz.GetComponent<ApuntadoAsistido>();
            if (comp == null)
            {
                comp = raiz.AddComponent<ApuntadoAsistido>();
            }
            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
