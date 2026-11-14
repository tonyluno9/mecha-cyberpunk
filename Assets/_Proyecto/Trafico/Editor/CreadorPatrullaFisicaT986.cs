using MechaCyberpunk.Trafico.Persecucion;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-986: la patrulla deja de ser cinemática: Patrulla.prefab queda con el Rigidbody de un carro de verdad (no cinemático, con interpolación), igual que el
    /// carro del jugador, y un conductor IA (<see cref="PatrullaMundo"/>) le da la entrada. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorPatrullaFisicaT986.Crear
    /// </summary>
    public static class CreadorPatrullaFisicaT986
    {
        [MenuItem("Mecha Cyberpunk/T-986/Patrulla con física de carro")]
        public static void Crear()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(CreadorPatrullaT707.RutaPatrulla);
            try
            {
                Rigidbody cuerpo = raiz.GetComponent<Rigidbody>();
                if (cuerpo == null || raiz.GetComponent<MechaCyberpunk.Vehiculos.Carro.CarroViejo>() == null)
                {
                    Debug.LogError("[Trafico] T-986: Patrulla.prefab no es un carro (corre CreadorPatrullaT707 antes).");
                    return;
                }
                cuerpo.isKinematic = false;
                cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
                PrefabUtility.SaveAsPrefabAsset(raiz, CreadorPatrullaT707.RutaPatrulla);
                Debug.Log("[Trafico] T-986: Patrulla.prefab con Rigidbody no cinemático (masa " + cuerpo.mass + " kg, interpolación " + cuerpo.interpolation + ").");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
