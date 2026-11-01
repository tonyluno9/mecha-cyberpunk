using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-502 en juego: el vapor de las ventilas corre y una cápsula del tamaño del jugador cruza el callejón sin chocar con nada de T502.</summary>
    public class CallejonT502PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T502_Callejon";
        private const float RadioJugador = 0.4f;
        private const float AlturaJugador = 1.8f;
        private const float ZRuta = -59f;
        private const float XEntrada = -58f;
        private const float XFondo = -85f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vapor_ParticulasVivasEnLasVentilas()
        {
            GameObject raiz = GameObject.Find(NombreRaiz);
            Assert.IsNotNull(raiz);
            yield return new WaitForSeconds(1.5f);
            ParticleSystem[] vapor = raiz.GetComponentsInChildren<ParticleSystem>();
            Assert.Greater(vapor.Length, 0);
            Assert.IsTrue(vapor.Any(p => p.isPlaying && p.particleCount > 0));
        }

        [UnityTest]
        public IEnumerator Recorrido_CapsulaDelJugadorCruzaSinChocarConT502()
        {
            yield return null;
            Vector3 abajo = new Vector3(XEntrada, RadioJugador + 0.1f, ZRuta);
            Vector3 arriba = abajo + Vector3.up * (AlturaJugador - 2f * RadioJugador);
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, arriba, RadioJugador, Vector3.left, XEntrada - XFondo, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit g in golpes)
            {
                Assert.IsFalse(g.collider.transform.IsChildOf(GameObject.Find(NombreRaiz).transform), "choca con " + g.collider.name);
            }
        }
    }
}
