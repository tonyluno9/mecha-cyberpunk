using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1304: cuántas de las 5 estrellas de búsqueda tiene el jugador (lo mismo que pinta el HUD): ninguna sin calor; con calor, tantas como el nivel o como la
    /// fracción del calor en quintos, la mayor. Desde <see cref="EstrellasNova"/> ya no viene solo la policía: viene Nova (y en <see cref="Total"/>, su jefe final).
    /// </summary>
    public static class EstrellasBusqueda
    {
        public const int Total = 5;
        /// <summary>A partir de estas estrellas viene Nova y las estrellas se ponen azules.</summary>
        public const int EstrellasNova = 4;

        public static int De(int nivel, float valor, float maximo)
        {
            if (nivel <= 0)
            {
                return 0;
            }
            int porValor = maximo > 0f ? Mathf.CeilToInt(Total * Mathf.Clamp01(valor / maximo)) : 0;
            return Mathf.Clamp(Mathf.Max(nivel, porValor), 1, Total);
        }

        public static int De(ICalor calor) => calor == null ? 0 : De(calor.Nivel, calor.Valor, calor.Maximo);

        /// <summary>True si con estas estrellas ya viene Nova.</summary>
        public static bool EsNova(int estrellas) => estrellas >= EstrellasNova;

        // T-1322: cuántas estrellas pone cada cosa que se le hace a la policía o a un carro delante de ella (más difícil que antes).
        public const int EstrellasPorDispararAPatrulla = 2;
        public const int EstrellasPorReventarPatrulla = 3;
        public const int EstrellasPorDispararAPolicia = 2;
        public const int EstrellasPorMatarPolicia = 3;
        public const int EstrellasPorDispararACarroFrenteAPolicia = 2;
        private const float FraccionPaso = 0.05f;
        private const int PasosMaximos = 25;

        /// <summary>T-1322: avisa con las estrellas nuevas cada vez que cambian. Lo publica quien vigila el calor (Trafico); lo puede escuchar quien necesite el "5 estrellas".</summary>
        public static event Action<int> EstrellasCambiaron;

        public static void PublicarCambio(int estrellas) => EstrellasCambiaron?.Invoke(estrellas);

        /// <summary>T-1322: true con el máximo de estrellas (5): llegan camionetas y drones; el gancho de "pasaste de 5" (Nova) cuelga de aquí.</summary>
        public static bool EnMaximo(int estrellas) => estrellas >= Total;

        /// <summary>
        /// T-1322: sube el calor, en pasos del 5 % del máximo, hasta que <see cref="De(ICalor)"/> llegue a <paramref name="estrellas"/> (los umbrales de nivel viven en
        /// los datos de Progresion). No baja nada y no hace nada si ya hay esas estrellas o más. Suma con Mision: la víctima es la policía, no pregunta a testigos.
        /// </summary>
        public static void SubirA(ICalor calor, int estrellas, MotivoCalor motivo = MotivoCalor.Mision)
        {
            if (calor == null || calor.Maximo <= 0f)
            {
                return;
            }
            estrellas = Mathf.Clamp(estrellas, 1, Total);
            float paso = calor.Maximo * FraccionPaso;
            for (int i = 0; i < PasosMaximos && De(calor) < estrellas && calor.Valor < calor.Maximo; i++)
            {
                calor.Sumar(paso, motivo);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => EstrellasCambiaron = null;
    }
}
