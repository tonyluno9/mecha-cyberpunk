using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-624: el prefab del jefe de los SKULLS usa Override_Jefe (Jefe_Idle, Jefe_Caminar, Jefe_Burla, Jefe_Ataque, Jefe_Muerte), y los visuales de SKULLS,
    /// del jefe y de los guardias de Nova ajustan la rapidez de su caminar y correr a los clips propios (medidos del propio clip). Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ConectarEnemigosT624.Conectar
    /// </summary>
    public static class ConectarEnemigosT624
    {
        [MenuItem("Mecha Cyberpunk/Personajes/T-624 Conectar enemigos")]
        public static void Conectar()
        {
            AjustarPrefab(CreadorVisualPersonaje.RutaPrefab("man"), "Skull_Caminar", "Loco_Correr", null);
            AjustarPrefab(CreadorVisualPersonaje.RutaPrefab("SkullHombre"), "Skull_Caminar", "Loco_Correr", null);
            AjustarPrefab(CreadorVisualPersonaje.RutaPrefab("GuardiaNova"), "Nova_Caminar", "Nova_Correr", null);
            AjustarPrefab(ConectarVisualesT510.RutaJefeSkulls, "Jefe_Caminar", "Loco_Correr", "Jefe");
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-624: enemigos conectados.");
        }

        private static void AjustarPrefab(string ruta, string clipCaminar, string clipCorrer, string papel)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                VisualPersonaje visual = contenido.GetComponentInChildren<VisualPersonaje>(true);
                if (visual == null)
                {
                    Debug.LogError("[Personaje] T-624: sin VisualPersonaje en " + ruta);
                    return;
                }
                if (papel != null)
                {
                    Animator animador = visual.GetComponentInChildren<Animator>(true);
                    animador.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride(papel));
                }
                visual.ConfigurarClips(ConectarControladorBase.Medir(clipCaminar), ConectarControladorBase.Medir(clipCorrer), ReglasAnimacion.RapidezMaximaLocomocion);
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }
    }
}
