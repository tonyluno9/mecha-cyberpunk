using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// El taller de la casa (T-086): donde descansa el mecha. Se registra en <c>Registro&lt;ITallerMecha&gt;</c>; hay uno por escena.
    /// La pose sale de dos transforms hijos para poder moverlos en el creador sin tocar código.
    /// </summary>
    public class TallerCasa : MonoBehaviour, ITallerMecha
    {
        [Tooltip("Base del mecha en el piso, con +Z hacia la salida.")]
        [SerializeField] private Transform estacionamiento;
        [Tooltip("Punto en el piso junto al banco de trabajo.")]
        [SerializeField] private Transform puntoInteraccion;

        public Pose Estacionamiento => new Pose(estacionamiento.position, estacionamiento.rotation);
        public Vector3 PuntoInteraccion => puntoInteraccion.position;

        private void OnEnable()
        {
            if (estacionamiento == null || puntoInteraccion == null)
            {
                Debug.LogError("[Mundo] TallerCasa en '" + name + "' necesita estacionamiento y punto de interacción.", this);
                enabled = false;
                return;
            }
            Registro<ITallerMecha>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<ITallerMecha>.Quitar(this);
        }
    }
}
