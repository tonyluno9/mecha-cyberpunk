using MechaCyberpunk.UI.Armas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class ReglaTutorialTinoPruebas
    {
        private static PasoTutorialTino Paso(bool completo, bool tienePistola, bool enMejorar, bool pistolaCompra, bool pistolaMejorar)
            => ReglaTutorialTino.Calcular(true, completo, tienePistola, enMejorar, pistolaCompra, pistolaMejorar);

        [Test]
        public void Calcular_Apagado_Ninguno()
        {
            Assert.AreEqual(PasoTutorialTino.Ninguno, ReglaTutorialTino.Calcular(false, false, false, false, false, false));
        }

        [Test]
        public void Calcular_SinPistolaYOtraArmaElegida_ElegirPistola()
        {
            Assert.AreEqual(PasoTutorialTino.ElegirPistola, Paso(false, false, false, false, false));
        }

        [Test]
        public void Calcular_SinPistolaYPistolaElegida_Comprar()
        {
            Assert.AreEqual(PasoTutorialTino.Comprar, Paso(false, false, false, true, false));
        }

        [Test]
        public void Calcular_SinPistolaEnMejorar_VolverAComprar()
        {
            Assert.AreEqual(PasoTutorialTino.VolverAComprar, Paso(false, false, true, false, false));
        }

        [Test]
        public void Calcular_ConPistolaEnComprar_IrAMejorar()
        {
            Assert.AreEqual(PasoTutorialTino.IrAMejorar, Paso(false, true, false, false, false));
        }

        [Test]
        public void Calcular_EnMejorarConOtraArma_ElegirPistolaEnMejorar()
        {
            Assert.AreEqual(PasoTutorialTino.ElegirPistolaEnMejorar, Paso(false, true, true, false, false));
        }

        [Test]
        public void Calcular_EnMejorarConPistolaElegida_Instalar()
        {
            Assert.AreEqual(PasoTutorialTino.Instalar, Paso(false, true, true, false, true));
        }

        [Test]
        public void Calcular_Completo_SalirSinImportarLaPestana()
        {
            Assert.AreEqual(PasoTutorialTino.Salir, Paso(true, true, true, false, true));
            Assert.AreEqual(PasoTutorialTino.Salir, Paso(true, true, false, false, false));
        }

        [Test]
        public void Numero_PasosExtra_SeAgrupanEnLosCinco()
        {
            Assert.AreEqual(1, ReglaTutorialTino.Numero(PasoTutorialTino.VolverAComprar));
            Assert.AreEqual(4, ReglaTutorialTino.Numero(PasoTutorialTino.ElegirPistolaEnMejorar));
            Assert.AreEqual(5, ReglaTutorialTino.Numero(PasoTutorialTino.Salir));
            Assert.AreEqual(0, ReglaTutorialTino.Numero(PasoTutorialTino.Ninguno));
        }

        [Test]
        public void Linea_Comprar_DiceElPasoYLaInstruccion()
        {
            Assert.AreEqual("PASO 2 DE 5: AHORA PULSA COMPRAR", ReglaTutorialTino.Linea(PasoTutorialTino.Comprar));
            Assert.AreEqual(string.Empty, ReglaTutorialTino.Linea(PasoTutorialTino.Ninguno));
        }

        [Test]
        public void Objetivos_CadaPaso_ApuntaAlControlCorrecto()
        {
            CollectionAssert.AreEqual(new[] { "BotonComprar" }, ReglaTutorialTino.Objetivos(PasoTutorialTino.Comprar));
            CollectionAssert.AreEqual(new[] { "Pestana_Mejorar" }, ReglaTutorialTino.Objetivos(PasoTutorialTino.IrAMejorar));
            CollectionAssert.AreEqual(new[] { "Arma_Pistola" }, ReglaTutorialTino.Objetivos(PasoTutorialTino.ElegirPistolaEnMejorar));
            CollectionAssert.AreEqual(new[] { "BotonMejorar" }, ReglaTutorialTino.Objetivos(PasoTutorialTino.Instalar));
            CollectionAssert.AreEqual(new[] { "Extra_CAMPO DE TIRO", "BotonSalirBanco" }, ReglaTutorialTino.Objetivos(PasoTutorialTino.Salir));
            Assert.IsTrue(ReglaTutorialTino.ResaltaFilaPistolaCompra(PasoTutorialTino.ElegirPistola));
        }

        [Test]
        public void PermiteSalir_SoloEnElPasoCinco()
        {
            Assert.IsTrue(ReglaTutorialTino.PermiteSalir(PasoTutorialTino.Salir));
            Assert.IsFalse(ReglaTutorialTino.PermiteSalir(PasoTutorialTino.Instalar));
            Assert.IsFalse(ReglaTutorialTino.PermiteSalir(PasoTutorialTino.ElegirPistola));
        }
    }
}
