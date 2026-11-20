using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1528: si el jugador trae puesto el visor (las gafas): el implante de ojos instalado y su modo CABEZA activo. Lo fija Personaje (HabilidadesProtesis) y lo leen
    /// las áreas que solo muestran algo con el visor (por ejemplo, el marcador de los drones de Trafico), sin referenciarse.
    /// </summary>
    public static class VisorJugador
    {
        /// <summary>True mientras la habilidad CABEZA (modo del visor) está activa. Lo fija HabilidadesProtesis.</summary>
        public static bool ModoActivo { get; set; }

        /// <summary>True con el implante de ojos instalado y el modo del visor activo.</summary>
        public static bool Puesto
        {
            get
            {
                IImplantes implantes = Registro<IImplantes>.Actual;
                return ModoActivo && implantes != null && implantes.Nivel(EspacioImplante.Ojos) > 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => ModoActivo = false;
    }
}
