using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-503 en juego: el vapor corre, una cápsula del tamaño del jugador cruza El Tianguis por el carril central sin chocar con T503, y la zona de la pelea con los SKULLS no trae nada de T503.</summary>
    public class TianguisT503PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T503_Tianguis";
        private const float RadioJugador = 0.4f;
        private const float AlturaJugador = 1.8f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vapor_ParticulasVivasEnLosPuestos()
        {
            GameObject raiz = GameObject.Find(NombreRaiz);
            Assert.IsNotNull(raiz);
            yield return new WaitForSeconds(1.5f);
            ParticleSystem[] vapor = raiz.GetComponentsInChildren<ParticleSystem>();
            Assert.Greater(vapor.Length, 0);
            Assert.IsTrue(vapor.Any(p => p.isPlaying && p.particleCount > 0));
        }

        [UnityTest]
        public IEnumerator Recorrido_CapsulaCruzaLaCalleSinChocarConT503()
        {
            yield return null;
            Transform raiz = GameObject.Find(NombreRaiz).transform;
            Vector3 abajo = new Vector3(37f, RadioJugador + 0.1f, 0f);
            Vector3 arriba = abajo + Vector3.up * (AlturaJugador - 2f * RadioJugador);
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, arriba, RadioJugador, Vector3.left, 74f, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit g in golpes)
            {
                Assert.IsFalse(g.collider.transform.IsChildOf(raiz), "choca con " + g.collider.name);
            }
        }

        [UnityTest]
        public IEnumerator ZonaDeLaPeleaConLosSkulls_NoTieneNadaDeT503()
        {
            yield return null;
            Transform raiz = GameObject.Find(NombreRaiz).transform;
            Bounds zona = new Bounds(new Vector3(-34f, 1f, 95f), new Vector3(30f, 4f, 60f));
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
            {
                Assert.IsFalse(r.bounds.Intersects(zona), r.name + " invade la zona de los SKULLS");
            }
        }
    }
}
