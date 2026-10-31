using System;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Las reglas del respeto y la conquista (T-404, D-tony-218). Pura, sin Unity. Las zonas se identifican por su índice;
    /// la zona tuya (la que empieza con dueño Jugador) es LA FOSA.
    /// +10 por pandillero vencido en su zona rival; el jefe la conquista (respeto 100). En LA FOSA +2 por cada vencido en cualquier
    /// lado y +20 por zona conquistada. −10 en la zona donde caes. Tope 0 a 100. Con 70 en una zona rival sale su jefe.
    /// </summary>
    public sealed class ReglaTerritorio
    {
        public const int Maximo = 100;
        public const int UmbralJefe = 70;
        public const int PorVencido = 10;
        public const int FosaPorVencido = 2;
        public const int FosaPorConquista = 20;
        public const int PorCaida = 10;

        private readonly int[] respeto;
        private readonly int[] dueno;
        private readonly int fosa;
        private readonly int duenoJugador;

        /// <param name="duenos">Dueño inicial de cada zona, como número de IdPandilla.</param>
        /// <param name="valorJugador">El número de IdPandilla.Jugador.</param>
        public ReglaTerritorio(int[] duenos, int valorJugador)
        {
            dueno = (int[])duenos.Clone();
            respeto = new int[duenos.Length];
            duenoJugador = valorJugador;
            fosa = Array.IndexOf(dueno, valorJugador);
        }

        public int Cantidad => respeto.Length;
        public int Respeto(int zona) => zona >= 0 && zona < respeto.Length ? respeto[zona] : 0;
        public int Dueno(int zona) => zona >= 0 && zona < dueno.Length ? dueno[zona] : 0;

        public bool JefeDisponible(int zona) => zona >= 0 && zona < respeto.Length && dueno[zona] != duenoJugador && respeto[zona] >= UmbralJefe;

        public void Poner(int zona, int nuevoRespeto, int nuevoDueno)
        {
            if (zona < 0 || zona >= respeto.Length)
            {
                return;
            }
            respeto[zona] = Math.Max(0, Math.Min(Maximo, nuevoRespeto));
            dueno[zona] = nuevoDueno;
        }

        /// <summary>Un pandillero cayó en <paramref name="zona"/> (-1 si fuera). Devuelve true si el jugador conquistó la zona.</summary>
        public bool RegistrarVencido(int zona, bool jefe)
        {
            bool conquistada = false;
            if (zona >= 0 && zona < respeto.Length && dueno[zona] != duenoJugador)
            {
                if (jefe)
                {
                    dueno[zona] = duenoJugador;
                    respeto[zona] = Maximo;
                    conquistada = true;
                }
                else
                {
                    respeto[zona] = Math.Min(Maximo, respeto[zona] + PorVencido);
                }
            }
            if (fosa >= 0)
            {
                respeto[fosa] = Math.Min(Maximo, respeto[fosa] + FosaPorVencido + (conquistada ? FosaPorConquista : 0));
            }
            return conquistada;
        }

        public void RegistrarCaida(int zona)
        {
            if (zona >= 0 && zona < respeto.Length)
            {
                respeto[zona] = Math.Max(0, respeto[zona] - PorCaida);
            }
        }

        /// <summary>Sube o baja el respeto de una zona (T-427: premio o castigo de un ataque rival), de 0 a el tope.</summary>
        public void Ajustar(int zona, int delta)
        {
            if (zona >= 0 && zona < respeto.Length)
            {
                respeto[zona] = Math.Max(0, Math.Min(Maximo, respeto[zona] + delta));
            }
        }
    }
}
