using MechaCyberpunk.Arena.Jaula;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arena.Editor
{
    /// <summary>
    /// T-044: coloca MechaJugador y FlujoPelea (prefabs de Tony, sin overrides) en Clandestino.unity y quita CamaraVistaArena.
    /// Orden: es el ultimo (despues de CreadorArena); usa CreadorTodo.Crear. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Arena.Editor.CreadorPeleaClandestino.Crear
    /// </summary>
    public static class CreadorPeleaClandestino
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Clandestino.unity";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-044")]
        public static void Crear()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            ArenaPelea arena = Object.FindFirstObjectByType<ArenaPelea>();
            if (arena == null)
            {
                throw new System.InvalidOperationException("Clandestino.unity no tiene la jaula (ArenaPelea).");
            }

            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (raiz.name == "CamaraVistaArena" || PrefabNombre(raiz, RutaJugador) || PrefabNombre(raiz, RutaFlujo))
                {
                    Object.DestroyImmediate(raiz);
                }
            }

            Pose inicio = arena.InicioJugador;
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), escena);
            jugador.transform.SetPositionAndRotation(inicio.position, inicio.rotation);

            GameObject flujo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujo), escena);
            flujo.transform.SetPositionAndRotation(arena.Centro, Quaternion.identity);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Arena] T-044 creada.");
        }

        private static bool PrefabNombre(GameObject go, string ruta)
        {
            return PrefabUtility.IsAnyPrefabInstanceRoot(go) && PrefabUtility.GetCorrespondingObjectFromSource(go) != null
                && AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(go)) == ruta;
        }
    }
}
