using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-803: el cuarto sin bolsa de basura en medio y con la mochila derecha en el piso; sin grafitis-logo en las fachadas de Ciudad_Prologo.</summary>
    public class LimpiezaS2nPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float ToleranciaPiso = 0.03f;
        private const float DerechaMinima = 0.95f;

        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform Cuarto()
        {
            return escena.GetRootGameObjects().First(g => g.name == "T550_Cuartos").transform.Find("Cuarto_Habitacion");
        }

        [Test]
        public void Cuarto_SinBolsaDeBasura_YMochilaDerechaEnElPiso()
        {
            Transform cuarto = Cuarto();
            Assert.IsFalse(cuarto.GetComponentsInChildren<Transform>().Any(t => t.name == "N06"), "sin la bolsa de basura en medio del cuarto");
            Transform mochila = cuarto.Find("I05");
            Assert.Greater(Vector3.Dot(mochila.up, Vector3.up), DerechaMinima, "mochila derecha, no volteada");
            Bounds b = mochila.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, x) => { a.Encapsulate(x); return a; });
            Assert.AreEqual(cuarto.position.y, b.min.y, ToleranciaPiso, "apoyada en el piso");
            float aLaPared = cuarto.InverseTransformPoint(b.center).x;
            Assert.Less(aLaPared, -0.9f, "recargada en la pared izquierda");
        }

        [Test]
        public void Casas_SinGrafitisLogoEnLasFachadas()
        {
            Transform grafitis = escena.GetRootGameObjects().First(g => g.name == "Barrio").transform.Find("Grafitis");
            Assert.IsTrue(grafitis == null || !grafitis.gameObject.activeSelf, "los grafitis-logo (cruz, calavera, rayo...) no se ven en Ciudad_Prologo");
        }
    }
}
