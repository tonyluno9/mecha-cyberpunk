using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-1516: un civil sentado en la banca PZ01 juega el clip de sentado y queda sobre el asiento, sin flotar ni hundirse.</summary>
    public class NpcSentadoBancaT1516Pruebas
    {
        private const string RutaBanca = "Assets/Arte/Zonas/PZ/PZ01_Banca.prefab";
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_NP07_Pescador_a.prefab";
        private const float AlturaBanca = 0.2f;

        private GameObject banca;
        private GameObject npc;

        [TearDown]
        public void Limpiar()
        {
            if (banca != null) { Object.Destroy(banca); }
            if (npc != null) { Object.Destroy(npc); }
        }

        [UnityTest]
        public IEnumerator Sentado_QuedaSobreElAsiento_ConLosPiesEnElPiso_YJuegaElClip()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            Assert.IsNotNull(config, "falta Resources/ConfigClipsNovaNpc");
            Assert.IsNotNull(config.SentadoBanca, "falta el clip de sentado en la config (ClipsNpcPlaticarT1516.AplicarConfig)");
            banca = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBanca));
            banca.transform.SetPositionAndRotation(new Vector3(0f, AlturaBanca, 0f), Quaternion.Euler(0f, 30f, 0f));
            npc = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual));
            NpcSentadoBanca sentado = npc.AddComponent<NpcSentadoBanca>();
            sentado.Sentar(banca.transform, 0f);
            for (int i = 0; i < 8; i++)
            {
                yield return null;
            }
            ReproductorClipUnico reproductor = npc.GetComponent<ReproductorClipUnico>();
            Assert.IsNotNull(reproductor, "el sentado agrega su reproductor");
            Assert.AreEqual(config.SentadoBanca, reproductor.ClipActual, "juega NPC_Sentado_Banca");
            Assert.IsTrue(reproductor.Repite, "en bucle");

            Animator animador = npc.GetComponentInChildren<Animator>();
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            Transform pie = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
            Assert.IsNotNull(cadera, "avatar humanoide con cadera");
            float sobreElAsiento = cadera.position.y - (AlturaBanca + NpcSentadoBanca.AlturaAsientoBanca);
            Assert.That(sobreElAsiento, Is.InRange(-0.05f, 0.45f), "la cadera queda sobre el asiento (ni hundida ni flotando): " + sobreElAsiento);
            Assert.That(pie.position.y - AlturaBanca, Is.InRange(-0.12f, 0.3f), "los pies cerca del piso de la banca");
            Vector3 centro = banca.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(cadera.position.x, cadera.position.z), new Vector2(centro.x, centro.z)), 0.6f, "la cadera está sobre la banca");
        }
    }
}
