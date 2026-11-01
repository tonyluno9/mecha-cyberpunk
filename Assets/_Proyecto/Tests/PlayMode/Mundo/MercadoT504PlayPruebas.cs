using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-504 en juego: una cápsula del tamaño del jugador llega de la boca del callejón al punto de la puerta de El Mercado sin chocar con T504, y las banderas y luces están vivas.</summary>
    public class MercadoT504PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T504_Mercado";
        private const float RadioJugador = 0.4f;
        private const float AlturaJugador = 1.8f;
        private const float XCarril = -22.9f;

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
                .First(x => x.name == "Entrada" && x.parent != null && x.parent.name == "Lugar_MercadoNegro").position;
        }

        [UnityTest]
        public IEnumerator Recorrido_CapsulaVaDeLaBocaAlPortonSinChocarConT504()
        {
            yield return null;
            Transform raiz = GameObject.Find(NombreRaiz).transform;
            Vector3 e = PuntoEntrada();
            Vector3[] ruta = { new Vector3(XCarril, 0f, 9.5f), new Vector3(XCarril, 0f, 21.5f), new Vector3(e.x, 0f, e.z) };
            for (int i = 0; i < ruta.Length - 1; i++)
            {
                Vector3 abajo = ruta[i] + Vector3.up * (RadioJugador + 0.1f);
                Vector3 arriba = abajo + Vector3.up * (AlturaJugador - 2f * RadioJugador);
                Vector3 delta = ruta[i + 1] - ruta[i];
                RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, arriba, RadioJugador, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit g in golpes)
                {
                    Assert.IsFalse(g.collider.transform.IsChildOf(raiz), "tramo " + i + " choca con " + g.collider.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator PapelPicadoYLucesNaranjas_EstanActivosEnJuego()
        {
            yield return null;
            GameObject raiz = GameObject.Find(NombreRaiz);
            Assert.IsNotNull(raiz);
            Assert.IsTrue(raiz.activeInHierarchy);
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<MeshRenderer>().Count(r => r.name.StartsWith("Papel_") && r.enabled), 40);
            Assert.IsTrue(raiz.GetComponentsInChildren<Light>().Any(l => l.enabled && l.color.r > 0.9f && l.color.b < 0.3f));
        }

        [UnityTest]
        public IEnumerator PuertaDeElMercado_SigueConSuLugarYSuPunto()
        {
            yield return null;
            Transform lugar = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Lugar_MercadoNegro");
            Assert.IsNotNull(lugar);
            Assert.IsNotNull(lugar.Find("Puerta"));
            Assert.IsNotNull(lugar.Find("Entrada"));
            Assert.IsNotNull(lugar.Find("Interior"));
        }
    }
}
