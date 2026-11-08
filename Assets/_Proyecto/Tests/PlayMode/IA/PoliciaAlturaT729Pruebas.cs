using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-729: los policías a pie miden 1.80 m animados, medidos con BakeMesh (no con los bounds de la pose de reposo).</summary>
    public class PoliciaAlturaT729Pruebas
    {
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        public const float AlturaEsperada = 1.8f;
        public const float Tolerancia = 0.06f;

        /// <summary>Altura (m) de la malla animada horneada: de la planta del pie al punto más alto de las mallas con piel.</summary>
        public static float AlturaAnimada(GameObject raiz)
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
        public IEnumerator Policia_AnimadoDePie_Mide180()
        {
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(3000f, -0.5f, 3000f);
            piso.transform.localScale = new Vector3(20f, 1f, 20f);
            GameObject policia = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), new Vector3(3000f, 0.05f, 3000f), Quaternion.identity);
            try
            {
                foreach (Animator a in policia.GetComponentsInChildren<Animator>(true)) a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                float fin = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < fin) yield return null;
                float altura = AlturaAnimada(policia);
                Debug.Log("[T-729] altura animada del policía: " + altura);
                Assert.AreEqual(AlturaEsperada, altura, Tolerancia, "El policía animado mide 1.80 m (mide " + altura + ")");
            }
            finally
            {
                Object.Destroy(policia);
                Object.Destroy(piso);
            }
        }
    }
}
