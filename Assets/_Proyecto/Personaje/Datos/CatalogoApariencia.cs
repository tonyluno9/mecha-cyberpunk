using System;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// Opciones de apariencia del jugador (T-123, D-tony-63): caras, ropas e implantes visibles. Los índices de
    /// <c>AparienciaJugador</c> apuntan a estas listas. Los nombres y colores son propuesta y se cambian en el asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Catálogo de apariencia", fileName = "CatalogoApariencia_Nuevo")]
    public class CatalogoApariencia : ScriptableObject
    {
        [SerializeField] private OpcionCara[] caras = Array.Empty<OpcionCara>();
        [SerializeField] private OpcionRopa[] ropas = Array.Empty<OpcionRopa>();
        [SerializeField] private OpcionImplante[] implantes = Array.Empty<OpcionImplante>();

        public int CantidadCaras => caras.Length;
        public int CantidadRopas => ropas.Length;
        public int CantidadImplantes => implantes.Length;

        /// <summary>Cara de ese índice; fuera de rango se toma con módulo (negativos incluidos).</summary>
        public OpcionCara Cara(int indice) => caras[Modulo(indice, caras.Length)];

        /// <summary>Ropa de ese índice; fuera de rango se toma con módulo.</summary>
        public OpcionRopa Ropa(int indice) => ropas[Modulo(indice, ropas.Length)];

        /// <summary>Implante de ese índice; fuera de rango se toma con módulo.</summary>
        public OpcionImplante Implante(int indice) => implantes[Modulo(indice, implantes.Length)];

        /// <summary>Si las tres listas tienen al menos una opción.</summary>
        public bool EstaCompleto => caras.Length > 0 && ropas.Length > 0 && implantes.Length > 0;

        /// <summary>Módulo que nunca es negativo.</summary>
        public static int Modulo(int indice, int cantidad)
        {
            int resto = indice % cantidad;
            return resto < 0 ? resto + cantidad : resto;
        }
    }
}
