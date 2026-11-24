using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.XR;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// T88: la ventana del PC muestra solo el ojo izquierdo del visor, sin el lado a lado ni el recorte del espejo por
    /// defecto. No agrega cámaras: solo cambia cómo URP copia la imagen del visor a la ventana
    /// (<see cref="XRDisplaySubsystem.SetPreferredMirrorBlitMode"/>), así que no cuesta nada en el visor.
    /// Se crea solo al cargar la primera escena y revisa cada <see cref="IntervaloRevision"/> s, por si XR se reinicia.
    /// </summary>
    public class VentanaPCVR : MonoBehaviour
    {
        /// <summary>Cada cuánto se revisa el modo del espejo (s). Hecho fijo: no se balancea.</summary>
        public const float IntervaloRevision = 1f;

        /// <summary>El modo deseado: un solo ojo (el izquierdo).</summary>
        public static readonly int ModoDeseado = (int)XRMirrorViewBlitMode.LeftEye;

        public static VentanaPCVR Instancia { get; private set; }

        /// <summary>Veces que se aplicó el modo (para pruebas y diagnóstico).</summary>
        public int VecesAplicado { get; private set; }

        private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        private float siguienteRevision;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Instancia = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            if (Instancia != null)
            {
                return;
            }
            var objeto = new GameObject("VentanaPCVR");
            DontDestroyOnLoad(objeto);
            objeto.AddComponent<VentanaPCVR>();
        }

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        private void OnEnable()
        {
            ModoVR.Cambio += AlCambiarModo;
        }

        private void OnDisable()
        {
            ModoVR.Cambio -= AlCambiarModo;
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        private void AlCambiarModo(bool activo)
        {
            siguienteRevision = 0f;
        }

        private void Update()
        {
            if (!ModoVR.Activo || Time.unscaledTime < siguienteRevision)
            {
                return;
            }
            siguienteRevision = Time.unscaledTime + IntervaloRevision;
            Revisar();
        }

        /// <summary>Revisa los displays XR que corren y pone el espejo en un solo ojo si hace falta.</summary>
        public void Revisar()
        {
            SubsystemManager.GetSubsystems(displays);
            for (int i = 0; i < displays.Count; i++)
            {
                XRDisplaySubsystem display = displays[i];
                if (display == null)
                {
                    continue;
                }
                bool corriendo = display.running;
                int actual = corriendo ? display.GetPreferredMirrorBlitMode() : ModoDeseado;
                if (!ReglaVentanaPCVR.DebeAplicar(ModoVR.Activo, ModoVR.Simulado, corriendo, actual, ModoDeseado))
                {
                    continue;
                }
                display.SetPreferredMirrorBlitMode(ModoDeseado);
                // Ruta vieja (sin SRP); en URP manda el modo del display, pero no estorba.
                XRSettings.gameViewRenderMode = GameViewRenderMode.LeftEye;
                VecesAplicado++;
                Debug.Log("[VR] Ventana del PC: un solo ojo (antes modo " + actual + ").");
            }
        }
    }
}
