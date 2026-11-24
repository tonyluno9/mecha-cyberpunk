using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>VR3: la regla pura del agarre (multiplicador del implante, lanzamiento, golpe, daño por choque y fin del vuelo).</summary>
    public class ReglaAgarreVRPruebas
    {
        [Test]
        public void Multiplicador_SinImplanteEsCero_Nivel1Tres_Nivel2Cuatro()
        {
            Assert.AreEqual(0f, ReglaAgarreVR.Multiplicador(0));
            Assert.AreEqual(3f, ReglaAgarreVR.Multiplicador(1));
            Assert.AreEqual(4f, ReglaAgarreVR.Multiplicador(2));
        }

        [Test]
        public void VelocidadLanzamiento_MultiplicaLaDeLaManoConTope()
        {
            Vector3 v = ReglaAgarreVR.VelocidadLanzamiento(new Vector3(0f, 0f, 4f), 1);
            Assert.AreEqual(12f, v.z, 1e-4f);
            Assert.AreEqual(ReglaAgarreVR.VelocidadMaxima, ReglaAgarreVR.VelocidadLanzamiento(Vector3.forward * 50f, 2).magnitude, 1e-3f);
        }

        [Test]
        public void VelocidadGolpe_EsFuerteEnLaDireccionDelGolpeYUnPocoArriba()
        {
            Vector3 v = ReglaAgarreVR.VelocidadGolpe(new Vector3(2f, 0f, 0f), 1);
            Assert.GreaterOrEqual(v.magnitude, ReglaAgarreVR.VelocidadMinimaGolpe - 1e-3f);
            Assert.Greater(v.x, 0f);
            Assert.Greater(v.y, 0f);
            Assert.AreEqual(Vector3.zero, ReglaAgarreVR.VelocidadGolpe(Vector3.zero, 1));
        }

        [Test]
        public void EsGolpe_SoloCercaYRapido()
        {
            Assert.IsTrue(ReglaAgarreVR.EsGolpe(0.05f, 2f));
            Assert.IsFalse(ReglaAgarreVR.EsGolpe(0.05f, 1f), "Lento no es golpe.");
            Assert.IsFalse(ReglaAgarreVR.EsGolpe(0.5f, 5f), "Lejos no es golpe.");
        }

        [Test]
        public void DanioChoque_CeroBajoElUmbral_CreceYTieneTope()
        {
            Assert.AreEqual(0f, ReglaAgarreVR.DanioChoque(ReglaAgarreVR.UmbralChoque));
            Assert.AreEqual(ReglaAgarreVR.DanioPorMetroSegundo * 5f, ReglaAgarreVR.DanioChoque(ReglaAgarreVR.UmbralChoque + 5f), 1e-4f);
            Assert.AreEqual(ReglaAgarreVR.DanioMaximoChoque, ReglaAgarreVR.DanioChoque(1000f));
        }

        [Test]
        public void VueloTermino_QuietoTrasElMinimo_OAlMaximo()
        {
            Assert.IsFalse(ReglaAgarreVR.VueloTermino(0.2f, 1f), "Antes del vuelo mínimo no cuenta lo quieto.");
            Assert.IsTrue(ReglaAgarreVR.VueloTermino(1f, ReglaAgarreVR.SegundosQuieto));
            Assert.IsFalse(ReglaAgarreVR.VueloTermino(1f, 0f));
            Assert.IsTrue(ReglaAgarreVR.VueloTermino(ReglaAgarreVR.SegundosVueloMaximo, 0f));
        }
    }
}
