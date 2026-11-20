using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1516: los clips de NPC que no se usaban (platicar, sentado en banca, agachado cubriéndose) y los civiles sentados en las bancas PZ01 de la ciudad.
    /// En este orden, cada uno con -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ClipsNpcPlaticarT1516.&lt;Método&gt; (los dos se pueden repetir):
    ///   AplicarConfig   pone los cuatro clips en Resources/ConfigClipsNovaNpc
    ///   ColocarSentados sienta civiles en las bancas PZ01_Banca de Ciudad_Prologo (rehace el grupo de hijos "Sentado_T1516_*")
    /// </summary>
    public static class ClipsNpcPlaticarT1516
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string PrefijoBanca = "PZ01_Banca";
        public const string PrefijoSentado = "Sentado_T1516_";
        public const float SeparacionEnBanca = 0.4f;
        private const int CadaCuantasBancasVacia = 4;
        private const int CadaCuantasBancasDosPersonas = 3;

        /// <summary>Visuales (sin "Visual_") de los que salen los sentados, por turnos.</summary>
        public static readonly string[] Visuales =
        {
            "NP07_Pescador_a", "NP14_Oficinista_a", "NP08_Turista_b", "NP12_Abuela_a", "NP11_Estibador_b", "NP13_Repartidor_a", "NP10_Elotero_a", "NP14_Oficinista_b",
        };

        [MenuItem("Mecha Cyberpunk/T-1516/Clips de plática, sentado y agachado en la config")]
        public static void AplicarConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            if (config == null)
            {
                ClipsNpcNovaT1506.CrearConfig();
                config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            }
            config.ConfigurarNpc(ClipsNpcNovaT1506.CargarClip("NPC_Platicar_1"), ClipsNpcNovaT1506.CargarClip("NPC_Platicar_2"),
                ClipsNpcNovaT1506.CargarClip("NPC_Sentado_Banca"), ClipsNpcNovaT1506.CargarClip("NPC_Agachado_CubriendoCabeza"));
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1516: config de clips NPC: platicar " + (config.Platicar1 != null && config.Platicar2 != null) + ", sentado "
                + (config.SentadoBanca != null) + ", agachado " + (config.Agachado != null));
        }

        [MenuItem("Mecha Cyberpunk/T-1516/Sentar civiles en las bancas PZ01")]
        public static void ColocarSentados()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var prefabs = Visuales
                .Select(v => AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(v)))
                .Where(p => p != null).ToList();
            if (prefabs.Count == 0)
            {
                Debug.LogError("[Personaje] T-1516: no hay visuales de peatón para sentar");
                return;
            }
            List<Transform> bancas = BuscarBancas(escena);
            int sentados = 0;
            for (int n = 0; n < bancas.Count; n++)
            {
                Transform banca = bancas[n];
                foreach (Transform hijo in banca.Cast<Transform>().Where(t => t.name.StartsWith(PrefijoSentado)).ToList())
                {
                    Object.DestroyImmediate(hijo.gameObject);
                }
                if (n % CadaCuantasBancasVacia == CadaCuantasBancasVacia - 1)
                {
                    continue;
                }
                float[] lugares = n % CadaCuantasBancasDosPersonas == 0 ? new[] { -SeparacionEnBanca, SeparacionEnBanca } : new[] { 0f };
                foreach (float lateral in lugares)
                {
                    GameObject prefab = prefabs[sentados % prefabs.Count];
                    var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, banca);
                    npc.name = PrefijoSentado + (sentados + 1);
                    npc.AddComponent<NpcSentadoBanca>().Sentar(banca, lateral);
                    sentados++;
                }
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Personaje] T-1516: " + sentados + " civiles sentados en " + bancas.Count + " bancas PZ01.");
        }

        /// <summary>Las bancas PZ01 de la escena (raíces de prefab activas, en orden de nombre y posición para que el reparto sea siempre el mismo).</summary>
        public static List<Transform> BuscarBancas(UnityEngine.SceneManagement.Scene escena)
        {
            var bancas = new List<Transform>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name.StartsWith(PrefijoBanca) && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    {
                        bancas.Add(t);
                    }
                }
            }
            return bancas.OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
        }
    }
}
