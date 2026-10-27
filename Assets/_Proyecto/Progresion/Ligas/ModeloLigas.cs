using System;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Qué pasó al registrar una victoria.</summary>
    public enum ResultadoVictoria
    {
        /// <summary>Revancha o pelea no permitida: sin cambios.</summary>
        Nada,
        /// <summary>Venció al siguiente rival de la liga.</summary>
        Avanzo,
        /// <summary>Venció al campeón y se abrió la siguiente liga.</summary>
        CampeonVencido,
        /// <summary>Venció al campeón de la última liga.</summary>
        CampeonClandestino
    }

    /// <summary>Lógica pura del progreso por ligas. Rivales y ligas se numeran desde 1; el último rival de cada liga es el campeón.</summary>
    public class ModeloLigas
    {
        public int CantidadLigas { get; }
        public int RivalesPorLiga { get; }

        /// <summary>La liga más alta abierta, 1..CantidadLigas.</summary>
        public int LigaActual { get; private set; } = 1;

        /// <summary>Siguiente rival por vencer en la liga actual (1..RivalesPorLiga); 0 si ya es campeón del clandestino.</summary>
        public int Siguiente { get; private set; } = 1;

        public int CampeonesVencidos { get; private set; }

        public event Action Cambio;
        public event Action<int> CampeonVencido;

        public ModeloLigas(int cantidadLigas, int rivalesPorLiga)
        {
            if (cantidadLigas < 1) throw new ArgumentOutOfRangeException(nameof(cantidadLigas));
            if (rivalesPorLiga < 2) throw new ArgumentOutOfRangeException(nameof(rivalesPorLiga));
            CantidadLigas = cantidadLigas;
            RivalesPorLiga = rivalesPorLiga;
        }

        public bool EsJefe(int indice) => indice == RivalesPorLiga;

        private bool Valido(int liga, int indice) => liga >= 1 && liga <= CantidadLigas && indice >= 1 && indice <= RivalesPorLiga;

        public bool Vencido(int liga, int indice)
        {
            if (!Valido(liga, indice)) return false;
            if (liga < LigaActual) return true;
            if (liga > LigaActual) return false;
            return Siguiente == 0 || indice < Siguiente;
        }

        public bool PuedeRetar(int liga, int indice)
        {
            if (!Valido(liga, indice)) return false;
            return Vencido(liga, indice) || (liga == LigaActual && indice == Siguiente);
        }

        public ResultadoVictoria RegistrarVictoria(int liga, int indice)
        {
            if (!PuedeRetar(liga, indice) || Vencido(liga, indice)) return ResultadoVictoria.Nada;
            if (!EsJefe(indice))
            {
                Siguiente++;
                Cambio?.Invoke();
                return ResultadoVictoria.Avanzo;
            }
            CampeonesVencidos++;
            ResultadoVictoria resultado;
            if (LigaActual < CantidadLigas)
            {
                LigaActual++;
                Siguiente = 1;
                resultado = ResultadoVictoria.CampeonVencido;
            }
            else
            {
                Siguiente = 0;
                resultado = ResultadoVictoria.CampeonClandestino;
            }
            Cambio?.Invoke();
            CampeonVencido?.Invoke(liga);
            return resultado;
        }

        /// <summary>Fija el estado (cargar y pruebas). False y sin cambios si algo está fuera de rango. No avisa CampeonVencido.</summary>
        public bool Poner(int liga, int siguiente, int campeones)
        {
            if (liga < 1 || liga > CantidadLigas) return false;
            if (campeones < 0 || campeones > CantidadLigas) return false;
            bool cerrado = siguiente == 0 && liga == CantidadLigas;
            if (!cerrado && (siguiente < 1 || siguiente > RivalesPorLiga)) return false;
            if (liga == LigaActual && siguiente == Siguiente && campeones == CampeonesVencidos) return true;
            LigaActual = liga;
            Siguiente = siguiente;
            CampeonesVencidos = campeones;
            Cambio?.Invoke();
            return true;
        }

        public void Reiniciar() => Poner(1, 1, 0);
    }
}
