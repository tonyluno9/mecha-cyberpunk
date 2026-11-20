using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1528: pide una marca en el indicador direccional de daño del HUD sin que haya daño: "algo está allá". Lo publica quien quiera señalar una fuente
    /// (los drones, solo con el visor puesto); lo escucha el indicador de daño de la UI, que apunta hacia ese punto del mundo.
    /// </summary>
    public static class EventosIndicadorDanio
    {
        /// <summary>Punto del mundo desde donde viene la amenaza.</summary>
        public static event Action<Vector3> FuenteMarcada;

        public static void MarcarFuente(Vector3 posicion) => FuenteMarcada?.Invoke(posicion);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => FuenteMarcada = null;
    }
}
