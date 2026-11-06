using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>La <see cref="IZonaBusqueda"/> que registra el gestor de la persecución: lee el estado de <see cref="ReglaBusquedaPolicial"/>.</summary>
    public sealed class ZonaBusqueda : IZonaBusqueda
    {
        private readonly ReglaBusquedaPolicial regla;

        public ZonaBusqueda(ReglaBusquedaPolicial reglaDeBusqueda) => regla = reglaDeBusqueda;

        public bool Activa => regla.Activa;
        public Vector3 Centro => regla.Centro;
        public float Radio => regla.Radio;
        public bool Visto => regla.Visto;
    }
}
