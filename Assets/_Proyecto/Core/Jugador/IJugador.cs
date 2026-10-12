using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El jugador en el mundo. La implementa Tony en el componente raíz del jugador (Alan puede implementarla en
    /// su cámara libre de pruebas). El mundo la usa para saber dónde está el jugador y en qué modo va.
    /// El componente está en la raíz del jugador, que tiene un collider (CharacterController), para que los
    /// triggers del mundo lo detecten con <c>GetComponentInParent&lt;IJugador&gt;()</c>.
    /// Se agrega a <c>Registro&lt;IJugador&gt;</c> en <c>OnEnable</c> y se quita en <c>OnDisable</c>;
    /// solo hay un jugador registrado a la vez.
    /// </summary>
    public interface IJugador
    {
        /// <summary>Transform raíz del jugador (donde está el collider).</summary>
        Transform Raiz { get; }

        /// <summary>Modo actual del jugador.</summary>
        ModoJugador Modo { get; }

        /// <summary>La cámara que ve el jugador ahora mismo; cambia con el modo.</summary>
        Camera Camara { get; }

        /// <summary>Se dispara cuando cambia el modo, con el modo nuevo.</summary>
        event Action<ModoJugador> ModoCambio;

        /// <summary>
        /// Teletransporta al jugador a la pose sin atravesar nada: si tiene CharacterController lo apaga, mueve
        /// el transform y lo vuelve a prender.
        /// </summary>
        void Colocar(Pose pose);
    }
}
