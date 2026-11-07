using System.Collections;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// S2c (7-nov) en juego: el reflejo de los charcos sigue a la cámara del jugador y pinta su textura; sin charcos a la vista (en la casa)
    /// no pinta nada.
    /// </summary>
    public class CalleDoradaS2cPlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Charcos_TodosCuentanSiSeVen()
        {
            yield return null;
            GameObject calle = GameObject.Find("S2c_CalleDorada");
            Assert.IsNotNull(calle);
            foreach (Renderer r in calle.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "Charco")
                {
                    Assert.IsNotNull(r.GetComponent<CharcoVisible>(), "cada charco avisa si se ve");
                }
            }
        }

        [UnityTest]
        public IEnumerator Reflejo_CamaraPrendidaEnElPisoDeLaCamaraPrincipal()
        {
            ReflejoCharcos reflejo = Object.FindFirstObjectByType<ReflejoCharcos>();
            Assert.IsNotNull(reflejo, "la escena tiene ReflejoCharcos");
            yield return null;
            yield return null;
            Assert.IsNotNull(Camera.main, "hay cámara principal");
            Assert.AreEqual(0, CharcoVisible.Visibles, "desde la casa no se ve ningún charco de la calle nueva");
            Assert.IsTrue(reflejo.Camara == null || !reflejo.Camara.enabled, "sin charcos a la vista no pinta el reflejo");
            Assert.IsTrue(reflejo.Actualizar(Camera.main), "con charcos a la vista sí");
            Assert.IsNotNull(reflejo.Camara, "creó su cámara");
            Assert.IsTrue(reflejo.Camara.enabled, "la cámara del reflejo pinta en juego");
            Assert.IsNotNull(reflejo.Camara.targetTexture);
            Assert.AreEqual(0, reflejo.Camara.cullingMask & (1 << ReflejoCharcos.CapaCharcos), "no ve los charcos");
            Assert.That(reflejo.Altura, Is.InRange(-0.5f, 0.6f), "el plano es el piso bajo la cámara");
            Vector3 ojo = Camera.main.transform.position;
            Assert.AreEqual(2f * reflejo.Altura - ojo.y, reflejo.Camara.transform.position.y, 0.01f, "ojo reflejado en el piso");
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_ReflejoCharcosHay"), 0.001f);
        }
    }
}
