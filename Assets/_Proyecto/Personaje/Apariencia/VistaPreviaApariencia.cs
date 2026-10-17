using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Apariencia
{
    /// <summary>
    /// Escenario pequeño lejos del mundo con un <see cref="CuerpoJugador"/>, una luz y una cámara que dibuja en una
    /// RenderTexture (T-123). La pantalla de creación y el espejo la muestran con una RawImage. Apagada no deja cámaras
    /// ni luces activas, y nunca agrega un AudioListener.
    /// </summary>
    public class VistaPreviaApariencia : MonoBehaviour
    {
        [SerializeField] private Camera camara;
        [SerializeField] private Light luz;
        [SerializeField] private CuerpoJugador cuerpo;
        [Tooltip("Pivote que gira; el cuerpo cuelga de él.")]
        [SerializeField] private Transform pivote;
        [Tooltip("Ancho de la RenderTexture (píxeles).")]
        [Min(16)] [SerializeField] private int ancho = 512;
        [Tooltip("Alto de la RenderTexture (píxeles).")]
        [Min(16)] [SerializeField] private int alto = 768;
        [Tooltip("Giro automático del cuerpo (°/s).")]
        [SerializeField] private float velocidadGiro = 25f;
        [Tooltip("Giro con las flechas cuando la pantalla lo pide (°/s).")]
        [Min(0f)] [SerializeField] private float velocidadGiroManual = 120f;
        [Tooltip("Acción de giro manual (← →, eje 1D).")]
        [SerializeField] private InputAction girar;
        [Tooltip("Si arranca encendida.")]
        [SerializeField] private bool encendidaAlInicio;

        private RenderTexture textura;
        private bool encendida;
        private bool giroManual;

        public RenderTexture Textura => textura;
        public Camera Camara => camara;
        public CuerpoJugador Cuerpo => cuerpo;
        public bool EstaEncendida => encendida;

        private void Awake()
        {
            if (camara == null || luz == null || cuerpo == null || pivote == null)
            {
                Debug.LogError("[Personaje] VistaPreviaApariencia en " + name + " tiene referencias vacías.", this);
                enabled = false;
                return;
            }
            textura = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32) { name = "VistaPreviaApariencia" };
            textura.Create();
            camara.targetTexture = textura;
            AudioListener oyente = camara.GetComponent<AudioListener>();
            if (oyente != null) Destroy(oyente);
            Aplicar(encendidaAlInicio);
        }

        private void OnEnable()
        {
            if (encendida && giroManual) girar.Enable();
        }

        private void OnDisable()
        {
            if (girar != null) girar.Disable();
        }

        private void OnDestroy()
        {
            if (textura != null)
            {
                if (camara != null) camara.targetTexture = null;
                textura.Release();
                Destroy(textura);
            }
        }

        /// <summary>Prende o apaga la vista previa: cámara, luz y cuerpo. Apagada no dibuja nada.</summary>
        public void Mostrar(bool prender)
        {
            if (textura == null) return;
            Aplicar(prender);
        }

        /// <summary>Si la pantalla pide el giro con ← → además del giro lento automático.</summary>
        public void PermitirGiroManual(bool permitir)
        {
            giroManual = permitir;
            if (permitir && encendida && isActiveAndEnabled) girar.Enable(); else girar.Disable();
        }

        /// <summary>Para el creador de prefabs: guarda la acción de giro configurada.</summary>
        public void ConfigurarGiro(InputAction accion) => girar = accion;

        private void Aplicar(bool prender)
        {
            encendida = prender;
            camara.enabled = prender;
            luz.enabled = prender;
            cuerpo.gameObject.SetActive(prender);
            if (prender && giroManual && isActiveAndEnabled) girar.Enable(); else girar.Disable();
        }

        private void Update()
        {
            if (!encendida) return;
            float grados = velocidadGiro;
            if (giroManual) grados += girar.ReadValue<float>() * velocidadGiroManual;
            pivote.Rotate(0f, grados * Time.unscaledDeltaTime, 0f, Space.World);
        }
    }
}
