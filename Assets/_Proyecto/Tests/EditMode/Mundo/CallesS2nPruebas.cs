using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-805 (noche 6-nov, sesión 2): la calle junto a la casa da vuelta en una calle real (ColocarS2nCalles) y el carro 17 está libre.</summary>
    public class CallesS2nPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        /// <summary>Lo que debe haber libre (m) delante de un carro robable para salir de su lugar.</summary>
        private const float LibreAdelante = 6f;

        private Scene escena;
        private List<Transform> todos;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform Raiz(string nombre) => escena.GetRootGameObjects().FirstOrDefault(g => g.name == nombre)?.transform;

        [Test]
        public void CalleCruzada_UneLasDosSalidasSur_ConPisoContinuo()
        {
            Transform piso = Raiz(ColocarS2nCalles.NombreRaiz)?.Find("Piso");
            Assert.IsNotNull(piso, "S2n_Calles/Piso");
            List<Bounds> calle = piso.Cast<Transform>().Where(t => t.name.StartsWith("CalleCruzada")).Select(t => t.GetComponent<Renderer>().bounds).OrderBy(b => b.min.x).ToList();
            Assert.Greater(calle.Count, 0);
            Assert.AreEqual(-ColocarS2nCalles.CruceX, calle.First().min.x, 0.05f, "empieza en la calle de la salida 4");
            Assert.AreEqual(ColocarS2nCalles.CruceX, calle.Last().max.x, 0.05f, "llega a la calle de la salida 5");
            for (int i = 1; i < calle.Count; i++)
            {
                Assert.AreEqual(calle[i - 1].max.x, calle[i].min.x, 0.05f, "tramos de asfalto sin hueco ni encimados");
            }
            foreach (Transform t in piso)
            {
                Assert.IsNotNull(t.GetComponent<Collider>(), t.name + " se pisa");
                Assert.AreEqual(0f, t.GetComponent<Renderer>().bounds.max.y, 0.21f, t.name + " a la altura de la calle o la banqueta");
            }
        }

        [Test]
        public void CalleCruzada_SinEdificiosEncima_YConEdificiosALosDosLados()
        {
            Bounds cruce = new Bounds(new Vector3(0f, 5f, (ColocarS2nCalles.CruceZ0 + ColocarS2nCalles.CruceZ1) * 0.5f),
                new Vector3(ColocarS2nCalles.CruceX * 2f - 0.2f, 8f, ColocarS2nCalles.CruceZ1 - ColocarS2nCalles.CruceZ0 + ColocarS2nCalles.Banqueta * 2f - 0.2f));
            List<Transform> edificios = todos.Where(t => t.name == "Edificio" && t.gameObject.activeInHierarchy && t.GetComponent<Renderer>() != null).ToList();
            Transform[] encima = edificios.Where(t => t.GetComponent<Renderer>().bounds.Intersects(cruce)).ToArray();
            Assert.IsEmpty(encima, "edificios sobre la calle nueva: " + string.Join(", ", encima.Select(t => t.GetComponent<Renderer>().bounds.center)));
            // De lado a lado, cada 2 m, hay un edificio pegado a cada banqueta: no queda callejón abierto al vacío.
            for (float x = -ColocarS2nCalles.CruceX + 4f; x <= ColocarS2nCalles.CruceX - 4f; x += 2f)
            {
                foreach (float z in new[] { ColocarS2nCalles.CruceZ1 + ColocarS2nCalles.Banqueta + 0.5f, ColocarS2nCalles.CruceZ0 - ColocarS2nCalles.Banqueta - 0.5f })
                {
                    Vector3 p = new Vector3(x, 2f, z);
                    Assert.IsTrue(edificios.Any(e => e.GetComponent<Renderer>().bounds.Contains(p)), "hueco entre edificios en " + p);
                }
            }
        }

        [Test]
        public void CarroRobable17_TieneLibreAdelante()
        {
            Transform carro = todos.FirstOrDefault(t => t.name == "CarroRobable_17");
            Assert.IsNotNull(carro);
            Vector3 frente = carro.forward;
            foreach (Transform otro in todos.Where(t => t != carro && t.name.StartsWith("CarroRobable_")))
            {
                Vector3 d = otro.position - carro.position;
                float adelante = Vector3.Dot(d, frente);
                float lado = Vector3.Cross(frente, d).magnitude;
                Assert.IsFalse(adelante > 0f && adelante < LibreAdelante && lado < 2.5f, "CarroRobable_17 pegado a " + otro.name);
            }
        }
    }
}
