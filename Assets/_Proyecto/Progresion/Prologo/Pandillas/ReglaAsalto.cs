using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>Las fases del asalto (T-717): cinemática, persecución del carro, pelea con el asaltante cuando el carro se detiene, y listo.</summary>
    public enum FaseAsalto
    {
        Inactivo,
        Cinematica,
        Persecucion,
        Enfrentando,
        Terminado
    }

    /// <summary>
    /// Reglas puras del asalto de la historia del dinero (T-717): cuánto te quitan, cuándo se puede saltar la cinemática, qué fase sigue y cuánto
    /// vuelve a tu cartera. No toca el mundo: <see cref="AsaltoDinero"/> la mueve.
    /// </summary>
    public sealed class ReglaAsalto
    {
        public FaseAsalto Fase { get; private set; } = FaseAsalto.Inactivo;

        /// <summary>El dinero que te quita el asaltante (todo lo que traes, nunca negativo).</summary>
        public int DineroRobado { get; private set; }

        /// <summary>True desde que te quitó el dinero hasta que lo recuperas (o se cancela y se devuelve).</summary>
        public bool DineroEnManosDelLadron { get; private set; }

        /// <summary>True una vez que el asaltante subió al carro.</summary>
        public bool Abordo { get; private set; }

        /// <summary>True durante la cinemática: Enter la salta.</summary>
        public bool PuedeSaltar => Fase == FaseAsalto.Cinematica;

        /// <summary>Empieza el asalto: el asaltante va por todo el dinero de la cartera.</summary>
        public void Iniciar(int dineroEnCartera)
        {
            Fase = FaseAsalto.Cinematica;
            DineroRobado = Mathf.Max(0, dineroEnCartera);
            DineroEnManosDelLadron = false;
            Abordo = false;
        }

        /// <summary>Te quita el dinero (una sola vez). Devuelve lo que hay que cobrar de la cartera.</summary>
        public int Robar()
        {
            if (Fase == FaseAsalto.Inactivo || Fase == FaseAsalto.Terminado || DineroEnManosDelLadron)
            {
                return 0;
            }
            DineroEnManosDelLadron = true;
            return DineroRobado;
        }

        /// <summary>El asaltante sube al carro: acaba la cinemática y empieza la persecución. Si faltaba el robo, se hace.</summary>
        public bool Abordar()
        {
            if (Fase != FaseAsalto.Cinematica)
            {
                return false;
            }
            Robar();
            Abordo = true;
            Fase = FaseAsalto.Persecucion;
            return true;
        }

        /// <summary>El carro se detuvo (llanta, choque, fin de la ruta o atorón): hay que enfrentar al asaltante.</summary>
        public bool CarroDetenido()
        {
            if (Fase != FaseAsalto.Persecucion)
            {
                return false;
            }
            Fase = FaseAsalto.Enfrentando;
            return true;
        }

        /// <summary>El asaltante cayó: devuelve el dinero que vuelve a la cartera (0 si no estaba en la fase de pelea).</summary>
        public int AsaltanteVencido()
        {
            if (Fase != FaseAsalto.Enfrentando)
            {
                return 0;
            }
            Fase = FaseAsalto.Terminado;
            int devuelto = DineroEnManosDelLadron ? DineroRobado : 0;
            DineroEnManosDelLadron = false;
            return devuelto;
        }

        /// <summary>Se cancela el asalto (otro paso de la historia, una carga). Devuelve el dinero que hay que reponer si te lo habían quitado.</summary>
        public int Cancelar()
        {
            int devuelto = DineroEnManosDelLadron ? DineroRobado : 0;
            DineroEnManosDelLadron = false;
            Fase = FaseAsalto.Inactivo;
            Abordo = false;
            return devuelto;
        }

        /// <summary>El objetivo de cada fase (frase corta). Vacío fuera del asalto.</summary>
        public static string TextoObjetivo(FaseAsalto fase)
        {
            switch (fase)
            {
                case FaseAsalto.Cinematica:
                case FaseAsalto.Persecucion:
                    return AsaltoDinero.TextoPerseguir;
                case FaseAsalto.Enfrentando:
                    return AsaltoDinero.TextoRecuperar;
                default:
                    return string.Empty;
            }
        }
    }
}
