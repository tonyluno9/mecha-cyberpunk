using System;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Datos
{
    /// <summary>
    /// El calendario de las ligas del clandestino (T-158): una fila por liga con su cartel por cada rival, en orden.
    /// El último rival de cada liga es el jefe. Los premios y momios se ajustan en el asset (D-tony-98).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Pelea/Calendario de ligas", fileName = "CalendarioLigas")]
    public class CalendarioLigas : ScriptableObject
    {
        /// <summary>Los carteles de una liga, en orden (el último es el jefe).</summary>
        [Serializable]
        public class FilaLiga
        {
            public CartelPelea[] carteles = Array.Empty<CartelPelea>();
        }

        [Tooltip("Una fila por liga, de la más baja a la más alta.")]
        [SerializeField] private FilaLiga[] ligas = Array.Empty<FilaLiga>();

        public int CantidadLigas => ligas == null ? 0 : ligas.Length;

        /// <summary>Cuántos rivales tiene la liga (desde 1); 0 si no existe.</summary>
        public int RivalesEn(int liga)
        {
            FilaLiga fila = Fila(liga);
            return fila == null || fila.carteles == null ? 0 : fila.carteles.Length;
        }

        /// <summary>El cartel del rival <paramref name="indice"/> de la liga <paramref name="liga"/>, ambos desde 1. Null si no existe.</summary>
        public CartelPelea Cartel(int liga, int indice)
        {
            FilaLiga fila = Fila(liga);
            if (fila == null || fila.carteles == null || indice < 1 || indice > fila.carteles.Length) return null;
            return fila.carteles[indice - 1];
        }

        private FilaLiga Fila(int liga)
        {
            if (ligas == null || liga < 1 || liga > ligas.Length) return null;
            return ligas[liga - 1];
        }
    }
}
