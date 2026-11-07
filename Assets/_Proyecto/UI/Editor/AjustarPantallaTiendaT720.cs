using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-720: con 13 filas en El Mercado el aviso ("COMPRASTE ...") tapaba la lista. Baja el aviso debajo de la lista y sube un poco la lista.
    /// Los mismos valores están en CreadorMostradorTienda. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.AjustarPantallaTiendaT720.Ajustar
    /// </summary>
    public static class AjustarPantallaTiendaT720
    {
        private const float YLista = 0f;
        private const float YAviso = -400f;

        public static void Ajustar()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents("Assets/_Proyecto/UI/Prefabs/PantallaTienda.prefab");
            try
            {
                foreach (RectTransform rect in raiz.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect.name == "ListaFilas")
                    {
                        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, YLista);
                    }
                    else if (rect.name == "Aviso" && rect.GetComponent<Text>() != null)
                    {
                        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, YAviso);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/_Proyecto/UI/Prefabs/PantallaTienda.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Debug.Log("[UI] T-720: aviso de la tienda debajo de la lista.");
        }
    }
}
