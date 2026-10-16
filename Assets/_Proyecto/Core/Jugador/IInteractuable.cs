using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Algo que el jugador a pie usa con E además de puertas y montables: el banco del taller (Tony) o, en A-F3,
    /// la cama y el espejo (Alan). Se agrega a <c>Registro&lt;IInteractuable&gt;</c> en <c>OnEnable</c> y se quita en
    /// <c>OnDisable</c>. Solo se usa a pie: en carro o mecha no se ofrece.
    /// El jugador (Tony) elige, entre puertas, montables e interactuables, el más cercano a 2.5 m o menos y a 60° o
    /// menos de hacia donde mira (D-054); no hace falta collider ni apuntar con precisión.
    /// </summary>
    public interface IInteractuable
    {
        /// <summary>Punto en el piso (m, en el mundo) frente a lo que se usa.</summary>
        Vector3 Posicion { get; }

        /// <summary>
        /// Lo que hace, corto y en español ("Reparar mecha · $650", "Dormir"). Puede cambiar en cada frame.
        /// </summary>
        string Texto { get; }

        /// <summary>
        /// True: el HUD muestra "E · " + <see cref="Texto"/> y la E llama a <see cref="Usar"/>.
        /// False: el HUD muestra solo <see cref="Texto"/> como aviso ("TALLER BLOQUEADO · debes $500 de renta")
        /// y la E no hace nada.
        /// </summary>
        bool PuedeUsar { get; }

        /// <summary>
        /// Lo llama el jugador (Tony) al pulsar E con <see cref="PuedeUsar"/> en true; hace la acción en el acto o la
        /// empieza. No lo llames desde otro lado.
        /// </summary>
        void Usar();
    }
}
