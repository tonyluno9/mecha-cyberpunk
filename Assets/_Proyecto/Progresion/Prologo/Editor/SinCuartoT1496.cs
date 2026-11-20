using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-1496 (playtest 19-nov): el juego empieza sin cuarto y el fondo del menú sin niebla pesada. Idempotente, no borra assets.
    /// Ciudad_Prologo: apaga Cuarto_Habitacion (sigue en la escena, solo no se usa) y pasa la aparición Casa_Cama al frente de la puerta de la
    /// calle (la pose de Casa_Puerta): ahí empieza el jugador y ahí lo deja la clínica del Doc y las reglas de arresto.
    /// Menu_Calles: baja la niebla de RenderSettings y apaga los objetos Niebla*, solo en el menú (la niebla del juego no cambia).
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.SinCuartoT1496.Todo (o Ciudad / Menu por separado).
    /// </summary>
    public static class SinCuartoT1496
    {
        public const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaMenu = "Assets/_Proyecto/Escenas/Menu_Calles.unity";
        public const string NombreCuarto = "Cuarto_Habitacion";
        public const string IdCama = "Casa_Cama";
        public const string IdPuerta = "Casa_Puerta";
        public const float DensidadNieblaMenu = 0.003f;

        [MenuItem("Mecha Cyberpunk/T-1496/Todo")]
        public static void Todo()
        {
            Ciudad();
            Menu();
        }

        [MenuItem("Mecha Cyberpunk/T-1496/Ciudad_Prologo sin cuarto")]
        public static void Ciudad()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaCiudad, OpenSceneMode.Single);
            Transform[] todas = Todas(escena);
            Transform cuarto = todas.FirstOrDefault(t => t.name == NombreCuarto);
            if (cuarto == null)
            {
                Debug.LogError("[T-1496] No encontré " + NombreCuarto + " en Ciudad_Prologo.");
            }
            else
            {
                cuarto.gameObject.SetActive(false);
            }
            Transform puerta = todas.FirstOrDefault(t => t.name == IdPuerta);
            Transform cama = todas.FirstOrDefault(t => t.name == IdCama);
            if (puerta == null || cama == null)
            {
                Debug.LogError("[T-1496] Faltan las apariciones " + IdPuerta + " o " + IdCama + ".");
            }
            else
            {
                cama.SetPositionAndRotation(puerta.position, puerta.rotation);
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        [MenuItem("Mecha Cyberpunk/T-1496/Menu_Calles sin niebla")]
        public static void Menu()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Single);
            RenderSettings.fog = true;
            RenderSettings.fogDensity = DensidadNieblaMenu;
            int apagados = 0;
            foreach (Transform t in Todas(escena).Where(t => t.name.StartsWith("Niebla")).ToArray())
            {
                if (t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    apagados++;
                }
            }
            Debug.Log("[T-1496] Menu_Calles: niebla " + DensidadNieblaMenu + ", objetos Niebla* apagados: " + apagados);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        private static Transform[] Todas(Scene escena)
        {
            return escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        }
    }
}
