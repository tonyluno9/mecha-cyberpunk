using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Casino.Tragamonedas
{
    /// <summary>
    /// Las reglas de una tragamonedas de 3 carretes (T-300), sin Unity. Cada carrete saca un símbolo según su peso; tres iguales pagan el multiplicador
    /// de ese símbolo y exactamente dos del símbolo parcial (las cerezas) pagan su propio multiplicador. El azar viene de fuera.
    /// </summary>
    public sealed class CalculoTragamonedas
    {
        public const int Carretes = 3;
        private const int SinSimbolo = -1;

        private readonly int[] pesos;
        private readonly int[] multiplicadoresTresIguales;
        private readonly int simboloDosIguales;
        private readonly int multiplicadorDosIguales;
        private readonly int pesoTotal;

        /// <param name="pesosSimbolos">Peso de cada símbolo (mayor que 0).</param>
        /// <param name="multiplicadoresTres">Multiplicador de la apuesta cuando salen tres iguales de cada símbolo.</param>
        /// <param name="simboloParcial">Símbolo que paga con exactamente dos iguales, o -1 si ninguno.</param>
        /// <param name="multiplicadorParcial">Multiplicador de la apuesta con dos iguales del símbolo parcial.</param>
        public CalculoTragamonedas(IReadOnlyList<int> pesosSimbolos, IReadOnlyList<int> multiplicadoresTres, int simboloParcial, int multiplicadorParcial)
        {
            if (pesosSimbolos == null || pesosSimbolos.Count == 0)
            {
                throw new ArgumentException("Hace falta al menos un símbolo.", nameof(pesosSimbolos));
            }
            if (multiplicadoresTres == null || multiplicadoresTres.Count != pesosSimbolos.Count)
            {
                throw new ArgumentException("Un multiplicador por símbolo.", nameof(multiplicadoresTres));
            }
            if (simboloParcial != SinSimbolo && (simboloParcial < 0 || simboloParcial >= pesosSimbolos.Count))
            {
                throw new ArgumentOutOfRangeException(nameof(simboloParcial));
            }
            pesos = new int[pesosSimbolos.Count];
            multiplicadoresTresIguales = new int[pesosSimbolos.Count];
            for (int i = 0; i < pesos.Length; i++)
            {
                if (pesosSimbolos[i] <= 0)
                {
                    throw new ArgumentException("Todos los pesos deben ser mayores que 0.", nameof(pesosSimbolos));
                }
                pesos[i] = pesosSimbolos[i];
                multiplicadoresTresIguales[i] = Math.Max(0, multiplicadoresTres[i]);
                pesoTotal += pesos[i];
            }
            simboloDosIguales = simboloParcial;
            multiplicadorDosIguales = Math.Max(0, multiplicadorParcial);
        }

        public int CantidadSimbolos => pesos.Length;

        /// <summary>Saca los tres símbolos (índices) de un giro.</summary>
        public int[] Girar(Random azar)
        {
            if (azar == null)
            {
                throw new ArgumentNullException(nameof(azar));
            }
            int[] resultado = new int[Carretes];
            for (int i = 0; i < Carretes; i++)
            {
                resultado[i] = SacarSimbolo(azar.Next(pesoTotal));
            }
            return resultado;
        }

        /// <summary>Cuánto paga el giro con esa apuesta. Una apuesta de 0 o negativa no paga nada.</summary>
        public int Pago(IReadOnlyList<int> simbolos, int apuesta)
        {
            if (apuesta <= 0 || simbolos == null || simbolos.Count != Carretes)
            {
                return 0;
            }
            int primero = simbolos[0];
            if (primero < 0 || primero >= pesos.Length)
            {
                return 0;
            }
            if (simbolos[1] == primero && simbolos[2] == primero)
            {
                return apuesta * multiplicadoresTresIguales[primero];
            }
            if (simboloDosIguales != SinSimbolo && CuantosSon(simbolos, simboloDosIguales) == 2)
            {
                return apuesta * multiplicadorDosIguales;
            }
            return 0;
        }

        /// <summary>Retorno exacto por cada unidad apostada, sumando las probabilidades de todos los pagos.</summary>
        public double RetornoEsperado()
        {
            double retorno = 0.0;
            for (int s = 0; s < pesos.Length; s++)
            {
                double p = (double)pesos[s] / pesoTotal;
                retorno += Math.Pow(p, Carretes) * multiplicadoresTresIguales[s];
                if (s == simboloDosIguales)
                {
                    // Exactamente dos del símbolo y uno distinto: C(3, 2) × p² × (1 - p).
                    retorno += 3.0 * p * p * (1.0 - p) * multiplicadorDosIguales;
                }
            }
            return retorno;
        }

        private int SacarSimbolo(int tirada)
        {
            int acumulado = 0;
            for (int i = 0; i < pesos.Length; i++)
            {
                acumulado += pesos[i];
                if (tirada < acumulado)
                {
                    return i;
                }
            }
            return pesos.Length - 1;
        }

        private static int CuantosSon(IReadOnlyList<int> simbolos, int simbolo)
        {
            int cuenta = 0;
            for (int i = 0; i < simbolos.Count; i++)
            {
                if (simbolos[i] == simbolo)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }
    }
}
