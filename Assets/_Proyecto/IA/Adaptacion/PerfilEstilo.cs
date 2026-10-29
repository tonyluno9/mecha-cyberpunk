using MechaCyberpunk.Core;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>Lo que pasó en una pelea que sirve para ver los hábitos del jugador (T-199, D-tony-193). Clase pura: solo cuenta.</summary>
    public class PerfilEstilo
    {
        /// <summary>Golpes que lanzó el jugador, peguen o no.</summary>
        public int GolpesLanzados { get; private set; }
        /// <summary>Golpes del jugador que conectaron con el rival.</summary>
        public int GolpesConectados { get; private set; }
        /// <summary>De los conectados, los que le pegaron al rival mientras se recuperaba de su golpe.</summary>
        public int ConectadosCastigando { get; private set; }
        /// <summary>Golpes del rival que llegaron al jugador.</summary>
        public int GolpesRival { get; private set; }
        /// <summary>De esos, los que llegaron con la guardia del jugador arriba.</summary>
        public int GolpesRivalCubiertos { get; private set; }

        public void RegistrarGolpeLanzado()
        {
            GolpesLanzados++;
        }

        public void RegistrarConectado(bool rivalRecuperando)
        {
            GolpesConectados++;
            if (rivalRecuperando)
            {
                ConectadosCastigando++;
            }
        }

        public void RegistrarGolpeRival(TipoBloqueo bloqueo)
        {
            GolpesRival++;
            if (bloqueo != TipoBloqueo.Ninguno)
            {
                GolpesRivalCubiertos++;
            }
        }

        public void Reiniciar()
        {
            GolpesLanzados = 0;
            GolpesConectados = 0;
            ConectadosCastigando = 0;
            GolpesRival = 0;
            GolpesRivalCubiertos = 0;
        }
    }
}
