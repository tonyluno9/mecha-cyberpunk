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
        public void CalleComercial_SinToldosConPatasEnElAireNiPuestosEnElArroyo()
        {
            Assert.IsEmpty(ColocarT1331Ajustes.SobrantesK10().Where(t => t.gameObject.activeInHierarchy).Select(t => t.name).ToArray());
        }

        [Test]
        public void Tino_ElFrenteLlevaElMuroLisoDeLaFachada()
        {
            var frente = ColocarT1331Ajustes.RenderersTino().Where(ColocarT1331Ajustes.EsFrenteTino).ToList();
            Assert.AreEqual(6, frente.Count, "tres paños de fuera, dos pilastras y el dintel");
            Assert.IsEmpty(frente.Where(r => r.sharedMaterial == null || r.sharedMaterial.name != ColocarT1331Ajustes.NombreFachadaLisa).Select(r => r.name).ToArray());
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
                // T-1392 (19-nov): en las zonas nuevas el sodio es 400 de intensidad y 22 m (en el Cerro 18 m, T-1365/T-1411); los barrios se quedan en 140 y 16 m.
                bool deZona = Mathf.Approximately(l.intensity, LookNocheT1392.IntensidadSodio);
                if (deZona)
                {
                    Assert.LessOrEqual(l.range, LookNocheT1392.AlcanceSodio + 0.001f, l.transform.parent.name);
                    continue;
                }
                Assert.AreEqual(ColocarS2oLuz.IntensidadPoste, l.intensity, 0.001f, l.transform.parent.name);
                Assert.AreEqual(ColocarS2oLuz.RangoPoste, l.range, 0.001f, l.transform.parent.name);
            }
        }
    }
}
