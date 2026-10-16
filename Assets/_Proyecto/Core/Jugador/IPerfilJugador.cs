using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Quién es el jugador: su nombre y su alias (el que grita el presentador del clandestino). La implementa
    /// <c>ServicioPerfil</c> (Progresion/, T-070), que va dentro de <c>JugadorAPie.prefab</c>, y se busca en
    /// <c>Registro&lt;IPerfilJugador&gt;</c>. La creación de personaje (T-F6) lo llena y el guardado (T-F5) lo conserva.
    /// Alan la lee si necesita el alias (carteles, grafitis) o la apariencia (cara, ropa, implantes visibles).
    /// </summary>
    public interface IPerfilJugador
    {
        /// <summary>Nombre del jugador. Nunca es null ni vacío: quien implementa pone un valor de respaldo.</summary>
        string Nombre { get; }

        /// <summary>Alias del jugador. Nunca es null ni vacío: quien implementa pone un valor de respaldo.</summary>
        string Alias { get; }

        /// <summary>Apariencia del jugador. Nunca es inválida: quien implementa pone <see cref="AparienciaJugador.Predeterminada"/> de respaldo.</summary>
        AparienciaJugador Apariencia { get; }

        /// <summary>Se avisa cada vez que cambia <see cref="Nombre"/>, <see cref="Alias"/> o <see cref="Apariencia"/>.</summary>
        event Action Cambio;
    }
}
