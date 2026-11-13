using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>Las fases del asalto (T-717, rehecho en T-728): cinemática, persecución del carro (primero te robas un carro), maletín en el piso cuando el carro se detiene (T-987), y listo.</summary>
    public enum FaseAsalto
    {
        Inactivo,
        Cinematica,
        Persecucion,
        Maletin,
        Terminado
    }

    /// <summary>
    /// Reglas puras del asalto de la historia del dinero (T-728): a quien asaltan es una persona de afuera, NO el jugador (no te quitan dinero). Cuánto le roban a la víctima,
    /// cuándo se puede saltar la cinemática, qué fase sigue, cuánto hay en el maletín y qué objetivo toca. No toca el mundo: <see cref="AsaltoDinero"/> la mueve.
    /// </summary>
    public sealed class ReglaAsalto
    {
        /// <summary>Lo que le roban a la víctima.</summary>
        public const int DineroDeLaVictima = 300;

        /// <summary>T-987: lo que hay en el maletín que sueltan los asaltantes.</summary>
        public const int DineroDelMaletin = 10000;

        public FaseAsalto Fase { get; private set; } = FaseAsalto.Inactivo;

        /// <summary>El dinero que el asaltante le quita a la víctima (no sale de tu cartera).</summary>
        public int DineroRobado { get; private set; }

        /// <summary>True desde que el asaltante le quitó el dinero a la víctima hasta que lo detienen (o se cancela).</summary>
        public bool DineroEnManosDelLadron { get; private set; }

        /// <summary>True una vez que el asaltante subió al carro.</summary>
        public bool Abordo { get; private set; }

        /// <summary>True durante la cinemática: Enter la salta.</summary>
        public bool PuedeSaltar => Fase == FaseAsalto.Cinematica;

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

        /// <summary>El carro se detuvo (llanta, choque, fin de la ruta o atorón): los asaltantes sueltan el maletín y hay que recogerlo.</summary>
        public bool CarroDetenido()
        {
            if (Fase != FaseAsalto.Persecucion)
            {
                return false;
            }
            Fase = FaseAsalto.Maletin;
            return true;
        }

        /// <summary>El jugador recogió el maletín: devuelve los $10,000 una sola vez (0 si no era la fase del maletín). Termina el asalto.</summary>
        public int MaletinRecogido()
        {
            if (Fase != FaseAsalto.Maletin)
            {
                return 0;
            }
            Fase = FaseAsalto.Terminado;
            DineroEnManosDelLadron = false;
            return DineroDelMaletin;
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
                case FaseAsalto.Maletin:
                    return enVehiculo ? AsaltoDinero.TextoMaletinEnCarro : AsaltoDinero.TextoMaletin;
                default:
                    return string.Empty;
            }
        }
    }
}
