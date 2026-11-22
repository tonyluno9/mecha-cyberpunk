using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1642 (T22): en Ciudad_Prologo no hay superficies lisas sin textura (salvo emisivos, vidrios y efectos), cada charco que suena al pisarlo
    /// se ve (agua de T-1503, encima del piso visible) y ningún piso suena a agua sin charco. Reglas en CallesTexturasT1642.
    /// </summary>
    public class CallesTexturasT1642Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(CallesTexturasT1642.RutaEscena, OpenSceneMode.Additive);
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        [Test]
        public void Renderers_SinMaterialLisoSinTextura()
        {
            string[] lisos = CallesTexturasT1642.SinTexturaEnEscena(escena)
                .Select(x => x.m.name + " en " + CallesTexturasT1642.Ruta(x.r.transform)).Distinct().Take(20).ToArray();
            Assert.IsEmpty(lisos);
        }

        [Test]
        public void Charcos_QueSuenan_SeVenYNoEstanTapados()
        {
            var charcos = CallesTexturasT1642.CharcosQueSuenan(escena);
            Assert.Greater(charcos.Count, 150, "los charcos de toda la ciudad");
            string[] noSeVen = charcos.Where(r => !CallesTexturasT1642.CharcoSeVe(r)).Select(r => CallesTexturasT1642.Ruta(r.transform)).Take(20).ToArray();
            Assert.IsEmpty(noSeVen, "suenan pero no se ven");
            string[] tapados = charcos.Where(r => CallesTexturasT1642.EstaTapado(escena, r)).Select(r => CallesTexturasT1642.Ruta(r.transform)).Take(20).ToArray();
            Assert.IsEmpty(tapados, "quedaron debajo del asfalto o la banqueta");
        }

        [Test]
        public void Pisos_NoSuenanAguaSinCharco()
        {
            string[] mojados = CallesTexturasT1642.PisosMojados(escena).Select(c => CallesTexturasT1642.NombrePiso(c)).Take(20).ToArray();
            Assert.IsEmpty(mojados);
        }

        [Test]
        public void CalleYCallejonDeLaCasa_TienenCharcosVisibles()
        {
            var charcos = CallesTexturasT1642.CharcosQueSuenan(escena).Where(CallesTexturasT1642.CharcoSeVe).Select(r => r.bounds.center).ToArray();
            Assert.GreaterOrEqual(charcos.Count(c => c.x > -54f && c.x < -40f && c.z > -90f && c.z < -50f), 3, "calle de la casa");
            Assert.GreaterOrEqual(charcos.Count(c => c.x > -86f && c.x < -54f && c.z > -62f && c.z < -56f), 2, "callejón de la vuelta");
        }

        [Test]
        public void Bordillos_EnLasManzanas()
        {
            GameObject raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == CallesTexturasT1642.NombreRaizBordillos);
            Assert.IsNotNull(raiz);
            Renderer[] rs = raiz.GetComponentsInChildren<Renderer>();
            Assert.GreaterOrEqual(rs.Length, 24, "4 lados de 6 manzanas");
            Assert.IsTrue(rs.All(r => r.sharedMaterial != null && r.sharedMaterial.GetTexture("_BaseMap") != null));
        }
    }
}
