using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-583: la estación del mecha del taller (T02_EstacionExo_Visual_T250) no tenía collider y el jugador la atravesaba. Le pone un BoxCollider del tamaño
    /// de su malla en Barrio.prefab. Idempotente. Si Barrio.prefab se regenera, hay que volver a correr este método.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.ColocarT583.Colocar
    /// </summary>
    public static class ColocarT583
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string NombreEstacion = "T02_EstacionExo_Visual_T250";

        [MenuItem("Mecha Cyberpunk/T-583/Collider de la estación del taller")]
        public static void Colocar()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaBarrio);
            try
            {
                int cambiados = 0;
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name == NombreEstacion))
                {
                    if (t.GetComponentInChildren<Collider>(true) != null)
                    {
                        continue;
                    }
                    MeshFilter[] mallas = t.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null).ToArray();
                    if (mallas.Length == 0)
                    {
                        continue;
                    }
                    Bounds local = default;
                    bool primero = true;
                    foreach (MeshFilter m in mallas)
                    {
                        Bounds b = m.sharedMesh.bounds;
                        for (int i = 0; i < 8; i++)
                        {
                            Vector3 esquina = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                            Vector3 enT = t.InverseTransformPoint(m.transform.TransformPoint(esquina));
                            if (primero)
                            {
                                local = new Bounds(enT, Vector3.zero);
                                primero = false;
                            }
                            else
                            {
                                local.Encapsulate(enT);
                            }
                        }
                    }
                    BoxCollider caja = t.gameObject.AddComponent<BoxCollider>();
                    caja.center = local.center;
                    caja.size = local.size;
                    cambiados++;
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaBarrio);
                Debug.Log("[Mundo] T-583: collider en " + cambiados + " estación(es) del taller.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
