using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1480: Ciudad_Prologo sin la expansión del 18-19 nov (Malecón, Cerro, Autopista, Puerto y lo que colgaba de ellos).</summary>
    public class RevertirMapaT1480Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private static readonly string[] RaicesQuitadas =
        {
            "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto", "T1388_Uniones", "T1461_Look",
            "T1358_Orillas", "T1365_Uniones", "T1392_Volumenes", "T1399_Bruma", "T1462_Aereas",
        };

        private static readonly string[] ZonasQuitadas =
        {
            "ZonaTrafico_Malecon", "ZonaTrafico_Cerro", "ZonaTrafico_Puerto", "ZonaTrafico_Autopista", "ZonaTrafico_Autopista_RampaEste",
        };

        private static readonly string[] ZonasQueQuedan =
        {
            "ZonaTrafico_Avenida_Centro", "ZonaTrafico_LasLomas_Oeste", "ZonaTrafico_LasLomas_Este", "ZonaTrafico_NeonAlto",
            "ZonaTrafico_NeonAlto_CorredorEste", "ZonaTrafico_Deshuesadero",
        };

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

        private GameObject Raiz(string nombre)
        {
            return escena.GetRootGameObjects().FirstOrDefault(g => g.name == nombre);
        }

        [Test]
        public void Escena_NoTieneLasRaicesDeLasZonas()
        {
            foreach (string nombre in RaicesQuitadas)
            {
                Assert.IsNull(Raiz(nombre), nombre + " no debe estar en la escena");
            }
        }

        [Test]
        public void Escena_ConservaAfueras_Fondo_YDronesAereos()
        {
            Assert.IsNotNull(Raiz("T530_Afueras"));
            Assert.IsNotNull(Raiz("T513_Fondo"));
            Assert.IsNotNull(Raiz("T1398_DronesAereos"));
        }

        [Test]
        public void Zonas_SoloQuedanLasDeLosBarrios()
        {
            GameObject zonas = Raiz("Zonas");
            Assert.IsNotNull(zonas);
            string[] hijos = zonas.transform.Cast<Transform>().Select(t => t.name).ToArray();
            foreach (string nombre in ZonasQuitadas)
            {
                CollectionAssert.DoesNotContain(hijos, nombre);
            }
            foreach (string nombre in ZonasQueQuedan)
            {
                CollectionAssert.Contains(hijos, nombre);
            }
        }

        [Test]
        public void Fondo_CuatroTramos_ActivosYConTransformIdentidad()
        {
            Transform fondo = Raiz("T513_Fondo").transform;
            Transform[] tramos = fondo.Cast<Transform>().Where(t => t.name.StartsWith("Tramo_")).ToArray();
            Assert.AreEqual(4, tramos.Length);
            foreach (Transform t in tramos)
            {
                Assert.IsTrue(t.gameObject.activeSelf, t.name + " debe estar activo");
                Assert.AreEqual(Vector3.zero, t.localPosition, t.name);
                Assert.AreEqual(Quaternion.identity, t.localRotation, t.name);
                Assert.AreEqual(Vector3.one, t.localScale, t.name);
            }
        }

        [Test]
        public void Afueras_NingunLimiteInvisibleApagado()
        {
            Transform afueras = Raiz("T530_Afueras").transform;
            Transform[] limites = afueras.GetComponentsInChildren<Transform>(true).Where(t => t.name == "LimiteInvisible").ToArray();
            Assert.Greater(limites.Length, 0);
            foreach (Transform t in limites)
            {
                Assert.IsTrue(t.gameObject.activeSelf, "LimiteInvisible apagado bajo " + t.parent.name);
            }
        }
    }
}
