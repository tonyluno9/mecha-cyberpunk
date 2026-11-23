using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.EditMode.IA
{
    public class ReglaSeparacionPandillaT1714Pruebas
    {
        [Test]
        public void PuntoAnillo_DoceMiembros_TodosDistintosYEnElAnillo()
        {
            var puntos = new List<Vector3>();
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = ReglaSeparacionPandilla.PuntoAnillo(new Vector3(110f, 0f, -44f), i);
                float r = new Vector2(p.x - 110f, p.z + 44f).magnitude;
                Assert.GreaterOrEqual(r, ReglaSeparacionPandilla.AnilloMin - 0.01f);
                Assert.LessOrEqual(r, ReglaSeparacionPandilla.AnilloMax + 0.01f);
                foreach (Vector3 otro in puntos)
                {
                    Assert.Greater(Vector3.Distance(p, otro), 0.5f);
                }
                puntos.Add(p);
            }
        }

        [Test]
        public void Empuje_VecinoCerca_EmpujaLejosDeEl()
        {
            Vector3 e = ReglaSeparacionPandilla.Empuje(Vector3.zero, new[] { new Vector3(0.5f, 0f, 0f) }, 0);
            Assert.Less(e.x, 0f);
            Assert.Greater(e.magnitude, 0f);
        }

        [Test]
        public void Empuje_VecinoLejos_NoEmpuja()
        {
            Assert.AreEqual(Vector3.zero, ReglaSeparacionPandilla.Empuje(Vector3.zero, new[] { new Vector3(3f, 0f, 0f) }, 0));
        }

        [Test]
        public void Empuje_MismoPunto_ElijeUnaDireccionPorSemilla()
        {
            Vector3 a = ReglaSeparacionPandilla.Empuje(Vector3.zero, new[] { Vector3.zero }, 1);
            Vector3 b = ReglaSeparacionPandilla.Empuje(Vector3.zero, new[] { Vector3.zero }, 2);
            Assert.Greater(a.magnitude, 0f);
            Assert.Greater(Vector3.Distance(a, b), 0.1f);
        }

        [Test]
        public void EstaEncima_PieAltoSobreOtro_True()
        {
            Assert.IsTrue(ReglaSeparacionPandilla.EstaEncima(new Vector3(0f, 1.6f, 0f), Vector3.zero));
            Assert.IsFalse(ReglaSeparacionPandilla.EstaEncima(new Vector3(0f, 0.2f, 0f), Vector3.zero));
            Assert.IsFalse(ReglaSeparacionPandilla.EstaEncima(new Vector3(3f, 1.6f, 0f), Vector3.zero));
        }

        [Test]
        public void Empuje_Encima_BajaMasRapidoQueElEmpujeSuave()
        {
            Vector3 suave = ReglaSeparacionPandilla.Empuje(new Vector3(0f, 0f, 0f), new[] { new Vector3(0.5f, 0f, 0f) }, 0);
            Vector3 encima = ReglaSeparacionPandilla.Empuje(new Vector3(0f, 1.6f, 0f), new[] { new Vector3(0.1f, 0f, 0f) }, 0);
            Assert.Greater(encima.magnitude, suave.magnitude);
        }
    }
}
