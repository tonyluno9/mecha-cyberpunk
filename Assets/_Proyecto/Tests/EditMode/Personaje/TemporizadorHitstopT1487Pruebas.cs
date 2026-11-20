using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1487: el hitstop dura lo pedido, se alarga con otro mayor y termina una sola vez.</summary>
    public class TemporizadorHitstopT1487Pruebas
    {
        [Test]
        public void Avanzar_TerminaAlCumplirseElTiempo()
        {
            TemporizadorHitstop t = new TemporizadorHitstop();
            t.Iniciar(0.07f);
            Assert.IsTrue(t.Activo);
            Assert.IsFalse(t.Avanzar(0.05f));
            Assert.IsTrue(t.Avanzar(0.05f));
            Assert.IsFalse(t.Activo);
            Assert.IsFalse(t.Avanzar(0.05f), "solo avisa una vez");
        }

        [Test]
        public void Iniciar_ConUnoMenorEnCurso_ConservaElMayor()
        {
            TemporizadorHitstop t = new TemporizadorHitstop();
            t.Iniciar(0.08f);
            t.Iniciar(0.05f);
            Assert.AreEqual(0.08f, t.Restante, 1e-5f);
            t.Iniciar(0.1f);
            Assert.AreEqual(0.1f, t.Restante, 1e-5f);
        }

        [Test]
        public void Cancelar_ApagaElHitstop()
        {
            TemporizadorHitstop t = new TemporizadorHitstop();
            t.Iniciar(0.08f);
            t.Cancelar();
            Assert.IsFalse(t.Activo);
        }
    }
}
