using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1603: regla de paso por cruce, sin Unity. Cada cruce (nodo de la red) deja pasar a la vez solo carros que van por el mismo eje (paralelos: mismo sentido u opuesto);
    /// un carro que gira a la izquierda pide el cruce para él solo. Quien no puede pasar queda en espera y, si lleva más tiempo esperando, los que llegan después
    /// (aunque sean paralelos a los que están adentro) no se le adelantan. Una reserva caduca sola (<see cref="SegundosVigencia"/>) para que un carro atorado no cierre el cruce para siempre.
    /// El tiempo entra como parámetro.
    /// </summary>
    public sealed class ReservaCruces
    {
        /// <summary>Segundos que dura una reserva si el carro no la suelta.</summary>
        public const float SegundosVigencia = 8f;
        /// <summary>Segundos que una espera sigue viva sin que el carro la renueve.</summary>
        public const float SegundosEspera = 0.5f;
        /// <summary>|coseno| desde el que dos direcciones cuentan como el mismo eje.</summary>
        public const float MinimoParalelo = 0.7f;

        private struct Entrada
        {
            public int carro;
            public float ax;
            public float az;
            public bool exclusivo;
            public float desde;
            public float vence;
        }

        private readonly Dictionary<int, List<Entrada>> dentro = new Dictionary<int, List<Entrada>>();
        private readonly Dictionary<int, List<Entrada>> esperando = new Dictionary<int, List<Entrada>>();

        /// <summary>Cuántos carros tienen reservado ese cruce (sin contar los caducados).</summary>
        public int Titulares(int nodo, float ahora)
        {
            Purgar(nodo, ahora);
            return dentro.TryGetValue(nodo, out List<Entrada> lista) ? lista.Count : 0;
        }

        /// <summary>True si el carro tiene reservado ese cruce.</summary>
        public bool Tiene(int nodo, int carro, float ahora)
        {
            Purgar(nodo, ahora);
            return Buscar(dentro, nodo, carro) >= 0;
        }

        /// <summary>
        /// Pide el cruce <paramref name="nodo"/> para el carro <paramref name="carro"/> que lo cruza por el eje (<paramref name="ejeX"/>, <paramref name="ejeZ"/>) (no hace falta que esté normalizado).
        /// Devuelve true si ya puede pasar (o ya lo tenía). Si no, queda en espera: hay que volver a pedirlo en cada cuadro mientras espera.
        /// </summary>
        public bool Solicitar(int nodo, int carro, float ejeX, float ejeZ, bool exclusivo, float ahora)
        {
            Purgar(nodo, ahora);
            if (Buscar(dentro, nodo, carro) >= 0)
            {
                return true;
            }
            float largo = (float)Math.Sqrt(ejeX * ejeX + ejeZ * ejeZ);
            if (largo > 0.0001f)
            {
                ejeX /= largo;
                ejeZ /= largo;
            }
            int iEspera = Buscar(esperando, nodo, carro);
            float miDesde = iEspera >= 0 ? esperando[nodo][iEspera].desde : ahora;
            Entrada yo = new Entrada { carro = carro, ax = ejeX, az = ejeZ, exclusivo = exclusivo, desde = miDesde, vence = ahora + SegundosEspera };

            bool bloqueado = false;
            if (dentro.TryGetValue(nodo, out List<Entrada> titulares))
            {
                foreach (Entrada t in titulares)
                {
                    if (Conflicto(yo, t))
                    {
                        bloqueado = true;
                        break;
                    }
                }
            }
            if (!bloqueado && esperando.TryGetValue(nodo, out List<Entrada> fila))
            {
                foreach (Entrada e in fila)
                {
                    if (e.carro != carro && Conflicto(yo, e) && (e.desde < yo.desde || (e.desde == yo.desde && e.carro < carro)))
                    {
                        bloqueado = true;
                        break;
                    }
                }
            }
            if (bloqueado)
            {
                Poner(esperando, nodo, yo, iEspera);
                return false;
            }
            if (iEspera >= 0)
            {
                esperando[nodo].RemoveAt(iEspera);
            }
            yo.vence = ahora + SegundosVigencia;
            Poner(dentro, nodo, yo, -1);
            return true;
        }

        /// <summary>Suelta la reserva (y la espera) del carro en ese cruce.</summary>
        public void Liberar(int nodo, int carro)
        {
            Quitar(dentro, nodo, carro);
            Quitar(esperando, nodo, carro);
        }

        /// <summary>Suelta todo lo que el carro tenga en cualquier cruce.</summary>
        public void LiberarTodo(int carro)
        {
            foreach (int nodo in new List<int>(dentro.Keys))
            {
                Quitar(dentro, nodo, carro);
            }
            foreach (int nodo in new List<int>(esperando.Keys))
            {
                Quitar(esperando, nodo, carro);
            }
        }

        /// <summary>Borra todo.</summary>
        public void Reiniciar()
        {
            dentro.Clear();
            esperando.Clear();
        }

        private static bool Conflicto(Entrada a, Entrada b)
        {
            if (a.exclusivo || b.exclusivo)
            {
                return true;
            }
            return Math.Abs(a.ax * b.ax + a.az * b.az) < MinimoParalelo;
        }

        private void Purgar(int nodo, float ahora)
        {
            PurgarLista(dentro, nodo, ahora);
            PurgarLista(esperando, nodo, ahora);
        }

        private static void PurgarLista(Dictionary<int, List<Entrada>> mapa, int nodo, float ahora)
        {
            if (mapa.TryGetValue(nodo, out List<Entrada> lista))
            {
                lista.RemoveAll(e => e.vence < ahora);
            }
        }

        private static int Buscar(Dictionary<int, List<Entrada>> mapa, int nodo, int carro)
        {
            if (!mapa.TryGetValue(nodo, out List<Entrada> lista))
            {
                return -1;
            }
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i].carro == carro)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void Poner(Dictionary<int, List<Entrada>> mapa, int nodo, Entrada entrada, int indice)
        {
            if (!mapa.TryGetValue(nodo, out List<Entrada> lista))
            {
                lista = new List<Entrada>();
                mapa[nodo] = lista;
            }
            if (indice >= 0 && indice < lista.Count)
            {
                lista[indice] = entrada;
            }
            else
            {
                lista.Add(entrada);
            }
        }

        private static void Quitar(Dictionary<int, List<Entrada>> mapa, int nodo, int carro)
        {
            int i = Buscar(mapa, nodo, carro);
            if (i >= 0)
            {
                mapa[nodo].RemoveAt(i);
            }
        }
    }
}
