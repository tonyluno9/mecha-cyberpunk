using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El objetivo actual de la historia: un texto y un sitio, que es un lugar de la ciudad o una posición del mundo.
    /// </summary>
    public readonly struct Objetivo
    {
        /// <summary>Texto que ve el jugador.</summary>
        public string Texto { get; }

        /// <summary>Lugar del objetivo. <c>Ninguno</c> si el objetivo es una posición.</summary>
        public IdLugar Lugar { get; }

        /// <summary>True si el objetivo es una posición del mundo en vez de un lugar.</summary>
        public bool TienePosicion { get; }

        /// <summary>Posición del mundo (m). Solo vale si <see cref="TienePosicion"/>.</summary>
        public Vector3 Posicion { get; }

        /// <summary>Objetivo que apunta a un lugar; no tiene posición.</summary>
        public Objetivo(string texto, IdLugar lugar)
        {
            Texto = texto;
            Lugar = lugar;
            TienePosicion = false;
            Posicion = Vector3.zero;
        }

        /// <summary>Objetivo que apunta a una posición del mundo.</summary>
        public Objetivo(string texto, Vector3 posicion)
        {
            Texto = texto;
            Lugar = IdLugar.Ninguno;
            TienePosicion = true;
            Posicion = posicion;
        }

        /// <summary>Sin objetivo.</summary>
        public static Objetivo Ninguno => default;
    }
}
