using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-673: el pecho al que apuntan los enemigos baja con la cápsula del jugador agachado.</summary>
    public class AlturaPechoT673Pruebas
    {
        [Test]
        public void AlturaPecho_DePieEsLaNormal_AgachadoBaja_ConPiso()
        {
            Assert.AreEqual(1.2f, ReglaPandilleroAPie.AlturaPechoSegunCuerpo(1.2f, 1.8f, 1.8f), 0.001f);
            Assert.AreEqual(1.2f * 1.0f / 1.8f, ReglaPandilleroAPie.AlturaPechoSegunCuerpo(1.2f, 1.0f, 1.8f), 0.001f);
            Assert.AreEqual(1.2f, ReglaPandilleroAPie.AlturaPechoSegunCuerpo(1.2f, 2.5f, 1.8f), 0.001f, "Más alto que de pie no sube");
            Assert.AreEqual(1.2f * 0.3f, ReglaPandilleroAPie.AlturaPechoSegunCuerpo(1.2f, 0.1f, 1.8f), 0.001f, "Nunca baja del 30 %");
            Assert.AreEqual(1.2f, ReglaPandilleroAPie.AlturaPechoSegunCuerpo(1.2f, 1.0f, 0f), 0.001f);
        }
    }
}
