using System;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>Lógica pura de la furia del jefe: entra una sola vez por pelea al bajar al umbral de su vida, y no en KO.</summary>
    public class ReglaFuriaJefe
    {
        // Los flotantes (0.5f * 150) pueden quedar un pelo debajo del valor exacto.
        private const float Tolerancia = 1e-4f;

        private readonly float umbralVida;

        /// <param name="umbralVida">Fracción de la vida máxima en la que entra en furia (0 a 1).</param>
        public ReglaFuriaJefe(float umbralVida)
        {
            this.umbralVida = Math.Max(0f, Math.Min(1f, umbralVida));
        }

        public bool EnFuria { get; private set; }

        /// <summary>Devuelve true solo en la llamada en que entra en furia; las siguientes devuelven false hasta <see cref="Reiniciar"/>.</summary>
        public bool Actualizar(float vidaActual, float vidaMaxima)
        {
            if (EnFuria || vidaMaxima <= 0f || vidaActual <= 0f || vidaActual > vidaMaxima * umbralVida + Tolerancia)
            {
                return false;
            }
            EnFuria = true;
            return true;
        }

        /// <summary>Quita la furia (revancha o pelea nueva).</summary>
        public void Reiniciar()
        {
            EnFuria = false;
        }
    }
}
