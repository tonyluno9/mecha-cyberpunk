using UnityEngine;

namespace MechaCyberpunk.Mecha.Kits
{
    /// <summary>Por qué se puede o no se puede usar un kit de reparación.</summary>
    public enum MotivoKit
    {
        Puede,
        FueraDePelea,
        EnKO,
        VidaCompleta,
        SinKits,
        LimitePorPelea,
        EnCurso
    }

    /// <summary>Lógica pura de los kits de reparación en plena pelea (T-148, D-tony-83).</summary>
    public static class ReglaKitReparacion
    {
        /// <summary>Decide si se puede usar un kit. El primer motivo que falla es el que se devuelve.</summary>
        public static MotivoKit Decidir(bool enPelea, bool enKO, float vida, float vidaMaxima, int kits, int usadosEnPelea, int topePorPelea,
            bool curando)
        {
            if (!enPelea) return MotivoKit.FueraDePelea;
            if (enKO) return MotivoKit.EnKO;
            if (curando) return MotivoKit.EnCurso;
            if (vida >= vidaMaxima) return MotivoKit.VidaCompleta;
            if (usadosEnPelea >= topePorPelea) return MotivoKit.LimitePorPelea;
            if (kits <= 0) return MotivoKit.SinKits;
            return MotivoKit.Puede;
        }

        /// <summary>Puntos de vida que cura un kit: la fracción de la vida máxima, sin pasar del máximo.</summary>
        public static float Curacion(float vida, float vidaMaxima, float fraccion)
        {
            return Mathf.Clamp(vidaMaxima * fraccion, 0f, Mathf.Max(0f, vidaMaxima - vida));
        }

        /// <summary>Cuánto se cura en un cuadro de <paramref name="dt"/> s, sin pasar de lo que falta por curar.</summary>
        public static float CuracionDelCuadro(float total, float restante, float segundos, float dt)
        {
            if (segundos <= 0f) return restante;
            return Mathf.Clamp(total / segundos * dt, 0f, restante);
        }
    }
}
