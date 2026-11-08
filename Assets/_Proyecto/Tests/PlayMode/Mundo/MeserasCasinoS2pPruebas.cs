using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-892 en la ciudad del juego: las meseras R01 caminan, cambian de lugar y miden como una persona ya animadas (BakeMesh, no bounds).</summary>
    public class MeserasCasinoS2pPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "S2p_Meseras";
        private const float SegundosDeRonda = 4f; // la tercera arranca a los 2.6 s
        private const float MovimientoMinimo = 0.5f;
        private const float AlturaMinima = 1.6f;
        private const float AlturaMaxima = 1.95f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        private static Transform[] Meseras()
        {
            GameObject raiz = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == NombreRaiz);
            Assert.IsNotNull(raiz, "Falta " + NombreRaiz);
            return raiz.transform.Cast<Transform>().ToArray();
        }

        [UnityTest]
        public IEnumerator Meseras_CaminanConSuAnimadorHumanoid()
        {
            Transform[] meseras = Meseras();
            Assert.GreaterOrEqual(meseras.Length, 2);
            Assert.LessOrEqual(meseras.Length, 3);
            Vector3[] antes = meseras.Select(m => m.position).ToArray();
            yield return new WaitForSeconds(SegundosDeRonda);
            for (int i = 0; i < meseras.Length; i++)
            {
                Animator a = meseras[i].GetComponent<Animator>();
                Assert.IsTrue(a.isHuman, meseras[i].name + " es Humanoid");
                Assert.IsNotNull(a.runtimeAnimatorController);
                float recorrido = Vector3.Distance(antes[i], meseras[i].position);
                Assert.Greater(recorrido, MovimientoMinimo, meseras[i].name + " camina");
            }
        }

        [UnityTest]
        public IEnumerator Meseras_MidenComoUnaPersonaYaAnimadas()
        {
            Transform m = Meseras()[0];
            yield return new WaitForSeconds(SegundosDeRonda);
            SkinnedMeshRenderer sm = m.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh horneada = new Mesh();
            sm.BakeMesh(horneada, true);
            float y0 = float.MaxValue, y1 = float.MinValue;
            foreach (Vector3 v in horneada.vertices)
            {
                float y = sm.transform.TransformPoint(v).y;
                y0 = Mathf.Min(y0, y);
                y1 = Mathf.Max(y1, y);
            }
            Object.Destroy(horneada);
            float alto = y1 - y0;
            Debug.Log("[Casino] Mesera animada mide " + alto.ToString("F2") + " m; pies en y = " + y0.ToString("F2"));
            Assert.That(alto, Is.InRange(AlturaMinima, AlturaMaxima), "altura animada");
            Assert.That(y0, Is.InRange(0.1f, 0.35f), "con los pies en el piso del casino");
        }
    }
}
