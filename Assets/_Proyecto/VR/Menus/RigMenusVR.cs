using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: un rig mínimo para las escenas sin jugador a pie (Inicio, Menu_Calles): cabeza (cámara del visor) y control derecho (para el láser),
    /// puesto donde está la cámara de la escena. En cuanto hay <see cref="AdaptadorVR"/> se quita y manda el rig del jugador.
    /// </summary>
    public sealed class RigMenusVR
    {
        /// <summary>Altura de la cabeza sobre el origen del rig con la que se coloca frente a la cámara de la escena (m).</summary>
        public const float AlturaCabeza = 1.6f;

        private GameObject raiz;

        public bool Existe => raiz != null;
        public Camera Camara { get; private set; }
        public Transform ManoDerecha { get; private set; }

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

            XROrigin origen = raiz.AddComponent<XROrigin>();
            origen.Origin = raiz;
            origen.CameraFloorOffsetObject = offset.gameObject;
            origen.Camera = Camara;
            origen.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            ColocarEnEscena();
            raiz.SetActive(true);
            SistemaVR.CamaraVR = Camara;
            Debug.Log("[VR] Rig de menús armado (escena sin jugador a pie).");
        }

        /// <summary>Pone el rig donde está la cámara de la escena (la cabeza queda en su lugar y mirando hacia donde mira).</summary>
        public void ColocarEnEscena()
        {
            if (raiz == null)
            {
                return;
            }
            Camera escena = Camera.main;
            if (escena == null || escena == Camara)
            {
                return;
            }
            Vector3 frente = escena.transform.forward;
            frente.y = 0f;
            Quaternion giro = frente.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(frente.normalized) : Quaternion.identity;
            raiz.transform.SetPositionAndRotation(escena.transform.position + Vector3.down * AlturaCabeza, giro);
        }

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
            Object.Destroy(raiz);
            raiz = null;
            Camara = null;
            ManoDerecha = null;
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
