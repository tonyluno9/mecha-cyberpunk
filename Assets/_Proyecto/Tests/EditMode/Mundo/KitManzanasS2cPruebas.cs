using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>S2c (7-nov), paso 2 de docs/plan-look-ciudad.md: el kit de manzanas (frentes A–F, esquina de K05 y frente de Nova de K06).</summary>
    public class KitManzanasS2cPruebas
    {
        private static GameObject Cargar(string nombre)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ColocarS2cKit.CarpetaKit + "/" + nombre + ".prefab");
            Assert.IsNotNull(p, "falta el prefab " + nombre);
            return p;
        }

        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        [TestCase("D")]
        [TestCase("E")]
        [TestCase("F")]
        public void Frente_DeSesentaMetrosConCasasPostesYBanqueta(string letra)
        {
            GameObject p = Cargar("Frente_" + letra);
            Transform[] casas = p.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Casa_")).ToArray();
            Assert.GreaterOrEqual(casas.Length, 9, "casas de 4 a 6 m en 60 m");
            Assert.IsTrue(casas.All(c => c.position.x >= -0.1f && c.position.x <= ColocarS2cKit.LargoFrente + 0.1f && Mathf.Abs(c.position.z) < 0.1f), "sobre la línea de fachada");
            Light[] postes = p.GetComponentsInChildren<Light>(true).Where(l => l.type == LightType.Spot).ToArray();
            Assert.GreaterOrEqual(postes.Length, 2, "postes con su luz");
            Assert.IsTrue(postes.All(l => l.GetComponent<LuzNocturna>() != null), "se prenden de noche");
            Bounds b = p.GetComponentsInChildren<Transform>().First(t => t.name == "Banqueta").GetComponent<Renderer>().bounds;
            Assert.AreEqual(ColocarS2cKit.LargoFrente, b.size.x, 0.05f);
            Assert.Less(b.max.z, 0.01f, "la banqueta queda del lado de la calle (-Z)");
        }

        [TestCase("Frente_A")]
        [TestCase("Frente_E")]
        [TestCase("Esquina_K05")]
        [TestCase("Frente_Nova")]
        public void Prefab_SusMallasSonAssets(string nombre)
        {
            foreach (MeshFilter mf in Cargar(nombre).GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.IsNotNull(mf.sharedMesh, nombre + ": " + mf.name + " sin malla");
                Assert.IsTrue(AssetDatabase.Contains(mf.sharedMesh), nombre + ": la malla de " + mf.name + " no es un asset");
            }
        }

        [Test]
        public void Frentes_SonDistintos()
        {
            int neonesA = Cargar("Frente_A").GetComponentsInChildren<Transform>().Count(t => t.name == "Neon");
            int neonesE = Cargar("Frente_E").GetComponentsInChildren<Transform>().Count(t => t.name == "Neon");
            int neonesD = Cargar("Frente_D").GetComponentsInChildren<Transform>().Count(t => t.name == "Neon");
            Assert.Greater(neonesE, neonesA + 4, "E es el de neón y A el de vivienda");
            Assert.AreEqual(0, neonesD, "D es de talleres, sin neón");
        }

        [Test]
        public void Esquina_ConFarmaciaSemaforoYCruce()
        {
            GameObject p = Cargar("Esquina_K05");
            Transform[] todo = p.GetComponentsInChildren<Transform>(true);
            Assert.IsTrue(todo.Any(t => t.name == "LetreroFarmacia"), "el letrero dice FARMACIA (letrero_farmacia del kit urbano)");
            Transform semaforo = todo.First(t => t.name == "Semaforo");
            Assert.AreEqual(3, semaforo.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Mica_")), "rojo, ámbar y verde");
            Assert.GreaterOrEqual(todo.Count(t => t.name == "Linea"), 5, "rayas del cruce peatonal");
            Assert.AreEqual(2, todo.Count(t => t.name.StartsWith("Casa_")), "casa de esquina con dos frentes");
        }

        [Test]
        public void Nova_EdificiosAltosYLuzFria()
        {
            GameObject p = Cargar("Frente_Nova");
            int torres = p.GetComponentsInChildren<Transform>().Count(t => t.parent == p.transform.GetChild(0) && t.name.StartsWith("Edificio_"));
            Assert.GreaterOrEqual(torres, 2, "edificios de oficinas");
            Light[] postes = p.GetComponentsInChildren<Light>(true).Where(l => l.type == LightType.Spot).ToArray();
            Assert.GreaterOrEqual(postes.Length, 2);
            Assert.IsTrue(postes.All(l => l.color.b > l.color.r), "luz fría, no de sodio");
        }
    }
}
