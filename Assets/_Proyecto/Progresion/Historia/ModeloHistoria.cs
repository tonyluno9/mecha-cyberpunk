using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lógica pura de la etapa del prólogo (T-124): solo avanza hacia adelante; <see cref="Reiniciar"/> vuelve al inicio.</summary>
    public class ModeloHistoria
    {
        public EtapaPrologo Etapa { get; private set; } = EtapaPrologo.Ninguna;

        /// <summary>Avisa una vez por cada cambio real, con la etapa nueva.</summary>
        public event Action<EtapaPrologo> EtapaCambio;

        /// <summary>Acepta solo una etapa posterior a la actual. False (sin cambio ni aviso) si no.</summary>
        public bool Avanzar(EtapaPrologo nueva)
        {
            if (nueva <= Etapa) return false;
            Etapa = nueva;
            EtapaCambio?.Invoke(Etapa);
            return true;
        }

        /// <summary>Nueva partida: vuelve a Ninguna (avisa si estaba en otra).</summary>
        public void Reiniciar() => Poner(EtapaPrologo.Ninguna);

        /// <summary>Fija la etapa sin importar el orden (cargar partida). Avisa solo si cambió.</summary>
        public void Poner(EtapaPrologo nueva)
        {
            if (nueva == Etapa) return;
            Etapa = nueva;
            EtapaCambio?.Invoke(Etapa);
        }
    }
}
