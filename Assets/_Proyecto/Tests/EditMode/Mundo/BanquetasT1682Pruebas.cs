using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1682 (T38 + E3): en Ciudad_Prologo ninguna banqueta tiene material de asfalto, todas llevan T02_Banqueta con losas de ~0.8 m,
    /// las banquetas S2c junto a un arroyo tienen bordillo y todos los charcos usan una máscara de T11 con el agua de T-1503.
    /// </summary>
    public class BanquetasT1682Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(BanquetasT1682.RutaEscena, OpenSceneMode.Additive);
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        [Test]
        public void Banquetas_NingunaConMaterialDeAsfalto()
        {
            string[] conAsfalto = BanquetasT1682.BanquetasConAsfalto(escena)
                .Select(c => CallesTexturasT1642.Ruta(c.Renderer.transform) + " " + c.Material.name).Take(20).ToArray();
            Assert.IsEmpty(conAsfalto);
        }

        [Test]
        public void Banquetas_TodasConT02BienEscalada()
        {
            var banquetas = BanquetasT1682.BanquetasPorNombre(escena);
            Assert.GreaterOrEqual(banquetas.Count, 19, "las 6 manzanas y las banquetas S2c");
            string[] sinT02 = banquetas.Where(r => r.sharedMaterials.Any(m => !BanquetasT1682.TieneT02(m)))
                .Select(r => CallesTexturasT1642.Ruta(r.transform)).Take(20).ToArray();
            Assert.IsEmpty(sinT02, "sin la textura T02_Banqueta");
            foreach (var c in BanquetasT1682.CarasBanqueta(escena).Where(c => BanquetasT1682.TieneT02(c.Material)))
            {
                float metrosPorRepeticion = c.MetrosPorUv / c.Material.GetTextureScale("_BaseMap").x;
                Assert.AreEqual(BanquetasT1682.MetrosPorRepeticion, metrosPorRepeticion, 0.1f, CallesTexturasT1642.Ruta(c.Renderer.transform));
            }
        }

        [Test]
        public void Bordillos_EnLasBanquetasS2c()
        {
            GameObject raiz = GameObject.Find(BanquetasT1682.NombreRaizBordillos);
            Assert.IsNotNull(raiz);
            Assert.GreaterOrEqual(raiz.transform.childCount, 12);
            GameObject t1642 = GameObject.Find(CallesTexturasT1642.NombreRaizBordillos);
            Assert.IsNotNull(t1642, "los bordillos de las manzanas (T-1642) siguen");
            Assert.GreaterOrEqual(t1642.transform.childCount, 24);
        }

        [Test]
        public void Charcos_TodosConMascaraT11YAguaT1503()
        {
            var charcos = CallesTexturasT1642.CharcosQueSuenan(escena);
            Assert.Greater(charcos.Count, 150);
            string[] sinT11 = charcos.Where(r => !BanquetasT1682.MascaraT11(r.sharedMaterial) || r.sharedMaterial.shader.name != AguaT1503.NombreShader)
                .Select(r => CallesTexturasT1642.Ruta(r.transform)).Take(20).ToArray();
            Assert.IsEmpty(sinT11);
        }
    }
}
