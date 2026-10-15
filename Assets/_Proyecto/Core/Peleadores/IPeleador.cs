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
        /// Restaura la vida (salvo en el mecha del jugador con daño persistente, T-F5, que conserva su daño y solo
        /// sube a un piso de emergencia si quedó muy bajo o en KO), coloca al peleador en la pose (si tiene CharacterController lo desactiva
        /// para moverlo y lo vuelve a activar), lo pone de pie y deja <see cref="ControlActivo"/> en false.
        /// </summary>
        void PrepararPelea(Pose inicio);
    }
}
