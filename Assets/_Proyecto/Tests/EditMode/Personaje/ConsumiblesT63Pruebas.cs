using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.EditMode.Personaje
{
    public class ConsumiblesT63Pruebas
    {
        [Test]
        public void Aviso_CuracionesSeguidas_SeSuman()
        {
            var aviso = new AvisoCuracion();
            aviso.Sumar(10f, 2f);
            aviso.Sumar(15f, 2f);
            Assert.IsTrue(aviso.Visible);
            Assert.AreEqual(25f, aviso.Total, 0.001f);
        }

        [Test]
        public void Aviso_SinCurarUnRato_SeBorra()
        {
            var aviso = new AvisoCuracion();
            aviso.Sumar(10f, 2f);
            aviso.Avanzar(2.1f);
            Assert.IsFalse(aviso.Visible);
            aviso.Sumar(5f, 2f);
            Assert.AreEqual(5f, aviso.Total, 0.001f, "Una racha nueva no arrastra lo anterior");
        }

        [Test]
        public void Aviso_CeroNoMuestraNada()
        {
            var aviso = new AvisoCuracion();
            aviso.Sumar(0f, 2f);
            Assert.IsFalse(aviso.Visible);
        }

        [Test]
        public void PuedeActuar_APieConControlYVivo_SirveParaCorrerOPelear()
        {
            Assert.IsTrue(ReglaConsumibles.PuedeActuar(ModoJugador.APie, true, true));
            Assert.IsFalse(ReglaConsumibles.PuedeActuar(ModoJugador.APie, true, false));
        }

        [Test]
        public void Sonidos_BebidaYPapitas_LataYBolsa()
        {
            CollectionAssert.AreEqual(new[] { "CON_LATA_ABRIR", "CON_BEBER" }, ReglaSonidoConsumible.Ids("bebida_energetica"));
            CollectionAssert.AreEqual(new[] { "CON_PAPITAS_ABRIR", "CON_PAPITAS_COMER" }, ReglaSonidoConsumible.Ids("papitas"));
        }
    }
}
