using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>Un componente del jugador que guarda el Animator de su modelo y debe enterarse cuando el modelo cambia (T-520: mujer u hombre).</summary>
    public interface IModeloCambiable
    {
        /// <summary>Se llama al cambiar el modelo del jugador; <paramref name="nuevo"/> es el Animator del modelo nuevo.</summary>
        void AlCambiarModelo(Animator nuevo);
    }
}
