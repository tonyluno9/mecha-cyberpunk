using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Economia.Editor
{
    /// <summary>
    /// Crea DatosApuestaPelea_Casera (momio 1.0, máximo $500, montos 100/250/500, Brazo de chatarra desde $250; D-tony-86) y agrega
    /// el ServicioApuestas a ServiciosEconomia.prefab sin regenerarlo (T-144). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Economia.Editor.CreadorApuestas.Crear
    /// </summary>
    public static class CreadorApuestas
    {
        private const string RutaDatos = "Assets/_Proyecto/Economia/Datos/DatosApuestaPelea_Casera.asset";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const string RutaPrefab = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";
        private const float MomioCasera = 1f;
        private const int MaximoCasera = 500;
        private const int MinimoPiezaCasera = 250;
        private static readonly int[] MontosCasera = { 100, 250, 500 };

        [MenuItem("Mecha Cyberpunk/Crear T-144 apuestas")]
        public static void Crear()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosApuestaPelea>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosApuestaPelea>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            var so = new SerializedObject(datos);
            so.FindProperty("momio").floatValue = MomioCasera;
            so.FindProperty("apuestaMaxima").intValue = MaximoCasera;
            SerializedProperty montos = so.FindProperty("montos");
            montos.arraySize = MontosCasera.Length;
            for (int i = 0; i < MontosCasera.Length; i++) montos.GetArrayElementAtIndex(i).intValue = MontosCasera[i];
            so.FindProperty("piezaEnJuego").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            so.FindProperty("apuestaMinimaPorPieza").intValue = MinimoPiezaCasera;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                if (raiz.GetComponent<ServicioApuestas>() == null) raiz.AddComponent<ServicioApuestas>();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Economia] T-144 apuestas creadas.");
        }
    }
}
