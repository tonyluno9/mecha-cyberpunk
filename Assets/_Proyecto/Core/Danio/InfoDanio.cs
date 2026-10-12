using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Datos de un daño que se quiere aplicar a un peleador.</summary>
    public readonly struct InfoDanio
    {
        /// <summary>Puntos de daño antes de bloqueos.</summary>
        public float Cantidad { get; }
        /// <summary>Clase de daño.</summary>
        public TipoDanio Tipo { get; }
        /// <summary>Punto del mundo donde ocurrió el impacto.</summary>
        public Vector3 Punto { get; }
        /// <summary>Dirección en que viajó el golpe, desde el atacante hacia el objetivo.</summary>
        public Vector3 Direccion { get; }
        /// <summary>Quien golpea. Puede ser null.</summary>
        public IPeleador Atacante { get; }

        public InfoDanio(float cantidad, TipoDanio tipo, Vector3 punto, Vector3 direccion, IPeleador atacante)
        {
            Cantidad = cantidad;
            Tipo = tipo;
            Punto = punto;
            Direccion = direccion;
            Atacante = atacante;
        }
    }
}
