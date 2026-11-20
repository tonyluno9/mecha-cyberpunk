using System.IO;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1503: la armería (gabinetes mirando hacia dentro, sin materiales rotos) y los charcos con el agua nueva. Las pruebas de escena
    /// necesitan que se hayan corrido antes los métodos de editor ArmeriaT1503 y AguaT1503 sobre Ciudad_Prologo.
    /// </summary>
    public class ArmeriaCharcosT1503Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir() => escena = EditorSceneManager.OpenScene(ArmeriaT1503.RutaEscena, OpenSceneMode.Additive);

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        [Test]
        public void MiraHacia_FrenteAlPunto_EsVerdadero_YAlReves_EsFalso()
        {
            GameObject g = new GameObject("I18");
            try
            {
                g.transform.position = new Vector3(-29.5f, 0f, 24f);
                g.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Assert.IsTrue(ArmeriaT1503.MiraHacia(g.transform, ArmeriaT1503.CentroLocal), "yaw 90 mira a +X, al interior");
                g.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                Assert.IsFalse(ArmeriaT1503.MiraHacia(g.transform, ArmeriaT1503.CentroLocal), "yaw -90 mira a la pared");
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [Test]
        public void Gabinetes_LaArmeria_ComoLosPidioTonyEnT1522()
        {
            // T-1522 (playtest 20-nov): el rack de la pared (I18) regresa a como estaba antes de T-1503 y la vitrina de enfrente del
            // armero (I17) se voltea; las dos quedan con yaw 270. Sustituye a la regla de T-1503 "mira hacia dentro".
            Transform[] gabinetes = ArmeriaT1503.Gabinetes().Where(t => t.gameObject.scene == escena && t.gameObject.activeInHierarchy).ToArray();
            Assert.Greater(gabinetes.Length, 0, "hay gabinetes en la armería");
            foreach (Transform t in gabinetes.Where(t => t.name.StartsWith("I17") || t.name.StartsWith("I18")))
            {
                Assert.AreEqual(270f, t.eulerAngles.y, 1f, t.name + " en " + t.position + " con el giro de T-1522");
            }
        }

        [Test]
        public void Armeria_NingunMaterialRoto()
        {
            foreach (GameObject raiz in escena.GetRootGameObjects().Where(g => g.name == ArmeriaT1503.RaizTienda || g.name == ArmeriaT1503.RaizCalle))
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.IsFalse(ArmeriaT1503.EstaRoto(m), r.name + " en " + raiz.name + " tiene un material roto");
                    }
                }
            }
        }

        [Test]
        public void Charcos_TodosUsanElMaterialNuevo()
        {
            MeshRenderer[] charcos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(AguaT1503.EsCharco).ToArray();
            Assert.Greater(charcos.Length, 0, "hay charcos");
            foreach (MeshRenderer r in charcos)
            {
                Assert.IsNotNull(r.sharedMaterial, r.name);
                StringAssert.StartsWith(AguaT1503.PrefijoMaterial, r.sharedMaterial.name, r.name + " en " + r.transform.position);
                Assert.AreEqual(AguaT1503.NombreShader, r.sharedMaterial.shader.name, r.name);
            }
        }

        [Test]
        public void Shader_TieneOndasDePisadasYReflejoDeSonda()
        {
            string fuente = File.ReadAllText("Assets/_Proyecto/Mundo/Ambiente/Shaders/Charco.shader");
            StringAssert.Contains("_OndasPisadas", fuente);
            StringAssert.Contains("_TiempoOndasCharco", fuente);
            StringAssert.Contains("unity_SpecCube0", fuente);
        }
    }
}
