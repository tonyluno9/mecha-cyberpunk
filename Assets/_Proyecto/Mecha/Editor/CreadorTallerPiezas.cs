using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Pone TallerPiezas (T-168) en MechaEstacionado.prefab, junto al EquipoMecha y el BancoTaller. Idempotente.
    /// Corre después de CreadorEquipoMecha. Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorTallerPiezas.Crear
    /// </summary>
    public static class CreadorTallerPiezas
    {
        private const string RutaVariante = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-168 taller de piezas")]
        public static void Crear()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaVariante);
            try
            {
                if (raiz.GetComponent<EquipoMecha>() == null || raiz.GetComponent<Taller.BancoTaller>() == null)
                {
                    Debug.LogError("[Mecha] Falta EquipoMecha o BancoTaller en MechaEstacionado: corre CreadorEquipoMecha primero.");
                    return;
                }
                if (raiz.GetComponent<Taller.TallerPiezas>() == null)
                {
                    raiz.AddComponent<Taller.TallerPiezas>();
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaVariante);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-168 taller de piezas creado.");
        }
    }
}
