using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-rendimiento-tope-24nov: la parte pura del modo de medición de <see cref="MedidorRendimiento"/>: si el argumento está en la línea de comandos,
    /// el plan de pasos (fases 1 a 6 con los topes del asset y luego la fase 6 con topes de enemigos crecientes), los topes derivados
    /// (patrullas, drones, cápsulas y robots grandes) y el cálculo de fps promedio y 1 % más bajo a partir de los tiempos por cuadro.
    /// </summary>
    public static class ReglaMedicionRendimiento
    {
        /// <summary>El argumento que enciende la medición: <c>CyberCity.exe -medirRendimiento -logFile x.log</c>.</summary>
        public const string Argumento = "-medirRendimiento";

        /// <summary>Número de fases del modo libre que se recorren con los topes del asset.</summary>
        public const int FasesLibre = 6;

        /// <summary>Topes de enemigos simultáneos que se prueban en la fase 6 (NOVA pesada).</summary>
        public static readonly int[] TopesPorOmision = { 6, 9, 12, 15, 18, 24, 30 };

        /// <summary>Fracción de cuadros más lentos que forman el "1 % más bajo".</summary>
        public const float FraccionPeores = 0.01f;

        private const int MinimoPatrullas = 3;
        private const int MinimoDrones = 4;
        private const int MinimoCapsulas = 2;
        private const int MinimoRobotsGrandes = 2;
        private const int EnemigosPorPatrulla = 4;
        private const int EnemigosPorDron = 3;
        private const int EnemigosPorCapsula = 6;
        private const int EnemigosPorRobotGrande = 8;

        /// <summary>True si la línea de comandos trae exactamente <see cref="Argumento"/> (sin importar mayúsculas).</summary>
        public static bool TieneArgumento(IReadOnlyList<string> argumentos)
        {
            if (argumentos == null)
            {
                return false;
            }
            foreach (string a in argumentos)
            {
                if (string.Equals(a, Argumento, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// El plan: fases 1..<paramref name="fases"/> con los topes del asset (tope 0) y luego, en la última fase, un paso por cada tope de <paramref name="topes"/>.
        /// </summary>
        public static List<PasoMedicionRendimiento> Plan(int fases, IReadOnlyList<int> topes)
        {
            var plan = new List<PasoMedicionRendimiento>();
            for (int f = 1; f < fases; f++)
            {
                plan.Add(new PasoMedicionRendimiento(f, 0));
            }
            if (fases >= 1)
            {
                if (topes == null || topes.Count == 0)
                {
                    plan.Add(new PasoMedicionRendimiento(fases, 0));
                }
                else
                {
                    foreach (int t in topes)
                    {
                        plan.Add(new PasoMedicionRendimiento(fases, Math.Max(1, t)));
                    }
                }
            }
            return plan;
        }

        public static int TopePatrullas(int enemigos) => Math.Max(MinimoPatrullas, Techo(enemigos, EnemigosPorPatrulla));
        public static int TopeDrones(int enemigos) => Math.Max(MinimoDrones, Techo(enemigos, EnemigosPorDron));
        public static int TopeCapsulas(int enemigos) => Math.Max(MinimoCapsulas, Techo(enemigos, EnemigosPorCapsula));
        public static int TopeRobotsGrandes(int enemigos) => Math.Max(MinimoRobotsGrandes, Techo(enemigos, EnemigosPorRobotGrande));

        private static int Techo(int a, int b) => b <= 0 ? a : (Math.Max(0, a) + b - 1) / b;

        /// <summary>Fps promedio: cuadros entre segundos totales (0 sin cuadros).</summary>
        public static float FpsPromedio(IReadOnlyList<float> segundosPorCuadro)
        {
            if (segundosPorCuadro == null || segundosPorCuadro.Count == 0)
            {
                return 0f;
            }
            double suma = 0;
            foreach (float s in segundosPorCuadro)
            {
                suma += Math.Max(0f, s);
            }
            return suma > 0 ? (float)(segundosPorCuadro.Count / suma) : 0f;
        }

        /// <summary>1 % más bajo: fps del promedio del 1 % de cuadros más lentos (al menos un cuadro). 0 sin cuadros.</summary>
        public static float FpsUnoPorCientoBajo(IReadOnlyList<float> segundosPorCuadro)
        {
            if (segundosPorCuadro == null || segundosPorCuadro.Count == 0)
            {
                return 0f;
            }
            var orden = new List<float>(segundosPorCuadro);
            orden.Sort((a, b) => b.CompareTo(a)); // los más lentos primero
            int n = Math.Max(1, (int)Math.Ceiling(orden.Count * FraccionPeores));
            double suma = 0;
            for (int i = 0; i < n; i++)
            {
                suma += Math.Max(0f, orden[i]);
            }
            return suma > 0 ? (float)(n / suma) : 0f;
        }
    }
}
