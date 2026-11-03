using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-536 (plan de la noche 3-nov, paso 1): puertas a escala de una persona de 1.8 m, la entrada del Casino ya no tapa el interior y
    /// el pasillo entre la fachada y cada local tiene luz (sin "pared negra" al cruzar).
    /// </summary>
    public class PuertasT536Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform[] Todos() => escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();

        [Test]
        public void Puertas_AEscalaHumana_ConMarco()
        {
            foreach (string lugar in new[] { "Lugar_Casa", "Lugar_Bar", "Lugar_Clandestino" })
            {
                Transform puerta = Todos().First(t => t.name == lugar).Find("Puerta");
                Bounds hoja = puerta.Find("Hoja").GetComponent<Renderer>().bounds;
                Assert.That(hoja.size.y, Is.InRange(2.1f, 2.5f), lugar + ": alto de puerta (persona de 1.8 m)");
                Assert.That(Mathf.Max(hoja.size.x, hoja.size.z), Is.InRange(0.9f, 1.8f), lugar + ": ancho de puerta");
                Assert.AreEqual(3, puerta.Find("Marco_T536").childCount, lugar + ": dos jambas y dintel");
            }
        }

        [Test]
        public void Casino_SinLaEntradaQueTapaba_YPasillosConLuz()
        {
            Assert.IsFalse(Todos().Any(t => t.name.StartsWith("E01_EntradaCasino") && t.gameObject.activeInHierarchy), "La E01 tenía la puerta cerrada en la malla");
            Light[] pasillos = Todos().Select(t => t.GetComponent<Light>()).Where(l => l != null && l.name == "LuzPasillo_T536").ToArray();
            Assert.AreEqual(5, pasillos.Length, "Una luz en el pasillo de cada local");
            Assert.IsTrue(pasillos.All(l => l.shadows == LightShadows.None && l.intensity > 1f));
        }
    }
}
