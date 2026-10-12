using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Un participante de la pelea: el jugador o el rival.</summary>
    public interface IPeleador
    {
        string Nombre { get; }
        LadoPelea Lado { get; }
        /// <summary>Transform raíz del peleador.</summary>
        Transform Raiz { get; }
        IVida Vida { get; }
        /// <summary>Si el peleador puede moverse y atacar.</summary>
        bool ControlActivo { get; }
        /// <summary>El flujo de pelea congela y libera a los peleadores con esto.</summary>
        void ActivarControl(bool activo);
        /// <summary>
        /// Restaura la vida, coloca al peleador en la pose (si tiene CharacterController lo desactiva
        /// para moverlo y lo vuelve a activar), lo pone de pie y deja <see cref="ControlActivo"/> en false.
        /// </summary>
        void PrepararPelea(Pose inicio);
    }
}
