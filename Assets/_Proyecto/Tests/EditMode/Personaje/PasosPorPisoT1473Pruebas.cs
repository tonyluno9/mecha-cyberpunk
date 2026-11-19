using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1473: el piso decide el ID del paso, los pasos tienen tiempo mínimo y los sonidos del jugador piden los IDs nuevos.</summary>
    public class PasosPorPisoT1473Pruebas
    {
        [TestCase("Lamina_Techo", "PAS_METAL")]
        [TestCase("Contenedor_Rojo", "PAS_METAL")]
        [TestCase("PU08_Piso", "PAS_METAL")]
        [TestCase("Rejilla", "PAS_METAL")]
        [TestCase("Muelle_Tablones", "PAS_MADERA")]
        [TestCase("Madera_Vieja", "PAS_MADERA")]
        [TestCase("Tierra_Lote", "PAS_TIERRA")]
        [TestCase("Deshuesadero_Suelo", "PAS_TIERRA")]
        [TestCase("Grava_Obra", "PAS_GRAVA")]
        [TestCase("Charco_01", "PAS_MOJADO")]
        [TestCase("Agua", "PAS_MOJADO")]
        [TestCase("Banqueta", "PAS_CONCRETO")]
        [TestCase("Asfalto", "PAS_CONCRETO")]
        [TestCase("|Material_Gris|Piso_Azotea|Edificio", "PAS_CONCRETO")]
        [TestCase("|M|Piso|Muelle_Norte", "PAS_MADERA")]
        public void Superficie_ElNombreDelPisoDaElId(string piso, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSuperficiePaso.IdPaso(IdLugar.Calle, piso, false));
        }

        [Test]
        public void Superficie_ConLluviaLaCalleSuenaMojada_PeroElMetalSigueMetal()
        {
            Assert.AreEqual("PAS_MOJADO", ReglaSuperficiePaso.IdPaso(IdLugar.Calle, "Asfalto", false, true));
            Assert.AreEqual("PAS_METAL", ReglaSuperficiePaso.IdPaso(IdLugar.Calle, "Metal", false, true));
        }

        [Test]
        public void Limitador_NoDejaDosPasosAntesDelMinimo()
        {
            LimitadorPasos l = new LimitadorPasos(0.25f);
            Assert.IsTrue(l.Permitir(1f));
            Assert.IsFalse(l.Permitir(1.1f));
            Assert.IsFalse(l.Permitir(1.24f));
            Assert.IsTrue(l.Permitir(1.3f));
        }

        [Test]
        public void Aterrizaje_SoloSuenaConCaidaMayorA1m20()
        {
            Assert.IsFalse(ReglaSonidosJugador.DebeSonarAterrizaje(0.5f));
            Assert.IsFalse(ReglaSonidosJugador.DebeSonarAterrizaje(1.2f));
            Assert.IsTrue(ReglaSonidosJugador.DebeSonarAterrizaje(2f));
        }

        [Test]
        public void Eventos_LosIdsDelJugadorSonLosNuevos()
        {
            Assert.AreEqual("JUG_ATERRIZAJE", ReglaSonidosJugador.Aterrizaje);
            Assert.AreEqual("JUG_RODADA", ReglaSonidosJugador.Rodada);
            Assert.AreEqual("JUG_TREPAR", ReglaSonidosJugador.Trepar);
            Assert.AreEqual("MEL_GOLPE_CARNE", ReglaSonidosJugador.IdGolpe(true));
            Assert.AreEqual("MEL_GOLPE_AIRE", ReglaSonidosJugador.IdGolpe(false));
        }
    }
}
