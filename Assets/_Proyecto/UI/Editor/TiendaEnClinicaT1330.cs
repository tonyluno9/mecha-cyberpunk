using MechaCyberpunk.UI.Tiendas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-1330: mete una PantallaTienda como hijo de MostradorClinica.prefab para que la pestaña [TAB] PRÓTESIS DE ATAQUE (brazo láser y hombrera
    /// de misiles) salga también en Ciudad, que no trae otra PantallaTienda. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.TiendaEnClinicaT1330.Aplicar
    /// </summary>
    public static class TiendaEnClinicaT1330
    {
        public const string RutaMostrador = "Assets/_Proyecto/UI/Prefabs/MostradorClinica.prefab";
        public const string RutaPantallaTienda = "Assets/_Proyecto/UI/Prefabs/PantallaTienda.prefab";

        [MenuItem("Mecha Cyberpunk/T-1330 Tienda de prótesis en la clínica")]
        public static void Aplicar()
        {
            GameObject pantalla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallaTienda);
            if (pantalla == null)
            {
                Debug.LogError("[UI] T-1330: falta " + RutaPantallaTienda);
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaMostrador);
            try
            {
                if (raiz.GetComponentInChildren<PantallaTienda>(true) == null)
                {
                    PrefabUtility.InstantiatePrefab(pantalla, raiz.transform);
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaMostrador);
                    Debug.Log("[UI] T-1330: PantallaTienda añadida a MostradorClinica.prefab");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
