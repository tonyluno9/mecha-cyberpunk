using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Economia
{
    /// <summary>Lógica pura de la renta (T-107): cobra sola si hay dinero; si no, queda adeudo y el taller se bloquea.</summary>
    public class ModeloRenta
    {
        private readonly int diasEntreCobros;

        public ModeloRenta(int monto, int diasEntreCobros, int primerDiaCobro)
        {
            Monto = monto;
            this.diasEntreCobros = Math.Max(1, diasEntreCobros);
            DiaProximoCobro = primerDiaCobro;
        }

        public int Monto { get; }
        public int DiaProximoCobro { get; private set; }
        public int Adeudo { get; private set; }
        public bool Bloqueado => Adeudo > 0;

        public event Action Cambio;

        /// <summary>Cobra cada renta vencida hasta <paramref name="dia"/>. Con cartera null todo va a adeudo.</summary>
        public void AlEmpezarDia(int dia, ICartera cartera)
        {
            bool cambio = false;
            while (dia >= DiaProximoCobro)
            {
                if (Adeudo == 0 && cartera != null && cartera.PuedePagar(Monto)) cartera.IntentarPagar(Monto, MotivoDinero.Renta);
                else Adeudo += Monto;
                DiaProximoCobro += diasEntreCobros;
                cambio = true;
            }
            if (cambio) Cambio?.Invoke();
        }

        /// <summary>Cobra todo el adeudo si la cartera alcanza. No mueve <see cref="DiaProximoCobro"/>.</summary>
        public bool PagarAdeudo(ICartera cartera)
        {
            if (Adeudo <= 0 || cartera == null || !cartera.PuedePagar(Adeudo)) return false;
            if (!cartera.IntentarPagar(Adeudo, MotivoDinero.Renta)) return false;
            Adeudo = 0;
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Fija el estado (carga de partida). No cobra nada.</summary>
        public void Restablecer(int diaProximoCobro, int adeudo)
        {
            adeudo = Math.Max(0, adeudo);
            if (diaProximoCobro == DiaProximoCobro && adeudo == Adeudo) return;
            DiaProximoCobro = diaProximoCobro;
            Adeudo = adeudo;
            Cambio?.Invoke();
        }
    }
}
