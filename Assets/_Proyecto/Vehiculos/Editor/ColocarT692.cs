using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-692: la patrulla recibe daño de explosiones, balas y fuego como cualquier carro. Le pone <see cref="DanioCarro"/> y una zona de daño
    /// (trigger, para no bloquear a nadie) del tamaño de su carrocería a Trafico/Prefabs/Patrulla.prefab. Se puede correr varias veces.
    /// Uso: Unity -batchmode -executeMethod MechaCyberpunk.Vehiculos.Editor.ColocarT692.Aplicar
    /// </summary>
    public static class ColocarT692
    {
        public const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        public const string NombreZona = "ZonaDanio";

        public static void Aplicar()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPatrulla);
            try
            {
                if (raiz.GetComponent<DanioCarro>() == null)
                {
                    raiz.AddComponent<DanioCarro>();
                }
                Transform zona = raiz.transform.Find(NombreZona);
                if (zona == null)
                {
                    zona = new GameObject(NombreZona).transform;
                    zona.SetParent(raiz.transform, false);
                }
                BoxCollider caja = zona.GetComponent<BoxCollider>();
                if (caja == null)
                {
                    caja = zona.gameObject.AddComponent<BoxCollider>();
                }
                caja.isTrigger = true;
                Bounds limites = new Bounds(Vector3.zero, Vector3.zero);
                bool hay = false;
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer)
                    {
                        continue;
                    }
                    Bounds local = new Bounds(raiz.transform.InverseTransformPoint(r.bounds.center), Vector3.zero);
                    local.Encapsulate(raiz.transform.InverseTransformPoint(r.bounds.min));
                    local.Encapsulate(raiz.transform.InverseTransformPoint(r.bounds.max));
                    if (!hay)
                    {
                        limites = local;
                        hay = true;
                    }
                    else
                    {
                        limites.Encapsulate(local);
                    }
                }
                caja.center = hay ? limites.center : new Vector3(0f, 0.8f, 0f);
                caja.size = hay ? limites.size : new Vector3(2f, 1.6f, 4.5f);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPatrulla);
                Debug.Log("[Vehiculos] T-692: Patrulla.prefab con DanioCarro y zona de daño " + caja.size);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
