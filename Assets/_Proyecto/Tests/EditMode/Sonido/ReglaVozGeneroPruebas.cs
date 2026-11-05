using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-597: la etiqueta de voz se separa del ID, la voz femenina es más aguda y las voces tienen su política.</summary>
    public class ReglaVozGeneroPruebas
    {
        [Test]
        public void Separar_SinEtiqueta_DevuelveElMismoId()
        {
            Assert.AreEqual("JUG_DANIO", ReglaVozGenero.Separar("JUG_DANIO", out string e));
            Assert.AreEqual(string.Empty, e);
            Assert.IsNull(ReglaVozGenero.Separar(null, out _));
        }

        [Test]
        public void Tono_LaFemeninaEsMasAgudaQueLaMasculina()
        {
            Assert.Greater(ReglaVozGenero.TonoDeGenero("F"), 1f);
            Assert.Less(ReglaVozGenero.TonoDeGenero("M"), 1f);
            Assert.AreEqual(1f, ReglaVozGenero.TonoDeGenero(string.Empty));
            Assert.AreEqual("JUG_DANIO_F", ReglaVozGenero.IdDelGenero("JUG_DANIO", "F"));
        }

        [TestCase("JUG_DANIO")]
        [TestCase("JUG_CAE_DERROTADO")]
        [TestCase("JUG_CORRER_RESPIRA")]
        [TestCase("PAN_RIVAL_DANIO")]
        [TestCase("PAN_RIVAL_CAE")]
        [TestCase("PAN_ALIADO_RECLUTA")]
        public void Voces_TienenLimiteDeVocesYVariacionDeTono(string id)
        {
            PoliticaSonido p = ReglaMezclaSonidos.Para(id);
            Assert.Greater(p.MaxVoces, 0, id);
            Assert.Greater(p.VariacionTono, 0f, id);
            Assert.Greater(p.DistanciaMaxima, 0f, id);
        }
    }
}
