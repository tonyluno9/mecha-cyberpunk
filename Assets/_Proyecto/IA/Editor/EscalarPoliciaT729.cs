using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-729: los policías a pie salían diminutos (1.23 m animados, medidos con BakeMesh en PoliciaAlturaT729Pruebas). Escala el modelo de Visual_Policia (Giro/Modelo) para que
    /// mida 1.80 m animado, como el jugador (EscalaJugadorT670Pruebas). El factor sale de la medida animada de partida y se aplica una sola vez sobre la escala que ya tenga el modelo.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.EscalarPoliciaT729.Aplicar
    /// </summary>
    public static class EscalarPoliciaT729
    {
        public const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_Policia.prefab";
        public const float AlturaObjetivo = 1.80f;
        public const float AlturaMedida = 1.229f;

        [MenuItem("Mecha Cyberpunk/T-729/Escalar policia a 1.80")]
        public static void Aplicar()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaVisual);
            try
            {
                Transform modelo = raiz.transform.Find("Giro/Modelo");
                if (modelo == null)
                {
                    Debug.LogError("[IA] T-729: Visual_Policia no tiene Giro/Modelo.");
                    return;
                }
                if (modelo.localScale.x > 1.2f)
                {
                    Debug.Log("[IA] T-729: Visual_Policia ya está escalado (" + modelo.localScale.x.ToString("F3") + "); no se vuelve a escalar.");
                    return;
                }
                float factor = AlturaObjetivo / AlturaMedida;
                modelo.localScale = modelo.localScale * factor;
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaVisual);
                Debug.Log("[IA] T-729: Visual_Policia escala " + modelo.localScale.x.ToString("F3") + " (factor " + factor.ToString("F3") + ").");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
