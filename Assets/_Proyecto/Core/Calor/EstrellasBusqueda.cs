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
    }
}
