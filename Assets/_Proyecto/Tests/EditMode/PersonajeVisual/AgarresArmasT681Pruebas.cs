using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-681: el cañón del arma se lleva exactamente hacia una dirección (el punto de mira), con la corredera arriba.</summary>
    public class AgarresArmasT681Pruebas
    {
        [TestCase(-0.5f, 0.2f, 0.85f)]
        [TestCase(0.6f, -0.3f, 0.7f)]
        [TestCase(0f, 0.9f, 0.4f)]
        public void CorreccionHaciaDireccion_LlevaElCanonALaDireccion_YNivelaLaCorredera(float x, float y, float z)
        {
            Quaternion arma = Quaternion.LookRotation(new Vector3(-0.5f, 0.2f, 0.85f).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 25f);
            Vector3 deseada = new Vector3(x, y, z).normalized;
            Quaternion c = AgarresArmas.CorreccionHaciaDireccion(arma, deseada, 1f, 180f);
            Quaternion final = c * arma;
            Assert.Less(Vector3.Angle(final * Vector3.forward, deseada), 0.1f, "El cañón apunta a la dirección");
            Assert.AreEqual(0f, Vector3.Dot(final * Vector3.right, Vector3.up), 0.01f, "La corredera queda arriba, sin ladearse");
            Assert.Greater(Vector3.Dot(final * Vector3.up, Vector3.up), 0f);
        }

        [Test]
        public void CorreccionHaciaDireccion_SinPeso_NoGira()
        {
            Quaternion arma = Quaternion.Euler(10f, 80f, 20f);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, AgarresArmas.CorreccionHaciaDireccion(arma, Vector3.forward, 0f, 90f)), 0.001f);
        }

        [Test]
        public void CorreccionHaciaDireccion_LimitaElGiro()
        {
            Quaternion arma = Quaternion.LookRotation(Vector3.back, Vector3.up);
            float grados = Quaternion.Angle(Quaternion.identity, AgarresArmas.CorreccionHaciaDireccion(arma, Vector3.forward, 1f, 30f));
            Assert.LessOrEqual(grados, 60.5f, "El cañón y el ladeo se limitan cada uno a 30°");
        }

        [Test]
        public void CorreccionHaciaDireccion_DireccionNula_NoGira()
        {
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, AgarresArmas.CorreccionHaciaDireccion(Quaternion.identity, Vector3.zero, 1f, 90f)), 0.001f);
        }
    }
}
