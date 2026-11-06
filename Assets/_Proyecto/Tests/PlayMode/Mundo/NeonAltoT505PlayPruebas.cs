using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-505 en juego: la cápsula del jugador, un carro y un mecha alto cruzan Neón Alto sin chocar con T505, y la ruta de la calle a EL HACKER queda libre.</summary>
    public class NeonAltoT505PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T505_NeonAlto";
        private const float RadioJugador = 0.4f;
        private const float AlturaJugador = 1.8f;
        private const float AlturaMecha = 6f;
        private const float RadioMecha = 1.2f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        private static Vector3 PuntoEntrada()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(x => x.name == "Entrada" && x.parent != null && x.parent.name == "Lugar_Casino").position;
        }

        private static void SinChocarConT505(Transform raiz, Vector3 desde, Vector3 hasta, float radio, float alto, string quien)
        {
            Vector3 abajo = desde + Vector3.up * (radio + 0.1f);
            Vector3 arriba = desde + Vector3.up * (alto - radio);
            Vector3 delta = hasta - desde;
            foreach (RaycastHit g in Physics.CapsuleCastAll(abajo, arriba, radio, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                Assert.IsFalse(g.collider.transform.IsChildOf(raiz), quien + " choca con " + g.collider.name);
            }
        }

        [UnityTest]
        public IEnumerator Recorrido_JugadorVaDeLaCalleALaPuertaDelCasinoYHaciaElHacker()
        {
            yield return null;
            Transform raiz = GameObject.Find(NombreRaiz).transform;
            Vector3 e = PuntoEntrada();
            Vector3[] ruta = { new Vector3(47f, 0f, 10f), new Vector3(47f, 0f, e.z), new Vector3(e.x, 0f, e.z), new Vector3(74.5f, 0f, e.z) };
            for (int i = 0; i < ruta.Length - 1; i++)
            {
                SinChocarConT505(raiz, ruta[i], ruta[i + 1], RadioJugador, AlturaJugador, "el jugador (tramo " + i + ")");
            }
        }

        [UnityTest]
        public IEnumerator Calle_UnCarroYUnMechaPasanPorDebajoDeLaAutopista()
        {
            yield return null;
            Transform raiz = GameObject.Find(NombreRaiz).transform;
            foreach (float x in new[] { 44f, 47f, 50f })
            {
                SinChocarConT505(raiz, new Vector3(x, 0f, 9f), new Vector3(x, 0f, 85f), RadioMecha, AlturaMecha, "el mecha en x " + x);
                Vector3 centro = new Vector3(x, 0.9f, 9f);
                foreach (RaycastHit g in Physics.BoxCastAll(centro, new Vector3(1f, 0.75f, 2.2f), Vector3.forward, Quaternion.identity, 76f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Assert.IsFalse(g.collider.transform.IsChildOf(raiz), "el carro en x " + x + " choca con " + g.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator Escena_LucesNiebla_Y_PantallasEstanVivas()
        {
            yield return null;
            GameObject raiz = GameObject.Find(NombreRaiz);
            Assert.IsNotNull(raiz);
            Assert.IsTrue(raiz.activeInHierarchy);
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<Light>().Count(l => l.enabled), 5);
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<MeshRenderer>().Count(r => r.enabled && r.name.StartsWith("Niebla_")), 5);
            // T-541 cambió las torres de bloques de T-505 (con sus quads "Pantalla") por torres del kit con pisos T22 de pantalla.
            int pantallasT505 = raiz.GetComponentsInChildren<Renderer>().Count(r => r.enabled && r.transform.root == raiz.transform && r.name == "Pantalla");
            GameObject torres = GameObject.Find("T541_Torres");
            int pisosPantalla = torres == null ? 0 : torres.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("PisoPantalla"));
            Assert.GreaterOrEqual(pantallasT505 + pisosPantalla, 10, "Pantallas de Neón Alto (T-505 + pisos de pantalla de T-541)");
        }

        [UnityTest]
        public IEnumerator PuertaDelCasino_SigueConSuLugarYSuPunto()
        {
            yield return null;
            Transform lugar = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Lugar_Casino");
            Assert.IsNotNull(lugar);
            Assert.IsNotNull(lugar.Find("Puerta"));
            Assert.IsNotNull(lugar.Find("Entrada"));
            Assert.IsNotNull(lugar.Find("Interior"));
        }
    }
}
