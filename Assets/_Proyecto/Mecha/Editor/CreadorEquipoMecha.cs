using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Pone EquipoMecha (con CatalogoPiezas_Base) en MechaEstacionado.prefab y lo quita de MechaJugador.prefab (T-167).
    /// Idempotente. Corre después de CreadorPiezas. Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorEquipoMecha.Crear
    /// </summary>
    public static class CreadorEquipoMecha
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaVariante = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";

        [MenuItem("Mecha Cyberpunk/Crear T-167 equipo del mecha")]
        public static void Crear()
        {
            CatalogoPiezas catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[Mecha] Falta CatalogoPiezas_Base: corre CreadorPiezas primero.");
                return;
            }

            GameObject baseMecha = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                EquipoMecha enBase = baseMecha.GetComponent<EquipoMecha>();
                if (enBase != null)
                {
                    Object.DestroyImmediate(enBase);
                    PrefabUtility.SaveAsPrefabAsset(baseMecha, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(baseMecha);
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaVariante);
            try
            {
                EquipoMecha equipo = raiz.GetComponent<EquipoMecha>();
                if (equipo == null)
                {
                    equipo = raiz.AddComponent<EquipoMecha>();
                }
                var so = new SerializedObject(equipo);
                so.FindProperty("catalogo").objectReferenceValue = catalogo;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaVariante);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-167 equipo del mecha creado.");
        }
    }
}
