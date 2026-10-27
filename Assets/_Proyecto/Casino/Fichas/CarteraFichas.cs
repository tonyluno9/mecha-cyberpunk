using System;

namespace MechaCyberpunk.Casino.Fichas
{
    /// <summary>Lógica pura del saldo de fichas (T-302): entero, nunca negativo, con cambio de dinero por fichas en lotes enteros.</summary>
    public class CarteraFichas
    {
        public int Saldo { get; private set; }

        public event Action<int> Cambio;

        public bool PuedeRetirar(int cantidad) => cantidad > 0 && Saldo >= cantidad;

        /// <summary>Suma fichas (compradas o ganadas). Ignora cantidades menores o iguales a 0 y no desborda.</summary>
        public void Comprar(int cantidad)
        {
            if (cantidad <= 0)
            {
                return;
            }
            int anterior = Saldo;
            Saldo = cantidad > int.MaxValue - Saldo ? int.MaxValue : Saldo + cantidad;
            if (Saldo != anterior)
            {
                Cambio?.Invoke(Saldo - anterior);
            }
        }

        /// <summary>Quita fichas (apuesta) solo si alcanzan; si no, no cambia nada.</summary>
        public bool Retirar(int cantidad)
        {
            if (!PuedeRetirar(cantidad))
            {
                return false;
            }
            Saldo -= cantidad;
            Cambio?.Invoke(-cantidad);
            return true;
        }

        /// <summary>Cuántos lotes completos de <paramref name="fichasPorLote"/> hay en el saldo.</summary>
        public int LotesCobrables(int fichasPorLote) => fichasPorLote > 0 ? Saldo / fichasPorLote : 0;

        /// <summary>
        /// Quita todos los lotes completos y devuelve las fichas quitadas (múltiplo de <paramref name="fichasPorLote"/>); lo que no
        /// completa un lote se queda para no perder valor. Devuelve 0 si no hay un lote.
        /// </summary>
        public int Cobrar(int fichasPorLote)
        {
            int lotes = LotesCobrables(fichasPorLote);
            if (lotes <= 0)
            {
                return 0;
            }
            int fichas = lotes * fichasPorLote;
            Retirar(fichas);
            return fichas;
        }

        /// <summary>Pone el saldo exacto (cargar partida). Un valor negativo se rechaza y devuelve false.</summary>
        public bool Restablecer(int saldo)
        {
            if (saldo < 0)
            {
                return false;
            }
            int anterior = Saldo;
            Saldo = saldo;
            if (Saldo != anterior)
            {
                Cambio?.Invoke(Saldo - anterior);
            }
            return true;
        }
    }
}
