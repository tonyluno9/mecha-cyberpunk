using MechaCyberpunk.VR;
using UnityEngine;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR21 (pruebas): mide cada cuadro, después de todos los LateUpdate del juego, cuánto se aparta la cabeza de su lugar en el vehículo
    /// (espacio local del vehículo) y si la viñeta de manejo llegó a ponerse. Así se ve si la cabeza va pegada sin retraso.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class MedidorCabezaVR : MonoBehaviour
    {
        private Transform cabeza;
        private Transform vehiculo;
        private ManejoVR manejo;
        private Vector3 referencia;
        private bool midiendo;

        /// <summary>Lo más que se apartó la cabeza de su lugar en el vehículo (m).</summary>
        public float DesvioMaximo { get; private set; }

        /// <summary>Cuadros medidos.</summary>
        public int Cuadros { get; private set; }

        /// <summary>True si la viñeta de manejo se puso alguna vez mientras medía.</summary>
        public bool VinetaVista { get; private set; }

        public void Empezar(Transform cabezaVR, Transform vehiculoVR, ManejoVR manejoVR)
        {
            cabeza = cabezaVR;
            vehiculo = vehiculoVR;
            manejo = manejoVR;
            referencia = vehiculo.InverseTransformPoint(cabeza.position);
            DesvioMaximo = 0f;
            Cuadros = 0;
            VinetaVista = false;
            midiendo = true;
        }

        public void Terminar()
        {
            midiendo = false;
        }

        private void LateUpdate()
        {
            if (!midiendo || cabeza == null || vehiculo == null)
            {
                return;
            }
            Cuadros++;
            DesvioMaximo = Mathf.Max(DesvioMaximo, (vehiculo.InverseTransformPoint(cabeza.position) - referencia).magnitude);
            if (manejo != null && manejo.VinetaPuesta)
            {
                VinetaVista = true;
            }
        }
    }
}
