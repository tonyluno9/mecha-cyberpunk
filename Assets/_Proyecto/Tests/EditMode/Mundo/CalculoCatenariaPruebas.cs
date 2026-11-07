using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>S2c (7-nov): los cables cuelgan entre sus dos puntos.</summary>
    public class CalculoCatenariaPruebas
    {
        private static readonly Vector3 A = new Vector3(0f, 7f, 0f);
        private static readonly Vector3 B = new Vector3(20f, 6f, 4f);

        [Test]
        public void Puntos_EmpiezanYTerminanEnLosExtremos()
        {
            Vector3[] p = CalculoCatenaria.Puntos(A, B, 0.8f, 10);
            Assert.AreEqual(11, p.Length);
            Assert.Less(Vector3.Distance(A, p[0]), 0.0001f);
            Assert.Less(Vector3.Distance(B, p[10]), 0.0001f);
        }

        [Test]
        public void Punto_EnMedioBajaLaFlecha()
        {
            Vector3 medio = CalculoCatenaria.Punto(A, B, 0.8f, 0.5f);
            Assert.AreEqual((A.y + B.y) * 0.5f - 0.8f, medio.y, 0.0001f);
            Assert.AreEqual(10f, medio.x, 0.0001f);
        }

        [Test]
        public void Puntos_NingunoArribaDeLaRecta()
        {
            foreach (Vector3 p in CalculoCatenaria.Puntos(A, B, 0.5f, 16))
            {
                float t = p.x / B.x;
                Assert.LessOrEqual(p.y, Mathf.Lerp(A.y, B.y, t) + 0.0001f);
            }
        }

        [Test]
        public void FlechaNatural_CreceConElLargoYTieneMinimo()
        {
            Assert.AreEqual(0.3f, CalculoCatenaria.FlechaNatural(2f, 0.03f, 0.3f), 0.0001f);
            Assert.AreEqual(0.9f, CalculoCatenaria.FlechaNatural(30f, 0.03f, 0.3f), 0.0001f);
        }
    }
}
