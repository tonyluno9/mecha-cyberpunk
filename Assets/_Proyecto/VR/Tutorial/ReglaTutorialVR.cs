using System;
using System.Collections.Generic;
using MechaCyberpunk.VR.Guia;

namespace MechaCyberpunk.VR.Tutorial
{
    /// <summary>
    /// VR62, lógica pura: la fila de tarjetas del tutorial de VR. Cada tarjeta (una página de la guía, <see cref="PaginaGuiaVR"/>) se pide cuando
    /// llega su momento y sale una sola vez por partida guardada: <see cref="Pedir"/> no la forma si ya se aprendió (la consulta se inyecta:
    /// el juego usa los avisos únicos de la partida con <see cref="Clave"/>) o si ya está en la fila. Salen en el orden en que se pidieron
    /// (así el campo de tiro va DISPARAR → RECARGAR → ARMA LARGA → RUEDA → GRANADA, una al terminar la anterior). La página de los botones no es
    /// tarjeta (solo va en la guía completa).
    /// </summary>
    public sealed class ReglaTutorialVR
    {
        /// <summary>Prefijo de la clave de cada tarjeta en los avisos únicos de la partida.</summary>
        public const string Prefijo = "vr_tarjeta_";

        /// <summary>Al aparecer en tu punto, antes de los 3 SKULLS.</summary>
        public static readonly PaginaGuiaVR[] Inicio = { PaginaGuiaVR.MoverseGirar, PaginaGuiaVR.Recentrar, PaginaGuiaVR.Golpe };

        /// <summary>En el campo de tiro, en orden.</summary>
        public static readonly PaginaGuiaVR[] CampoTiro =
        {
            PaginaGuiaVR.Disparar, PaginaGuiaVR.Recargar, PaginaGuiaVR.ArmaLarga, PaginaGuiaVR.RuedaArmas, PaginaGuiaVR.Granada
        };

        private readonly List<PaginaGuiaVR> fila = new List<PaginaGuiaVR>();
        private readonly Func<string, bool> yaAprendida;

        /// <param name="yaAprendida">Dice si la clave (<see cref="Clave"/>) ya se aprendió en esta partida.</param>
        public ReglaTutorialVR(Func<string, bool> yaAprendida)
        {
            this.yaAprendida = yaAprendida;
        }

        /// <summary>La clave de una tarjeta en los avisos únicos ("vr_tarjeta_moversegirar").</summary>
        public static string Clave(PaginaGuiaVR pagina) => Prefijo + pagina.ToString().ToLowerInvariant();

        /// <summary>Las tarjetas que esperan su turno, en orden.</summary>
        public IReadOnlyList<PaginaGuiaVR> Fila => fila;

        public bool Aprendida(PaginaGuiaVR pagina) => yaAprendida != null && yaAprendida(Clave(pagina));

        /// <summary>Pone la tarjeta al final de la fila. False si es la de botones, ya se aprendió o ya está en la fila.</summary>
        public bool Pedir(PaginaGuiaVR pagina)
        {
            if (pagina == PaginaGuiaVR.Botones || Aprendida(pagina) || fila.Contains(pagina))
            {
                return false;
            }
            fila.Add(pagina);
            return true;
        }

        /// <summary>Pide varias en orden; devuelve cuántas entraron.</summary>
        public int Pedir(IEnumerable<PaginaGuiaVR> paginas)
        {
            int n = 0;
            foreach (PaginaGuiaVR p in paginas)
            {
                if (Pedir(p))
                {
                    n++;
                }
            }
            return n;
        }

        /// <summary>La siguiente tarjeta por mostrar (sin quitarla), saltando las que se aprendieron mientras esperaban. False si no hay.</summary>
        public bool Siguiente(out PaginaGuiaVR pagina)
        {
            while (fila.Count > 0)
            {
                if (!Aprendida(fila[0]))
                {
                    pagina = fila[0];
                    return true;
                }
                fila.RemoveAt(0);
            }
            pagina = PaginaGuiaVR.Botones;
            return false;
        }

        /// <summary>Quita la tarjeta de la fila (al mostrarla).</summary>
        public void Quitar(PaginaGuiaVR pagina) => fila.Remove(pagina);

        /// <summary>Vacía la fila (partida nueva o cargada).</summary>
        public void Vaciar() => fila.Clear();
    }
}
