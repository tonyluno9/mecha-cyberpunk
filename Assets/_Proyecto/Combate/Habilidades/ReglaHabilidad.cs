using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>Por qué se puede o no se puede usar una habilidad. El primero que falla, en este orden.</summary>
    public enum MotivoHabilidad
    {
        Puede,
        SinHabilidad,
        FueraDePelea,
        SinControl,
        Aturdido,
        GuardiaArriba,
        Enfriando,
        SinMunicion
    }

    /// <summary>Lógica pura de las habilidades especiales (T-174, D-tony-135, D-tony-138).</summary>
    public static class ReglaHabilidad
    {
        /// <summary>Tope de la reducción de enfriamiento (fracción).</summary>
        public const float ReduccionMaxima = 0.4f;

        /// <summary>Decide si se puede usar. <paramref name="guardiaArriba"/> solo cuenta para la del brazo: el llamador pasa false para la del torso.</summary>
        public static MotivoHabilidad Decidir(bool tieneHabilidad, bool enPelea, bool tieneControl, bool aturdido, bool guardiaArriba,
            float enfriamientoRestante, int municion)
        {
            if (!tieneHabilidad) return MotivoHabilidad.SinHabilidad;
            if (!enPelea) return MotivoHabilidad.FueraDePelea;
            if (!tieneControl) return MotivoHabilidad.SinControl;
            if (aturdido) return MotivoHabilidad.Aturdido;
            if (guardiaArriba) return MotivoHabilidad.GuardiaArriba;
            if (enfriamientoRestante > 0f) return MotivoHabilidad.Enfriando;
            if (municion <= 0) return MotivoHabilidad.SinMunicion;
            return MotivoHabilidad.Puede;
        }

        /// <summary>Multiplicador de daño y de duración del escudo por nivel: 1 + 0.25 × (nivel − 1). Nivel menor que 1 cuenta como 1.</summary>
        public static float Potencia(int nivel)
        {
            return 1f + 0.25f * (Mathf.Max(1, nivel) - 1);
        }

        /// <summary>Enfriamiento final (s) con la reducción limitada a [0, 0.4].</summary>
        public static float Enfriamiento(float baseSegundos, float reduccion)
        {
            return Mathf.Max(0f, baseSegundos) * (1f - Mathf.Clamp(reduccion, 0f, ReduccionMaxima));
        }

        /// <summary>Enfriamiento que queda tras <paramref name="dt"/> s, sin bajar de 0.</summary>
        public static float Avanzar(float restante, float dt)
        {
            return Mathf.Max(0f, restante - Mathf.Max(0f, dt));
        }
    }
}
