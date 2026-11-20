using System;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1526: qué modelos de carro son de la policía (V05 patrulla, V06 patrulla camioneta, V08 moto de patrulla, V11 camioneta de policía, PA01 y PA02 las patrullas nuevas).
    /// El tráfico civil nunca los saca: un modelo de patrulla con el componente de un carro civil no reacciona a nada (ni sirena, ni persecución, ni arresto).
    /// Las patrullas de verdad salen de Patrulla.prefab y PatrullaCamioneta.prefab con <c>PatrullaMundo</c>. Pura: sin Unity.
    /// </summary>
    public static class ReglaModeloDePolicia
    {
        /// <summary>Códigos de los modelos de policía (los de <c>ColocarT532.DeLaPolicia</c> más V11 y las PA).</summary>
        public static readonly string[] Codigos = { "V05", "V06", "V08", "V11", "PA01", "PA02" };

        /// <summary>true si el nombre de un prefab (CarroTrafico_V05, Carro_PA01, CarroTrafico_V06_Rojo...) es de un modelo de policía.</summary>
        public static bool EsDePolicia(string nombrePrefab)
        {
            if (string.IsNullOrEmpty(nombrePrefab))
            {
                return false;
            }
            if (nombrePrefab.StartsWith("Patrulla", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            foreach (string codigo in Codigos)
            {
                int i = nombrePrefab.IndexOf(codigo, StringComparison.Ordinal);
                while (i >= 0)
                {
                    bool empiezaBien = i == 0 || nombrePrefab[i - 1] == '_';
                    int fin = i + codigo.Length;
                    bool terminaBien = fin >= nombrePrefab.Length || nombrePrefab[fin] == '_' || nombrePrefab[fin] == '(' || nombrePrefab[fin] == ' ';
                    if (empiezaBien && terminaBien)
                    {
                        return true;
                    }
                    i = nombrePrefab.IndexOf(codigo, i + 1, StringComparison.Ordinal);
                }
            }
            return false;
        }
    }
}
