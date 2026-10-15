using System;
using System.Collections.Generic;
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
        // Absorbe el error de redondeo al sumar duraciones en float.
        private const float Tolerancia = 1e-4f;

        private readonly float segundosPorNumero;
        private readonly float pausaDespuesKO;
        private readonly float[] finesDeLinea;
        private readonly bool presentarEnRevancha;
        private float tiempoEnEtapa;
        private int numeroActual;
        private int lineaActual;
        private bool esRevancha;

        /// <param name="duracionesLineas">Segundos de cada línea del presentador. Null o vacía = sin presentación.</param>
        /// <param name="presentarEnRevancha">Si es falso, la presentación solo ocurre en la primera pelea.</param>
        public MaquinaFlujoPelea(float segundosPorNumero, float pausaDespuesKO,
            IReadOnlyList<float> duracionesLineas = null, bool presentarEnRevancha = false)
        {
            this.segundosPorNumero = Math.Max(segundosPorNumero, float.Epsilon);
            this.pausaDespuesKO = Math.Max(pausaDespuesKO, 0f);
            this.presentarEnRevancha = presentarEnRevancha;
            int cantidad = duracionesLineas != null ? duracionesLineas.Count : 0;
            finesDeLinea = new float[cantidad];
            float acumulado = 0f;
            for (int i = 0; i < cantidad; i++)
            {
                acumulado += Math.Max(duracionesLineas[i], 0f);
                finesDeLinea[i] = acumulado;
            }
        }

        public EstadoFlujoPelea Estado { get; private set; } = EstadoFlujoPelea.Preparando;
        /// <summary>Quien ganó. Válido desde el estado KO.</summary>
        public LadoPelea Ganador { get; private set; }
        /// <summary>Segundos desde que empezó la pelea (después del conteo) hasta el KO.</summary>
        public float DuracionPelea { get; private set; }

        /// <summary>Avisa cada línea del presentador con su índice (0, 1, 2...).</summary>
        public event Action<int> LineaCambio;
        /// <summary>Avisa cada número del conteo: 3, 2, 1 y 0.</summary>
        public event Action<int> ConteoCambio;
        /// <summary>Avisa al terminar el conteo, al pasar a Peleando.</summary>
        public event Action PeleaIniciada;
        /// <summary>Avisa el primer KO, con el lado que ganó.</summary>
        public event Action<LadoPelea> KOOcurrido;
        /// <summary>Avisa al pasar a Resultado.</summary>
        public event Action<ResultadoPelea> ResultadoListo;

        /// <summary>
        /// Empieza desde Preparando: la presentación si hay líneas (y no es una revancha sin presentación) o, si no, el
        /// conteo con su primer número. Fuera de Preparando no hace nada.
        /// </summary>
        public bool IniciarConteo()
        {
            if (Estado != EstadoFlujoPelea.Preparando)
            {
                return false;
            }
            tiempoEnEtapa = 0f;
            if (finesDeLinea.Length > 0 && (!esRevancha || presentarEnRevancha))
            {
                Estado = EstadoFlujoPelea.Presentacion;
                lineaActual = 0;
                LineaCambio?.Invoke(lineaActual);
                return true;
            }
            EmpezarConteo();
            return true;
        }

        private void EmpezarConteo()
        {
            Estado = EstadoFlujoPelea.Conteo;
            numeroActual = NumeroInicial;
            ConteoCambio?.Invoke(numeroActual);
        }

        private void AvanzarPresentacion()
        {
            while (Estado == EstadoFlujoPelea.Presentacion && tiempoEnEtapa >= finesDeLinea[lineaActual] - Tolerancia)
            {
                lineaActual++;
                if (lineaActual >= finesDeLinea.Length)
                {
                    // El sobrante de la última línea ya cuenta como tiempo del conteo.
                    tiempoEnEtapa = Math.Max(tiempoEnEtapa - finesDeLinea[finesDeLinea.Length - 1], 0f);
                    EmpezarConteo();
                }
                else
                {
                    LineaCambio?.Invoke(lineaActual);
                }
            }
        }

        /// <summary>Avanza el tiempo. Un <paramref name="dt"/> grande recorre todos los números que le toquen.</summary>
        public void Avanzar(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }
            tiempoEnEtapa += dt;
            if (Estado == EstadoFlujoPelea.Presentacion)
            {
                AvanzarPresentacion();
            }
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
            esRevancha = true;
            return true;
        }
    }
}
