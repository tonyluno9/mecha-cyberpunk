using System;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Lógica pura de la reserva de balas por arma (T-720). Las armas de fuego "limitadas" (escopeta, fusil y sniper) gastan balas de
    /// reserva al recargar; la pistola y los puños no. Cada arma recibe sus balas iniciales una sola vez (<see cref="Asegurar"/>).
    /// </summary>
    public class ModeloReservaBalas
    {
        private readonly Dictionary<ArmaAPie, int> balas = new Dictionary<ArmaAPie, int>();

        public event Action Cambio;

        /// <summary>True si el arma gasta balas de reserva al recargar.</summary>
        public static bool EsLimitada(ArmaAPie arma) => arma == ArmaAPie.Escopeta || arma == ArmaAPie.Fusil || arma == ArmaAPie.Sniper;

        /// <summary>True si la reserva del arma ya existe (ya se dieron sus balas iniciales).</summary>
        public bool Existe(ArmaAPie arma) => balas.ContainsKey(arma);

        public int Cantidad(ArmaAPie arma) => balas.TryGetValue(arma, out int n) ? n : 0;

        /// <summary>Crea la reserva del arma con sus balas iniciales si aún no existe. Devuelve true si la creó.</summary>
        public bool Asegurar(ArmaAPie arma, int iniciales, int maximo)
        {
            if (!EsLimitada(arma) || balas.ContainsKey(arma))
            {
                return false;
            }
            balas[arma] = Math.Max(0, Math.Min(iniciales, maximo));
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Cuántas balas caben todavía.</summary>
        public int Espacio(ArmaAPie arma, int maximo) => Math.Max(0, maximo - Cantidad(arma));

        /// <summary>Suma balas sin pasar del máximo. Devuelve cuántas entraron.</summary>
        public int Agregar(ArmaAPie arma, int cantidad, int maximo)
        {
            if (!EsLimitada(arma) || cantidad <= 0)
            {
                return 0;
            }
            int entran = Math.Min(cantidad, Espacio(arma, maximo));
            if (entran > 0)
            {
                balas[arma] = Cantidad(arma) + entran;
                Cambio?.Invoke();
            }
            return entran;
        }

        /// <summary>Saca hasta <paramref name="cantidad"/> balas. Devuelve las que sacó.</summary>
        public int Tomar(ArmaAPie arma, int cantidad)
        {
            int saca = Math.Min(Math.Max(0, cantidad), Cantidad(arma));
            if (saca > 0)
            {
                balas[arma] = Cantidad(arma) - saca;
                Cambio?.Invoke();
            }
            return saca;
        }

        /// <summary>Para el guardado: las armas con reserva y sus balas.</summary>
        public IEnumerable<KeyValuePair<ArmaAPie, int>> Todas => balas;

        /// <summary>Para la carga: reemplaza todo.</summary>
        public void Restablecer(IEnumerable<KeyValuePair<ArmaAPie, int>> nuevas)
        {
            balas.Clear();
            foreach (KeyValuePair<ArmaAPie, int> par in nuevas)
            {
                if (EsLimitada(par.Key))
                {
                    balas[par.Key] = Math.Max(0, par.Value);
                }
            }
            Cambio?.Invoke();
        }
    }
}
