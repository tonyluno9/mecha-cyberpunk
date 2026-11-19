using System.Collections.Generic;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1366: lógica pura de la medición de FPS (promedio, peor 1 %, CSV) y la lista de vistas.</summary>
    public class MedidorFpsT1366Pruebas
    {
        [Test]
        public void Promedio_TiemposConocidos_DaLaMedia()
        {
            Assert.AreEqual(20.0, CalculoFpsT1366.Promedio(new List<float> { 10f, 20f, 30f }), 1e-6);
        }

        [Test]
        public void Promedio_ListaVacia_DaCero()
        {
            Assert.AreEqual(0.0, CalculoFpsT1366.Promedio(new List<float>()), 1e-9);
        }

        [Test]
        public void PeorPorcentaje_CienFrames_PromediaElMasLento()
        {
            List<float> tiempos = new List<float>();
            for (int i = 0; i < 99; i++) tiempos.Add(10f);
            tiempos.Add(50f);
            Assert.AreEqual(50.0, CalculoFpsT1366.PeorPorcentaje(tiempos), 1e-6);
        }

        [Test]
        public void PeorPorcentaje_DosCientosFrames_PromediaLosDosMasLentos()
        {
            List<float> tiempos = new List<float>();
            for (int i = 0; i < 198; i++) tiempos.Add(10f);
            tiempos.Add(40f);
            tiempos.Add(20f);
            Assert.AreEqual(30.0, CalculoFpsT1366.PeorPorcentaje(tiempos), 1e-6);
        }

        [Test]
        public void PeorPorcentaje_PocosFrames_UsaAlMenosUno()
        {
            Assert.AreEqual(25.0, CalculoFpsT1366.PeorPorcentaje(new List<float> { 5f, 25f, 10f }), 1e-6);
        }

        [Test]
        public void Fps_SesentaYSeisMilisegundos_Redondea()
        {
            Assert.AreEqual(60.0, CalculoFpsT1366.Fps(1000.0 / 60.0), 1e-6);
            Assert.AreEqual(0.0, CalculoFpsT1366.Fps(0.0), 1e-9);
        }

        [Test]
        public void FilaCsv_Resultado_UsaPuntoDecimalYElOrdenDeLaCabecera()
        {
            CalculoFpsT1366.Resultado r = new CalculoFpsT1366.Resultado
            {
                Zona = "Cerro", Vista = "aerea", Frames = 300, MsPromedio = 16.6667, MsPeor1Pct = 33.3333,
                CpuMs = 5.5, GpuMs = -1.0, RenderersVisibles = 1200, Triangulos = 450000, Luces60m = 14, LucesEnVista = 20,
                DrawCalls = 900, SetPassCalls = -1,
            };
            Assert.AreEqual("Cerro;aerea;300;60.0;30.0;16.67;33.33;5.50;-1.00;1200;450000;14;20;900;-1", CalculoFpsT1366.FilaCsv(r));
        }

        [Test]
        public void DocumentoCsv_DosResultados_LlevaCabeceraYDosFilas()
        {
            List<CalculoFpsT1366.Resultado> lista = new List<CalculoFpsT1366.Resultado>
            {
                new CalculoFpsT1366.Resultado { Zona = "A", Vista = "x", MsPromedio = 10.0, MsPeor1Pct = 20.0 },
                new CalculoFpsT1366.Resultado { Zona = "B", Vista = "y", MsPromedio = 20.0, MsPeor1Pct = 40.0 },
            };
            string[] lineas = CalculoFpsT1366.DocumentoCsv(lista).TrimEnd('\n').Split('\n');
            Assert.AreEqual(3, lineas.Length);
            Assert.AreEqual(CalculoFpsT1366.Cabecera, lineas[0]);
            Assert.AreEqual(CalculoFpsT1366.Cabecera.Split(';').Length, lineas[1].Split(';').Length);
            StringAssert.StartsWith("B;y;", lineas[2]);
        }

        [Test]
        public void Resumen_VariasVistas_ListaLaMasLentaPrimero()
        {
            List<CalculoFpsT1366.Resultado> lista = new List<CalculoFpsT1366.Resultado>
            {
                new CalculoFpsT1366.Resultado { Zona = "Rapida", Vista = "a", MsPromedio = 5.0, MsPeor1Pct = 6.0 },
                new CalculoFpsT1366.Resultado { Zona = "Lenta", Vista = "b", MsPromedio = 50.0, MsPeor1Pct = 60.0 },
            };
            string texto = CalculoFpsT1366.Resumen(lista);
            Assert.Less(texto.IndexOf("Lenta/b"), texto.IndexOf("Rapida/a"));
        }

        [Test]
        public void Vistas_Lista_CubreLasCuatroZonasYTresBarrios()
        {
            HashSet<string> zonas = new HashSet<string>();
            int barrios = 0;
            foreach (VistaFpsT1366 v in VistasFpsT1366.Todas)
            {
                zonas.Add(v.Zona);
                if (v.Zona == "Barrio") barrios++;
            }
            CollectionAssert.IsSupersetOf(zonas, new[] { "Malecon", "Cerro", "Autopista", "Puerto" });
            Assert.AreEqual(3, barrios);
        }

        [Test]
        public void Resolver_SueloEnDiez_PonePosicionSobreElSuelo()
        {
            VistaFpsT1366 vista = new VistaFpsT1366("Z", "v", 1f, 2f, 3f, 4f, 1.7f, 0f);
            VistasFpsT1366.Resolver(vista, (x, z, y) => 10f, out UnityEngine.Vector3 pos, out UnityEngine.Vector3 mirar);
            Assert.AreEqual(11.7f, pos.y, 1e-4f);
            Assert.AreEqual(10f, mirar.y, 1e-4f);
            Assert.AreEqual(3f, mirar.x, 1e-4f);
        }
    }
}
