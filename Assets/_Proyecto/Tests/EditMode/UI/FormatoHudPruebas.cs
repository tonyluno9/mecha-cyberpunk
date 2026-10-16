using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    public class FormatoHudPruebas
    {
        private ConfigHudAPie config;

        [SetUp]
        public void Setup()
        {
            config = ScriptableObject.CreateInstance<ConfigHudAPie>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [TestCase(100f, "VIDA 100")]
        [TestCase(75f, "VIDA 75")]
        [TestCase(74.2f, "VIDA 75")]
        [TestCase(0.1f, "VIDA 1")]
        [TestCase(0f, "VIDA 0")]
        [TestCase(-5f, "VIDA 0")]
        public void TextoVida_DaElTextoRedondeadoHaciaArriba(float actual, string esperado)
        {
            Assert.AreEqual(esperado, FormatoHud.TextoVida(actual, config));
        }

        [TestCase(100f, 100f, 1f)]
        [TestCase(75f, 100f, 0.75f)]
        [TestCase(0f, 100f, 0f)]
        [TestCase(150f, 100f, 1f)]
        [TestCase(-10f, 100f, 0f)]
        [TestCase(50f, 0f, 0f)]
        public void Fraccion_QuedaEntreCeroYUno(float actual, float maxima, float esperada)
        {
            Assert.AreEqual(esperada, FormatoHud.Fraccion(actual, maxima), 0.0001f);
        }

        [Test]
        public void EsVidaBaja_EnElUmbralYDebajoEsBaja_ArribaNo()
        {
            Assert.IsTrue(FormatoHud.EsVidaBaja(30f, 100f, 0.3f), "En el umbral cuenta como baja");
            Assert.IsTrue(FormatoHud.EsVidaBaja(25f, 100f, 0.3f));
            Assert.IsTrue(FormatoHud.EsVidaBaja(0f, 100f, 0.3f));
            Assert.IsFalse(FormatoHud.EsVidaBaja(31f, 100f, 0.3f));
            Assert.IsFalse(FormatoHud.EsVidaBaja(100f, 100f, 0.3f));
        }

        [Test]
        public void TextoArma_Punos_DicePunos()
        {
            Assert.AreEqual("PUÑOS", FormatoHud.TextoArma(ArmaAPie.Punos, 8, 8, false, config));
        }

        [Test]
        public void TextoArma_PistolaConBalas_MuestraBalasYCargador()
        {
            Assert.AreEqual("PISTOLA 7/8", FormatoHud.TextoArma(ArmaAPie.Pistola, 7, 8, false, config));
            Assert.AreEqual("PISTOLA 8/8", FormatoHud.TextoArma(ArmaAPie.Pistola, 8, 8, false, config));
        }

        [Test]
        public void TextoArma_PistolaRecargando_DiceRecargando()
        {
            Assert.AreEqual("PISTOLA · RECARGANDO", FormatoHud.TextoArma(ArmaAPie.Pistola, 3, 8, true, config));
        }

        [Test]
        public void TextoArma_PistolaVacia_PideRecargar()
        {
            Assert.AreEqual("PISTOLA 0/8 · R", FormatoHud.TextoArma(ArmaAPie.Pistola, 0, 8, false, config));
        }

        [TestCase(600, MotivoDinero.Premio, "+$600 · PREMIO")]
        [TestCase(-650, MotivoDinero.Reparacion, "-$650 · REPARACIÓN")]
        [TestCase(-500, MotivoDinero.Renta, "-$500 · RENTA")]
        [TestCase(500, MotivoDinero.Otro, "+$500 · OTRO")]
        [TestCase(1250, MotivoDinero.Casino, "+$1,250 · CASINO")]
        public void TextoAvisoDinero_DaSignoCantidadYMotivo(int diferencia, MotivoDinero motivo, string esperado)
        {
            Assert.AreEqual(esperado, FormatoHud.TextoAvisoDinero(diferencia, motivo, config));
        }

        [TestCase(600)]
        [TestCase(-300)]
        public void TextoAvisoDinero_Carga_EsVacio(int diferencia)
        {
            Assert.AreEqual(string.Empty, FormatoHud.TextoAvisoDinero(diferencia, MotivoDinero.Carga, config));
        }

        [Test]
        public void TextoRentaVencida_MuestraElAdeudo()
        {
            Assert.AreEqual("RENTA VENCIDA · $500", FormatoHud.TextoRentaVencida(500, config));
        }
    }
}
