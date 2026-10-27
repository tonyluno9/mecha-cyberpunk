using UnityEngine;

namespace MechaCyberpunk.Combate.Peleador
{
    /// <summary>Lógica pura del aturdimiento (T-175): un aturdimiento nuevo no se suma, se queda el que termina más tarde.</summary>
    public static class ReglaAturdimiento
    {
        /// <summary>Instante en que termina: el mayor entre el fin actual y <c>ahora + segundos</c>.</summary>
        public static float NuevoFin(float finActual, float ahora, float segundos)
        {
            return Mathf.Max(finActual, ahora + Mathf.Max(0f, segundos));
        }
    }
}
