using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>S2c (7-nov): El Mercado (K02), el tianguis entre casas del kit, sin focos colgando, sin tapar el clandestino, el bar ni el callejón de armas.</summary>
    public class MercadoS2cPruebas
    {
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            raiz = GameObject.Find(ColocarS2cMercado.Raiz)?.transform;
        }

        [Test]
        public void Tianguis_EntreCasasDelKit()
        {
            Assert.IsNotNull(raiz);
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Casa_")), 16);
            foreach (string e in ColocarS2cMercado.Edificios)
            {
                Transform t = ColocarS2.Buscar(e);
                Assert.IsTrue(t == null || !t.gameObject.activeInHierarchy, e + " sigue prendido");
            }
        }

        [TestCase("Barrio/Manzana_2/Lugar_Clandestino")]
        [TestCase("Barrio/Manzana_5/Lugar_Bar")]
        [TestCase("Barrio/Manzana_2/PuestosCallejon")]
        public void Lugares_SeQuedan(string ruta)
        {
            Transform t = ColocarS2.Buscar(ruta);
            Assert.IsNotNull(t, ruta);
            Assert.IsTrue(t.gameObject.activeInHierarchy, ruta + " se apagó");
        }

        [Test]
        public void Tianguis_SinFocosColgando()
        {
            Bounds tianguis = new Bounds(new Vector3(0f, 6f, 0f), new Vector3(80f, 12f, 22f));
            string[] focos = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => tianguis.Contains(r.bounds.center) && r.bounds.center.y > 3f)
                .Select(r => ColocarS2.Ruta(r.transform))
                .Where(n => n.Contains("Foco") || n.Contains("Guirnalda") || n.Contains("Bombilla") || n.Contains("Serie"))
                .ToArray();
            Assert.IsEmpty(focos);
        }

        [Test]
        public void Casas_NoTapanLosLugares()
        {
            foreach (string ruta in new[] { "Barrio/Manzana_2/Lugar_Clandestino", "Barrio/Manzana_5/Lugar_Bar" })
            {
                Bounds lugar = ColocarS2.Buscar(ruta).GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                lugar.Expand(new Vector3(-0.6f, 0f, -0.6f));
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>().Where(c => c.enabled))
                {
                    Assert.IsFalse(c.bounds.Intersects(lugar), ColocarS2.Ruta(c.transform) + " se mete en " + ruta);
                }
            }
        }
    }
}
