using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-508 en juego: la gente de las banquetas se queda parada con su animador vivo y los adornos no tienen física que frene al jugador.</summary>
    public class CallesLlenasT508PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int GenteMinima = 25;
        private const int CuadrosEspera = 30;
        private const float MovimientoMaximo = 0.05f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Gente_SeQuedaParadaConAnimadorVivo()
        {
            GameObject raiz = GameObject.Find("T508_CallesLlenas");
            Assert.IsNotNull(raiz);
            Animator[] animadores = raiz.GetComponentsInChildren<Animator>(true);
            Assert.GreaterOrEqual(animadores.Length, GenteMinima);
            Vector3[] antes = animadores.Select(a => a.transform.position).ToArray();
            for (int i = 0; i < CuadrosEspera; i++)
            {
                yield return null;
            }
            for (int i = 0; i < animadores.Length; i++)
            {
                Assert.Less(Vector3.Distance(antes[i], animadores[i].transform.position), MovimientoMaximo, "la gente de la banqueta no camina: " + animadores[i].transform.parent.name + "/" + animadores[i].name);
                Assert.IsTrue(animadores[i].enabled);
                Assert.IsNotNull(animadores[i].runtimeAnimatorController);
            }
        }

        [UnityTest]
        public IEnumerator Adornos_NoTienenFisicaQueFreneAlJugador()
        {
            GameObject raiz = GameObject.Find("T508_CallesLlenas");
            Assert.IsNotNull(raiz);
            Assert.IsEmpty(raiz.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(raiz.GetComponentsInChildren<Rigidbody>(true));
            Assert.IsEmpty(raiz.GetComponentsInChildren<CharacterController>(true));
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("Gente_")).Take(10))
            {
                Collider[] tocados = Physics.OverlapCapsule(t.position + Vector3.up * 0.4f, t.position + Vector3.up * 1.4f, 0.3f);
                Assert.IsFalse(tocados.Any(c => c.transform.IsChildOf(raiz.transform)), t.name);
            }
            yield return null;
        }
    }
}
