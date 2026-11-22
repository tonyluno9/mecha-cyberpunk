using System.Collections.Generic;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1645: lista (<see cref="Encontrar"/>) y apaga (<see cref="Aplicar"/>, idempotente) los vehículos de Ciudad_Prologo que están fuera del muro,
    /// como el CarroRobable_17 en (-20, -127.8). Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.VehiculosFueraDelMuroT1645.Aplicar
    /// </summary>
    public static class VehiculosFueraDelMuroT1645
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        /// <summary>Los vehículos activos de la escena fuera del muro (o bajo la altura mínima), con su posición.</summary>
        public static List<GameObject> Encontrar(Scene escena)
        {
            AreaMuro.Definir(new Rect());
            Rect? area = AreaMuro.Actual();
            List<GameObject> fuera = new List<GameObject>();
            HashSet<GameObject> vistos = new HashSet<GameObject>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Component c in raiz.GetComponentsInChildren<Component>(false))
                {
                    if ((c is CarroViejo || c is CarroTrafico || c is PatrullaMundo) && c.gameObject.activeInHierarchy && vistos.Add(c.gameObject)
                        && VigilanteVehiculosMuro.Decidir(c.transform.position, area, false, true) != VigilanteVehiculosMuro.Accion.Nada)
                    {
                        fuera.Add(c.gameObject);
                    }
                }
            }
            return fuera;
        }

        [MenuItem("Mecha Cyberpunk/T-1645/Apagar vehículos fuera del muro")]
        public static void Aplicar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            List<GameObject> fuera = Encontrar(escena);
            foreach (GameObject go in fuera)
            {
                Debug.Log("[T-1645] apagado " + go.name + " en " + go.transform.position);
                go.SetActive(false);
            }
            if (fuera.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
            Debug.Log("[T-1645] " + fuera.Count + " vehículos fuera del muro apagados.");
        }
    }
}
