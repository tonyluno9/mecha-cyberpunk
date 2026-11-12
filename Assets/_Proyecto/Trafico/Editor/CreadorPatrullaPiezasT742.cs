using MechaCyberpunk.Vehiculos.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-742: la patrulla (Trafico/Prefabs/Patrulla.prefab, copia del carro V05 desde T-707) pasa al V05 armado por piezas: la carrocería sin puertas (T-972: tampoco
    /// piezas de puerta) y las cuatro llantas RU01 que giran y doblan. Conserva en la raíz el <c>PatrullaMundo</c>, las luces de la sirena,
    /// la física, los colliders y el asiento (el robo y el GPS de T-707 siguen igual): solo cambian los visuales y se renombran Asiento y PuntoSubida.
    /// Corre después de <see cref="CreadorCarrosPiezasT742.ArmarTodos"/>. Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPatrullaPiezasT742.Armar
    /// </summary>
    public static class CreadorPatrullaPiezasT742
    {
        public const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";

        [MenuItem("Mecha Cyberpunk/T-742/Patrulla por piezas")]
        public static void Armar()
        {
            AssetDatabase.Refresh();
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPatrulla);
            try
            {
                string informe = CreadorCarrosPiezasT742.Armar(raiz, "V05");
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPatrulla);
                Debug.Log("[Trafico] T-742: Patrulla.prefab por piezas: " + informe);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
