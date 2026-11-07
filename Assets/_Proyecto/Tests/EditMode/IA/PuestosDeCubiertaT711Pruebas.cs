using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-711: cada tirador toma su propio puesto detrás de la cubierta; los policías prefieren las esquinas.</summary>
    public class PuestosDeCubiertaT711Pruebas
    {
        [Test]
        public void Policia_ProbaPrimeroLasDosEsquinas_LaDeSuLadoPrimero()
        {
            List<float> d = new List<float>();
            PuestosDeCubierta.Ordenar(d, 2f, 1f, true);
            Assert.AreEqual(2f, d[0], 0.001f);
            Assert.AreEqual(-2f, d[1], 0.001f);
            Assert.AreEqual(0f, d[2], 0.001f);
            PuestosDeCubierta.Ordenar(d, 2f, -1f, true);
            Assert.AreEqual(-2f, d[0], 0.001f);
            Assert.AreEqual(2f, d[1], 0.001f);
        }

        [Test]
        public void Pandillero_ProbaElCentroYLuegoLosLados()
        {
            List<float> d = new List<float>();
            PuestosDeCubierta.Ordenar(d, 3f, 1f, false);
            Assert.AreEqual(0f, d[0], 0.001f);
            Assert.AreEqual(PuestosDeCubierta.Separacion, d[1], 0.001f);
            Assert.AreEqual(-PuestosDeCubierta.Separacion, d[2], 0.001f);
            foreach (float o in d)
            {
                Assert.LessOrEqual(System.Math.Abs(o), 3f + 0.001f, "Ningún puesto pasa del tope");
            }
        }

        [Test]
        public void CubiertaCorta_SoloTieneElCentro()
        {
            List<float> d = new List<float>();
            PuestosDeCubierta.Ordenar(d, 0.05f, 1f, true);
            CollectionAssert.AreEqual(new[] { 0f }, d);
        }

        [Test]
        public void DosPolicias_ConLaMismaCubierta_QuedanEnEsquinasDistintas()
        {
            List<float> d = new List<float>();
            List<(float x, float z)> tomados = new List<(float x, float z)>();
            float[] elegidos = new float[2];
            for (int policia = 0; policia < 2; policia++)
            {
                PuestosDeCubierta.Ordenar(d, 2f, 1f, true);
                foreach (float o in d)
                {
                    if (!PuestosDeCubierta.Ocupado(o, 0f, tomados))
                    {
                        elegidos[policia] = o;
                        tomados.Add((o, 0f));
                        break;
                    }
                }
            }
            Assert.AreEqual(2f, elegidos[0], 0.001f);
            Assert.AreEqual(-2f, elegidos[1], 0.001f);
            Assert.GreaterOrEqual(System.Math.Abs(elegidos[0] - elegidos[1]), PuestosDeCubierta.Separacion);
        }

        [Test]
        public void UnPuesto_Ocupado_SoloSiHayOtroACercaDeLaSeparacion()
        {
            List<(float x, float z)> tomados = new List<(float x, float z)> { (0f, 0f) };
            Assert.IsTrue(PuestosDeCubierta.Ocupado(PuestosDeCubierta.Separacion - 0.1f, 0f, tomados));
            Assert.IsFalse(PuestosDeCubierta.Ocupado(PuestosDeCubierta.Separacion + 0.1f, 0f, tomados));
        }
    }
}
