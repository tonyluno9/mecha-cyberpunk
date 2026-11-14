using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Charcos que reflejan (docs/plan-look-ciudad.md §2.3): una cámara propia se pone en el ojo de la cámara principal reflejado en el piso de
    /// la calle, con un plano de corte oblicuo en el piso, y pinta una textura global (_ReflejoCharcosTex). El shader
    /// MechaCyberpunk/Mundo/Charco proyecta cada punto del charco en esa cámara (_ReflejoCharcosVP). Los charcos van en la capa Water,
    /// que esta cámara no ve, para que no se reflejen a sí mismos.
    /// </summary>
    public class ReflejoCharcos : MonoBehaviour
    {
        /// <summary>Capa integrada de Unity donde van los charcos (no es una capa nueva).</summary>
        public const int CapaCharcos = 4;
        private const int CapaUI = 5;

        private static readonly int IdTextura = Shader.PropertyToID("_ReflejoCharcosTex");
        private static readonly int IdMatriz = Shader.PropertyToID("_ReflejoCharcosVP");
        private static readonly int IdHay = Shader.PropertyToID("_ReflejoCharcosHay");

        [SerializeField] private ConfigCharcos config;

        private Camera camara;
        private RenderTexture textura;
        private float altura = float.NaN;
        private readonly RaycastHit[] golpes = new RaycastHit[8];

        /// <summary>Altura del plano del reflejo en este cuadro (m): el piso debajo de la cámara.</summary>
        public float Altura => altura;

        /// <summary>La cámara del reflejo (para pruebas y fotos de editor); null hasta que hace falta.</summary>
        public Camera Camara => camara;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[Mundo] ReflejoCharcos en '" + name + "' necesita su ConfigCharcos.", this);
                enabled = false;
            }
        }

        private void OnDisable() => Apagar();

        private void OnDestroy()
        {
            Soltar();
        }

        // Solo si se ve algún charco (CharcoVisible) y la calidad lo pide: sin charcos a la vista no hace falta el segundo render de la escena.
        private void LateUpdate()
        {
            // T-978: en calidad BAJO no hay reflejos (CalidadVisual de Core lo fija según Opciones).
            if (CalidadVisual.ReflejosCharcos && CharcoVisible.Visibles > 0)
            {
                Actualizar(Camera.main);
            }
            else
            {
                Apagar();
            }
        }

        private void Apagar()
        {
            if (camara != null)
            {
                camara.enabled = false;
            }
            Shader.SetGlobalFloat(IdHay, 0f);
        }

        /// <summary>
        /// Pone la cámara del reflejo para <paramref name="principal"/> (cada cuadro con Camera.main; el código de editor la llama para sus
        /// fotos y después pinta <see cref="Camara"/> a mano). Devuelve false si no hay cámara principal.
        /// </summary>
        public bool Actualizar(Camera principal)
        {
            if (principal == null || config == null)
            {
                Shader.SetGlobalFloat(IdHay, 0f);
                return false;
            }
            Preparar(principal);
            Transform t = principal.transform;
            altura = Piso(t.position);
            camara.transform.SetPositionAndRotation(ReglaReflejoPlano.Reflejar(t.position, altura), ReglaReflejoPlano.Giro(t.forward, t.up));
            camara.ResetProjectionMatrix();
            camara.aspect = principal.aspect;
            camara.fieldOfView = Mathf.Min(principal.fieldOfView + config.MargenCampo, 170f);
            camara.farClipPlane = config.Lejos;
            // Plano de corte oblicuo en el piso: solo lo que está arriba de la calle sale en el reflejo.
            Matrix4x4 aCamara = camara.worldToCameraMatrix;
            Vector3 p = aCamara.MultiplyPoint(new Vector3(0f, altura - config.SeparacionCorte, 0f));
            Vector3 n = aCamara.MultiplyVector(Vector3.up).normalized;
            camara.projectionMatrix = camara.CalculateObliqueMatrix(new Vector4(n.x, n.y, n.z, -Vector3.Dot(p, n)));
            camara.enabled = Application.isPlaying;
            Shader.SetGlobalTexture(IdTextura, textura);
            Shader.SetGlobalMatrix(IdMatriz, camara.projectionMatrix * camara.worldToCameraMatrix);
            Shader.SetGlobalFloat(IdHay, 1f);
            return true;
        }

        // Los charcos están en el piso que pisa el jugador: el plano del reflejo es el piso debajo de la cámara (la calle de la casa está a
        // 0 m y la calle chica de la manzana a 0.22 m). Si no hay piso razonable debajo (techo de un carro, azotea), se queda el anterior.
        private float Piso(Vector3 desde)
        {
            if (float.IsNaN(altura))
            {
                altura = config.AlturaPlano;
            }
            int n = Physics.RaycastNonAlloc(desde, Vector3.down, golpes, config.BuscarPiso, ~(1 << CapaCharcos), QueryTriggerInteraction.Ignore);
            float mejor = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                float y = golpes[i].point.y;
                if (y >= config.PisoMinimo && y <= config.PisoMaximo && y > mejor)
                {
                    mejor = y;
                }
            }
            if (!float.IsNegativeInfinity(mejor))
            {
                altura = mejor;
            }
            return altura;
        }

        /// <summary>Para el código de editor: suelta la cámara y la textura del reflejo.</summary>
        public void Soltar()
        {
            if (camara != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(camara.gameObject);
                }
                else
                {
                    DestroyImmediate(camara.gameObject);
                }
                camara = null;
            }
            if (textura != null)
            {
                textura.Release();
                if (Application.isPlaying)
                {
                    Destroy(textura);
                }
                else
                {
                    DestroyImmediate(textura);
                }
                textura = null;
            }
        }

        private void Preparar(Camera principal)
        {
            int ancho = Mathf.Max(64, Mathf.RoundToInt(principal.pixelWidth * config.EscalaResolucion));
            int alto = Mathf.Max(64, Mathf.RoundToInt(principal.pixelHeight * config.EscalaResolucion));
            if (textura != null && (textura.width != ancho || textura.height != alto))
            {
                textura.Release();
                textura.width = ancho;
                textura.height = alto;
                textura.Create();
            }
            if (camara != null)
            {
                return;
            }
            textura = new RenderTexture(ancho, alto, 24, RenderTextureFormat.DefaultHDR) { name = "ReflejoCharcos" };
            GameObject go = new GameObject("CamaraReflejoCharcos");
            go.hideFlags = HideFlags.DontSave;
            camara = go.AddComponent<Camera>();
            camara.targetTexture = textura;
            camara.depth = -60f;
            camara.clearFlags = CameraClearFlags.Skybox;
            camara.nearClipPlane = 0.05f;
            camara.cullingMask = ~((1 << CapaCharcos) | (1 << CapaUI));
            camara.allowMSAA = false;
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderShadows = false;
            datos.renderPostProcessing = false;
            datos.requiresDepthOption = CameraOverrideOption.Off;
            datos.requiresColorOption = CameraOverrideOption.Off;
            camara.enabled = false;
        }
    }
}
