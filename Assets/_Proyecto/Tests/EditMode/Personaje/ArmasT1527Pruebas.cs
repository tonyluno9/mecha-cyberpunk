using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1527: zoom del francotirador con la rueda, una arma por tipo, daño del francotirador y sonido de la cámara lenta.</summary>
    public class ArmasT1527Pruebas
    {
        private const float VidaPandillero = 60f;

        [Test]
        public void RuedaAjustaZoom_SoloConSniperApuntando()
        {
            Assert.IsTrue(ReglaZoomSniper.RuedaAjustaZoom(ArmaAPie.Sniper, true));
            Assert.IsFalse(ReglaZoomSniper.RuedaAjustaZoom(ArmaAPie.Sniper, false), "sin apuntar, la rueda cambia de arma");
            Assert.IsFalse(ReglaZoomSniper.RuedaAjustaZoom(ArmaAPie.Fusil, true));
        }

        [Test]
        public void AjustarFov_RuedaArribaAcerca_AbajoAleja_YRespetaLimites()
        {
            Assert.Less(ReglaZoomSniper.AjustarFov(8f, 1f, 1.3f, 3f, 24f), 8f);
            Assert.Greater(ReglaZoomSniper.AjustarFov(8f, -1f, 1.3f, 3f, 24f), 8f);
            Assert.AreEqual(3f, ReglaZoomSniper.AjustarFov(3.1f, 1f, 1.3f, 3f, 24f), 0.0001f);
            Assert.AreEqual(24f, ReglaZoomSniper.AjustarFov(23f, -1f, 1.3f, 3f, 24f), 0.0001f);
            Assert.AreEqual(8f, ReglaZoomSniper.AjustarFov(8f, 0f, 1.3f, 3f, 24f), 0.0001f);
        }

        [Test]
        public void Sniper_NoMataDeUnTiroAlCuerpo_PeroLaCabezaSi()
        {
            for (int nivel = 0; nivel < 4; nivel++)
            {
                DatosArma datos = UnityEditor.AssetDatabase.LoadAssetAtPath<DatosArma>("Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_sniper_" + nivel + ".asset");
                Assert.IsNotNull(datos);
                Assert.Less(datos.Danio, VidaPandillero, "cuerpo, nivel " + nivel);
                Assert.GreaterOrEqual(datos.Danio * ReglaZonaDanio.Multiplicador(true), VidaPandillero, "cabeza, nivel " + nivel);
            }
        }

        [TestCase(ArmaAPie.Pistola, ArmaAPie.Revolver, true)]
        [TestCase(ArmaAPie.Lanzagranadas, ArmaAPie.RiflePlasma, true)]
        [TestCase(ArmaAPie.Fusil, ArmaAPie.Subfusil, false)]
        [TestCase(ArmaAPie.Escopeta, ArmaAPie.Sniper, false)]
        public void MismoTipo_AgrupaComoLaRegla(ArmaAPie a, ArmaAPie b, bool esperado)
        {
            Assert.AreEqual(esperado, ReglaTipoArma.MismoTipo(a, b));
        }

        [Test]
        public void Reemplazadas_SeQuedaLaUltimaEnLlegar()
        {
            var llegada = new System.Collections.Generic.List<ArmaAPie> { ArmaAPie.Pistola, ArmaAPie.Fusil, ArmaAPie.Revolver, ArmaAPie.Sniper };
            var vieja = ReglaTipoArma.Reemplazadas(llegada);
            CollectionAssert.AreEqual(new[] { ArmaAPie.Pistola }, vieja);
        }

        [Test]
        public void Reemplazadas_SinRepetidos_NoQuitaNada()
        {
            var llegada = new System.Collections.Generic.List<ArmaAPie> { ArmaAPie.Pistola, ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper, ArmaAPie.Subfusil };
            Assert.IsEmpty(ReglaTipoArma.Reemplazadas(llegada));
        }

        [Test]
        public void CamaraLenta_YaNoSuenaElGolpe()
        {
            Assert.AreEqual("UI_CAMARA_LENTA", MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.IdCamaraLenta);
            Assert.AreNotEqual("PUN_GOLPE_AIRE", MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.IdCamaraLenta);
        }
    }
}
