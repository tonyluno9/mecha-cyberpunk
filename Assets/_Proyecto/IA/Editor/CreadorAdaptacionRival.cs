using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea los datos de adaptación de T-199: DatosAdaptacionRival_Base (nivelMaximo 1) y _Avanzado (nivelMaximo 2) con los valores de
    /// D-tony-193 a D-tony-196. Idempotente: un asset que ya existe no se toca, así los valores que Tony cambie se quedan.
    /// T-200: también agrega AdaptacionRival a los 20 prefabs de liga (ligas 1 y 2 con _Base, 3 y 4 con _Avanzado) y a RivalCasero (_Base),
    /// y MemoriaRivales a ServiciosProgresion. Se corre después de CreadorRivalesLiga y CreadorHabilidadesJefes. No toca RivalBasico,
    /// RivalTutorial ni RivalPandilla. Un prefab que ya tiene el componente no se vuelve a guardar.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorAdaptacionRival.Crear
    /// </summary>
    public static class CreadorAdaptacionRival
    {
        public const string RutaBase = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Base.asset";
        public const string RutaAvanzado = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Avanzado.asset";

        public const string RutaRivalCasero = "Assets/_Proyecto/IA/Prefabs/RivalCasero.prefab";
        public const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const int LigasConNivelUno = 2;

        [MenuItem("Mecha Cyberpunk/Crear T-199 adaptación")]
        public static void Crear()
        {
            CrearAsset(RutaBase, 1);
            CrearAsset(RutaAvanzado, 2);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-199 datos de adaptación creados.");
            CrearPrefabs();
        }

        /// <summary>T-200: agrega AdaptacionRival a los rivales de liga y al casero, y MemoriaRivales a los servicios.</summary>
        public static void CrearPrefabs()
        {
            DatosAdaptacionRival nivelUno = AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(RutaBase);
            DatosAdaptacionRival nivelDos = AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(RutaAvanzado);
            for (int liga = 1; liga <= CreadorRivalesLiga.Ligas; liga++)
            {
                for (int n = 1; n <= CreadorRivalesLiga.RivalesPorLiga; n++)
                {
                    PonerAdaptacion(CreadorRivalesLiga.RutaPrefab(liga, n), liga <= LigasConNivelUno ? nivelUno : nivelDos);
                }
            }
            PonerAdaptacion(RutaRivalCasero, nivelUno);
            PonerMemoria();
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-200 adaptación puesta en los rivales de liga, el casero y los servicios.");
        }

        private static void PonerAdaptacion(string rutaPrefab, DatosAdaptacionRival datos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
            if (prefab == null || datos == null)
            {
                Debug.LogError("[IA] Falta " + (prefab == null ? rutaPrefab : "el DatosAdaptacionRival") + ": corre antes CreadorRivalesLiga.");
                return;
            }
            if (prefab.GetComponent<AdaptacionRival>() != null)
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                AdaptacionRival adaptacion = contenido.AddComponent<AdaptacionRival>();
                SerializedObject so = new SerializedObject(adaptacion);
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void PonerMemoria()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios);
            if (prefab == null)
            {
                Debug.LogError("[IA] Falta " + RutaServicios);
                return;
            }
            if (prefab.GetComponent<MemoriaRivales>() != null)
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaServicios);
            try
            {
                contenido.AddComponent<MemoriaRivales>();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaServicios);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void CrearAsset(string ruta, int nivelMaximo)
        {
            if (AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(ruta) != null)
            {
                return;
            }
            DatosAdaptacionRival datos = ScriptableObject.CreateInstance<DatosAdaptacionRival>();
            AssetDatabase.CreateAsset(datos, ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nivelMaximo").intValue = nivelMaximo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}
