using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1632 (T18): un civil sentado con el clip de sentado de PJ01 queda con la cadera sobre el asiento y los pies cerca del piso, dos en la misma banca se miran un poco,
    /// y al oír un disparo cercano (EventosRuido.DisparoConRadio) se levanta y se aleja corriendo; lejos del disparo no se mueve.
    /// </summary>
    public class NpcSentadoT1632Pruebas
    {
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_NP07_Pescador_a.prefab";
        private const string CarpetaClips = "Assets/Arte/Personajes/PJ01/Clips/";

        private GameObject piso;
        private GameObject banca;
        private GameObject npcA;
        private GameObject npcB;

        [SetUp]
        public void Preparar()
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "Piso_T1632";
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(120f, 1f, 120f);
            banca = new GameObject("Banca_T1632");
            banca.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 30f, 0f));
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject g in new[] { piso, banca, npcA, npcB })
            {
                if (g != null)
                {
                    Object.Destroy(g);
                }
            }
        }

        private static AnimationClip Clip(string nombre)
        {
            return AssetDatabase.LoadAllAssetsAtPath(CarpetaClips + nombre + ".fbx").OfType<AnimationClip>().FirstOrDefault(c => c.name == nombre);
        }

        private NpcSentado Sentar(out GameObject npc, float lateral, float desfase)
        {
            AnimationClip sentado = Clip("PJ01_Sentado");
            Assert.IsNotNull(sentado, "falta el clip PJ01_Sentado");
            npc = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual));
            NpcSentado s = npc.AddComponent<NpcSentado>();
            s.Configurar(banca.transform, lateral, true, null, sentado, Clip("NPC_Asustarse"), Clip("NPC_Huir_Corriendo"), desfase, 1f, 0f);
            return s;
        }

        private static IEnumerator Cuadros(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Sentado_CaderaSobreElAsiento_PiesEnElPiso_YDeFrente()
        {
            NpcSentado s = Sentar(out npcA, 0f, 0.3f);
            yield return Cuadros(8);
            Assert.IsTrue(s.PosicionCadera.HasValue, "avatar humanoide con cadera");
            Assert.Less(s.DiferenciaCadera, 0.10f, "la cadera queda a menos de 10 cm del punto sobre el asiento: " + s.DiferenciaCadera);
            float? pie = s.AlturaPieMasBajo;
            Assert.IsTrue(pie.HasValue);
            Assert.That(pie.Value, Is.InRange(-0.15f, 0.30f), "los pies cerca del piso: " + pie.Value);
            Assert.Greater(Vector3.Dot(npcA.transform.forward, -banca.transform.forward), 0.99f, "mira al frente de la banca (-forward)");
            Assert.AreEqual(NpcSentado.Fase.Sentado, s.FaseActual);
        }

        [UnityTest]
        public IEnumerator DosEnLaMismaBanca_SeMiranUnPoco()
        {
            NpcSentado a = Sentar(out npcA, -0.4f, 0.1f);
            NpcSentado b = Sentar(out npcB, 0.4f, 0.6f);
            a.Acompanar(b);
            b.Acompanar(a);
            yield return Cuadros(8);
            float entre = Vector3.Angle(npcA.transform.forward, npcB.transform.forward);
            Assert.That(entre, Is.InRange(30f, 70f), "cada uno gira unos 25 grados hacia el otro: " + entre);
            Vector3 deAaB = (npcB.transform.position - npcA.transform.position).normalized;
            Vector3 frente = -banca.transform.forward;
            Assert.Greater(Vector3.Dot(npcA.transform.forward, deAaB), Vector3.Dot(frente, deAaB), "A gira hacia B");
            Assert.Less(a.DiferenciaCadera, 0.10f);
            Assert.Less(b.DiferenciaCadera, 0.10f);
        }

        [UnityTest]
        public IEnumerator DisparoCerca_SeLevantaYSeAleja()
        {
            NpcSentado s = Sentar(out npcA, 0f, 0.3f);
            yield return Cuadros(8);
            Vector3 inicio = npcA.transform.position;
            Vector3 disparo = new Vector3(8f, 1.5f, 0f);
            EventosRuido.PublicarDisparoConRadio(disparo, 25f);
            float t = 0f;
            while (s.FaseActual == NpcSentado.Fase.Sentado && t < 2f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreNotEqual(NpcSentado.Fase.Sentado, s.FaseActual, "dejó de estar sentado al oír el disparo");
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(NpcSentado.Fase.Huyendo, s.FaseActual, "corre");
            Vector3 ahora = npcA.transform.position;
            float antes = Vector3.Distance(new Vector3(inicio.x, 0f, inicio.z), new Vector3(disparo.x, 0f, disparo.z));
            float despues = Vector3.Distance(new Vector3(ahora.x, 0f, ahora.z), new Vector3(disparo.x, 0f, disparo.z));
            Assert.Greater(despues, antes + 3f, "se alejó del disparo: de " + antes + " a " + despues);
            Assert.Less(ahora.y, 0.3f, "ya está de pie en el piso, no flotando sobre la banca: " + ahora.y);
        }

        [UnityTest]
        public IEnumerator DisparoLejos_SeQuedaSentado()
        {
            NpcSentado s = Sentar(out npcA, 0f, 0.3f);
            yield return Cuadros(8);
            EventosRuido.PublicarDisparoConRadio(new Vector3(50f, 1.5f, 0f), 25f);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(NpcSentado.Fase.Sentado, s.FaseActual, "fuera del radio de oído no reacciona");
        }
    }
}
