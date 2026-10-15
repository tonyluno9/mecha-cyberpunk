using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla de la cabina que guía el encendido: el paso en curso, una barra de progreso y un parpadeo rojo si se pulsa una tecla
    /// equivocada. Se apaga cuando el mecha está encendido (después de mostrar SISTEMAS EN LÍNEA si venía del paso 3).
    /// </summary>
    public class PantallaEncendido : MonoBehaviour
    {
        [SerializeField] private EncendidoMecha encendido;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Text texto;
        [SerializeField] private Image fondo;
        [SerializeField] private Image hueco;
        [SerializeField] private Image relleno;
        [SerializeField] private ConfigPantallaEncendido config;

        private PasoEncendido pasoAnterior = PasoEncendido.Apagado;
        private float restoParpadeo;
        private float restoEnLinea;

        public Text Texto => texto;
        public bool Visible => lienzo != null && lienzo.enabled;
        public float Relleno => relleno != null ? relleno.fillAmount : 0f;

        private void Awake()
        {
            if (encendido == null || lienzo == null || texto == null || relleno == null || config == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaEncendido de " + name, this);
                enabled = false;
                return;
            }
            if (relleno.sprite == null)
            {
                Texture2D blanco = Texture2D.whiteTexture;
                relleno.sprite = Sprite.Create(blanco, new Rect(0f, 0f, blanco.width, blanco.height), new Vector2(0.5f, 0.5f));
            }
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            relleno.color = config.ColorNormal;
            if (fondo != null)
            {
                fondo.color = config.ColorFondo;
            }
            if (hueco != null)
            {
                hueco.color = config.ColorHueco;
            }
            lienzo.enabled = false;
        }

        private void OnEnable()
        {
            if (!enabled)
            {
                return;
            }
            encendido.PasoCambio += AlCambiarPaso;
            encendido.TeclaIncorrecta += AlEquivocarse;
            AlCambiarPaso(encendido.Paso);
        }

        private void OnDisable()
        {
            if (encendido == null)
            {
                return;
            }
            encendido.PasoCambio -= AlCambiarPaso;
            encendido.TeclaIncorrecta -= AlEquivocarse;
        }

        private void AlCambiarPaso(PasoEncendido paso)
        {
            ConfigEncendido textos = encendido.Config;
            switch (paso)
            {
                case PasoEncendido.Paso1:
                    Mostrar(textos.TextoPaso1);
                    break;
                case PasoEncendido.Paso2:
                    Mostrar(textos.TextoPaso2);
                    break;
                case PasoEncendido.Paso3:
                    Mostrar(textos.TextoPaso3);
                    break;
                case PasoEncendido.Encendido:
                    if (pasoAnterior == PasoEncendido.Paso3)
                    {
                        Mostrar(textos.TextoEncendido);
                        relleno.fillAmount = 1f;
                        restoEnLinea = config.SegundosEnLinea;
                    }
                    else
                    {
                        lienzo.enabled = false;
                    }
                    break;
                default:
                    lienzo.enabled = false;
                    break;
            }
            pasoAnterior = paso;
        }

        private void Mostrar(string linea)
        {
            lienzo.enabled = true;
            texto.text = linea;
            texto.color = config.ColorNormal;
            relleno.fillAmount = 0f;
            restoParpadeo = 0f;
            restoEnLinea = 0f;
        }

        private void AlEquivocarse()
        {
            restoParpadeo = config.SegundosParpadeo;
        }

        private void Update()
        {
            if (!lienzo.enabled)
            {
                return;
            }
            float dt = Time.deltaTime;
            if (encendido.EstaEncendido)
            {
                restoEnLinea -= dt;
                if (restoEnLinea <= 0f)
                {
                    lienzo.enabled = false;
                }
                return;
            }

            relleno.fillAmount = encendido.Progreso;
            if (restoParpadeo > 0f)
            {
                restoParpadeo -= dt;
                bool rojo = restoParpadeo > 0f && Mathf.FloorToInt(restoParpadeo * config.CambiosPorSegundo) % 2 == 0;
                texto.color = rojo ? config.ColorError : config.ColorNormal;
            }
        }
    }
}
