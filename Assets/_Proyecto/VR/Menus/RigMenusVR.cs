using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: un rig mínimo para las escenas sin jugador a pie (Inicio, Menu_Calles): cabeza (cámara del visor) y control derecho (para el láser).
    /// V12: vive en un "cuarto oscuro" quieto, lejos de la ciudad (<see cref="PosicionCuarto"/>), con fondo negro y poca distancia de dibujo:
    /// la cabeza solo se mueve con el visor, nunca con una animación. Lo que en PC se ve con la cámara de la escena (el recorrido de
    /// Menu_Calles, el video del logo) en VR va en la pantalla de cine (<see cref="InicioCineVR"/>, que se arma con el rig).
    /// En cuanto hay <see cref="AdaptadorVR"/> se quita y manda el rig del jugador.
    /// La cámara no lleva la etiqueta MainCamera ni AudioListener: <c>Camera.main</c> y el oído siguen siendo los de la escena,
    /// y al llegar el jugador no quedan dos.
    /// </summary>
    public sealed class RigMenusVR
    {
        /// <summary>Altura de la cabeza sobre el origen del rig mientras el visor no da pose (m).</summary>
        public const float AlturaCabeza = 1.6f;
        /// <summary>V12: dónde queda el cuarto oscuro (sin geometría cerca: la ciudad está ~500 m arriba).</summary>
        public static readonly Vector3 PosicionCuarto = new Vector3(0f, -500f, 0f);
        /// <summary>V12: hasta dónde dibuja la cabeza del rig (m): la pantalla de cine está a 4 m y la ciudad no se ve.</summary>
        public const float DistanciaDibujo = 30f;
        /// <summary>V12: color de fondo del cuarto oscuro.</summary>
        public static readonly Color ColorCuarto = new Color(0.01f, 0.01f, 0.02f, 1f);
        // Se dibuja después de las cámaras de la escena (que van a la textura de la pantalla de cine): la ve del cuadro actual.
        private const float OrdenCamara = 100f;

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
            Camara.depth = OrdenCamara;
            UniversalAdditionalCameraData datos = Camara.GetUniversalAdditionalCameraData();
            if (datos != null)
            {
                datos.allowXRRendering = true; // V12: en URP esto (no stereoTargetEye) decide qué va al visor
                // V12: sin postproceso: la pantalla de cine ya trae el de la cámara de la escena (dos veces se vería lavado).
                datos.renderPostProcessing = false;
                datos.renderShadows = false;
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
            // V12: la cámara de la escena va a la pantalla de cine mientras exista el rig.
            raiz.AddComponent<InicioCineVR>();
            ColocarEnCuarto();
            SistemaVR.CamaraVR = Camara;
            raiz.SetActive(true);
            Debug.Log("[VR] Rig de menús armado en el cuarto oscuro (escena sin jugador a pie).");
        }

        /// <summary>
        /// V12: deja el rig en el cuarto oscuro, siempre en la misma pose (la cabeza no se mueve aunque cambie la escena), con fondo negro,
        /// distancia de dibujo corta y sin postproceso. Antes se ponía donde la cámara de la escena: en el visor se veía la ciudad alrededor.
        /// </summary>
        public void ColocarEnCuarto()
        {
            if (raiz == null)
            {
                return;
            }
            if (raiz.transform.position != PosicionCuarto || raiz.transform.rotation != Quaternion.identity)
            {
                raiz.transform.SetPositionAndRotation(PosicionCuarto, Quaternion.identity);
            }
            if (Camara == null)
            {
                return;
            }
            Camara.clearFlags = CameraClearFlags.SolidColor;
            Camara.backgroundColor = ColorCuarto;
            Camara.cullingMask = ~0;
            Camara.farClipPlane = DistanciaDibujo;
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
