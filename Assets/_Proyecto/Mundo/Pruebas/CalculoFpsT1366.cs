using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>Lógica pura de la medición de FPS (T-1366): promedio, peor 1 % y formato del CSV.</summary>
    public static class CalculoFpsT1366
    {
        public const string Cabecera = "zona;vista;frames;fps_promedio;fps_p1_bajo;ms_promedio;ms_peor1pct;cpu_ms;gpu_ms;renderers_visibles;triangulos;luces_activas_60m;luces_en_vista;draw_calls;setpass_calls";

        /// <summary>Resultado de una vista. Lo que no se pudo medir vale -1.</summary>
        public struct Resultado
        {
            public string Zona;
            public string Vista;
            public int Frames;
            public double MsPromedio;
            public double MsPeor1Pct;
            public double CpuMs;
            public double GpuMs;
            public int RenderersVisibles;
            public long Triangulos;
            public int Luces60m;
            public int LucesEnVista;
            public long DrawCalls;
            public long SetPassCalls;
        }

        /// <summary>Promedio de los tiempos (ms). 0 si no hay.</summary>
        public static double Promedio(IReadOnlyList<float> tiemposMs)
        {
            if (tiemposMs == null || tiemposMs.Count == 0) return 0.0;
            double suma = 0.0;
            for (int i = 0; i < tiemposMs.Count; i++) suma += tiemposMs[i];
            return suma / tiemposMs.Count;
        }

        /// <summary>Promedio de los frames más lentos que forman el <paramref name="porcentaje"/> (0.01 = 1 %); al menos uno. 0 si no hay.</summary>
        public static double PeorPorcentaje(IReadOnlyList<float> tiemposMs, double porcentaje = 0.01)
        {
            if (tiemposMs == null || tiemposMs.Count == 0) return 0.0;
            List<float> orden = new List<float>(tiemposMs);
            orden.Sort();
            int cuantos = (int)Math.Ceiling(orden.Count * porcentaje);
            if (cuantos < 1) cuantos = 1;
            if (cuantos > orden.Count) cuantos = orden.Count;
            double suma = 0.0;
            for (int i = orden.Count - cuantos; i < orden.Count; i++) suma += orden[i];
            return suma / cuantos;
        }

        /// <summary>FPS que corresponden a un tiempo por frame en ms. 0 si el tiempo no es positivo.</summary>
        public static double Fps(double ms) => ms > 0.0 ? 1000.0 / ms : 0.0;

        public static string FilaCsv(Resultado r)
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return string.Join(";",
                r.Zona, r.Vista, r.Frames.ToString(c),
                Fps(r.MsPromedio).ToString("F1", c), Fps(r.MsPeor1Pct).ToString("F1", c),
                r.MsPromedio.ToString("F2", c), r.MsPeor1Pct.ToString("F2", c),
                r.CpuMs.ToString("F2", c), r.GpuMs.ToString("F2", c),
                r.RenderersVisibles.ToString(c), r.Triangulos.ToString(c),
                r.Luces60m.ToString(c), r.LucesEnVista.ToString(c),
                r.DrawCalls.ToString(c), r.SetPassCalls.ToString(c));
        }

        public static string DocumentoCsv(IReadOnlyList<Resultado> resultados)
        {
            StringBuilder sb = new StringBuilder(Cabecera).Append('\n');
            for (int i = 0; i < resultados.Count; i++) sb.Append(FilaCsv(resultados[i])).Append('\n');
            return sb.ToString();
        }

        /// <summary>Texto legible, de la vista más lenta a la más rápida (por ms promedio).</summary>
        public static string Resumen(IReadOnlyList<Resultado> resultados)
        {
            List<Resultado> orden = new List<Resultado>(resultados);
            orden.Sort((a, b) => b.MsPromedio.CompareTo(a.MsPromedio));
            CultureInfo c = CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder("FPS por vista (peor a mejor)\n");
            foreach (Resultado r in orden)
            {
                sb.Append(r.Zona).Append('/').Append(r.Vista).Append(": ")
                  .Append(Fps(r.MsPromedio).ToString("F1", c)).Append(" fps (p1 bajo ").Append(Fps(r.MsPeor1Pct).ToString("F1", c))
                  .Append("), cpu ").Append(r.CpuMs.ToString("F1", c)).Append(" ms, gpu ").Append(r.GpuMs.ToString("F1", c))
                  .Append(" ms, ").Append(r.RenderersVisibles.ToString(c)).Append(" renderers, ")
                  .Append(r.Triangulos.ToString(c)).Append(" tris, ").Append(r.Luces60m.ToString(c)).Append(" luces\n");
            }
            return sb.ToString();
        }
    }
}
