using System.Collections.Generic;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-rendimiento-tope-24nov: la parte pura del modo de medición (argumento, plan de pasos, topes derivados, fps promedio y 1 % bajo).</summary>
    public class ReglaMedicionRendimientoPruebas
    {
        [Test]
        public void TieneArgumento_SoloConElArgumentoExacto()
        {
            Assert.IsTrue(ReglaMedicionRendimiento.TieneArgumento(new[] { "CyberCity.exe", "-logFile", "x.log", "-medirrendimiento" }));
            Assert.IsFalse(ReglaMedicionRendimiento.TieneArgumento(new[] { "CyberCity.exe", "-medirRendimientoVR" }));
            Assert.IsFalse(ReglaMedicionRendimiento.TieneArgumento(new[] { "CyberCity.exe" }));
            Assert.IsFalse(ReglaMedicionRendimiento.TieneArgumento(null));
        }

        [Test]
        public void Plan_Fases1a5ConElAsset_YLaFase6ConCadaTope()
        {
            List<PasoMedicionRendimiento> plan = ReglaMedicionRendimiento.Plan(6, ReglaMedicionRendimiento.TopesPorOmision);
            Assert.AreEqual(5 + 7, plan.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i + 1, plan[i].Fase);
                Assert.AreEqual(0, plan[i].TopeEnemigos);
            }
            int[] esperados = { 6, 9, 12, 15, 18, 24, 30 };
            for (int i = 0; i < esperados.Length; i++)
            {
                Assert.AreEqual(6, plan[5 + i].Fase);
                Assert.AreEqual(esperados[i], plan[5 + i].TopeEnemigos);
            }
        }

        [Test]
        public void TopesDerivados_NuncaBajanDelMinimo_YCrecenConLosEnemigos()
        {
            Assert.AreEqual(3, ReglaMedicionRendimiento.TopePatrullas(6));
            Assert.AreEqual(8, ReglaMedicionRendimiento.TopePatrullas(30));
            Assert.AreEqual(4, ReglaMedicionRendimiento.TopeDrones(6));
            Assert.AreEqual(10, ReglaMedicionRendimiento.TopeDrones(30));
            Assert.AreEqual(2, ReglaMedicionRendimiento.TopeCapsulas(9));
            Assert.AreEqual(5, ReglaMedicionRendimiento.TopeCapsulas(30));
            Assert.AreEqual(2, ReglaMedicionRendimiento.TopeRobotsGrandes(12));
            Assert.AreEqual(4, ReglaMedicionRendimiento.TopeRobotsGrandes(30));
        }

        [Test]
        public void Fps_PromedioY1PorCientoBajo_DeLosTiemposPorCuadro()
        {
            // 198 cuadros de 10 ms y 2 de 50 ms: promedio 200 / 2.08 s; el 1 % más lento (2 cuadros) va a 20 fps.
            var cuadros = new List<float>();
            for (int i = 0; i < 198; i++)
            {
                cuadros.Add(0.01f);
            }
            cuadros.Insert(50, 0.05f);
            cuadros.Insert(120, 0.05f);
            Assert.AreEqual(200f / 2.08f, ReglaMedicionRendimiento.FpsPromedio(cuadros), 0.05f);
            Assert.AreEqual(20f, ReglaMedicionRendimiento.FpsUnoPorCientoBajo(cuadros), 0.01f);
            Assert.AreEqual(0f, ReglaMedicionRendimiento.FpsPromedio(new List<float>()));
            Assert.AreEqual(0f, ReglaMedicionRendimiento.FpsUnoPorCientoBajo(null));
        }
    }
}
