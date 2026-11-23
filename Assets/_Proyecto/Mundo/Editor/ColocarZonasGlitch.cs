using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1709 (T74): pone zonas de glitch en la escena abierta. <see cref="Colocar"/> acepta la ruta de un prefab (.prefab: lo instancia en la posición) o el
    /// nombre/ruta de un objeto ya en la escena (GameObject.Find: le agrega ZonaGlitch y, si se da posición, lo mueve ahí). Idempotente: no duplica el componente.
    /// Se usa desde una sesión de escena con -executeMethod o desde el menú; guardar la escena queda a quien llama.
    /// </summary>
    public static class ColocarZonasGlitch
    {
        /// <summary>Devuelve el objeto con ZonaGlitch o null si no se encontró.</summary>
        public static GameObject Colocar(string ruta, Vector3 posicion, bool mover = true)
        {
            GameObject objeto;
            if (ruta.EndsWith(".prefab"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null)
                {
                    Debug.LogError("[T-1709] No existe el prefab " + ruta);
                    return null;
                }
                objeto = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                objeto.transform.position = posicion;
            }
            else
            {
                objeto = GameObject.Find(ruta);
                if (objeto == null)
                {
                    Debug.LogError("[T-1709] No encuentro el objeto " + ruta + " en la escena abierta.");
                    return null;
                }
                if (mover)
                {
                    objeto.transform.position = posicion;
                }
            }
            if (objeto.GetComponent<ZonaGlitch>() == null)
            {
                Undo.AddComponent<ZonaGlitch>(objeto);
            }
            EditorUtility.SetDirty(objeto);
            Debug.Log("[T-1709] Zona de glitch en " + objeto.name + " (" + objeto.transform.position + ").");
            return objeto;
        }
    }
}
