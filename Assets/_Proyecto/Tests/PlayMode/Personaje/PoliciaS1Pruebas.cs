using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-680: el policía a pie (C01) se ve con su uniforme, mide lo normal y se distingue de un SKULL. Capturas con CAPTURAR_S1=1.</summary>
    public class PoliciaS1Pruebas
    {
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private GameObject piso;
        private GameObject policia;
        private GameObject skull;

        [TearDown]
        public void Limpiar()
        {
            Object.Destroy(piso);
            Object.Destroy(policia);
            Object.Destroy(skull);
        }

        private static float AlturaAnimada(GameObject raiz)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            Mesh malla = new Mesh();
            foreach (SkinnedMeshRenderer r in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!r.enabled || r.sharedMesh == null) continue;
                r.BakeMesh(malla, true);
                foreach (Vector3 v in malla.vertices)
                {
                    float y = r.transform.TransformPoint(v).y;
                    min = Mathf.Min(min, y);
                    max = Mathf.Max(max, y);
                }
            }
            Object.Destroy(malla);
            return max - min;
        }

        [UnityTest]
        public IEnumerator Policia_SeVeConUniforme_MideComoPersona_ConPistola()
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(60f, 1f, 60f);
            GameObject sol = new GameObject("SolPrueba");
            sol.AddComponent<Light>().type = LightType.Directional;
            sol.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            policia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), new Vector3(-0.8f, 0.05f, 0f), Quaternion.Euler(0f, 180f, 0f));
            skull = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull), new Vector3(0.8f, 0.05f, 0f), Quaternion.Euler(0f, 180f, 0f));
            yield return new WaitForSeconds(0.15f);
            SkinnedMeshRenderer cuerpo = policia.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsTrue(cuerpo.name.Contains("df8dc0e4"), "Es el modelo C01");
            foreach (Animator animado in policia.GetComponentsInChildren<Animator>(true)) animado.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return new WaitForSeconds(0.5f);
            // T-746: se mide la malla animada horneada (BakeMesh); los bounds del renderer de piel no son la altura real.
            float alto = AlturaAnimada(policia);
            Assert.That(alto, Is.InRange(1.74f, 1.86f), "Mide 1.80 m animado (mide " + alto + ")");
            Assert.IsNotNull(policia.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "W01"), "Pistola en la mano");
            Animator a = policia.GetComponentInChildren<Animator>();
            Assert.IsTrue(a.isHuman && a.avatar.isValid, "Humanoide con avatar propio");
            Assert.IsNotNull(a.runtimeAnimatorController, "Con las animaciones humanoides del proyecto");
            Transform p = policia.transform;
            Vector3 pie = Vector3.ProjectOnPlane(a.GetBoneTransform(HumanBodyBones.LeftToes).position - a.GetBoneTransform(HumanBodyBones.LeftFoot).position, Vector3.up);
            Animator aS = skull.GetComponentInChildren<Animator>();
            Vector3 pieS = Vector3.ProjectOnPlane(aS.GetBoneTransform(HumanBodyBones.LeftToes).position - aS.GetBoneTransform(HumanBodyBones.LeftFoot).position, Vector3.up);
            Assert.AreEqual(Mathf.Sign(Vector3.Dot(pieS.normalized, skull.transform.forward)), Mathf.Sign(Vector3.Dot(pie.normalized, p.forward)), "Policía y SKULL miran igual respecto a su raíz: " + Vector3.Dot(pieS.normalized, skull.transform.forward) + " vs " + Vector3.Dot(pie.normalized, p.forward));
            CapturaS1.Foto("s1t-T-680-policia-y-skull", p, new Vector3(0.2f, 1.3f, 3.2f), new Vector3(0.2f, 0.95f, 0f), 40f, 900, 700);
            CapturaS1.Foto("s1t-T-680-policia-frente", p, new Vector3(0f, 1.3f, 2.2f), new Vector3(0f, 0.95f, 0f), 35f, 700, 700);
            CapturaS1.Foto("s1t-T-680-policia-atras", p, new Vector3(0f, 1.3f, -2.2f), new Vector3(0f, 0.95f, 0f), 35f, 700, 700);
            CapturaS1.Foto("s1t-T-680-policia-lado", p, new Vector3(2.2f, 1.2f, 0f), new Vector3(0f, 0.95f, 0f), 35f, 700, 700);
        }
    }
}
