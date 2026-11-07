using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-880 (playtest 7-nov, sesión 2): la noche como K01 y pareja en toda la ciudad. La calle de prueba se veía de atardecer: exposición
    /// alta, luna fuerte, postes que alumbraban 26 m, niebla delgada, nubes, y el APV viejo que dejaba casas negras y otras quemadas.
    /// </summary>
    public class LuzK01S2oPruebas
    {
        [OneTimeSetUp]
        public void Abrir() => EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);

        [Test]
        public void Noche_ExposicionBajaYLunaDebil()
        {
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ColocarS2cLook.RutaPerfilNoche);
            Assert.IsTrue(noche.TryGet(out ColorAdjustments color));
            Assert.LessOrEqual(color.postExposure.value, 2f, "antes 2.5: se veía de atardecer");
            ConfigCicloDia ciclo = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            Assert.LessOrEqual(ciclo.IntensidadLuz(23f), 0.5f, "la luna no alumbra como el sol");
            Assert.AreEqual(ciclo.IntensidadLuz(1f), ciclo.IntensidadLuz(23f), 0.01f, "pareja toda la noche");
        }

        [Test]
        public void Noche_NieblaAzulEspesaYCieloLiso()
        {
            ConfigCicloDia ciclo = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            Assert.GreaterOrEqual(ciclo.DensidadNiebla(23f), 0.03f, "el fondo de la calle se pierde");
            Color niebla = ciclo.ColorNiebla(23f);
            Assert.Greater(niebla.b, niebla.r, "niebla azul");
            Color cielo = ciclo.ColorCielo(23f);
            Assert.Greater(cielo.b, cielo.r * 2f, "cielo azul marino");
            Assert.Less(cielo.maxColorComponent, 0.2f, "oscuro");
            ConfigAmbienteNoche ambiente = AssetDatabase.LoadAssetAtPath<ConfigAmbienteNoche>(ColocarS2oLuz.RutaAmbienteNoche);
            Assert.AreEqual(0f, ambiente.OpacidadNubes, 0.001f, "sin el domo de nubes");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Arte/Fondos/Materiales" }))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m.shader.name == "MechaCyberpunk/FondoCiudad")
                {
                    Assert.Greater(m.GetFloat("_BrumaNoche"), 0.8f, m.name + ": el telón pintado (nubes, atardecer) no se ve de noche");
                }
            }
        }

        [Test]
        public void Postes_ElNaranjaQuedaBajoCadaPoste()
        {
            Light[] postes = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(ColocarS2oLuz.EsPoste).ToArray();
            Assert.Greater(postes.Length, 50);
            foreach (Light l in postes)
            {
                Assert.LessOrEqual(l.range, 12f, ColocarS2.Ruta(l.transform) + ": alcance corto");
            }
        }

        [Test]
        public void LuzPareja_SinElApvViejo()
        {
            Assert.IsTrue(Object.FindObjectsByType<ProbeVolumePerSceneData>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(d => !d.enabled),
                "el APV horneado con la ciudad de antes dejaba casas negras");
            Transform apv = ColocarS2.Buscar(ColocarS2oLuz.RutaApv);
            Assert.IsTrue(apv == null || !apv.gameObject.activeSelf);
            Assert.IsTrue(Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(ColocarS2oLuz.EsRelleno).All(l => l.intensity <= 4f),
                "las luces de relleno de los edificios ya no queman fachadas");
        }

        [Test]
        public void CalleDePrueba_CerradaAlFondoConCasasDelKit()
        {
            Transform fondo = ColocarS2.Buscar(ColocarS2oLuz.Raiz + "/Fondo");
            Assert.IsNotNull(fondo);
            Bounds b = ColocarS2oLuz.Limites(fondo);
            Assert.Less(b.max.x, ColocarS2cCalle.XBordePoniente + 0.5f, "detrás del borde de la calle");
            Assert.Less(b.min.z, ColocarS2cCalle.ZSur, "tapa todo el ancho de la calle");
            Assert.Greater(b.max.z, ColocarS2cCalle.ZNorte);
            Assert.Greater(b.size.y, 9f, "más alto que las casas de la calle: tapa el horizonte");
            Transform cosas = ColocarS2.Buscar(ColocarS2oLuz.Raiz + "/Cosas");
            Assert.GreaterOrEqual(cosas.Cast<Transform>().Count(t => t.name == "Neon"), 8, "más neones");
            Assert.GreaterOrEqual(cosas.Cast<Transform>().Count(t => t.name == "Charco"), 8, "más charcos");
        }
    }
}
