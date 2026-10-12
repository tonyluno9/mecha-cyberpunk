using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Una arena donde se pelea (la jaula del clandestino). La implementa Alan en un MonoBehaviour de la jaula
    /// y la usa el flujo de pelea de Tony sin referenciar el área de Alan (D-027).
    /// Se agrega a <c>Registro&lt;IArenaPelea&gt;</c> en <c>OnEnable</c> y se quita en <c>OnDisable</c>.
    /// Si hay varias arenas registradas, el flujo usa la más cercana a él. Escala (D-014): jaula de 24 x 24 m,
    /// peleadores a 12 m uno del otro.
    /// </summary>
    public interface IArenaPelea
    {
        /// <summary>Nombre para mostrar de la arena. No puede ser null.</summary>
        string Nombre { get; }

        /// <summary>
        /// Dónde empieza el jugador: posición en el piso (la base del peleador) y rotación mirando hacia el rival.
        /// </summary>
        Pose InicioJugador { get; }

        /// <summary>
        /// Dónde empieza el rival: posición en el piso y rotación mirando hacia el jugador.
        /// </summary>
        Pose InicioRival { get; }

        /// <summary>Centro del piso de la jaula, en coordenadas del mundo.</summary>
        Vector3 Centro { get; }

        /// <summary>
        /// Transform de la pantalla de la arena. Va en el centro de la cara visible de la pantalla y su +Z (forward)
        /// apunta desde la jaula hacia la pantalla, como la mirada de quien la ve. Así un Canvas World Space alineado
        /// con este Transform se lee desde dentro de la jaula. El flujo de pelea pone ahí el conteo y el resultado.
        /// </summary>
        Transform PantallaArena { get; }

        /// <summary>Ancho (x) y alto (y) de la zona útil de la pantalla, en metros.</summary>
        Vector2 TamanoPantalla { get; }
    }
}
