using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1331: sin toldos flotando ni sobre la calle, tienda de Tino sin materiales rotos ni planos encimados, sin estelas raras, carros sin objetos dentro y postes más fuertes.</summary>
    public class AjustesT1331Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir() => escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        [Test]
        public void Toldos_NingunoFlotaNiEstaSobreLaCalle()
        {
            var malos = ColocarT1331Ajustes.ToldosMalos();
            Assert.IsEmpty(malos.Select(m => m.motivo + ": " + m.toldo.name + " " + m.toldo.position.ToString("F1")).ToArray());
        }

        [Test]
        public void Tino_SinMaterialesRotosNiPlanosEncimados()
        {
            var renderers = ColocarT1331Ajustes.RenderersTino();
            Assert.Greater(renderers.Count, 5, "la zona de Tino tiene piezas");
            Assert.IsEmpty(renderers.Where(r => r.sharedMaterials.Any(ColocarT1331Ajustes.MaterialRoto)).Select(r => r.name).ToArray());
            Assert.IsEmpty(ColocarT1331Ajustes.PlanosEncimados(renderers).Select(r => r.name).ToArray());
        }

        [Test]
        public void Estelas_NoQuedanEstelasRaras()
        {
            Assert.IsEmpty(ColocarT1331Ajustes.EstelasRaras().Select(e => e.motivo + ": " + e.objeto.name).ToArray());
        }

        [Test]
        public void Carros_NingunoEstaSobreObjetos()
        {
            Physics.SyncTransforms();
            var carros = ColocarT1331Ajustes.CarrosDeLaCiudad();
            Assert.Greater(carros.Count, 3, "hay carros estacionados");
            foreach (var c in carros)
            {
                Assert.IsEmpty(ColocarT1331Ajustes.Estorbos(c.transform).Select(e => e.transform.root.name + "/" + e.name).ToArray(), c.name + " en " + c.transform.position.ToString("F1"));
            }
        }

        [Test]
        public void Postes_TienenLaLuzNueva()
        {
            Assert.AreEqual(140f, ColocarS2oLuz.IntensidadPoste);
            Assert.AreEqual(16f, ColocarS2oLuz.RangoPoste);
            foreach (Light l in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).Where(ColocarS2oLuz.EsPoste))
            {
                Assert.AreEqual(ColocarS2oLuz.IntensidadPoste, l.intensity, 0.001f, l.transform.parent.name);
                Assert.AreEqual(ColocarS2oLuz.RangoPoste, l.range, 0.001f, l.transform.parent.name);
            }
        }
    }
}
