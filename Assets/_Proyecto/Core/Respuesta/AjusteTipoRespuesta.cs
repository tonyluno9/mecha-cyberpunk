using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>T-1620: topes y tiempos de un tipo de unidad de respuesta.</summary>
    [Serializable]
    public class AjusteTipoRespuesta
    {
        public TipoRespuesta Tipo;
        [Tooltip("Máximo de vivos de este tipo a la vez.")]
        public int Tope = 1;
        [Tooltip("Cuántos puede haber a la vez al empezar (sin haber perdido ninguno).")]
        public int CupoInicial = 1;
        [Tooltip("Segundos mínimos entre perder una unidad y que llegue su reemplazo (s).")]
        public float CooldownMin = 30f;
        [Tooltip("Segundos máximos entre perder una unidad y que llegue su reemplazo (s).")]
        public float CooldownMax = 30f;

        public AjusteTipoRespuesta() { }

        public AjusteTipoRespuesta(TipoRespuesta tipo, int tope, int cupoInicial, float cooldownMin, float cooldownMax)
        {
            Tipo = tipo;
            Tope = tope;
            CupoInicial = cupoInicial;
            CooldownMin = cooldownMin;
            CooldownMax = cooldownMax;
        }
    }
}
