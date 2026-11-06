using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Espejo de verdad (T-854): va en el quad del vidrio (su cara visible mira a -Z, hacia el cuarto). Una cámara propia se pone en el ojo de la
    /// cámara principal reflejado en el vidrio, mira hacia el vidrio con un plano de corte oblicuo justo en él (no ve la pared de atrás) y pinta
    /// una textura; el shader MechaCyberpunk/Mundo/EspejoReflejo proyecta cada punto del vidrio en esa cámara, así se ve el cuarto y el
    /// personaje del jugador reflejados. Solo trabaja si la cámara principal está cerca y del lado del cuarto.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class EspejoReflejo : MonoBehaviour
    {
        private static readonly int IdTextura = Shader.PropertyToID("_ReflejoTex");
        private static readonly int IdMatriz = Shader.PropertyToID("_ReflejoVP");

        [Tooltip("Lado de la textura del reflejo (px).")]
        [Range(128, 2048)] [SerializeField] private int resolucion = 1024;
        [Tooltip("Distancia máxima de la cámara principal al espejo para que refleje (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaMaxima = 9f;
        [Tooltip("Lo más lejos que se ve en el reflejo (m).")]
        [Min(1f)] [SerializeField] private float lejos = 40f;
        [Tooltip("Margen del campo de visión sobre el que cubre el vidrio (°).")]
        [Range(0f, 30f)] [SerializeField] private float margenCampo = 4f;
        [Tooltip("El plano de corte va así de adelante del vidrio (m), para no ver el marco del propio espejo.")]
        [Range(0f, 0.1f)] [SerializeField] private float separacionCorte = 0.01f;

        private Renderer render;
        private Camera camara;
        private RenderTexture textura;
        private MaterialPropertyBlock bloque;
        private readonly Vector3[] esquinas = new Vector3[4];

        /// <summary>La cámara del reflejo (para las pruebas); null hasta que hace falta.</summary>
        public Camera Camara => camara;

        /// <summary>True si este cuadro se pinta el reflejo.</summary>
        public bool Reflejando => camara != null && camara.enabled;

        private void Awake()
        {
            render = GetComponent<Renderer>();
            bloque = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            if (camara != null)
            {
                camara.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (camara != null)
            {
                Destroy(camara.gameObject);
            }
            if (textura != null)
            {
                textura.Release();
                Destroy(textura);
            }
        }

        /// <summary>Para el código de editor: suelta la cámara y la textura del reflejo.</summary>
        public void Soltar()
        {
            if (camara != null)
            {
                DestroyImmediate(camara.gameObject);
            }
            if (textura != null)
            {
                textura.Release();
                DestroyImmediate(textura);
            }
        }

        private void LateUpdate() => Actualizar(Camera.main);

        /// <summary>
        /// Pone la cámara del reflejo para el ojo de <paramref name="principal"/> (cada cuadro con Camera.main; el código de editor la llama para
        /// sus fotos). Devuelve true si refleja: la cámara está cerca y del lado del cuarto y, en juego, el vidrio se ve.
        /// </summary>
        public bool Actualizar(Camera principal)
        {
            if (render == null)
            {
                Awake();
            }
            Vector3 centro = transform.position;
            Vector3 normal = -transform.forward;
            bool sirve = principal != null && (render.isVisible || !Application.isPlaying)
                && Vector3.Distance(principal.transform.position, centro) < distanciaMaxima
                && Vector3.Dot(principal.transform.position - centro, normal) > 0f;
            if (!sirve)
            {
                if (camara != null)
                {
                    camara.enabled = false;
                }
                return false;
            }
            Preparar();
            Vector3 ojo = ReglaEspejo.Reflejar(principal.transform.position, centro, normal);
            Vector3 adelante = centro - ojo;
            esquinas[0] = transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0f));
            esquinas[1] = transform.TransformPoint(new Vector3(0.5f, -0.5f, 0f));
            esquinas[2] = transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0f));
            esquinas[3] = transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f));
            camara.transform.SetPositionAndRotation(ojo, Quaternion.LookRotation(adelante, Vector3.up));
            camara.ResetProjectionMatrix();
            camara.aspect = 1f;
            camara.farClipPlane = lejos;
            camara.fieldOfView = ReglaEspejo.CampoParaCubrir(ojo, adelante, esquinas, margenCampo, 10f, 150f);
            // Plano de corte oblicuo en el vidrio: deja solo lo que está del lado del cuarto.
            Matrix4x4 aCamara = camara.worldToCameraMatrix;
            Vector3 p = aCamara.MultiplyPoint(centro + normal * separacionCorte);
            Vector3 n = aCamara.MultiplyVector(normal).normalized;
            camara.projectionMatrix = camara.CalculateObliqueMatrix(new Vector4(n.x, n.y, n.z, -Vector3.Dot(p, n)));
            camara.enabled = Application.isPlaying;
            render.GetPropertyBlock(bloque);
            bloque.SetTexture(IdTextura, textura);
            bloque.SetMatrix(IdMatriz, camara.projectionMatrix * camara.worldToCameraMatrix);
            render.SetPropertyBlock(bloque);
            return true;
        }

        private void Preparar()
        {
            if (camara != null)
            {
                return;
            }
            textura = new RenderTexture(resolucion, resolucion, 24, RenderTextureFormat.ARGB32) { name = "Espejo_" + name };
            GameObject go = new GameObject("CamaraEspejo_" + name);
            go.hideFlags = HideFlags.DontSave;
            camara = go.AddComponent<Camera>();
            camara.targetTexture = textura;
            camara.depth = -50f;
            camara.clearFlags = CameraClearFlags.Skybox;
            camara.nearClipPlane = 0.05f;
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderShadows = false;
            datos.renderPostProcessing = false;
            datos.requiresDepthOption = CameraOverrideOption.Off;
            datos.requiresColorOption = CameraOverrideOption.Off;
            camara.enabled = false;
        }
    }
}
