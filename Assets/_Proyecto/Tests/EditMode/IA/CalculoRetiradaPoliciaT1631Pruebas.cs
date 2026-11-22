using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1631: reglas puras de la retirada de un policía a pie cuando Nova interviene.</summary>
    public class CalculoRetiradaPoliciaT1631Pruebas
    {
        [Test]
        public void Rumbo_EsHaciaAfuera_EnElPlano()
        {
            Vector3 r = CalculoRetiradaPolicia.Rumbo(new Vector3(10f, 3f, 0f), new Vector3(0f, 0f, 0f), Vector3.forward);
            Assert.AreEqual(1f, r.x, 0.0001f);
            Assert.AreEqual(0f, r.y, 0.0001f);
            Assert.AreEqual(0f, r.z, 0.0001f);
        }

        [Test]
        public void Rumbo_Encimados_UsaElRespaldo()
        {
            Vector3 r = CalculoRetiradaPolicia.Rumbo(Vector3.zero, Vector3.zero, Vector3.right);
            Assert.AreEqual(1f, r.x, 0.0001f);
        }

        [Test]
        public void RumboDesviado_AlternaLados_Y_CadaVezMasAbierto()
        {
            Vector3 rumbo = Vector3.forward;
            Assert.AreEqual(0f, Vector3.SignedAngle(rumbo, CalculoRetiradaPolicia.RumboDesviado(rumbo, 0), Vector3.up), 0.01f);
            Assert.AreEqual(50f, Vector3.SignedAngle(rumbo, CalculoRetiradaPolicia.RumboDesviado(rumbo, 1), Vector3.up), 0.01f);
            Assert.AreEqual(-50f, Vector3.SignedAngle(rumbo, CalculoRetiradaPolicia.RumboDesviado(rumbo, 2), Vector3.up), 0.01f);
            Assert.AreEqual(100f, Vector3.SignedAngle(rumbo, CalculoRetiradaPolicia.RumboDesviado(rumbo, 3), Vector3.up), 0.01f);
        }

        [Test]
        public void PuedeDesaparecer_SoloLejosYSinQueNadieLoVea()
        {
            Assert.IsTrue(CalculoRetiradaPolicia.PuedeDesaparecer(20f, false, 15f));
            Assert.IsFalse(CalculoRetiradaPolicia.PuedeDesaparecer(20f, true, 15f), "nunca frente al jugador");
            Assert.IsFalse(CalculoRetiradaPolicia.PuedeDesaparecer(5f, false, 15f), "ni cerca aunque no se vea");
        }
    }
}
