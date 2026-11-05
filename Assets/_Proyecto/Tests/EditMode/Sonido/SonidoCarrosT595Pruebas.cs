using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-595: el claxon queda limitado, variado y con alcance corto.</summary>
    public class SonidoCarrosT595Pruebas
    {
        [Test]
        public void Claxon_TieneLimiteDeVocesMinimoDeTiempoVariacionYAlcanceCorto()
        {
            PoliticaSonido p = ReglaMezclaSonidos.Para("VEH_CLAXON_TRAFICO");
            Assert.AreEqual(2, p.MaxVoces);
            Assert.GreaterOrEqual(p.SegundosMinimos, 0.5f);
            Assert.GreaterOrEqual(p.VariacionTono, 0.1f);
            Assert.LessOrEqual(p.DistanciaMaxima, 30f);
            Assert.Less(p.Volumen, 1f);
            Assert.AreEqual(1, ReglaMezclaSonidos.Para("VEH_PASA_CERCA").MaxVoces);
        }
    }
}
