using MechaCyberpunk.UI.Inicio;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: un rig mínimo para las escenas sin jugador a pie (Inicio, Menu_Calles): cabeza (cámara del visor) y control derecho (para el láser),
    /// puesto donde está la cámara de la escena. En cuanto hay <see cref="AdaptadorVR"/> se quita y manda el rig del jugador.
    /// La cámara no lleva la etiqueta MainCamera ni AudioListener: <c>Camera.main</c> y el oído siguen siendo los de la escena,
    /// y al llegar el jugador no quedan dos.
    /// </summary>
    public sealed class RigMenusVR
    {
        /// <summary>Altura de la cabeza sobre el origen del rig con la que se coloca frente a la cámara de la escena (m).</summary>
        public const float AlturaCabeza = 1.6f;
        private const float MargenLejano = 1f;

        private GameObject raiz;

        public bool Existe => raiz != null;
        /// <summary>La raíz del rig (con el XR Origin), o null si no hay.</summary>
        public GameObject Raiz => raiz;
        public Camera Camara { get; private set; }
        public Transform ManoDerecha { get; private set; }
        public XROrigin Origen { get; private set; }

        public void Crear(Transform padre)
        {
            if (raiz != null)
            {
                return;
            }
            raiz = new GameObject("RigMenusVR");
            raiz.SetActive(false);
            raiz.transform.SetParent(padre, false);
            Transform offset = new GameObject("CameraOffset").transform;
            offset.SetParent(raiz.transform, false);

            var objetoCamara = new GameObject("CamaraMenusVR");
            objetoCamara.transform.SetParent(offset, false);
            objetoCamara.transform.localPosition = new Vector3(0f, AlturaCabeza, 0f);
            Camara = objetoCamara.AddComponent<Camera>();
            Camara.nearClipPlane = 0.05f;
            Camara.stereoTargetEye = StereoTargetEyeMask.Both;
            UniversalAdditionalCameraData datos = Camara.GetUniversalAdditionalCameraData();
            if (datos != null)
            {
                // El neón del menú es postproceso (bloom); el antialias de pantalla no, lo hace el MSAA en VR.
                datos.renderPostProcessing = true;
                datos.antialiasing = AntialiasingMode.None;
            }
            TrackedPoseDriver cabeza = objetoCamara.AddComponent<TrackedPoseDriver>();
            Configurar(cabeza, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");

            var objetoMano = new GameObject("ManoDerechaMenusVR");
            objetoMano.transform.SetParent(offset, false);
            objetoMano.transform.localPosition = new Vector3(0.2f, 1.1f, 0.3f);
            TrackedPoseDriver pose = objetoMano.AddComponent<TrackedPoseDriver>();
            Configurar(pose, EntradaVR.Der + "/devicePosition", EntradaVR.Der + "/deviceRotation");
            ManoDerecha = objetoMano.transform;
            RecursosVR recursos = RecursosVR.Cargar();
            if (recursos != null && recursos.controlDerecho != null)
            {
                GameObject modelo = Object.Instantiate(recursos.controlDerecho, ManoDerecha, false);
                modelo.name = "Modelo";
            }

            Origen = raiz.AddComponent<XROrigin>();
            Origen.Origin = raiz;
            Origen.CameraFloorOffsetObject = offset.gameObject;
            Origen.Camera = Camara;
            Origen.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            ColocarEnEscena();
            raiz.SetActive(true);
            SistemaVR.CamaraVR = Camara;
            Debug.Log("[VR] Rig de menús armado (escena sin jugador a pie).");
        }

        /// <summary>
        /// Pone el rig donde está la cámara de la escena (la cabeza queda en su lugar y mirando hacia donde mira) y copia su aspecto
        /// (fondo, capas, distancia de dibujo). Con Menu_Calles cargada manda la cámara de las calles reales, que es la que se ve.
        /// </summary>
        public void ColocarEnEscena()
        {
            if (raiz == null)
            {
                return;
            }
            Camera escena = CamaraDeEscena();
            if (escena == null)
            {
                return;
            }
            Vector3 frente = escena.transform.forward;
            frente.y = 0f;
            Quaternion giro = frente.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(frente.normalized) : Quaternion.identity;
            raiz.transform.SetPositionAndRotation(escena.transform.position + Vector3.down * AlturaCabeza, giro);
            CopiarAspecto(escena);
        }

        private Camera CamaraDeEscena()
        {
            RecorridoCallesMenu recorrido = RecorridoCallesMenu.Actual;
            if (recorrido != null && recorrido.isActiveAndEnabled && recorrido.Camara != null && recorrido.Camara != Camara)
            {
                return recorrido.Camara;
            }
            Camera principal = Camera.main;
            return principal != null && principal != Camara ? principal : null;
        }

        private void CopiarAspecto(Camera escena)
        {
            if (Camara == null)
            {
                return;
            }
            Camara.clearFlags = escena.clearFlags;
            Camara.backgroundColor = escena.backgroundColor;
            Camara.cullingMask = escena.cullingMask;
            Camara.farClipPlane = Mathf.Max(escena.farClipPlane, Camara.nearClipPlane + MargenLejano);
            if (escena.TryGetComponent(out UniversalAdditionalCameraData suyos))
            {
                UniversalAdditionalCameraData propios = Camara.GetUniversalAdditionalCameraData();
                if (propios != null)
                {
                    propios.renderPostProcessing = suyos.renderPostProcessing;
                }
            }
        }

        /// <summary>Quita el rig: se apaga en el acto (un solo XR Origin y una sola cámara de cabeza) y se destruye al final del cuadro.</summary>
        public void Destruir()
        {
            if (raiz == null)
            {
                return;
            }
            if (SistemaVR.CamaraVR == Camara)
            {
                SistemaVR.CamaraVR = null;
            }
            raiz.SetActive(false);
            Object.Destroy(raiz);
            raiz = null;
            Camara = null;
            ManoDerecha = null;
            Origen = null;
            Debug.Log("[VR] Rig de menús quitado.");
        }

        private static void Configurar(TrackedPoseDriver tpd, string posicion, string rotacion)
        {
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            tpd.ignoreTrackingState = true;
            tpd.positionInput = new InputActionProperty(new InputAction(posicion, InputActionType.Value, posicion, expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction(rotacion, InputActionType.Value, rotacion, expectedControlType: "Quaternion"));
        }
    }
}
