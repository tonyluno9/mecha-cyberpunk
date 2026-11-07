using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>S2c (7-nov): la calle de Nova (K06) en la Manzana_6, con torres a los dos lados, luz fría y paso libre.</summary>
    public class NovaS2cPruebas
    {
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            raiz = GameObject.Find(ColocarS2cNova.Raiz)?.transform;
            Physics.SyncTransforms();
        }

        [Test]
        public void Nova_TorresALosDosLadosYLuzFria()
        {
            Assert.IsNotNull(raiz);
            Transform[] frentes = raiz.Cast<Transform>().Where(t => t.name == "Frente_Nova").ToArray();
            Assert.AreEqual(2, frentes.Length, "un frente de cada lado");
            foreach (Transform f in frentes)
            {
                Assert.GreaterOrEqual(f.Cast<Transform>().Count(t => t.name.StartsWith("T2")), 3, "torres T20–T23");
            }
            Light[] postes = raiz.GetComponentsInChildren<Light>(true).Where(l => l.type == LightType.Spot).ToArray();
            Assert.GreaterOrEqual(postes.Length, 4);
            Assert.IsTrue(postes.All(l => l.color.b > l.color.r), "luz fría");
            foreach (string e in ColocarS2cNova.Edificios)
            {
                Transform t = ColocarS2.Buscar(e);
                Assert.IsTrue(t == null || !t.gameObject.activeInHierarchy, e + " sigue prendido");
            }
        }

        [Test]
        public void Calle_SeCruzaSinChocar()
        {
            float piso = KitS2c.Suelo(new Vector3(94f, 0f, ColocarS2cNova.ZCentro), 0.2f);
            Vector3 abajo = new Vector3(ColocarS2cNova.X0 + 1f, piso + 0.65f, ColocarS2cNova.ZCentro);
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, abajo + Vector3.up, 0.4f, Vector3.right, ColocarS2cNova.X1 - ColocarS2cNova.X0 - 2f, ~(1 << ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore);
            Assert.IsEmpty(golpes.Select(g => ColocarS2.Ruta(g.collider.transform)).ToArray());
        }
    }
}
