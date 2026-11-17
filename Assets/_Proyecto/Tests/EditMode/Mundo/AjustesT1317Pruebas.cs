using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Vehiculos.Efectos;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1317: sin letrero ENCARGOS afuera de la casa ni letrero NOVA, postes con la intensidad nueva, todo barril con collider y BarrilExplosivo.</summary>
    public class AjustesT1317Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir() => escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        private Transform[] Todos() => escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();

        [Test]
        public void SinLetreroNova_NiEncargosAfueraDeLaCasa()
        {
            Assert.IsEmpty(Todos().Where(t => t.name == "Letrero_NOVA" || t.name == "Haz_Nova" || t.name == "Tablero_Texto" || t.name == "Tablero_C15a").Select(t => t.name).ToArray());
            Assert.IsEmpty(Todos().Select(t => t.GetComponent<UnityEngine.UI.Text>()).Where(x => x != null && x.text.Contains("ENCARGOS") && x.gameObject.activeInHierarchy).Select(x => x.name).ToArray());
        }

        [Test]
        public void Postes_TienenLaIntensidadNueva()
        {
            Light[] postes = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).Where(ColocarS2oLuz.EsPoste).ToArray();
            Assert.Greater(postes.Length, 50);
            Assert.AreEqual(54f, ColocarS2oLuz.IntensidadPoste);
            foreach (Light l in postes)
            {
                Assert.AreEqual(ColocarS2oLuz.IntensidadPoste, l.intensity, 0.001f, l.transform.parent.name);
            }
        }

        [Test]
        public void TodoBarril_TieneColliderYBarrilExplosivo()
        {
            Transform[] barriles = Todos().Where(ColocarT1317Ajustes.EsBarrilDeCiudad).ToArray();
            Assert.Greater(barriles.Length, 40);
            foreach (Transform b in barriles)
            {
                Assert.IsNotNull(b.GetComponentInChildren<Collider>(true), b.name + " sin collider");
                Assert.IsTrue(b.GetComponent<BarrilExplosivo>() != null || b.GetComponentInParent<BarrilExplosivo>(true) != null, b.name + " sin BarrilExplosivo");
            }
        }
    }
}
