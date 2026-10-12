using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lógica pura del objetivo actual. Avisa <c>Cambio</c> solo si el objetivo cambió.</summary>
    public class ModeloObjetivos : IObjetivos
    {
        public bool HayObjetivo { get; private set; }
        public Objetivo Actual { get; private set; }

        public event Action<Objetivo> Cambio;

        public void Fijar(Objetivo objetivo)
        {
            if (HayObjetivo && Igual(Actual, objetivo)) return;
            Actual = objetivo;
            HayObjetivo = true;
            Cambio?.Invoke(Actual);
        }

        public void Quitar()
        {
            if (!HayObjetivo) return;
            Actual = Objetivo.Ninguno;
            HayObjetivo = false;
            Cambio?.Invoke(Actual);
        }

        private static bool Igual(Objetivo a, Objetivo b)
        {
            return a.Texto == b.Texto && a.Lugar == b.Lugar
                && a.TienePosicion == b.TienePosicion && a.Posicion == b.Posicion;
        }
    }
}
