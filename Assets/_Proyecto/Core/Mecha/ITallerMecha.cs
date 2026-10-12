using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El taller de la casa, donde descansa el mecha. Lo implementa Alan en la casa y se agrega a
    /// <c>Registro&lt;ITallerMecha&gt;</c> en <c>OnEnable</c> (se quita en <c>OnDisable</c>); hay uno por escena.
    /// El mecha de Tony se coloca solo en <see cref="Estacionamiento"/> al arrancar y vuelve ahí cuando regresa:
    /// Alan no mueve el mecha. Si no hay taller registrado, el mecha usa como casa la pose donde arrancó.
    /// </summary>
    public interface ITallerMecha
    {
        /// <summary>
        /// Base del mecha en el piso del taller, con +Z hacia la salida. Necesita 2 m de radio libre y 5 m de alto.
        /// </summary>
        Pose Estacionamiento { get; }

        /// <summary>
        /// Punto en el piso (m, en el mundo) junto al banco de trabajo donde el jugador repara y mejora con E.
        /// Lo usa T-F5; en T-F4 solo se lee <see cref="Estacionamiento"/>.
        /// </summary>
        Vector3 PuntoInteraccion { get; }
    }
}
