using System;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Qué clase de pelea ofrece el mostrador (T-159, D-tony-99).</summary>
    public enum TipoOpcionCartelera
    {
        /// <summary>El siguiente rival de la liga actual: si ganas, avanza el progreso.</summary>
        Siguiente,
        /// <summary>Un rival ya vencido de la liga actual: paga una fracción del premio y no cambia el progreso.</summary>
        Revancha,
        /// <summary>La pelea casera de siempre.</summary>
        Casera,
        /// <summary>Aviso de la liga que sigue, todavía cerrada: no se puede retar.</summary>
        Bloqueada
    }

    /// <summary>
    /// Una pelea de la cartelera del mostrador. Liga e índice van desde 1 (0 en la casera). En <see cref="TipoOpcionCartelera.Bloqueada"/>
    /// la liga es la que sigue y el índice es el del jefe de la liga actual, el que hay que vencer para abrirla.
    /// </summary>
    public readonly struct OpcionCartelera : IEquatable<OpcionCartelera>
    {
        public TipoOpcionCartelera Tipo { get; }
        public int Liga { get; }
        public int Indice { get; }
        public bool EsJefe { get; }
        /// <summary>Premio de ganar que se muestra (en la revancha, ya con la fracción).</summary>
        public int Premio { get; }

        public OpcionCartelera(TipoOpcionCartelera tipo, int liga, int indice, bool esJefe, int premio)
        {
            Tipo = tipo;
            Liga = liga;
            Indice = indice;
            EsJefe = esJefe;
            Premio = premio;
        }

        public bool Equals(OpcionCartelera otra) =>
            Tipo == otra.Tipo && Liga == otra.Liga && Indice == otra.Indice && EsJefe == otra.EsJefe && Premio == otra.Premio;

        public override bool Equals(object objeto) => objeto is OpcionCartelera otra && Equals(otra);

        public override int GetHashCode() => HashCode.Combine(Tipo, Liga, Indice, EsJefe, Premio);

        public override string ToString() => Tipo + " " + Liga + "-" + Indice + " $" + Premio;
    }
}
