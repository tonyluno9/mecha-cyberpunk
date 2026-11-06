using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-853 (playtest 6-nov tarde): la casa sin piso encimado, con medidor de tamaño real y sin cables, cargador ni banco en la banqueta.</summary>
    public class CasaS2tPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir() => escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        private Transform Buscar(string ruta)
        {
            int i = ruta.IndexOf('/');
            GameObject raiz = escena.GetRootGameObjects().First(g => g.name == ruta.Substring(0, i));
            return raiz.transform.Find(ruta.Substring(i + 1));
        }

        [Test]
        public void PisoDelCuarto_NoSeEncimaConLaBanqueta()
        {
            float banqueta = Buscar("Barrio/Manzana_4/Banqueta").GetComponent<Renderer>().bounds.max.y;
            foreach (string r in new[] { ColocarS2tCasa.RutaPisoCuarto, ColocarS2tCasa.RutaPisoPasillo })
            {
                float piso = Buscar(r).GetComponent<Renderer>().bounds.center.y;
                Assert.Greater(piso - banqueta, 0.01f, r + " está encima de la banqueta, no a la misma altura");
            }
        }

        [Test]
        public void Medidor_DeTamanoReal()
        {
            Assert.AreEqual(ColocarS2tCasa.AltoMedidor, ArteT250.Limites(Buscar(ColocarS2tCasa.RutaMedidor).gameObject).size.y, 0.03f);
        }

        [Test]
        public void Banqueta_SinCablesCargadorNiBanco()
        {
            Transform[] todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).ToArray();
            string[] sobran = todos.Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("CY03_MazoCables") || t.name.StartsWith("CY10") || t.name.StartsWith("T03_BancoTrabajo"))
                    && !t.root.name.StartsWith("T550") && ColocarS2tCasa.BanquetaCasa.Contains(new Vector3(t.position.x, 0f, t.position.z)))
                .Select(t => t.name + " " + t.position).ToArray();
            CollectionAssert.IsEmpty(sobran);
            Transform tablero = Buscar(ColocarS2tCasa.RutaTablero);
            Assert.Less(Vector3.Distance(tablero.position, ColocarS2tCasa.LugarTablero), 0.05f, "el tablero de encargos está en la pared de la casa");
            Assert.IsNotNull(Buscar(ColocarS2tCasa.NombreRaiz + "/Tablero_C15a"), "con su letrero del kit");
        }
    }
}
