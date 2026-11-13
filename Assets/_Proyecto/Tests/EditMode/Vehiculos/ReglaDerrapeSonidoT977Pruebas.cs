using MechaCyberpunk.Vehiculos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-977: las llantas rechinan al frenar fuerte rápido, con freno de mano o al irse de lado; no a cada rato ni despacio.</summary>
    public class ReglaDerrapeSonidoT977Pruebas
    {
        private const float Descansado = 10f;

        [Test]
        public void FrenarFuerteRapido_Rechina_YDespacioNo()
        {
            Assert.IsTrue(ReglaDerrapeSonido.Rechina(15f, 0f, 1f, 0f, false, Descansado));
            Assert.IsFalse(ReglaDerrapeSonido.Rechina(6f, 0f, 1f, 0f, false, Descansado), "frenar a 6 m/s no rechina");
            Assert.IsFalse(ReglaDerrapeSonido.Rechina(15f, 0f, 0.3f, 0f, false, Descansado), "frenar suave no rechina");
        }

        [Test]
        public void VueltaRapidaDeLado_YFrenoDeMano_Rechinan()
        {
            Assert.IsTrue(ReglaDerrapeSonido.Rechina(12f, 4f, 0f, 0f, false, Descansado));
            Assert.IsTrue(ReglaDerrapeSonido.Rechina(8f, 0f, 0f, 1f, false, Descansado));
            Assert.IsFalse(ReglaDerrapeSonido.Rechina(12f, 1f, 0f, 0f, false, Descansado), "una vuelta normal no");
            Assert.IsFalse(ReglaDerrapeSonido.Rechina(2f, 0f, 0f, 1f, false, Descansado), "freno de mano parado no");
        }

        [Test]
        public void ConHisteresis_SigueHastaQueSeEndereza_YDuraLoMinimo()
        {
            Assert.IsTrue(ReglaDerrapeSonido.Rechina(12f, 2.5f, 0f, 0f, true, 1f), "ya rechinando, 2.5 m/s de lado sigue");
            Assert.IsFalse(ReglaDerrapeSonido.Rechina(12f, 2.5f, 0f, 0f, false, Descansado), "pero no arranca con eso");
            Assert.IsFalse(ReglaDerrapeSonido.Rechina(12f, 0.5f, 0f, 0f, true, 1f), "enderezado, para");
            Assert.IsTrue(ReglaDerrapeSonido.Rechina(12f, 0f, 0f, 0f, true, 0.1f), "no corta antes de lo mínimo");
        }

        [Test]
        public void TrasUnDerrape_DescansaAntesDelSiguiente()
        {
            Assert.IsFalse(ReglaDerrapeSonido.Rechina(15f, 5f, 1f, 1f, false, 0.5f), "recién paró: no rearranca");
            Assert.IsTrue(ReglaDerrapeSonido.Rechina(15f, 5f, 1f, 1f, false, ReglaDerrapeSonido.SegundosDescanso + 0.01f));
        }
    }
}
