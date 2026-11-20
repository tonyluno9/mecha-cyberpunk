using MechaCyberpunk.IA.APie;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1495: pone <see cref="AnimacionCivilMundo"/> en la raíz de Trafico/Prefabs/Peaton.prefab para que los civiles reaccionen al daño y caigan con animación.
    /// Se puede repetir. Batchmode: -executeMethod MechaCyberpunk.IA.Editor.ConectarCivilesT1495.Aplicar
    /// </summary>
    public static class ConectarCivilesT1495
    {
        public const string RutaPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";

        [MenuItem("Mecha Cyberpunk/IA/T-1495 Civiles con muerte animada")]
        public static void Aplicar()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPeaton);
            try
            {
                if (raiz.GetComponent<AnimacionCivilMundo>() == null)
                {
                    raiz.AddComponent<AnimacionCivilMundo>();
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPeaton);
                Debug.Log("[IA] T-1495: Peaton con AnimacionCivilMundo.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
