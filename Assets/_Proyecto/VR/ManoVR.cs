using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// Una mano (control) del rig VR: la mueve su TrackedPoseDriver. Mide la velocidad de la mano (promedio de los
    /// últimos cuadros) en el mundo y relativa al rig (sin contar lo que camina el jugador), y hace vibrar el control.
    /// VR17: la mano va en la pose "grip" (empuñadura); su hijo <see cref="Apuntado"/> va en la pose "aim" (pointer) de OpenXR: sale de la punta
    /// del control y apunta como él. Con eso salen el láser y el cañón de las armas. Si el control no da la pose aim (el simulador de XR),
    /// Apuntado coincide con la mano (o con <see cref="ApuntadoSinPose"/>, para las pruebas).
    /// Sirve también fuera del jugador: agregar ManoVR a un objeto movido por un TrackedPoseDriver con devicePosition/deviceRotation
    /// y llamar a <see cref="Configurar"/> para tener su Apuntado.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ManoVR : MonoBehaviour
    {
        private const int Muestras = 4;
        private const float VentanaSegundos = 0.05f;

        /// <summary>Nombre del hijo con la pose aim.</summary>
        public const string NombreApuntado = "Apuntado";

        /// <summary>
        /// Para pruebas: la pose de Apuntado relativa a la mano cuando el control no da la pose aim (el simulador). Null = la misma de la mano.
        /// </summary>
        public static Pose? ApuntadoSinPose { get; set; }

        [SerializeField] private bool derecha;
        private Transform apuntado;
        private InputAction posicionAim;
        private InputAction rotacionAim;
        private InputAction posicionGrip;
        private InputAction rotacionGrip;
        private bool accionesPara;
        private bool hayPoseAim;

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

        /// <summary>
        /// VR17: el hijo de la mano con la pose aim (pointer) de OpenXR: el origen es la punta del control y +Z es hacia donde apunta.
        /// Úsalo para rayos (láser), cañones y miras. No cambia de nombre (lo usan otros scripts).
        /// </summary>
        public Transform Apuntado
        {
            get
            {
                AsegurarApuntado();
                return apuntado;
            }
        }

        /// <summary>True si el control da la pose aim de verdad (Quest por OpenXR); false en el simulador (Apuntado = mano).</summary>
        public bool TienePoseAim => hayPoseAim;

        /// <summary>Punto de agarre / golpe: el centro del control (unos cm delante del pivote).</summary>
        public Vector3 Palma => transform.TransformPoint(new Vector3(0f, -0.02f, 0.03f));

        /// <summary>VR35: distancia (m) de los nudillos delante del pivote de la pose grip.</summary>
        public const float AdelanteNudillos = 0.04f;

        /// <summary>
        /// VR35: los nudillos del puño cerrado, en el mundo (unos cm delante de la pose grip). De aquí salen los golpes con las manos; el modelo
        /// del puño se acomoda para que sus nudillos queden en este punto.
        /// </summary>
        public Vector3 Nudillos => transform.TransformPoint(new Vector3(0f, -AdelanteNudillos, 0f));

        /// <summary>
        /// VR35: velocidad del control que mide el visor (en ejes del mundo, sin lo que camina el jugador por stick). False si el visor no la da
        /// (sin visor o en el simulador): ahí se usa <see cref="VelocidadRelativa"/>.
        /// </summary>
        public bool VelocidadDispositivo(out Vector3 velocidad)
        {
            velocidad = Vector3.zero;
            UnityEngine.XR.InputDevice dispositivo = InputDevices.GetDeviceAtXRNode(derecha ? XRNode.RightHand : XRNode.LeftHand);
            if (!dispositivo.isValid || !dispositivo.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 enCuarto))
            {
                return false;
            }
            velocidad = transform.parent != null ? transform.parent.TransformDirection(enCuarto) : enCuarto;
            return true;
        }

        private Quaternion rotacionPrevia;

        public void Configurar(bool esDerecha, Transform raizRig)
        {
            derecha = esDerecha;
            rig = raizRig;
            if (posicionAim != null && accionesPara != derecha)
            {
                TirarAcciones();
                CrearAcciones();
            }
            AsegurarApuntado();
            ActualizarApuntado();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            ApuntadoSinPose = null;
        }

        private void Awake()
        {
            AsegurarApuntado();
        }

        private void OnEnable()
        {
            historial.Clear();
            rotacionPrevia = transform.rotation;
            CrearAcciones();
        }

        private void OnDisable()
        {
            TirarAcciones();
        }

        private void OnDestroy()
        {
            TirarAcciones();
        }

        private void AsegurarApuntado()
        {
            if (apuntado != null)
            {
                return;
            }
            Transform hijo = transform.Find(NombreApuntado);
            if (hijo == null)
            {
                hijo = new GameObject(NombreApuntado).transform;
                hijo.SetParent(transform, false);
            }
            apuntado = hijo;
        }

        private void CrearAcciones()
        {
            if (posicionAim != null)
            {
                return;
            }
            string mano = derecha ? EntradaVR.Der : EntradaVR.Izq;
            accionesPara = derecha;
            posicionAim = AccionPose(mano + "/pointerPosition", "Vector3");
            rotacionAim = AccionPose(mano + "/pointerRotation", "Quaternion");
            posicionGrip = AccionPose(mano + "/devicePosition", "Vector3");
            rotacionGrip = AccionPose(mano + "/deviceRotation", "Quaternion");
        }

        private static InputAction AccionPose(string ruta, string tipo)
        {
            var a = new InputAction(ruta, InputActionType.Value, ruta, expectedControlType: tipo);
            a.Enable();
            return a;
        }

        private void TirarAcciones()
        {
            foreach (InputAction a in new[] { posicionAim, rotacionAim, posicionGrip, rotacionGrip })
            {
                if (a != null)
                {
                    a.Disable();
                    a.Dispose();
                }
            }
            posicionAim = null;
            rotacionAim = null;
            posicionGrip = null;
            rotacionGrip = null;
        }

        /// <summary>
        /// VR17, lógica pura: la pose aim relativa a la grip (las dos en el mismo espacio, el del cuarto). Es la pose local de
        /// <see cref="Apuntado"/> dentro de la mano.
        /// </summary>
        public static Pose AimRelativaAGrip(Vector3 posicionGrip, Quaternion rotacionGrip, Vector3 posicionAim, Quaternion rotacionAim)
        {
            Quaternion inversa = Quaternion.Inverse(rotacionGrip);
            return new Pose(inversa * (posicionAim - posicionGrip), inversa * rotacionAim);
        }

        private static bool RotacionValida(Quaternion q) => q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f;

        private static bool TieneControl(InputAction a) => a != null && a.controls.Count > 0;

        // La diferencia entre aim y grip es fija para cada control: se mide cada cuadro con las dos poses del mismo cuadro.
        private void ActualizarApuntado()
        {
            AsegurarApuntado();
            if (TieneControl(posicionAim) && TieneControl(rotacionAim) && TieneControl(posicionGrip) && TieneControl(rotacionGrip))
            {
                hayPoseAim = true;
                Quaternion rGrip = rotacionGrip.ReadValue<Quaternion>();
                Quaternion rAim = rotacionAim.ReadValue<Quaternion>();
                if (RotacionValida(rGrip) && RotacionValida(rAim))
                {
                    Pose local = AimRelativaAGrip(posicionGrip.ReadValue<Vector3>(), rGrip.normalized, posicionAim.ReadValue<Vector3>(), rAim.normalized);
                    // Un salto raro del rastreo (más de 30 cm entre las dos poses) no mueve el rayo.
                    if (local.position.sqrMagnitude < 0.09f)
                    {
                        apuntado.SetLocalPositionAndRotation(local.position, local.rotation);
                    }
                }
                return;
            }
            hayPoseAim = false;
            Pose sin = ApuntadoSinPose ?? new Pose(Vector3.zero, Quaternion.identity);
            apuntado.SetLocalPositionAndRotation(sin.position, sin.rotation);
        }

        private void Update()
        {
            ActualizarApuntado();
        }

        private void LateUpdate()
        {
            ActualizarApuntado();
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
            UnityEngine.XR.InputDevice dispositivo = InputDevices.GetDeviceAtXRNode(derecha ? XRNode.RightHand : XRNode.LeftHand);
            if (dispositivo.isValid)
            {
                dispositivo.SendHapticImpulse(0u, Mathf.Clamp01(amplitud), Mathf.Max(0.01f, segundos));
            }
        }
    }
}
