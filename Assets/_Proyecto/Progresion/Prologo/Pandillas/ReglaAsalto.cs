using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>Las fases del asalto (T-717, rehecho en T-728): cinemática, persecución del carro (primero te robas un carro), pelea con el asaltante cuando el carro se detiene, y listo.</summary>
    public enum FaseAsalto
    {
        Inactivo,
        Cinematica,
        Persecucion,
        Enfrentando,
        Terminado
    }

    /// <summary>
    /// Reglas puras del asalto de la historia del dinero (T-728): a quien asaltan es una persona de afuera, NO el jugador (no te quitan dinero). Cuánto le roban a la víctima,
    /// cuándo se puede saltar la cinemática, qué fase sigue, cuánto te da la víctima al detener al asaltante y qué objetivo toca. No toca el mundo: <see cref="AsaltoDinero"/> la mueve.
    /// </summary>
    public sealed class ReglaAsalto
    {
        /// <summary>Lo que le roban a la víctima.</summary>
        public const int DineroDeLaVictima = 300;

        /// <summary>Qué parte de lo robado te da la víctima de agradecimiento (decisión de T-728: la mitad).</summary>
        public const float FraccionRecompensa = 0.5f;

        public FaseAsalto Fase { get; private set; } = FaseAsalto.Inactivo;

        /// <summary>El dinero que el asaltante le quita a la víctima (no sale de tu cartera).</summary>
        public int DineroRobado { get; private set; }

        /// <summary>True desde que el asaltante le quitó el dinero a la víctima hasta que lo detienen (o se cancela).</summary>
        public bool DineroEnManosDelLadron { get; private set; }

        /// <summary>True una vez que el asaltante subió al carro.</summary>
        public bool Abordo { get; private set; }

        /// <summary>True durante la cinemática: Enter la salta.</summary>
        public bool PuedeSaltar => Fase == FaseAsalto.Cinematica;

        /// <summary>Lo que te da la víctima si detienes al asaltante.</summary>
        public static int Recompensa(int robado) => Mathf.RoundToInt(Mathf.Max(0, robado) * FraccionRecompensa);

        /// <summary>Empieza el asalto: el asaltante va por el dinero de la víctima.</summary>
        public void Iniciar()
        {
            Fase = FaseAsalto.Cinematica;
            DineroRobado = DineroDeLaVictima;
            DineroEnManosDelLadron = false;
            Abordo = false;
        }

        /// <summary>El asaltante le quita el dinero a la víctima (una sola vez). Devuelve lo robado (0 si ya lo había robado o no hay asalto).</summary>
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

        /// <summary>El asaltante cayó: devuelve lo que te da la víctima (0 si no estaba en la fase de pelea).</summary>
        public int AsaltanteVencido()
        {
            if (Fase != FaseAsalto.Enfrentando)
            {
                return 0;
            }
            Fase = FaseAsalto.Terminado;
            int premio = DineroEnManosDelLadron ? Recompensa(DineroRobado) : 0;
            DineroEnManosDelLadron = false;
            return premio;
        }

        /// <summary>Se cancela el asalto (otro paso de la historia, una carga). No hay dinero tuyo que reponer.</summary>
        public void Cancelar()
        {
            DineroEnManosDelLadron = false;
            Fase = FaseAsalto.Inactivo;
            Abordo = false;
        }

        /// <summary>El objetivo de cada fase (frase corta). Vacío fuera del asalto. Desde la cinemática te robas un carro y, ya en uno, los persigues.</summary>
        public static string TextoObjetivo(FaseAsalto fase, bool enVehiculo)
        {
            switch (fase)
            {
                case FaseAsalto.Cinematica:
                case FaseAsalto.Persecucion:
                    return enVehiculo ? AsaltoDinero.TextoPerseguir : AsaltoDinero.TextoRobarCarro;
                case FaseAsalto.Enfrentando:
                    return AsaltoDinero.TextoRecuperar;
                default:
                    return string.Empty;
            }
        }
    }
}
