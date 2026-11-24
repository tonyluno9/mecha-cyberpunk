using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR14: el HUD de PC como visor de casco. Una cámara propia (no va al visor) dibuja los Canvas del HUD
    /// (<see cref="TipoLienzoVR.Casco"/>, en Screen Space Camera sobre ella) a una textura de 1920 x 1080 con fondo transparente, igual que la
    /// pantalla de PC; la textura se ve en un panel curvo (<see cref="MallaCurvaVR"/>) a <see cref="ReglaLienzosVR.RadioCasco"/> m y
    /// <see cref="ReglaLienzosVR.GradosCasco"/>° de ancho, pegado a la cabeza: la posición va con ella y el giro la sigue con un leve retraso.
    /// Solo lo dibuja la cámara de la cabeza (ni el minimapa ni otras cámaras lo ven) y encima del mundo.
    /// </summary>
    public sealed class HudCascoVR
    {
        public const string NombrePanel = "HudCascoVR";
        public const string NombreCamara = "CamaraHudCascoVR";
        public const string RutaShader = "HudCascoVR";
        public const string NombreShader = "Hidden/MechaCyberpunk/HudCascoVR";
        /// <summary>La cámara del HUD va lejos del mundo, para que su frustum (de unos metros) no vea nada más que los Canvas.</summary>
        private static readonly Vector3 PosicionCamara = new Vector3(0f, -5000f, 0f);
        private const float CercaCamara = 0.01f;
        private const float LejosCamara = 10f;
        /// <summary>
        /// Se dibuja antes que la cabeza, para que el visor use la textura de este cuadro. Siempre encendida: si se apagara, los Canvas en
        /// Screen Space Camera cambiarían de tamaño o se irían a la pantalla del PC.
        /// </summary>
        private const float ProfundidadCamara = -50f;
        private const int ProfundidadTextura = 24;
        private const int SegmentosMalla = 64;

        private GameObject panel;
        private MeshRenderer renderer;
        private Mesh malla;
        private Material material;
        private Camera camara;
        private RenderTexture textura;
        private Transform cabeza;
        private bool visible;
        private bool suscrito;

        /// <summary>La cámara del HUD (null antes de crear).</summary>
        public Camera Camara => camara;
        public RenderTexture Textura => textura;
        /// <summary>La raíz del panel curvo: en la cabeza, con el centro del panel en su +Z a <see cref="ReglaLienzosVR.RadioCasco"/> m.</summary>
        public Transform Panel => panel != null ? panel.transform : null;
        public bool Existe => panel != null;
        /// <summary>True si el panel se está mostrando (hay HUD y no hay menú encima).</summary>
        public bool Visible => visible;

        /// <summary>El centro del panel en el mundo.</summary>
        public Vector3 Centro => panel != null ? panel.transform.TransformPoint(Vector3.forward * ReglaLienzosVR.RadioCasco) : Vector3.zero;

        /// <summary>Crea la cámara, la textura y el panel (colgados de <paramref name="padre"/>).</summary>
        public void Crear(Transform padre)
        {
            Destruir();
            textura = new RenderTexture((int)ReglaLienzosVR.AnchoReferencia, (int)ReglaLienzosVR.AltoReferencia, ProfundidadTextura,
                RenderTextureFormat.ARGB32) { name = "HudCascoVR", useMipMap = false, antiAliasing = 1, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            textura.Create();

            var objetoCamara = new GameObject(NombreCamara);
            objetoCamara.transform.SetParent(padre, false);
            objetoCamara.transform.SetPositionAndRotation(PosicionCamara, Quaternion.identity);
            camara = objetoCamara.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camara.orthographic = true;
            camara.nearClipPlane = CercaCamara;
            camara.farClipPlane = LejosCamara;
            camara.depth = ProfundidadCamara;
            camara.allowHDR = false;
            camara.allowMSAA = false;
            camara.useOcclusionCulling = false;
            camara.targetTexture = textura;
            UniversalAdditionalCameraData datos = camara.GetUniversalAdditionalCameraData();
            if (datos != null)
            {
                datos.renderPostProcessing = false;
                datos.antialiasing = AntialiasingMode.None;
                datos.renderShadows = false;
                datos.requiresColorOption = CameraOverrideOption.Off;
                datos.requiresDepthOption = CameraOverrideOption.Off;
            }
            SistemaVR.PermitirXR(camara, false);

            panel = new GameObject(NombrePanel);
            panel.transform.SetParent(padre, false);
            malla = MallaCurvaVR.Crear(ReglaLienzosVR.RadioCasco, ReglaLienzosVR.GradosCasco, ReglaLienzosVR.AltoCasco, SegmentosMalla);
            panel.AddComponent<MeshFilter>().sharedMesh = malla;
            renderer = panel.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
            material = CrearMaterial(textura);
            renderer.sharedMaterial = material;
            renderer.enabled = false;
            visible = false;
            RenderPipelineManager.beginCameraRendering += AntesDeDibujar;
            Application.onBeforeRender += AntesDelCuadro;
            suscrito = true;
            Debug.Log("[VR] HUD de casco listo (" + textura.width + "x" + textura.height + ", " + ReglaLienzosVR.GradosCasco + "° a "
                + ReglaLienzosVR.RadioCasco + " m; shader " + material.shader.name + ").");
        }

        /// <summary>True si este Canvas (o su raíz) se dibuja en el HUD de casco.</summary>
        public bool Usa(Canvas lienzo)
        {
            if (lienzo == null || camara == null)
            {
                return false;
            }
            Canvas raiz = lienzo.rootCanvas;
            return raiz != null && raiz.renderMode == RenderMode.ScreenSpaceCamera && raiz.worldCamera == camara;
        }

        /// <summary>
        /// Dónde toca un rayo del mundo (el láser) el panel curvo, pasado al espacio de la cámara del HUD (donde están los Canvas del casco):
        /// con ese punto, <c>RectTransform.InverseTransformPoint</c> da la posición en px del Canvas. False si no toca el panel.
        /// </summary>
        public bool PuntoEnHud(Ray rayoMundo, out Vector3 punto)
        {
            punto = Vector3.zero;
            if (panel == null || camara == null)
            {
                return false;
            }
            Transform t = panel.transform;
            var local = new Ray(t.InverseTransformPoint(rayoMundo.origin), t.InverseTransformDirection(rayoMundo.direction));
            if (!MallaCurvaVR.Tocar(local, ReglaLienzosVR.RadioCasco, ReglaLienzosVR.GradosCasco, ReglaLienzosVR.AltoCasco, out Vector2 uv, out _))
            {
                return false;
            }
            punto = camara.ViewportToWorldPoint(new Vector3(uv.x, uv.y, LienzoConvertidoVR.DistanciaPlanoCasco));
            return true;
        }

        // El shader propio mezcla premultiplicado (como dibuja UI/Default); si faltara, el material de la malla curva.
        private static Material CrearMaterial(Texture tex)
        {
            Shader shader = Resources.Load<Shader>(RutaShader);
            if (shader == null)
            {
                shader = Shader.Find(NombreShader);
            }
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[VR] Falta el shader del HUD de casco: se usa UI/Default.");
                return MallaCurvaVR.Material(tex, true);
            }
            return new Material(shader) { name = "HudCascoVR", mainTexture = tex, renderQueue = (int)RenderQueue.Transparent };
        }

        /// <summary>
        /// Cada cuadro (LateUpdate, tiempo real): sigue a <paramref name="nuevaCabeza"/> y se muestra si <paramref name="mostrar"/>.
        /// Al aparecer se pone de golpe frente a la cabeza; luego el giro la sigue suave.
        /// </summary>
        public void Actualizar(Transform nuevaCabeza, float dt, bool mostrar)
        {
            if (panel == null)
            {
                return;
            }
            cabeza = nuevaCabeza;
            bool ahora = mostrar && cabeza != null;
            Transform t = panel.transform;
            if (ahora)
            {
                Quaternion giro = visible ? Quaternion.Slerp(t.rotation, cabeza.rotation, 1f - Mathf.Exp(-ReglaLienzosVR.RapidezCasco * dt))
                    : cabeza.rotation;
                t.SetPositionAndRotation(cabeza.position, giro);
            }
            if (ahora != visible)
            {
                visible = ahora;
                if (!ahora)
                {
                    renderer.enabled = false;
                }
            }
        }

        // La cabeza puede moverse otra vez justo antes de dibujar (TrackedPoseDriver): la posición se vuelve a pegar.
        private void AntesDelCuadro()
        {
            if (visible && panel != null && cabeza != null)
            {
                panel.transform.position = cabeza.position;
            }
        }

        // Solo la cámara de la cabeza dibuja el panel.
        private void AntesDeDibujar(ScriptableRenderContext contexto, Camera c)
        {
            if (renderer == null)
            {
                return;
            }
            bool dibujar = visible && cabeza != null && c != null && c.transform == cabeza;
            if (renderer.enabled != dibujar)
            {
                renderer.enabled = dibujar;
            }
        }

        public void Destruir()
        {
            if (suscrito)
            {
                RenderPipelineManager.beginCameraRendering -= AntesDeDibujar;
                Application.onBeforeRender -= AntesDelCuadro;
                suscrito = false;
            }
            if (camara != null)
            {
                camara.targetTexture = null;
                Object.Destroy(camara.gameObject);
            }
            if (panel != null)
            {
                Object.Destroy(panel);
            }
            if (malla != null)
            {
                Object.Destroy(malla);
            }
            if (material != null)
            {
                Object.Destroy(material);
            }
            if (textura != null)
            {
                textura.Release();
                Object.Destroy(textura);
            }
            camara = null;
            panel = null;
            renderer = null;
            malla = null;
            material = null;
            textura = null;
            cabeza = null;
            visible = false;
        }
    }
}
