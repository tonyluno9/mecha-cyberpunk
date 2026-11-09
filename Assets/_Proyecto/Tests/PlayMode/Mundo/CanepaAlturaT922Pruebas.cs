using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-922 (playtest 9-nov): Canepa, ya en la ciudad en juego, mide 1.80 m animado (medido con BakeMesh; los bounds de la pose de reposo mienten).</summary>
    public class CanepaAlturaT922Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float Esperada = 1.8f;
        private const float Tolerancia = 0.05f;

        /// <summary>Altura (m) de la malla animada horneada: de la planta del pie al punto más alto de las mallas con piel.</summary>
        private static float AlturaAnimada(GameObject raiz)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            Mesh malla = new Mesh();
            foreach (SkinnedMeshRenderer r in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!r.enabled || r.sharedMesh == null)
                {
                    continue;
                }
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
        public IEnumerator Canepa_EnLaCiudad_Mide180()
        {
            yield return Medir(RutaEscena);
        }

        private static IEnumerator Medir(string ruta)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
            GameObject canepa = GameObject.Find("GTA_T417/Apoyo_CANEPA");
            Assert.IsNotNull(canepa, "Canepa está en la ciudad");
            foreach (Animator a in canepa.GetComponentsInChildren<Animator>(true))
            {
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            float fin = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            float altura = AlturaAnimada(canepa);
            Debug.Log("[T-922] altura animada de Canepa: " + altura + " escala del modelo: " + canepa.transform.lossyScale);
            Assert.AreEqual(Esperada, altura, Tolerancia, "Canepa animado mide 1.80 m (mide " + altura + ")");
        }
    }
}
