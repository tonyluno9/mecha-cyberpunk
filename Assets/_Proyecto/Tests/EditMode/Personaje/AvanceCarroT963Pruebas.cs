using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-963: el cuerpo entra y sale del carro al ritmo con el que la cadera del clip llega al asiento (o sale de él).</summary>
    public class AvanceCarroT963Pruebas
    {
        [Test]
        public void Subir_AvanzaSegunLoQueLeFaltaALaCadera_YNuncaRetrocede()
        {
            float[] avance = ReglasEntradaCarro.AvanceDesdeDistancias(new[] { 1.8f, 1.8f, 1.2f, 1.3f, 0.4f, 0f }, true);
            Assert.AreEqual(0f, avance[0], 1e-4f);
            Assert.AreEqual(0f, avance[1], 1e-4f, "mientras la cadera no se mueve, el cuerpo tampoco");
            Assert.AreEqual(1f - 1.2f / 1.8f, avance[2], 1e-4f);
            Assert.AreEqual(avance[2], avance[3], 1e-4f, "no retrocede si la cadera se aleja un poco");
            Assert.AreEqual(1f, avance[5], 1e-4f);
        }

        [Test]
        public void Bajar_AvanzaSegunLoQueLaCaderaSeAlejaDelAsiento()
        {
            float[] avance = ReglasEntradaCarro.AvanceDesdeDistancias(new[] { 0f, 0.5f, 1f }, false);
            CollectionAssert.AreEqual(new[] { 0f, 0.5f, 1f }, avance);
        }

        [Test]
        public void ClipQueCasiNoMueveLaCadera_NoDaAvance()
        {
            Assert.IsNull(ReglasEntradaCarro.AvanceDesdeDistancias(new[] { 0.05f, 0f }, true));
            Assert.IsNull(ReglasEntradaCarro.AvanceDesdeDistancias(null, true));
        }

        [Test]
        public void AvanceEn_InterpolaEntreMuestras()
        {
            float[] avance = { 0f, 0.5f, 1f };
            Assert.AreEqual(0.25f, ReglasEntradaCarro.AvanceEn(avance, 0.25f), 1e-4f);
            Assert.AreEqual(1f, ReglasEntradaCarro.AvanceEn(avance, 2f), 1e-4f);
        }
    }
}
