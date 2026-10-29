using System;
using System.Collections.Generic;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// Regla pura de las FINTAS del rival (T-202, D-tony-192 y D-tony-196). Al empezar cada golpe elige cuánto alarga su aviso (azar por el
    /// máximo de su nivel) y si amaga (enseña el golpe y lo guarda). Nunca amaga dos veces seguidas; el aviso nunca es más corto que el de
    /// serie. Nivel 0: sin extra y sin amagos. El azar se inyecta.
    /// </summary>
    public class ReglaFintas
    {
        private readonly IReadOnlyList<float> extraMaximo;
        private readonly IReadOnlyList<float> probabilidadAmago;
        private readonly float fraccionAmago;
        private readonly float esperaTrasAmago;
        private readonly Func<float> azar;
        private bool anteriorFueAmago;

        /// <param name="extraMaximo">Lo más que alarga cada aviso por nivel (índice 0 = nivel I) (s).</param>
        /// <param name="probabilidadAmago">Probabilidad de amagar por nivel (0 a 1).</param>
        /// <param name="fraccionAmago">Fracción del aviso de serie a la que se cancela el amago (0 a 1).</param>
        /// <param name="esperaTrasAmago">Espera entre el amago y el siguiente golpe (s).</param>
        /// <param name="azar">Devuelve un número de 0 a 1.</param>
        public ReglaFintas(IReadOnlyList<float> extraMaximo, IReadOnlyList<float> probabilidadAmago, float fraccionAmago,
            float esperaTrasAmago, Func<float> azar)
        {
            this.extraMaximo = extraMaximo;
            this.probabilidadAmago = probabilidadAmago;
            this.fraccionAmago = fraccionAmago;
            this.esperaTrasAmago = esperaTrasAmago;
            this.azar = azar;
        }

        public float EsperaTrasAmago => esperaTrasAmago;

        /// <summary>Segundos de aviso a los que se cancela un amago, dada la preparación de serie del rival.</summary>
        public float SegundosDeAmago(float preparacionDeSerie)
        {
            return fraccionAmago * preparacionDeSerie;
        }

        /// <summary>Olvida que el golpe anterior fue un amago (el rival perdió el control).</summary>
        public void Reiniciar()
        {
            anteriorFueAmago = false;
        }

        /// <summary>
        /// Se llama una vez al empezar cada golpe. <paramref name="extra"/> son los segundos que se suman al aviso de serie (0 en un amago:
        /// el amago se ve igual que un golpe de serie hasta que se cancela). Devuelve si este golpe es un amago.
        /// </summary>
        public bool Empezar(int nivel, out float extra)
        {
            extra = 0f;
            if (nivel <= 0 || extraMaximo == null || extraMaximo.Count == 0)
            {
                anteriorFueAmago = false;
                return false;
            }
            int indice = Math.Min(nivel, extraMaximo.Count) - 1;
            float aviso = Limitar01(azar()) * Math.Max(0f, extraMaximo[indice]);
            bool amaga = false;
            if (!anteriorFueAmago && probabilidadAmago != null && probabilidadAmago.Count > 0)
            {
                int i = Math.Min(nivel, probabilidadAmago.Count) - 1;
                amaga = azar() < probabilidadAmago[i];
            }
            anteriorFueAmago = amaga;
            extra = amaga ? 0f : aviso;
            return amaga;
        }

        private static float Limitar01(float valor)
        {
            return valor < 0f ? 0f : (valor > 1f ? 1f : valor);
        }
    }
}
