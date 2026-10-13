using System;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// Lógica pura de la piedad del rival: limita el daño para que el objetivo nunca baje de un piso de su vida máxima
    /// y decide cuándo el rival se debilita (una vez débil sigue débil hasta <see cref="Reiniciar"/>).
    /// </summary>
    public class ReglaPiedad
    {
        // Los flotantes (0.35f * 100) pueden quedar un pelo debajo del valor exacto.
        private const float Tolerancia = 1e-4f;

        private readonly float fraccionPiso;
        private readonly float umbralDebilitar;

        /// <param name="fraccionPiso">Fracción de la vida máxima del objetivo que sus golpes nunca quitan (0 a 1).</param>
        /// <param name="umbralDebilitar">Con la vida del objetivo en esta fracción o menos, el rival se debilita (0 a 1).</param>
        public ReglaPiedad(float fraccionPiso, float umbralDebilitar)
        {
            this.fraccionPiso = Math.Max(0f, Math.Min(1f, fraccionPiso));
            this.umbralDebilitar = Math.Max(0f, Math.Min(1f, umbralDebilitar));
        }

        public bool EstaDebil { get; private set; }

        /// <summary>
        /// Daño que conviene pedir: el normal, recortado para no pasar del piso. En el piso (o debajo) pide 0.
        /// Con vida máxima 0 o menos no hay nada que proteger y devuelve el daño normal.
        /// </summary>
        public float DanioAPedir(float danio, float vidaActual, float vidaMaxima)
        {
            danio = Math.Max(0f, danio);
            if (vidaMaxima <= 0f)
            {
                return danio;
            }
            float disponible = Math.Max(0f, vidaActual - vidaMaxima * fraccionPiso);
            return Math.Min(danio, disponible);
        }

        /// <summary>Revisa la vida del objetivo y devuelve si el rival está débil. No se quita sola.</summary>
        public bool Actualizar(float vidaActual, float vidaMaxima)
        {
            if (!EstaDebil && vidaMaxima > 0f && vidaActual <= vidaMaxima * umbralDebilitar + Tolerancia)
            {
                EstaDebil = true;
            }
            return EstaDebil;
        }

        /// <summary>Quita la debilidad (revancha).</summary>
        public void Reiniciar()
        {
            EstaDebil = false;
        }
    }
}
