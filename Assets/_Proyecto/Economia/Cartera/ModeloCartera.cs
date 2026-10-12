using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Economia
{
    /// <summary>Lógica pura del dinero del jugador.</summary>
    public class ModeloCartera : ICartera
    {
        public ModeloCartera(int dineroInicial = 0)
        {
            Dinero = dineroInicial;
        }

        public int Dinero { get; private set; }

        public event Action<int, MotivoDinero> Cambio;

        public bool PuedePagar(int cantidad) => cantidad > 0 && Dinero >= cantidad;

        public bool IntentarPagar(int cantidad, MotivoDinero motivo)
        {
            if (!PuedePagar(cantidad)) return false;
            Aplicar(-cantidad, motivo);
            return true;
        }

        public void Abonar(int cantidad, MotivoDinero motivo)
        {
            if (cantidad <= 0) return;
            Aplicar(cantidad, motivo);
        }

        public void CargarDeuda(int cantidad, MotivoDinero motivo)
        {
            if (cantidad <= 0) return;
            Aplicar(-cantidad, motivo);
        }

        private void Aplicar(int diferencia, MotivoDinero motivo)
        {
            Dinero += diferencia;
            Cambio?.Invoke(diferencia, motivo);
        }
    }
}
