using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.EditMode.IA
{
    /// <summary>T-1623: la cola de voces de los SKULLS (una a la vez, la ambiental espera más, la prioridad corta).</summary>
    public class ReglaVocesPandillaPruebas
    {
        [Test]
        public void Intentar_OtraVozMientrasSuena_NoSuena()
        {
            ReglaVocesPandilla r = new ReglaVocesPandilla();
            Assert.IsTrue(r.Intentar(10f, "SKU_INSULTO", false, false));
            Assert.IsFalse(r.Intentar(10.5f, "SKU_DOLOR", false, false));
        }

        [Test]
        public void Intentar_PasadoElHueco_SuenaOtra()
        {
            ReglaVocesPandilla r = new ReglaVocesPandilla();
            r.Intentar(10f, "SKU_INSULTO", false, false);
            Assert.IsTrue(r.Intentar(10f + ReglaVocesPandilla.SegundosOcupada + ReglaVocesPandilla.SegundosHueco + 0.01f, "SKU_DOLOR", false, false));
        }

        [Test]
        public void Intentar_Ambiente_EsperaSilencioExtra()
        {
            ReglaVocesPandilla r = new ReglaVocesPandilla();
            r.Intentar(10f, "SKU_INSULTO", false, false);
            float normal = 10f + ReglaVocesPandilla.SegundosOcupada + ReglaVocesPandilla.SegundosHueco + 0.01f;
            Assert.IsFalse(r.Intentar(normal, "SKU_AMBIENTE", true, false));
            Assert.IsTrue(r.Intentar(normal + ReglaVocesPandilla.SegundosExtraAmbiente, "SKU_AMBIENTE", true, false));
        }

        [Test]
        public void Intentar_PrioridadCortaAVozNormal_PeroNoAOtraPrioridadReciente()
        {
            ReglaVocesPandilla r = new ReglaVocesPandilla();
            r.Intentar(10f, "SKU_INSULTO", false, false);
            Assert.IsTrue(r.Intentar(10.2f, "SKU_HUIR", false, true));
            Assert.IsFalse(r.Intentar(10.4f, "SKU_GANAMOS", false, true));
            Assert.IsTrue(r.Intentar(10.9f, "SKU_GANAMOS", false, true));
        }

        [Test]
        public void SegundosHastaPlatica_RespetaElRango()
        {
            Assert.AreEqual(ReglaVocesPandilla.MinimoPlatica, ReglaVocesPandilla.SegundosHastaPlatica(0f), 0.001f);
            Assert.That(ReglaVocesPandilla.SegundosHastaPlatica(0.9999f), Is.LessThanOrEqualTo(ReglaVocesPandilla.MaximoPlatica));
        }
    }
}
