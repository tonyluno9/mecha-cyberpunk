using System;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>Lo que guarda el jugador con la clave <c>jugador</c> (T-412): escena, posición, giro y el arma en la mano.</summary>
    [Serializable]
    public class PosicionGuardada
    {
        public string escena;
        public Vector3 posicion;
        /// <summary>Giro del cuerpo alrededor de Y (°).</summary>
        public float giro;
        public string arma;
    }
}
