using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Una zona desde la que el jugador puede llamar a su mecha, y el sitio donde el mecha aterriza. La implementa
    /// Alan en el mundo y se agrega a <c>Registro&lt;IPuntoLlamadoMecha&gt;</c> en <c>OnEnable</c>
    /// (se quita en <c>OnDisable</c>). Quien llama (Tony, Mecha/) usa el punto <see cref="Habilitado"/> más cercano que
    /// tenga al jugador dentro de su <see cref="Radio"/> (en horizontal). Si el jugador está fuera de todos los puntos,
    /// es zona prohibida: el mecha no llega. Si dos zonas se enciman, gana la <see cref="Llegada"/> más cercana al jugador.
    /// El mecha baja del cielo: el sitio de <see cref="Llegada"/> necesita cielo abierto y un círculo libre de 3 m de
    /// radio y 6 m de alto. No lo pongas en interiores ni bajo techos.
    /// </summary>
    public interface IPuntoLlamadoMecha
    {
        /// <summary>
        /// Posición en el piso y rotación con la que aterriza el mecha (+Z hacia donde mira al llegar).
        /// </summary>
        Pose Llegada { get; }

        /// <summary>
        /// Distancia horizontal (m, mayor que 0) desde <see cref="Llegada"/> dentro de la cual el jugador puede llamar.
        /// </summary>
        float Radio { get; }

        /// <summary>
        /// false cierra la zona por ahora (por historia o por una persecución): un punto deshabilitado no cuenta.
        /// </summary>
        bool Habilitado { get; }
    }
}
