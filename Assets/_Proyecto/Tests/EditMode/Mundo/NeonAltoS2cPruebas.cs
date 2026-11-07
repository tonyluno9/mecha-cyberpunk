using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>S2c (7-nov): Neón Alto como K03, neones magenta y turquesa en las fachadas de la calle del casino, cables, charcos y luz sin verde.</summary>
    public class NeonAltoS2cPruebas
    {
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            raiz = GameObject.Find(ColocarS2cNeonAlto.Raiz)?.transform;
        }

        [Test]
        public void Calle_ConNeonesCablesYCharcos()
        {
            Assert.IsNotNull(raiz);
            Assert.GreaterOrEqual(raiz.Cast<Transform>().Count(t => t.name == "Neon"), 25);
            Assert.IsNotNull(raiz.Find("Cables"), "cables de fachada a fachada");
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<CharcoVisible>().Length, 15);
        }

        [Test]
        public void Neones_PegadosAUnaFachada()
        {
            Physics.SyncTransforms();
            foreach (Transform n in raiz.Cast<Transform>().Where(t => t.name == "Neon"))
            {
                Collider[] cerca = Physics.OverlapSphere(n.position, 1.2f).Where(c => !c.transform.IsChildOf(raiz)).ToArray();
                Assert.IsNotEmpty(cerca, n.position + ": el neón no está junto a ninguna pared");
            }
        }

        [Test]
        public void Luces_SinVerde()
        {
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(l => l.type != LightType.Directional && ColocarS2cNeonAlto.Zona.Contains(l.transform.position)))
            {
                Assert.IsFalse(l.color.g > l.color.r + 0.15f && l.color.g > l.color.b + 0.2f, ColocarS2.Ruta(l.transform) + " sigue verde");
            }
        }
    }
}
