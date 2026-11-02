using System.Linq;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-512 (paso 4): luz por zona en Ciudad_Prologo. Callejón y pasillo de El Mercado con lámparas N01 y neón magenta que solo se
    /// prenden de noche, El Tianguis con hileras de puestos a las orillas sin tapar el carril central, una luz de luna sobre cada azotea
    /// y lluvia ligera de día.
    /// </summary>
    public class LuzZonasT512Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T512_LuzZonas";
        private const float CarrilLibre = 3.6f;

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().First(g => g.name == NombreRaiz).transform;
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        [Test]
        public void CallejonYMercado_LamparasN01ConLuzCalida_YNeonMagenta()
        {
            foreach (string zona in new[] { "Callejon", "Mercado" })
            {
                Transform g = raiz.Find(zona);
                Assert.IsNotNull(g, zona);
                Light[] luces = g.GetComponentsInChildren<Light>(true);
                Assert.GreaterOrEqual(g.Cast<Transform>().Count(t => t.name.StartsWith("N01")), 5, zona + ": lámparas N01");
                Assert.GreaterOrEqual(luces.Count(l => l.color.r > l.color.b && l.color.g > l.color.b), 5, zona + ": luz cálida");
                Assert.GreaterOrEqual(luces.Count(l => l.color.r > l.color.g && l.color.b > l.color.g), 2, zona + ": neón magenta");
                Assert.IsTrue(luces.All(l => l.shadows == LightShadows.None), zona + ": sin sombras (rendimiento)");
                Assert.IsTrue(luces.All(l => l.GetComponent<LuzNocturna>() != null), zona + ": solo de noche");
                Assert.IsTrue(luces.All(l => l.intensity <= 15f), zona + ": sin perder lo oscuro");
            }
        }

        [Test]
        public void Porton_LuzNaranjaMasFuerte()
        {
            Light[] porton = escena.GetRootGameObjects().First(g => g.name == "T504_Mercado").GetComponentsInChildren<Light>(true)
                .Where(l => l.name == "Luz_Adentro" || l.name == "Luz_Derrame").ToArray();
            Assert.AreEqual(2, porton.Length);
            Assert.IsTrue(porton.All(l => l.intensity >= 8f), "Más fuerte que en T-504 (6 y 5)");
        }

        [Test]
        public void Tianguis_PuestosEnHilerasALasDosOrillas_SinTaparElCarrilCentral()
        {
            Transform[] puestos = raiz.Find("Tianguis").Cast<Transform>().ToArray();
            Assert.GreaterOrEqual(puestos.Length, 12, "Más puestos");
            Assert.GreaterOrEqual(puestos.Count(p => p.position.z > 0f), 4, "Orilla norte");
            Assert.GreaterOrEqual(puestos.Count(p => p.position.z < 0f), 4, "Orilla sur");
            Assert.GreaterOrEqual(puestos.Select(p => p.name.Replace("_T512", "")).Distinct().Count(), 3, "Mezcla N03a, N03b, C12 y C13");
            foreach (Transform p in puestos)
            {
                BoxCollider caja = p.GetComponent<BoxCollider>();
                Assert.IsNotNull(caja, p.name + " conserva su collider");
                Bounds b = caja.bounds;
                Assert.GreaterOrEqual(Mathf.Min(Mathf.Abs(b.min.z), Mathf.Abs(b.max.z)), CarrilLibre - 0.05f, p.name + " tapa el carril central");
            }
        }

        [Test]
        public void Azoteas_LuzDeLunaSobreLosTechos_SoloDeNoche()
        {
            Light[] luna = raiz.Find("Azoteas").GetComponentsInChildren<Light>(true);
            Assert.GreaterOrEqual(luna.Length, 50, "Una por techo");
            Assert.IsTrue(luna.All(l => l.transform.position.y > 9f), "Encima de los techos");
            Assert.IsTrue(luna.All(l => l.color.b > l.color.r && l.shadows == LightShadows.None), "Luz de luna fría y sin sombra");
            Assert.IsTrue(luna.All(l => l.GetComponent<LuzNocturna>() != null), "Solo de noche");
        }

        [Test]
        public void Lluvia_TambienDeDia_Ligera()
        {
            ConfigLluvia config = AssetDatabase.LoadAssetAtPath<ConfigLluvia>("Assets/_Proyecto/Mundo/Datos/ConfigLluvia_Mundo.asset");
            Assert.IsTrue(config.LluviaDeDia);
            Assert.That(config.FraccionDeDia, Is.InRange(0.1f, 0.5f), "De día llueve poco");
        }
    }
}
