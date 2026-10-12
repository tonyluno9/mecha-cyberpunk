using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Un lugar donde puede aparecer el jugador (la cama de su casa, la puerta del clandestino). La implementa
    /// Alan en el mundo y se agrega a <c>Registro&lt;IPuntoAparicion&gt;</c> en <c>OnEnable</c>
    /// (se quita en <c>OnDisable</c>). Quien lo use busca por <see cref="Id"/> y coloca al jugador con
    /// <see cref="IJugador.Colocar"/>.
    /// </summary>
    public interface IPuntoAparicion
    {
        /// <summary>Identificador único en la escena, por ejemplo "Casa_Cama" o "Clandestino_Puerta".</summary>
        string Id { get; }

        /// <summary>Posición en el piso y rotación con la que aparece el jugador.</summary>
        Pose Pose { get; }

        /// <summary>En qué modo aparece el jugador ahí.</summary>
        ModoJugador Modo { get; }
    }
}
