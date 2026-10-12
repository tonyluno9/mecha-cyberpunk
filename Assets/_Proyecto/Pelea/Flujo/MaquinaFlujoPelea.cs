using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Pelea.Flujo
{
    /// <summary>
    /// Lógica pura del flujo de una pelea: conteo, pelea, KO, resultado y revancha. No usa Unity: recibe el tiempo
    /// en <see cref="Avanzar"/> y los KO en <see cref="NotificarKO"/>.
    /// </summary>
    public class MaquinaFlujoPelea
    {
        /// <summary>Primer número del conteo (3, 2, 1 y luego 0 = ¡pelea!).</summary>
        public const int NumeroInicial = 3;

        private readonly float segundosPorNumero;
        private readonly float pausaDespuesKO;
        private float tiempoEnEtapa;
        private int numeroActual;

        public MaquinaFlujoPelea(float segundosPorNumero, float pausaDespuesKO)
        {
            this.segundosPorNumero = Math.Max(segundosPorNumero, float.Epsilon);
            this.pausaDespuesKO = Math.Max(pausaDespuesKO, 0f);
        }

        public EstadoFlujoPelea Estado { get; private set; } = EstadoFlujoPelea.Preparando;
        /// <summary>Quien ganó. Válido desde el estado KO.</summary>
        public LadoPelea Ganador { get; private set; }
        /// <summary>Segundos desde que empezó la pelea (después del conteo) hasta el KO.</summary>
        public float DuracionPelea { get; private set; }

        /// <summary>Avisa cada número del conteo: 3, 2, 1 y 0.</summary>
        public event Action<int> ConteoCambio;
        /// <summary>Avisa al terminar el conteo, al pasar a Peleando.</summary>
        public event Action PeleaIniciada;
        /// <summary>Avisa el primer KO, con el lado que ganó.</summary>
        public event Action<LadoPelea> KOOcurrido;
        /// <summary>Avisa al pasar a Resultado.</summary>
        public event Action<ResultadoPelea> ResultadoListo;

        /// <summary>Empieza el conteo desde Preparando y avisa el primer número. Fuera de Preparando no hace nada.</summary>
        public bool IniciarConteo()
        {
            if (Estado != EstadoFlujoPelea.Preparando)
            {
                return false;
            }
            Estado = EstadoFlujoPelea.Conteo;
            tiempoEnEtapa = 0f;
            numeroActual = NumeroInicial;
            ConteoCambio?.Invoke(numeroActual);
            return true;
        }

        /// <summary>Avanza el tiempo. Un <paramref name="dt"/> grande recorre todos los números que le toquen.</summary>
        public void Avanzar(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }
            tiempoEnEtapa += dt;
            if (Estado == EstadoFlujoPelea.Conteo)
            {
                while (Estado == EstadoFlujoPelea.Conteo && tiempoEnEtapa >= segundosPorNumero)
                {
                    tiempoEnEtapa -= segundosPorNumero;
                    numeroActual--;
                    ConteoCambio?.Invoke(numeroActual);
                    if (numeroActual <= 0)
                    {
                        Estado = EstadoFlujoPelea.Peleando;
                        DuracionPelea = 0f;
                        PeleaIniciada?.Invoke();
                    }
                }
                // El sobrante del conteo ya cuenta como tiempo de pelea.
                if (Estado == EstadoFlujoPelea.Peleando)
                {
                    DuracionPelea = tiempoEnEtapa;
                    tiempoEnEtapa = 0f;
                }
            }
            else if (Estado == EstadoFlujoPelea.Peleando)
            {
                DuracionPelea += dt;
                tiempoEnEtapa = 0f;
            }
            else if (Estado == EstadoFlujoPelea.KO && tiempoEnEtapa >= pausaDespuesKO)
            {
                Estado = EstadoFlujoPelea.Resultado;
                ResultadoListo?.Invoke(new ResultadoPelea(Ganador, DuracionPelea));
            }
        }

        /// <summary>
        /// Avisa que un peleador cayó. Solo cuenta en Peleando; el segundo KO se ignora (gana quien dejó primero en KO al otro).
        /// </summary>
        public void NotificarKO(LadoPelea ladoEnKO)
        {
            if (Estado != EstadoFlujoPelea.Peleando)
            {
                return;
            }
            Ganador = ladoEnKO == LadoPelea.Jugador ? LadoPelea.Rival : LadoPelea.Jugador;
            Estado = EstadoFlujoPelea.KO;
            tiempoEnEtapa = 0f;
            KOOcurrido?.Invoke(Ganador);
        }

        /// <summary>Solo funciona en Resultado: vuelve a Preparando. Devuelve si lo hizo.</summary>
        public bool PedirRevancha()
        {
            if (Estado != EstadoFlujoPelea.Resultado)
            {
                return false;
            }
            Estado = EstadoFlujoPelea.Preparando;
            tiempoEnEtapa = 0f;
            DuracionPelea = 0f;
            return true;
        }
    }
}
