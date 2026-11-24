using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// Una mano (control) del rig VR: la mueve su TrackedPoseDriver. Mide la velocidad de la mano (promedio de los
    /// últimos cuadros) en el mundo y relativa al rig (sin contar lo que camina el jugador), y hace vibrar el control.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ManoVR : MonoBehaviour
    {
        private const int Muestras = 4;
        private const float VentanaSegundos = 0.05f;

        [SerializeField] private bool derecha;

        private readonly Queue<(Vector3 mundo, Vector3 rig, float t)> historial = new Queue<(Vector3, Vector3, float)>();
        private Transform rig;

        /// <summary>True si es la mano derecha.</summary>
        public bool Derecha => derecha;

        /// <summary>Velocidad de la mano en el mundo (m/s).</summary>
        public Vector3 Velocidad { get; private set; }

        /// <summary>Velocidad de la mano relativa al rig, en ejes del mundo (m/s): sin lo que el jugador camina o gira por stick.</summary>
        public Vector3 VelocidadRelativa { get; private set; }

        /// <summary>Velocidad de giro aproximada (°/s), para lanzar con efecto (opcional).</summary>
        public Vector3 VelocidadAngular { get; private set; }

        /// <summary>Punto de agarre / golpe: el centro del control (unos cm delante del pivote).</summary>
        public Vector3 Palma => transform.TransformPoint(new Vector3(0f, -0.02f, 0.03f));

        private Quaternion rotacionPrevia;

        public void Configurar(bool esDerecha, Transform raizRig)
        {
            derecha = esDerecha;
            rig = raizRig;
        }

        private void OnEnable()
        {
            historial.Clear();
            rotacionPrevia = transform.rotation;
        }

        private void LateUpdate()
        {
            float t = Time.time;
            // Posición del control en el cuarto (la pone el TrackedPoseDriver): no cambia al caminar con el stick ni al reacomodar el rig.
            Vector3 enRig = transform.localPosition;
            historial.Enqueue((transform.position, enRig, t));
            // Ventana de ~50 ms (4–5 cuadros a 90 Hz); con cuadros muy rápidos se guardan más muestras.
            while (historial.Count > Muestras && t - historial.Peek().t > VentanaSegundos)
            {
                historial.Dequeue();
            }
            while (historial.Count > 64)
            {
                historial.Dequeue();
            }
            var primero = historial.Peek();
            float dt = t - primero.t;
            if (dt > 1e-4f)
            {
                Velocidad = (transform.position - primero.mundo) / dt;
                Vector3 relativaLocal = (enRig - primero.rig) / dt;
                VelocidadRelativa = transform.parent != null ? transform.parent.TransformDirection(relativaLocal) : relativaLocal;
            }
            float dtCuadro = Mathf.Max(Time.deltaTime, 1e-4f);
            Quaternion delta = transform.rotation * Quaternion.Inverse(rotacionPrevia);
            delta.ToAngleAxis(out float angulo, out Vector3 eje);
            if (angulo > 180f)
            {
                angulo -= 360f;
            }
            VelocidadAngular = float.IsNaN(eje.x) ? Vector3.zero : eje * (angulo / dtCuadro);
            rotacionPrevia = transform.rotation;
        }

        /// <summary>Vibra el control (amplitud 0..1, segundos). Sin visor no hace nada.</summary>
        public void Vibrar(float amplitud, float segundos)
        {
            InputDevice dispositivo = InputDevices.GetDeviceAtXRNode(derecha ? XRNode.RightHand : XRNode.LeftHand);
            if (dispositivo.isValid)
            {
                dispositivo.SendHapticImpulse(0u, Mathf.Clamp01(amplitud), Mathf.Max(0.01f, segundos));
            }
        }
    }
}
